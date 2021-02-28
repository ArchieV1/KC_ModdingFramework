using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using UnityEngine;

namespace KaC_Modding_Engine_API
{
    public class Main : MonoBehaviour
    {
        public KCModHelper Helper { get; private set; }
        public static Main Inst;

        public const int NumberDefaultResources = 9;

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
        private List<ResourceType> _unassignedResourceTypes;
        private Dictionary<ResourceTypeBase, ResourceType> _assignedResourceTypes = new Dictionary<ResourceTypeBase, ResourceType>();
        
        // List of modConfigs
        public List<ModConfig> ModConfigs = new List<ModConfig>();

        #region Initialisation

        // Initial order goes #ctor > PreScriptLoad > Preload > SceneLoaded > Start
        // Therefore #ctor has no Helper class instantiated
        public Main()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var harmony = HarmonyInstance.Create("uk.ArchieV.KaC-Modding-Engine-API");
            harmony.PatchAll(assembly);
        }

        public void PreScriptLoad(KCModHelper helper)
        {
            Helper = helper;
            Helper.Log("PreScriptLoad");
        }

        public void Preload(KCModHelper helper)
        {
            Helper = helper;
            Helper.Log("Preload");
        }

        public void SceneLoaded(KCModHelper helper)
        {
            Helper = helper;
            Helper.Log(("Scene loaded"));
        }

        public void Start()
        {
            Helper.Log($"Starting KaC-Modding-Engine-API at {DateTime.Now}");
            Inst = this;

            _unassignedResourceTypes = Enum.GetValues(typeof(ResourceType)).Cast<ResourceType>().ToList();

            // Mark the default values as assigned (ironDeposit, stoneDeposit etc) 
            // Ignore intelliSense it CANNOT be a foreach loop as it will edit the list while it goes
            for (int x = 0; x < _unassignedResourceTypes.Count; x++)
            {
                ResourceType resourceType = _unassignedResourceTypes[x];
                if (listOfDefaultResources.Contains(resourceType))
                {
                    Helper.Log($"Removing {resourceType} from unassigned list as it is a default ResourceType");

                    if (MarkResourceTypeAssigned(resourceType))
                    {
                        Helper.Log($"Removed {resourceType} from unassigned list");
                        // List is now shorter so need to not keep iterating
                        x--;
                    }
                    else
                    {
                        Helper.Log($"WARNING\n" +
                                   $"Failed to remove {resourceType} from unassigned list");
                    }
                }
            }

            Helper.Log("Finished removing default ResourceTypes from _unassignedResourceTypes");

            Helper.Log("Registering test mod:");
            // BUNDLE NAME WILL BE LOWER CASE
            // ASSET NAME DEPENDS ON THE MODDER (GoldDeposit in this case)
            
            // These two will be supplied by the 3rd party mod
            AssetBundle goldMinesAssetBundle = KCModHelper.LoadAssetBundle($"{Helper.modPath}", "golddeposit"); 
            //GoldDeposit goldDeposit = new GoldDeposit("GoldDeposit");
            GoldDeposit goldDeposit = new GoldDeposit("/Assets/KCAssets/GameMaterial/GoldDeposit.prefab");

            if (goldMinesAssetBundle == null)
            {
                Helper.Log("GOLD MINES ASSET BUNDLE IS NULL");
            }

            if (goldDeposit.Model == null)
            {
                Helper.Log("GOLDDEPOSIT MODEL IS BLANK");
            }
            
            ModConfig goldMinesMod = new ModConfig
            {
                Author = "ArchieV1",
                ModName = "GoldMines mod",
                Generators = new GeneratorBase[]
                {
                    new GoldDepositGenerator("GoldDeposit_Generator", new ResourceTypeBase[]{ goldDeposit }, goldMinesAssetBundle), 
                }
            };
            
            RegisterMod(goldMinesMod);
            Helper.Log($"Test mod is registered: {goldMinesMod.Registered.ToString()}");
            
            LogDump();
        }

        #endregion

