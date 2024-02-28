using System;
using System.Collections.Generic;
using System.Linq;
using KaC_Modding_Engine_API.Shared.ArchieV1;
using KaC_Modding_Engine_API.Objects.Generators;
using KaC_Modding_Engine_API.Tools;
using Newtonsoft.Json;
using HarmonyLib;
using Priority_Queue;
using UnityEngine;
using UnityEngine.Events;
using KaC_Modding_Engine_API.Objects.Resources;
using KaC_Modding_Engine_API.Objects.ModConfig;
using KaC_Modding_Engine_API.Names;
using Zat.InterModComm;
using Zat.Debugging;
using static KaC_Modding_Engine_API.Objects.Resources.VanillaModdedResourceTypes;

public class ModdingFramework : MonoBehaviour
{
    /// <summary>
    /// The KCModHelper injected by KC
    /// </summary>
    public KCModHelper Helper { get; private set; }

    /// <summary>
    /// Gets a Random to be used. Can be set with seed to remove randomness for testing.
    /// </summary>
    public System.Random Random { get; } = new System.Random(123456789);

    /// <summary>
    /// The name of this mod
    /// </summary>
    public string cat => "KCModdingFramework";

    /// <summary>
    /// The instance of this object
    /// </summary>
    public static ModdingFramework Inst { get; private set; }

    /// <summary>
    /// List all of the ResourceTypes that have been assigned to ModdedResourceTypes (Including default ones).
    /// </summary>
    public IEnumerable<ResourceType> AssignedResourceTypes
    {
        get
        {
            return RegisteredModdedResourceTypes.Select(mrt => mrt.ResourceType);
        }
    }

    /// <summary>
    /// Gets all ModdedResourceTypes from all ModConfigs.
    /// </summary>
    public IEnumerable<ModdedResourceType> ModdedResourceTypes
    {
        get
        {
            return ModConfigs.SelectMany(mc => mc.ModdedResourceTypes);
        }
    }

    /// <summary>
    /// Gets all <see cref="ModdedResourceType"/> that have been registered with the ModdingFramework (Including <see cref="VanillaModdedResourceTypes"/>.
    /// </summary>
    public IEnumerable<ModdedResourceType> RegisteredModdedResourceTypes
    {
        get
        {
            return RegisteredModConfigs.SelectMany(rmc => rmc.ModdedResourceTypes).Union(VanillaModdedResourceTypes);
        }
    }

    /// <summary>
    /// Gets all of the Vanilla ResourceTypes represented as ModdedResourceTypes.
    /// </summary>
    public ICollection<ModdedResourceType> VanillaModdedResourceTypes { get; }

    /// <summary>
    /// Gets all<see cref="ModdedResourceType"/> that have NOT been registered with the ModdingFramework.
    /// </summary>
    public IEnumerable<ModdedResourceType> UnregisteredModdedResourceTypes
    {
        get
        {
            return UnregisteredModConfigs.SelectMany(rmc => rmc.ModdedResourceTypes);
        }
    }

    /// <summary>
    /// Gets all ModConfigs (Registered or not)
    /// </summary>
    public List<ModConfigMF> ModConfigs { get; private set; }

    /// <summary>
    /// Gets all Registered ModConfigs.
    /// </summary>
    public IEnumerable<ModConfigMF> RegisteredModConfigs 
    {
        get
        {
            return ModConfigs.Where(c => c.Registered);
        }
    }

    /// <summary>
    /// Gets all Unregistered ModConfigs.
    /// </summary>
    public IEnumerable<ModConfigMF> UnregisteredModConfigs
    {
        get
        {
            return ModConfigs.Where(c => !c.Registered);
        }
    }

    /// <summary>
    /// Gets or sets all of the Generators from all of the ModConfigs.
    /// </summary>
    public IEnumerable<GeneratorBase> Generators
    {
        get
        {
            return ModConfigs.SelectMany(mc => mc.Generators);
        }
    }

    /// <summary>
    /// Gets all Registered Generators.
    /// </summary>
    public IEnumerable<GeneratorBase> RegisteredGenerators
    {
        get
        {
            return Generators.Where(g => g.Registered);
        }
    }

    /// <summary>
    /// Gets all Unregistered Generators.
    /// </summary>
    public IEnumerable<GeneratorBase> UnregisteredGenerators
    {
        get
        {
            return Generators.Where(g => !g.Registered);
        }
    }

