using System;
using System.Collections.Generic;
using System.Linq;
using Harmony;
using Newtonsoft.Json;
using Priority_Queue;
using UnityEngine;
using UnityEngine.Events;

public class ModdingFramework : MonoBehaviour
{
    public KCModHelper Helper { get; private set; }
    public static string cat = "ModdingFramework";
    public static ModdingFramework Inst { get; private set; }

    private ResourceTypeBase _defaultResourceTypeBase = new ResourceTypeBase
    {
        DefaultResource = true
    };
    public ResourceType[] listOfDefaultResources =
    {
        ResourceType.None,
        ResourceType.Stone,
        ResourceType.Water,
        ResourceType.Wood,
        ResourceType.EmptyCave,
        ResourceType.IronDeposit,
        ResourceType.UnusableStone,
        ResourceType.WitchHut,
        ResourceType.WolfDen
    };
   

    // List of all ResourceTypes
    private Dictionary<ResourceType, ResourceTypeBase> _assignedResourceTypes = new Dictionary<ResourceType, ResourceTypeBase>();
    
    // List of modConfigs
    public List<ModConfigMF> RegisteredModConfigs = new List<ModConfigMF>();
    public List<ModConfigMF> UnregisteredModConfigs = new List<ModConfigMF>();
    public List<GeneratorBase> RegisteredGenerators = new List<GeneratorBase>();

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
    }

    public void Preload(KCModHelper helper)
    {
        Helper = helper;
        Debugging.Helper = Helper;
        Debugging.Active = true;
        Helper.Log($"Loading KCModdingFramework at {DateTime.Now}");
        Helper.Log($"===============Preload===============");
        
        // Mark the default values as assigned (ironDeposit, stoneDeposit etc)
        Helper.Log("Adding default ResourceTypes to _assignedResourceTypes");
        foreach (ResourceType resource in listOfDefaultResources)
        {
            _assignedResourceTypes.Add(resource, _defaultResourceTypeBase);
        }
        Helper.Log("Finished adding default ResourceTypes to _assignedResourceTypes");
        
        LogDump();
    }
    
    public void PreScriptLoad(KCModHelper helper)
    {
        Helper.Log($"===============PreScriptLoad===============");
    }

    public void SceneLoaded(KCModHelper helper)
    {
        // Register mods here??
        Helper.Log(($"===============SceneLoaded==============="));
        
        // Assign port
        transform.name = ModdingFrameworkNames.Objects.ModdingFrameworkName;
        gameObject.name = ModdingFrameworkNames.Objects.ModdingFrameworkName;
        port = gameObject.AddComponent<IMCPort>();
        port.RegisterReceiveListener<ModConfigMF>(ModdingFrameworkNames.Methods.RegisterMod, RegisterModHandler);
    }

    public void Start()
    {
        Helper.Log($"===============Start===============");
        LogDump();
    }
    #endregion
    
    private void RegisterModHandler(IRequestHandler handler, string source, ModConfigMF mod)
    {
        Helper.Log($"Received message from {source}");
        Helper.Log($"Registering mod (Handler)\n" +
                   $"Mod: `{mod}`\n" +
                   $"Source: {source}\n" +
                   $"Handler: {handler}");
        Helper.Log(Tools.GetCallingMethodsAsString());
        ULogger.Log(cat, $"Registering: `{mod}`");
        ULogger.Log(cat, $"Source: `{source}`");
        
        try
        {
            RegisterMod(mod);
            handler.SendResponse(port.gameObject.name,$"Successfully registered mod from {port.gameObject.name}");
        }
        catch (Exception e)
        {
            ULogger.Log(cat, $"Failed to register mod {source}\n");
            ULogger.Log(cat, e);
            handler.SendError(port.gameObject.name, e);
        }
    }

    /// <summary>
    /// Registers the given mod. Changes "Registered" to true (Even if it failed to register the mod in its entirety
    /// </summary>
    /// <param name="ModConfigMF"></param>
    /// <returns>Returns false if failed to register mod in its entirety</returns>
    public void RegisterMod(ModConfigMF ModConfigMF)
    {
        if (ModConfigMF == null)
        {
            Helper.Log("ModConfigMF is null. ABORTING");
            return;
        }
        
        // Check if mod can be encoded (No self referencing loops)
        try
        {
            _ = IMCMessage.CreateRequest(name, name,
                JsonConvert.SerializeObject(ModConfigMF, IMCPort.serializerSettings));
        }
        catch (Exception e)
        {
            Helper.Log("Failed to encode mod with error:");
            Helper.Log(e.ToString());
        }

        Helper.Log($"Registering {ModConfigMF.ModName} by {ModConfigMF.Author}...");

        // Assigns each GeneratorBase an unassignedResourceType
        int numInitialGenRegistered = RegisteredGenerators.Count;
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
        int numGeneratorsRegistered = RegisteredGenerators.Count - numInitialGenRegistered;
        
        
        if (ModConfigMF.Generators.Length != numGeneratorsRegistered)
        {
            Helper.Log($"Failed to register {ModConfigMF.ModName} by {ModConfigMF.Author}.\n" +
                       $"Registered {numGeneratorsRegistered} generators.\n" +
                       $"Failed to register {ModConfigMF.Generators.Length - numGeneratorsRegistered} generators.");
            throw new Exception("Failed to register mod");
        }

        Helper.Log($"Registered {ModConfigMF.ModName} by {ModConfigMF.Author} successfully!\n" +
                   $"Registered {numGeneratorsRegistered} generators.");
        ModConfigMF.Registered = true;
        RegisteredModConfigs.Add(ModConfigMF);
    }

    /// <summary>
    /// Registers the given generator to assign each Resource in it an unassigned ResourceType
    /// </summary>
    /// <param name="generator"></param>
    private void RegisterGenerator(GeneratorBase generator)
    {
        foreach(ResourceTypeBase resourceTypeBase in generator.Resources)
        {
            AssignResourceTypeBase(resourceTypeBase);
            resourceTypeBase.LoadAssetBundle(Helper);
            resourceTypeBase.LoadModel();
        }
        RegisteredGenerators.Add(generator);
    }

    /// <summary>
    /// Assigns the resourceTypeBase an unassigned ResourceType
    /// </summary>
    /// <param name="resourceTypeBase"></param>
    /// <param name="assetBundle"></param>
    /// <returns></returns>
    private void RegisterResource(ResourceTypeBase resourceTypeBase, AssetBundle assetBundle)
    {
        Helper.Log($"Registering resourceTypeBase {resourceTypeBase}");
        resourceTypeBase.Model = assetBundle.LoadAsset(resourceTypeBase.AssetBundlePath) as GameObject; // Not loaded earlier as breaks JSON encoding
        
        AssignResourceTypeBase(resourceTypeBase);
    }

    #region ResourceTypeAssigning

    /// <summary>
    /// Assigns given ResourceTypeBase an unassigned ResourceType
    /// </summary>
    /// <param name="resourceTypeBase"></param>
    /// <returns></returns>
    private void AssignResourceTypeBase(ResourceTypeBase resourceTypeBase)
    {
        ResourceType resourceType = (ResourceType) int.MaxValue;
        foreach (int val in Enumerable.Range(0, int.MaxValue).ToArray())
        {
            if (!_assignedResourceTypes.ContainsKey((ResourceType) val))
            {
                AssignResourceTypeBase((ResourceType) val, resourceTypeBase);
            }
        }
    }
    
    /// <summary>
    /// Adds resourceType/resourceTypeBase pair to _assignedResourceTypes
    /// </summary>
    /// <param name="resourceType"></param>
    /// <param name="resourceTypeBase"></param>
    /// <returns>True if success</returns>
    private void AssignResourceTypeBase(ResourceType resourceType, ResourceTypeBase resourceTypeBase)
    {
        if (_assignedResourceTypes.ContainsKey(resourceType))
        {
            Helper.Log($"DID NOT ASSIGN {resourceTypeBase.Name} resourceType: {resourceTypeBase.ResourceType}");
            throw new ArgumentException($"Key, {resourceType}, is already assigned.");
        }
        
        _assignedResourceTypes.Add(resourceType, resourceTypeBase);
        resourceTypeBase.ResourceType = resourceType;

        Helper.Log($"Assigned {resourceTypeBase.Name} resourceType: {resourceTypeBase.ResourceType}");
    }

    /// <summary>
    /// Removes ResourceType/ResourceTypeBase pair by resourceType from _assignedResourceTypes
    /// </summary>
    /// <param name="resourceType"></param>
    private void UnassignResourceType(ResourceType resourceType)
    {
        if (_assignedResourceTypes.ContainsKey(resourceType))
        {
            _assignedResourceTypes.Remove(resourceType);
            _assignedResourceTypes[resourceType].ResourceType = ResourceType.None;
        }
        else
        {
            throw new ArgumentException($"{resourceType} is not assigned");
        }
    }

    /// <summary>
    /// Removes ResourceType/ResourceTypeBase pair by resourceTypeBase from _assignedResourceTypes
    /// </summary>
    /// <param name="resourceTypeBase"></param>
    private void UnassignResourceType(ResourceTypeBase resourceTypeBase)
    {
        UnassignResourceType(GetResourceType(resourceTypeBase));
    }
    
    public ResourceTypeBase GetResourceTypeBase(ResourceType resourceType)
    {
        return _assignedResourceTypes[resourceType];
    }

    /// <summary>
    /// DOES NOT PROMISE TO FIND CORRECT VALUE. MAY FIND DEFAULT
    /// </summary>
    /// <param name="resourceTypeBase"></param>
    /// <returns></returns>
    public ResourceType GetResourceType(ResourceTypeBase resourceTypeBase)
    {
        return _assignedResourceTypes.FirstOrDefault(x => x.Value == resourceTypeBase).Key;
    }
    
    #endregion

    

    /// <summary>
    /// Logs everything about ModdingFramework
    /// </summary>
    public void LogDump()
    {
        Helper.Log("=========LOG DUMP=========");

        Helper.Log($"RANDOM:");
        Helper.Log($"Helper: {Helper}");
        Helper.Log($"");

        Helper.Log($"RESOURCES:");
        Helper.Log($"_assignedResourceTypes: {_assignedResourceTypes.Count}");
        foreach (KeyValuePair<ResourceType, ResourceTypeBase> pair in _assignedResourceTypes)
        {
            Helper.Log($"{pair.Key, 4} | {pair.Value.Name, 20}");
        }
        
        Helper.Log("");

        Helper.Log($"RegisteredModConfigs: {RegisteredModConfigs.Count}");
        Helper.Log(string.Join("\n ", RegisteredModConfigs));
        
        
        Helper.Log("=========END DUMP=========");
    }
}