        /// <summary>
        /// Registers the given mod. Changes "Registered" to true (Even if it failed to register the mod in its entirety
        /// </summary>
        /// <param name="modConfig"></param>
        /// <returns>Returns false if failed to register mod in its entirety</returns>
        public bool RegisterMod(ModConfig modConfig)
        {
            Helper.Log($"Registering {modConfig.ModName} by {modConfig.Author}...");
            int numGeneratorsRegistered = 0;

            // Assigns each GeneratorBase an unassignedResourceType
            foreach (GeneratorBase generator in modConfig.Generators)
            {
                bool success = RegisterGenerator(generator);
                if (success)
                {
                    numGeneratorsRegistered += 1;
                }
                else
                {
                    Helper.Log($"WARNING. FAILED TO REGISTER {generator.Name}.\n" +
                               $"There are {_unassignedResourceTypes.Count} _unassignedResourceTypes");
                }
            }
            
            modConfig.Registered = true;
            if (modConfig.Generators.Length != numGeneratorsRegistered)
            {
                Helper.Log($"Failed to register {modConfig.ModName} by {modConfig.Author}.\n" +
                           $"Registered {numGeneratorsRegistered} generators.\n" +
                           $"Failed to register {modConfig.Generators.Length - numGeneratorsRegistered} generators.");
                
                return false;
            }
            else
            {
                Helper.Log($"Registered {modConfig.ModName} by {modConfig.Author} successfully!\n" +
                           $"Registered {numGeneratorsRegistered} generators.");
                ModConfigs.Add(modConfig);
                return true;
            }
        }

        /// <summary>
        /// Registers the given generator to assign each Resource in it an unassigned ResourceType
        /// </summary>
        /// <param name="generator"></param>
        /// <returns>False upon failure of at least one </returns>
        private bool RegisterGenerator(GeneratorBase generator)
        {
            int succeeded = generator.Resources.Count(resourceTypeBase => RegisterResource(resourceTypeBase));

            return succeeded == generator.Resources.Length;
        }

        /// <summary>
        /// Assigns the resourceTypeBase an unassigned ResourceType
        /// </summary>
        /// <param name="resourceTypeBase"></param>
        /// <returns></returns>
        private bool RegisterResource(ResourceTypeBase resourceTypeBase)
        {
            if (_unassignedResourceTypes.IsNull() || _unassignedResourceTypes.Count == 0)
            {
                return false;
            }

            return AssignResourceTypeDataAResourceType(resourceTypeBase);
        }

        #region ResourceTypeAssigning

        /// <summary>
        /// Assigns given ResourceTypeBase an unassigned ResourceType
        /// </summary>
        /// <param name="resourceTypeBase"></param>
        /// <returns></returns>
        public bool AssignResourceTypeDataAResourceType(ResourceTypeBase resourceTypeBase)
        {
            ResourceType resourceType = _unassignedResourceTypes[0];

            return AssignResourceTypeData(resourceTypeBase, resourceType);
        }
        