    /// <summary>
    /// Gets or sets the port that IMC messages will be received in/sent from.
    /// </summary>
    public IMCPort Port { get; set; }

    #region Initialisation

    // Initial order goes #ctor > PreScriptLoad > Preload > SceneLoaded > Start
    // Therefore #ctor has no Helper class instantiated
    public ModdingFramework()
    {
        Harmony harmony = new Harmony("uk.ArchieV.KCModdingFramework");
        harmony.PatchAll();
        
        Inst = this;

        ULogger.Log(cat, $"Adding VanilliaModdedResourceTypes to RegisteredModdedResourceTypes");
        this.VanillaModdedResourceTypes = (ICollection<ModdedResourceType>)GenerateList();
    }

    public void Preload(KCModHelper helper)
    {
        Helper = helper;
        Debugging.Helper = Helper;
        Debugging.Active = true;

        ULogger.Log(cat, $"Loading KCModdingFramework at {DateTime.Now}");
        ULogger.Log(cat, $"===============Preload===============");
        
        LogDump();
    }
    
    public void PreScriptLoad(KCModHelper helper)
    {
        ULogger.Log(cat, $"===============PreScriptLoad===============");
    }

    public void SceneLoaded(KCModHelper helper)
    {
        ULogger.Log(cat, $"===============SceneLoaded===============");
    }

    public void Start()
    {
        ULogger.Log(cat, $"===============Start===============");
        LogDump();

        // Assign port
        transform.name = ObjectNames.ModdingFrameworkName;
        gameObject.name = ObjectNames.ModdingFrameworkName;
        Port = gameObject.AddComponent<IMCPort>();
        Port.RegisterReceiveListener<ModConfigMF>(MethodNames.RegisterMod, RegisterModHandler);

        // How to know which mods use ModConfig?
        // TODO question above
        // Perhaps ping them all?
        // Check that they have an XML file? Will KC mod uploader even allow non-.cs files?
        // Can check they have file using console? Make it error with a certain error code for has-file and another for has-no-file

        // Calculate ModLoadOrder
        List<List<ModConfigMF>> modLoadOrder = (List<List<ModConfigMF>>)GenerateModLoadOrder();

        // Register all loaded mods
        for (int i = 0; i < modLoadOrder.Count(); i++)
        {
            IEnumerable<ModConfigMF> loadRound = modLoadOrder[i];
            for (int j = 0; j < loadRound.Count(); j++)
            {
                ModConfigMF mod = ModConfigs[j];
                RegisterMod(ref mod);
            }

        }

        // Tell all mods what has been registered
        foreach(ModConfigMF modConfig in ModConfigs)
        {
            // TODO this should probably be a StartCoroutine() sort of thing
            SendAllModsRegistered(modConfig);
        }
    }
    #endregion
    
    /// <summary>
    /// Sends <see cref="MethodNames.AllModsRegistered"/> message to the given mod.
    /// </summary>
    /// <param name="targetMod"></param>
    /// <param name="retries">Number of times to attempt to send this message.</param>
    private void SendAllModsRegistered(ModConfigMF targetMod, int retries = 5)
    {
        Port.RPC<ICollection<ModConfigMF>>(targetMod.ModName, MethodNames.AllModsRegistered, ModConfigs, 3f, () =>
        {
            ULogger.Log(cat, $"Successfully sent '{MethodNames.AllModsRegistered}' message to '{targetMod.ModName}'");
        },
        (error) =>
        {
            if (retries > 0)
            {
                SendAllModsRegistered(targetMod, retries - 1);
            }
            else
            {
                ULogger.Log(cat, $"Unable to send '{MethodNames.AllModsRegistered}' message to '{targetMod.ModName}'");
                ULogger.Log(cat, targetMod);
            }
        });
    }

    /// <summary>
    /// Registers the mod with the KCModdingFramework
    /// </summary>
    /// <param name="handler"></param>
    /// <param name="source"></param>
    /// <param name="mod"></param>
    private void RegisterModHandler(IRequestHandler handler, string source, ModConfigMF mod)
    {
        ULogger.Log(cat, $"Received message from {source}");
        ULogger.Log(
            cat,
            $"Registering mod (Handler)\n" +
            $"Mod: `{mod}`\n" +
            $"Source: {source}\n" +
            $"Handler: {handler}");

        // Add to ModConfid list ready to be registered
        mod.Registered = false;
        ModConfigs.Add(mod);
    }