#region HarmonyPatches
[HarmonyPatch(typeof(World), "GenLand")]
class GenLand_Patch
{
    /// <summary>
    /// Runs after map has been generated and adds every resource with a Generate() method
    /// </summary>
    /// <param name="__instance"></param>
    public static void Postfix(ref World __instance)
    {
        KCModHelper helper = ModdingFramework.Inst.Helper;
        if (helper == null) return;
        
        helper.Log($"POSTFIXING \"GenLand\" with seed: {__instance.seed.ToString()}");
        helper.Log("Calling methods: " + Tools.GetCallingMethodsAsString());

        if (ModdingFramework.Inst.RegisteredModConfigs == null) return;
        foreach (ModConfigMF modConfig in ModdingFramework.Inst.RegisteredModConfigs)
        {
            if (modConfig.Generators == null) continue;
            helper.Log($"Mod {modConfig.ModName} contains {modConfig.Generators.Length} generators");
            foreach (GeneratorBase generator in modConfig.Generators)
            {
                helper.Log($"Generator: {generator}");
                helper.Log($"Contains:");
                if (generator.Resources == null) continue;
                foreach (var resource in generator.Resources)
                {
                    helper.Log($"\t{resource.Name}");
                }

                // Go and use this GeneratorBase's Generate method using its assigned ResourceTypes (AV00, AV01 etc)
                bool implemented = false;
                try
                {
                    implemented = generator.Generate(__instance);
                }
                catch (Exception e)
                {
                    helper.Log(e.ToString());
                    helper.Log($"Failed to generate: {generator}");
                }

                helper.Log($"{(implemented ? "Used" : "Didn't use")} {generator}");
            }
        }
        helper.Log("Finished postfixing");
    }
}

[HarmonyPatch(typeof(World.WorldSaveData), "Unpack")]
class Unpack_Patch
{
    /// <summary>
    /// Unpack all of the modded ResourceTypes
    /// </summary>
    /// <param name="__result"></param>
    public static void Postfix(ref World __result)
    {
        try
        {
            //TODO Determine if this is needed (I think yes 20/04/2021)
            KCModHelper helper = ModdingFramework.Inst.Helper;
            World world = __result;

            helper.Log($"POSTFIXING \"Unpack\" with seed {__result.seed}");

            // Create generator to be able to use it's methods
            ResourceTypeBase[] resourceTypeBases = new ResourceTypeBase[256]; // 256 is max KaC modding engine installer does
            foreach (var generatorBase in ModdingFramework.Inst.RegisteredModConfigs.SelectMany(modConfig => modConfig.Generators))
            {
                resourceTypeBases.AddRangeToArray(generatorBase.Resources);
            }    
            if (resourceTypeBases.Length == 0) return;
            
            Cell[] cellData = Tools.GetCellData(world);
            Cell.CellSaveData[] cellSaveData =
                (Cell.CellSaveData[]) Tools.GetPrivateWorldField(world, "cellSaveData");
            bool hasDeepWater = false;

            for (int i = 0; i < cellData.Length - 1; i++)
            {
                Cell cell = cellData[i];
                int x = i % world.GridWidth;
                int z = i / world.GridWidth;

                // Setting the cell.type was done in the method before it this Postfix
                // It was set if needed for default resources in 
                if (ModdingFramework.Inst.listOfDefaultResources.Contains(cell.Type)) continue;

                cell.Type = cellSaveData[i].type;

                helper.Log($"Cell {cell.x}, {cell.z} is: {cell.Type}");

                // This is the equivalent of World.SetupStoneForCell
                ResourceTypeBase currentResourceTypeBase = ModdingFramework.Inst.GetResourceTypeBase(cell.Type);
                GeneratorBase.TryPlaceResource(cell, currentResourceTypeBase, deleteTrees: false,
                    storePostGenerationType: true);

                helper.Log($"Set Cell to {currentResourceTypeBase}");
            }

            Tools.SetCellData(world, cellData);
            helper.Log("Finished patching Unpack");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}

[HarmonyPatch(typeof(KCModHelper.ModLoader), "SendScenePreloadSignal")]
class SendScenePreloadSignal_Patch
{
    public static void Postfix()
    {
        ModdingFramework.Inst.Helper.Log("SendScenePreloadSignal postfix");
        ModdingFramework.Inst.Helper.Log(Tools.GetCallingMethodsAsString());
        ModdingFramework.Inst.Helper.Log("Finished postfixing");

        // Initialise my mod in PreloadSignal
        // That way it is the last thing loaded
        // Wants other mods to register in SceneLoaded or before
    }
}
#endregion

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

#region ToolTips
public class ToolTipText
{
    /// <summary>
    /// A dictionary of ISO639_3 to ToolTip
    /// </summary>
    private Dictionary<string, string> dict = new Dictionary<string, string>();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="languageTextDictionary"></param>
    /// <param name="ISOCode">The ISO639 standard that the dictionary uses</param>
    public ToolTipText(Dictionary<string, string> languageTextDictionary, ISO639Code ISOCode = ISO639Code.ISO639_3)
    {
        // Set from whatever ISO639 code to ISO639-3
        string[] langArray = ISO639.GetLangArrayFromISO639(ISOCode);
        Dictionary<string, string> newDict = new Dictionary<string, string>();

        foreach (var langCode in langArray)
        {
            languageTextDictionary.TryGetValue(langCode, out string toolTip);

            if (dict.ContainsKey(langCode))
            {
                dict.Remove(langCode);
            }

            dict.Add(ISO639.ConvertStandard(langCode, ISOCode), toolTip);
        }
    }

    public ToolTipText(string[] textArray)
    {
        for(int x = 0; x < textArray.Length; x++)
        {
            dict[ISO639.languagesISO639_3[x]] = textArray[x];
        }
    }
}

public enum ISO639Code
{
    ISO639_1,
    ISO639_2_T,
    ISO639_2_B,
    ISO639_3,
}

public static class ISO639
{
    // Private arrays for converting from any language code to language code `ISO 639-3`
    public static readonly string[] languagesISO639_1 = new[]
    {
        "en",
        "de",
        "fr",
        "zh1",
        "zh2",
        "es",
        "nl",
        "pt",
        "it",
        "ja",
        "ko",
        "no",
        "pl",
        "ro",
        "ru",
        "uk",
        "sv",
        "tr"
    };

    public static readonly string[] languagesISO639_2_T = new[]
    {
        "eng",
        "deu",
        "fra",
        "zho1",
        "zho2",
        "spa",
        "nld",
        "por",
        "ita",
        "jpn",
        "kor",
        "nor",
        "pol",
        "ron",
        "rus",
        "ukr",
        "swe",
        "tur"
    };

    public static readonly string[] languagesISO639_2_B = new[]
    {
        "eng",
        "ger",
        "fre",
        "chi1",
        "chi2",
        "spa",
        "dut",
        "por",
        "ita",
        "jpn",
        "kor",
        "nor",
        "pol",
        "rum",
        "rus",
        "ukr",
        "swe",
        "tur"
    };

    /// <summary>
    /// This is the preferred ISO language code of this mod (See wiki on Chinese variation)
    /// </summary>
    public static readonly string[] languagesISO639_3 = new[]
    {
        "eng",
        "deu",
        "fra",
        "zho1",
        "zho2",
        "spa",
        "nld",
        "por",
        "ita",
        "jpn",
        "kor",
        "nor",
        "pol",
        "ron",
        "rus",
        "ukr",
        "swe",
        "tur"
    };

    public static string[] GetLangArrayFromISO639(ISO639Code ISOCode)
    {
        string[] langArray;
        switch (ISOCode)
        {
            case ISO639Code.ISO639_1:
                langArray = ISO639.languagesISO639_1;
                break;
            case ISO639Code.ISO639_2_T:
                langArray = ISO639.languagesISO639_2_T;
                break;
            case ISO639Code.ISO639_2_B:
                langArray = ISO639.languagesISO639_2_B;
                break;
            case ISO639Code.ISO639_3:
                langArray = ISO639.languagesISO639_3;
                break;
            default:
                langArray = ISO639.languagesISO639_3;
                break;
        }

        return langArray;
    }

    /// <summary>
    /// Converts between standards
    /// </summary>
    /// <param name="str"></param>
    /// <param name="codeFrom"></param>
    /// <param name="codeTo"></param>
    /// <returns></returns>
    public static string ConvertStandard(string str, ISO639Code codeFrom, ISO639Code codeTo = ISO639Code.ISO639_3)
    {
        string[] fromArray = GetLangArrayFromISO639(codeFrom);

        for (int x = 0; x < fromArray.Length; x++)
        {
            if (fromArray[x] == str)
            {
                return GetLangArrayFromISO639(codeTo)[x];
            }
        }

        return null;
    }
}
#endregion