        /// <summary>
        /// Adds resourceTypeBase/resourceType to _assignedResourceTypes and removes resourceType from _unassignedResourceTypes
        /// </summary>
        /// <param name="resourceType"></param>
        /// <param name="resourceTypeBase"></param>
        /// <returns>True if success</returns>
        private bool AssignResourceTypeData(ResourceTypeBase resourceTypeBase, ResourceType resourceType)
        {
            if (_unassignedResourceTypes.Contains(resourceType)
                && !_assignedResourceTypes.ContainsValue(resourceType))
            {
                _unassignedResourceTypes.Remove(resourceType);
                _assignedResourceTypes.Add(resourceTypeBase, resourceType);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes resourceType from _assignedResourceTypes and Adds resourceType to _unassignedResourceTypes
        /// </summary>
        /// <param name="resourceType"></param>
        /// <param name="base"></param>
        /// <returns>True if success</returns>
        private bool UnassignResourceTypeData(ResourceTypeBase resourceTypeBase, ResourceType resourceType)
        {
            if (!_unassignedResourceTypes.Contains(resourceType)
                && _assignedResourceTypes.ContainsValue(resourceType))
            {
                _unassignedResourceTypes.Add(resourceType);
                _assignedResourceTypes.Remove(resourceTypeBase);
                return true;
            }

            return false;
        }

        /// <summary>
        /// For removing default ResourceTypes from _unassignedResourceTypes (But not adding it to _assignedResourceTypes)
        /// </summary>
        /// <param name="resourceType"></param>
        /// <returns></returns>
        private bool MarkResourceTypeAssigned(ResourceType resourceType)
        {
            if (_unassignedResourceTypes.Contains(resourceType))
            {
                _unassignedResourceTypes.Remove(resourceType);
                return true;
            }

            return false;
        }

        #endregion

        /// <summary>
        /// Get the assigned resource types that have the given fieldName true
        /// </summary>
        /// <param name="generationType">The type of ResourceTypeGeneration generation</param>
        /// <returns>A dictionary of Generators that have the given fieldName true.
        /// Returns null if the given fieldName is invalid</returns>
        /// <exception cref="ArgumentException">fieldName cannot be null</exception>
        public Dictionary<ResourceTypeBase, ResourceType> GetResourceTypeByGenerationType(GenerationType generationType)
        {
            Dictionary<ResourceTypeBase, ResourceType> list = this._assignedResourceTypes;
            string fieldName = generationType.ToString();
            
            if (fieldName.IsNull())
            {
                throw new ArgumentException("fieldName cannot be null");
            }

            var result = new Dictionary<ResourceTypeBase, ResourceType>();

            var search = from x in list
                where (bool) x.Key?.GetType().GetProperty(fieldName)?.GetValue(x, null) == true
                select x;

            foreach (var KeyValuePair in search)
            {
                result.Add(KeyValuePair);
            }

            return result;
        }

        /// <summary>
        /// Logs everything about Main
        /// </summary>
        public void LogDump()
        {
            Helper.Log("=========LOG DUMP=========");

            Helper.Log($"Random:");
            Helper.Log($"Helper: {Helper}");

            Helper.Log($"Resources:");
            Helper.Log($"_assignedResourceTypes:");
            foreach (KeyValuePair<ResourceTypeBase, ResourceType> pair in _assignedResourceTypes)
            {
                Helper.Log($"{pair.Key.AssetName, 20} | {pair.Value, 4}");
            }

            Helper.Log("_unassignedResourceTypes:");
            Helper.Log(string.Join(", ", _unassignedResourceTypes));

            Helper.Log("ModConfigs:");
            Helper.Log(string.Join("\n ", ModConfigs));
            
            
            Helper.Log("=========END DUMP=========");
        }
    }
    
    public class ModConfig
    {
        public string ModName;
        public string Author;
        public GeneratorBase[] Generators;

        public bool Registered = false;
        
        public ModConfig(){}

        public override string ToString()
        {
            string generatorNames = Generators.Aggregate("", (current, generator) => current + generator);

            return $"{ModName} by {Author}. Generators included are {generatorNames}";
        }
    }
    
    public class GoldDeposit : ResourceTypeBase
    {
        public GoldDeposit(string assetName) : base(assetName)
        {
        }
    }

    public class GoldDepositGenerator : GeneratorBase
    {
        public GoldDepositGenerator(string name, ResourceTypeBase[] resourceTypeBases, AssetBundle assetBundle) : base(name, resourceTypeBases, assetBundle)
        {
            Console.WriteLine("Instatitated GoldDepositGenerator");
        }

        public override bool Generate(World world)
        {
            Console.WriteLine("Generating GoldDeposit"); 
            
            Cell cell;
            try
            {
                cell = world.GetCellData(world.GridHeight - 3, world.GridHeight - 3);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error getting cell");
                Console.WriteLine(e);
                return false;
            }
            
            ResourceTypeBase goldDeposit = Resources[0];

            if (cell == null || goldDeposit == null)
            {
                Console.WriteLine($"Cell: {cell}, GoldDeposit: {goldDeposit}");
                return false;
            }
            cell.Type = goldDeposit.ResourceType;
            cell.StorePostGenerationType();
            TreeSystem.inst.DeleteTreesAt(cell);
        
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(goldDeposit.Model);
            gameObject.transform.position = cell.Position;
            
            //gameObject.transform.localScale = new Vector3(RANDOMISATION);
            //gameObject.transform.Rotate(new Vector3(RANDOMISATION)); // 0 or 180 for Z axis

            if (cell.Models == null)
            {
                cell.Models = new List<GameObject>();
            }
            cell.Models.Add(gameObject);

            Console.WriteLine("Finished generating GoldDeposit"); 
            return true;
        }
    }
    
    /// <summary>
    /// Base class for a modder to expand/implement with their new resource
    /// </summary>
    public class ResourceTypeBase
    {
        public string AssetName;
        public GameObject Model;
        public ResourceType ResourceType;
        
        public ResourceTypeBase(string assetName)
        {
            AssetName = assetName;
        }
    }
    
    public class GeneratorBase
    {
        public string Name; // The name of this resource type
        public readonly AssetBundle AssetBundle;
        public readonly ResourceTypeBase[] Resources; // The thing with .Generate()
        
        public GameObject Model; // To appear on the map

        /// <summary>
        /// Create a ResourceTypeBase with multiple resources contained within it.
        /// Use this if generation code required multiple 
        /// </summary>
        /// <param name="name">The name of the resource generatorBase (For logging)</param>
        /// <param name="resourceTypeBases">The list of resources this generatorBase will use</param>
        /// <param name="assetBundle">The asset bundle all of the resource assets are located in</param>
        public GeneratorBase(string name, ResourceTypeBase[] resourceTypeBases,AssetBundle assetBundle)
        {
            Main.Inst.Helper.Log($"Creating GeneratorBase ({name}) from AssetBundle: {assetBundle}.\n" +
                                 $"It contains the resources:\n {resourceTypeBases.ToList().Join(null, "\n")}");

            Name = name ?? "DEFAULT_NAME";
            Resources = resourceTypeBases;
            AssetBundle = assetBundle;

            bool loadedModels = AttemptLoadModels();

            Main.Inst.Helper.Log($"Loaded models successfully: {loadedModels}");
            Main.Inst.Helper.Log($"Created {Name}");
        }

        private bool AttemptLoadModels()
        {
            bool success = true;
            // Load all of the Models into the resources
            foreach (var resource in Resources)
            {
                resource.Model = AssetBundle.LoadAsset(resource.AssetName) as GameObject;
                if (resource.Model == null)
                {
                    success = false;
                }
            }

            return success;
        }
        /// <summary>
        /// Generates the resources in this generatorBase into the, already generated, world.
        /// Look at World.GenLand() for or http://www.ArchieV.uk/GoldMines for examples
        /// Please use World.inst.seed and SRand for randomness.
        /// </summary>
        /// <param name="world"></param>
        /// <returns>Returns true if implemented</returns>
        public virtual bool Generate(World world)
        {
            return false;
        }

        public override string ToString()
        {
            return $"Generator_{Name}";
        }
        
        #region Useful Generation Methods

        // Places a small amount of "type" at x/y with some unusable stone around it
        // PlaceSmallStoneFeature(int x, int y, ResourceType type)
        
        // Places a large amount of "type" at x/y with some unusable stone around it
        // PlaceLargeStoneFeature(int x, int y, ResourceType type)
        
        // Sets cell at x/y's type to be "type".
        // Deleted the trees at that location
        // Calls SetupStoneForCell(Cell cell) to instantiate the model
        // PlaceStone(int x, int y, ResourceType type)
        
        // Instantiates the model and 
        // gameObject = UnityEngine.Object.Instantiate<GameObject>(CORRECT_MODEL); // Instantiate the mode
        // gameObject.GetComponent<MeshRenderer>().sharedMaterial = this.uniMaterial[0]; // Apply the mesh. Always [0]. What is uniMaterial
        // gameObject.transform.parent = this.resourceContainer.transform; // Smth to do with moving it relative to the parent but not in the world
        // SetupStoneForCell(Cell cell)
        
        #endregion
    }
    
    /// <summary>
    /// The different default ways a GeneratorBase can be generated
    /// </summary>
    public enum GenerationType
    {
        Stone = 0,
        Iron = 1,
        Fish = 2,
        Tree = 3,
    }
    
    [HarmonyPatch(typeof(World), "GenLand")]
    class GenLand_Patch
    {
        /// <summary>
        /// Runs after map has been generated and adds every resource with a Generate() method
        /// </summary>
        /// <param name="__instance"></param>
        //[HarmonyPatch]
        //[HarmonyPatch(typeof(World))]
        //[HarmonyPatch("GenLand")]
        static void Postfix(ref World __instance)
        {
            KCModHelper helper = Main.Inst.Helper;
            
            helper.Log($"Postfixing GenLand with seed: {__instance.seed.ToString()}");
            helper.Log(__instance.darkStoneModel.name);
            helper.Log(__instance.lightStoneModel.name);
            helper.Log(__instance.ironStoneModel.name);

            foreach (ModConfig modConfig in Main.Inst.ModConfigs)
            {
                helper.Log($"Mod {modConfig.ModName} contains {modConfig.Generators.Length} generators");
                foreach (GeneratorBase generator in modConfig.Generators)
                {
                    helper.Log($"Generator: {generator}");
                    helper.Log($"Contains:");
                    foreach (var resource in generator.Resources)
                    {
                        helper.Log($"\t{resource.GetType()}");
                    }
                    
                    // Go and use this GeneratorBase's Generate method using its assigned ResourceTypes (AV00, AV01 etc)
                    bool implemented = false;
                    try
                    {
                        implemented = generator.Generate(__instance);
                    }
                    catch
                    {
                        Main.Inst.Helper.Log($"Failed to generate: {generator}");
                    }

                    helper.Log($"{(implemented ? "Used" : "Didn't use")} {generator}");
                }
            }
        }
    }
}