    /// <summary>
    /// Registers the given mod. Changes "Registered" to true (Unless it failed to register any part of it)
    /// </summary>
    /// <param name="ModConfigMF"></param>
    /// <returns>Returns false if failed to register mod in its entirety</returns>
    public void RegisterMod(ref ModConfigMF ModConfigMF)
    {
        if (ModConfigMF == null)
        {
            Helper.Log("ModConfigMF is null. ABORTING");
            return;
        }
        
        // Check if mod can be encoded (No self referencing loops)
        try
        {
            _ = IMCMessage.CreateRequest(
                ObjectNames.ModdingFrameworkName,
                ModConfigMF.ModName,
                JsonConvert.SerializeObject(ModConfigMF, IMCPort.serializerSettings));
        }
        catch (Exception e)
        {
            ULogger.Log(cat, "Failed to encode mod with error:");
            ULogger.Log(cat, e.ToString());
        }

        ULogger.Log(cat, $"Registering {ModConfigMF.ModName} by {ModConfigMF.Author}...");

        // Registers all ModdedResourceType 
        foreach (ModdedResourceType moddedResourceType in ModConfigMF.ModdedResourceTypes)
        {
            try
            {
                RegisterResource(moddedResourceType);
            }
            catch (Exception e)
            {
                ULogger.Log(e);
            }
        }

        // Registers each Generator
        foreach (GeneratorBase generator in ModConfigMF.Generators)
        {
            try
            {
                RegisterGenerator(generator);
            }
            catch (Exception e)
            {
                ULogger.Log(e);
            }
        }        

        ULogger.Log($"Registered {ModConfigMF.ModName} by {ModConfigMF.Author} successfully!\n" +
                   $"Registered {ModConfigMF.Generators.Count()} generators.");
        ModConfigMF.Registered = true;
    }

    /// <summary>
    /// Registers the given generator to assign each Resource in it an unassigned ResourceType
    /// </summary>
    /// <param name="generator"></param>
    private void RegisterGenerator(GeneratorBase generator)
    {
        for (int i = 0; i < generator.Resources.Count(); i++)
        {
            ModdedResourceType moddedResourceType = generator.Resources[i];
            AssignModdedResourceType(ref moddedResourceType);
            moddedResourceType.LoadAssetBundle(Helper);
            moddedResourceType.LoadModel();
        }
        RegisteredGenerators.AddItem(generator);
    }

    /// <summary>
    /// Assigns the resourceTypeBase an unassigned ResourceType
    /// </summary>
    /// <param name="moddedResourceType"></param>
    /// <param name="assetBundle"></param>
    /// <returns></returns>
    private void RegisterResource(ModdedResourceType moddedResourceType)
    {
        ULogger.Log($"Registering ModdedResourceType: {moddedResourceType}");

        // Not loaded earlier as breaks JSON encoding
        moddedResourceType.LoadModel();
        
        AssignModdedResourceType(ref moddedResourceType);
    }

    /// <summary>
    /// Assigns given <paramref name="moddedResourceType"/> an unassigned ResourceType
    /// </summary>
    /// <param name="moddedResourceType"></param>
    private void AssignModdedResourceType(ref ModdedResourceType moddedResourceType)
    {
        bool notAssigned = true;
        while (notAssigned)
        {
            ResourceType possibleResourceType = (ResourceType) Random.Next();
            if (!AssignedResourceTypes.Contains(possibleResourceType))
            {
                moddedResourceType.ResourceType = possibleResourceType;
                moddedResourceType.Registered = true;

                notAssigned = false;
            }
        }

        ULogger.Log(cat, $"Assigned {moddedResourceType.Name} to {moddedResourceType.ResourceType}");
    }

    /// <summary>
    /// Removes ResourceType/ResourceTypeBase pair by resourceTypeBase from _assignedResourceTypes
    /// </summary>
    /// <param name="moddedResourceType"></param>
    private void UnassignModdedResourceType(ref ModdedResourceType moddedResourceType)
    {
        moddedResourceType.ResourceType = ResourceType.None;
        moddedResourceType.Registered = false;
    }

