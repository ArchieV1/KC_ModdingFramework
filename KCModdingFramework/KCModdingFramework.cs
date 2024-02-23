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
using static KaC_Modding_Engine_API.Objects.Resources.VanillaModdedResourceTypes;
using System.Runtime.InteropServices;

public class ModdingFramework : MonoBehaviour
{
    /// <summary>
    /// The KCModHelper injected by KC
    /// </summary>
    public KCModHelper Helper { get; private set; }

    /// <summary>
    /// Gets a Random to be used. Can be set with seed to remove randomness for testing.
    /// </summary>
    public Random Random { get; } = new Random(123456789);

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

    // For mod registering
    public IMCPort port;

    #region Initialisation

    // Initial order goes #ctor > PreScriptLoad > Preload > SceneLoaded > Start
    // Therefore #ctor has no Helper class instantiated
    public ModdingFramework()
    {
        var harmony = HarmonyInstance.Create("uk.ArchieV.KCModdingFramework");
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
        port = gameObject.AddComponent<IMCPort>();
        port.RegisterReceiveListener<ModConfigMF>(MethodNames.RegisterMod, RegisterModHandler);

        // How to know which mods use ModConfig?
        // TODO question above

        // Register all loaded mods
        for (int i = 0; i < ModConfigs.Count(); i++)
        {
            ModConfigMF mod = ModConfigs[i];
            try
            {
                RegisterMod(ref mod);
                handler.SendResponse(port.gameObject.name, $"Successfully registered mod from {port.gameObject.name}");
            }
            catch (Exception e)
            {
                ULogger.Log(cat, $"Failed to register mod {source}\n");
                ULogger.Log(cat, e);
                handler.SendError(port.gameObject.name, e);
            }
        }
    }
    #endregion
    
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
                ModConfigMF,
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
                RegisterResource(moddedResourceType, ModConfigMF.AssetBundles);
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
    private void RegisterResource(ModdedResourceType moddedResourceType, AssetBundle assetBundle)
    {
        ULogger.Log($"Registering ModdedResourceType: {moddedResourceType}");

        // Not loaded earlier as breaks JSON encoding
        moddedResourceType.Model = assetBundle.LoadAsset(moddedResourceType.AssetBundlePath) as GameObject;
        
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
    /// Logs everything about ModdingFramework
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