    /// <summary>
    /// Gets a RegisteredModdedResource by its name. Will return empty object if one is not found.
    /// </summary>
    /// <param name="name">The name of the ModdedResourceType</param>
    /// <returns>The requested ModdedResourceType or an empty object.</returns>
    public ModdedResourceType GetRegisteredModdedResourceTypeByName(string name)
    {
        // For mod A to use resource from mod B via ModdingFramework
        return RegisteredModdedResourceTypes.Where(mrt => mrt.Name.ToLowerInvariant() == name.ToLowerInvariant()).FirstOrDefault();
    }

    /// <summary>
    /// Gets a RegisteredModdedResource by its ResourceType. Will return emtpy object is one is not found.
    /// </summary>
    /// <param name="resourceType"></param>
    /// <returns></returns>
    public ModdedResourceType GetModdedResourceType(ResourceType resourceType)
    {
        return RegisteredModdedResourceTypes.Where(mrt => mrt.ResourceType == resourceType).FirstOrDefault();
    }

    /// <summary>
    /// Logs everything about ModdingFramework.
    /// </summary>
    public void LogDump()
    {
        ULogger.Log("=========LOG DUMP=========");

        ULogger.Log($"Resources:");
        ULogger.Log($"{nameof(AssignedResourceTypes)}: {AssignedResourceTypes.Count()}");
        foreach (ModdedResourceType mrt in RegisteredModdedResourceTypes)
        {
            ULogger.Log($"{mrt.Name, 20} | {mrt.ResourceType}");
        }
        ULogger.Log();

        ULogger.Log($"ModdedResourceTypes:");
        ULogger.Log($"{nameof(RegisteredModdedResourceTypes)}: {RegisteredModdedResourceTypes.Count()}");
        ULogger.Log(string.Join("\n ", RegisteredModdedResourceTypes));
        ULogger.Log($"{nameof(UnregisteredModdedResourceTypes)}: {UnregisteredModdedResourceTypes.Count()}");
        ULogger.Log(string.Join("\n ", UnregisteredModdedResourceTypes));
        ULogger.Log();

        ULogger.Log("ModConfigs:");
        ULogger.Log($"{nameof(RegisteredModConfigs)}: {RegisteredModConfigs.Count()}");
        ULogger.Log(string.Join("\n ", RegisteredModConfigs));
        ULogger.Log($"{nameof(UnregisteredModConfigs)}: {UnregisteredModConfigs.Count()}");
        ULogger.Log(string.Join("\n ", UnregisteredModConfigs));

        ULogger.Log();

        ULogger.Log($"OTHER:");
        ULogger.Log($"{nameof(Helper)}: {Helper} (Should be registered in Preload)");
        ULogger.Log("=========END DUMP=========");
    }

    /// <summary>
    /// Logs the dependencies of all <see cref="ModConfigs"/>.
    /// </summary>
    public void LogModDependencies()
    {
        ULogger.Log(cat, "Mod Dependencies. Open using:");
        ULogger.Log(cat, "https://omute.net/editor");
        ULogger.Log(cat, GenerateModDependencyTree());
    }

    /// <summary>
    /// Creates dependency tree as JSON for use in: https://omute.net/editor
    /// </summary>
    /// <returns></returns>
    public string GenerateModDependencyTree()
    {
        ICollection<ModConfigMF> modConfigs = this.ModConfigs;
        modConfigs.Add(new ModConfigMF() { ModName = this.name });

        modConfigs.Select(mc => new ModDependency(ModConfigs, mc));

        return JsonConvert.SerializeObject(modConfigs);
    }

    /// <summary>
    /// Calculates the mod load order based on dependencies.
    /// A List of Lists will be returned with the first value being the first to be loaded.
    /// The order of mods inside of the inner list does not matter for loading order though is alphabetical by <see cref="ModConfigMF.ModName"/>.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<IEnumerable<ModConfigMF>> GenerateModLoadOrder()
    {
        // TODO this 100% allows infinite issues
        List<ModConfigMF> notAttemptedToLoad = ModConfigs;
        List<ModConfigMF> loaded = new List<ModConfigMF>();
        // Each list is a "round of loading"
        List<List<ModConfigMF>> loadOrderLists = new List<List<ModConfigMF>>();

        // Counter is to stop this being infinite. ATM if there is an invalid name it will 100% error
        int counter = 50;
        while (notAttemptedToLoad.Count > 0 && counter > 0)
        {
            List<ModConfigMF> loadRound = new List<ModConfigMF>();

            foreach (ModConfigMF modConfig in notAttemptedToLoad)
            {
                // If mod has all dependencies loaded. Add to to current round of to load.
                if (!HasUnloadedDependencies(modConfig, loaded))
                {
                    loadRound.AddItem(modConfig);
                    loaded.Add(modConfig);
                    notAttemptedToLoad.Remove(modConfig); // TODO is this allowed in a foreach?
                }
            }

            loadRound = loadRound.OrderBy(mc => mc.ModName).ToList();
            loadOrderLists.AddItem(loadRound);
            counter++;
        }

        return loadOrderLists;
    }

    /// <summary>
    /// If the current given <paramref name="modConfig"/> has dependencies not included in <paramref name="loaded"/>.
    /// </summary>
    /// <param name="modConfig"></param>
    /// <param name="loaded"></param>
    /// <returns></returns>
    private bool HasUnloadedDependencies(ModConfigMF modConfig, IEnumerable<ModConfigMF> loaded)
    {
        // No dependencies means it cannot have any that are not loaded
        if (modConfig.Dependencies.Count() == 0)
        {
            return false;
        }

        // If modConfig has a dependency not currently inside of loaded. It has an unloaded dependency.
        List<string> loadedModNames = loaded.SelectMany(l => l.Dependencies).ToList();
        bool hasUnloaded = false;
        foreach(string dependencyName in modConfig.Dependencies)
        {
            if (!loadedModNames.Contains(dependencyName))
            {
                hasUnloaded = true;
            }
        }

        return hasUnloaded;
    }

    public class ModDependency
    {
        public string ModName { get; set; }

        public IEnumerable<ModDependency> Dependencies { get; set; }

        public ModDependency(ICollection<ModConfigMF> modConfigs, ModConfigMF modConfig)
        {
            ModName = modConfig.ModName;
            Dependencies = modConfig.Dependencies.Select(mc => new ModDependency(modConfigs, modConfig));
        }
    }

    public ModConfigMF GetModConfig(string modName)
    {
        return ModConfigs.Where(mc => mc.ModName.ToLowerInvariant() ==  modName.ToLowerInvariant()).FirstOrDefault();
    }
}

public class WorldFields
{
    // Private fields World has
    public CellInfluence[] cellInfluenceData;
    public Cell[] cellData;
    public Timer screenshotTimer = new Timer(10f);
    //public int gridWidth;
    //public int gridHeight;
    public SimplePriorityQueue<Cell> findQueue = new SimplePriorityQueue<Cell>();
    public static int bridgeHash = "bridge".GetHashCode(); 
    public static int drawbridgeHash = "drawbridge".GetHashCode();
    public System.Random randomStoneState;
    public int frameToWaitBeforeSeen = 5;
    public List<GameObject> stoneList = new List<GameObject>();
    public List<CombineInstance> combineList = new List<CombineInstance>();
    public List<Cell> stoneGrowList = new List<Cell>();
    public bool placedCavesWitches;
    //public List<LandmassStartInfo> landmassStartInfo = new List<World.LandmassStartInfo>(); // Can't be done as private class
    public ArrayExt<Villager> emptyVillagerList = new ArrayExt<Villager>(0);
    public ArrayExt<ArrayExt<Villager>> villagersPerLandMass = new ArrayExt<ArrayExt<Villager>>(10);
    public bool regenRimCells;
    public int framesAfterGenerate;
    public string screenieSavePath = string.Empty;
    public UnityAction onScreenshotComplete;
    public Vector3[] cardinalOffsets = new Vector3[]
    {
        new Vector3(1f, 0f, 0f),
        new Vector3(-1f, 0f, 0f),
        new Vector3(0f, 0f, 1f),
        new Vector3(0f, 0f, -1f)
    };
    public Cell[] scratchNeighbors = new Cell[8];
    public Cell[] scratchNeighborsExtendedPlus = new Cell[12];
    public int stonebridgeHash = "stonebridge".GetHashCode();
    public static int stockpileHash = "stockpile".GetHashCode();
    public Vector3[] cellBoundOffsets = new Vector3[]
    {
        Vector3.zero,
        Vector3.forward,
        new Vector3(1f, 0f, 1f),
        Vector3.right
    };
    public TreeGrowth treeGrowth;
    public int numLandMasses;
        
    /// <summary>
    /// Contains all of the private fields for a given world
    /// </summary>
    public WorldFields()
    {
        
    }
}