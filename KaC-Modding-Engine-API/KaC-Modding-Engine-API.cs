using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Harmony;
using JetBrains.Annotations;
using Priority_Queue;
using UnityEngine;
using UnityEngine.Events;

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
            
            Inst = this;

            _unassignedResourceTypes = Enum.GetValues(typeof(ResourceType)).Cast<ResourceType>().ToList();
        }

        public void PreScriptLoad(KCModHelper helper)
        {
            Helper = helper;
            Helper.Log("PreScriptLoad");
            
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
            
            // BUNDLE NAME WILL BE LOWER CASE
            // ASSET NAME DEPENDS ON THE MODDER
            // In this case: GoldDeposit (Shader) and golddeposit1 (Model)
            
            // These two will be supplied by the 3rd party mod
            AssetBundle goldMinesAssetBundle = KCModHelper.LoadAssetBundle($"{Helper.modPath}", "golddeposit");
            GameObject goldDepositModel = goldMinesAssetBundle.LoadAsset("assets/workspace/golddeposit1.prefab") as GameObject;
            GoldDeposit goldDeposit = new GoldDeposit(goldDepositModel);
            
            Helper.Log("Registering test mod: (Registering Generators and ResourceTypeBases)");
            ModConfig goldMinesMod = new ModConfig
            {
                Author = "ArchieV1",
                ModName = "GoldMines mod",
                Generators = new GeneratorBase[]
                {
                    new GoldDepositGenerator(new ResourceTypeBase[]{ goldDeposit }), 
                }
            };
            
            Helper.Log($"Gold mines asset bundle: {goldMinesAssetBundle}");
            Helper.Log($"goldDeposit model exists: {goldDeposit.Model != null}");
            
            RegisterMod(goldMinesMod);
            Helper.Log($"Test mod is registered: {goldMinesMod.Registered.ToString()}");
            
            LogDump();
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
        public bool AssignResourceTypeData(ResourceTypeBase resourceTypeBase, ResourceType resourceType)
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
        public GoldDeposit(GameObject model) : base(model)
        {
        }
    }

    public class GoldDepositGenerator : GeneratorBase
    {
        public GoldDepositGenerator(ResourceTypeBase[] resourceTypeBases) : base(resourceTypeBases)
        {
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
        public GameObject Model; // Generator sets this
        public ResourceType ResourceType;
        
        public ResourceTypeBase(GameObject model)
        {
            Model = model;
            Main.Inst.Helper.Log($"Model for {GetType()} loaded: {Model != null}");
        }
    }
    
    public class GeneratorBase
    {
        public readonly string Name; // The name of this resource type
        public readonly ResourceTypeBase[] Resources; // The resources to be used in .Generate()

        /// <summary>
        /// Create a ResourceTypeBase with multiple resources contained within it.
        /// Use this if generation code required multiple 
        /// </summary>
        /// <param name="resourceTypeBases">The list of resources this generatorBase will use</param>
        public GeneratorBase(ResourceTypeBase[] resourceTypeBases)
        {
            Main.Inst.Helper.Log($"Creating GeneratorBase ({GetType()}).\n" +
                                 $"It contains the resources:\n {resourceTypeBases.ToList().Join(null, "\n")}");

            Name = GetType().ToString();
            Resources = resourceTypeBases;

            Main.Inst.Helper.Log($"Created {Name}");
        }
        
        /// <summary>
        /// Generates the resources in this generatorBase into the, already generated, world.
        /// Look at World.GenLand() for or http://www.ArchieV.uk/GoldMines for examples
        /// Please use World.inst.seed, SRand, and this.RandomStoneState for randomness.
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

        /* All private values from `World` recreated with the CORRECT VALUES
         * The comment next to it is where is is set in World
         * These are not common to all Generator classes. Edits here will not effect other Generator classes
        */ 
        
        
        /// <summary>
        /// Sets many private values to what they are in the base `World` but are private so cannot be accessed with __instance
        /// </summary>
        /// <param name="width">this.width</param>
        /// <param name="height">this.height</param>
        /// <returns></returns>
        private bool Setup(int width, int height)
        {
            try
            {
                // randomStoneState = new System.Random(1234567);
                // gridWidth = width;
                // gridHeight = height;
                // cellData = new Cell[gridWidth * gridHeight];
                // cellInfluenceData = new CellInfluence[gridWidth * gridHeight];
                
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }
        

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
    /// Use this Generator if you wish your modded resource to spawn like vanilla Stone does. Requires two resourceTypes
    /// </summary>
    public class StoneLikeGenerator : GeneratorBase
    {
        public StoneLikeGenerator(ResourceTypeBase[] resourceTypeBases) : base(resourceTypeBases)
        {
        }

        /// <summary>
        /// Stone generation requires two resources.
        /// A good resource and a dud resource. Can use vanilla resources for either. Just use their data in your ResourceTypeBase implementation
        /// </summary>
        /// <param name="world"></param>
        /// <returns></returns>
        public override bool Generate(World world)
        {
            if (Resources.Length <= 2)
            {
                return false;
            }
            // Do StoneLike generation


            return base.Generate(world);
        }
    }

    public class WoodLikeGenerator : GeneratorBase
    {
        public WoodLikeGenerator(ResourceTypeBase[] resourceTypeBases) : base(resourceTypeBases)
        {
        }

        public override bool Generate(World world)
        {
            return base.Generate(world);
        }
    }

    public class IronLikeGenerator : GeneratorBase
    {
        public IronLikeGenerator(ResourceTypeBase[] resourceTypeBases) : base(resourceTypeBases)
        {
        }

        public override bool Generate(World world)
        {
            return base.Generate(world);
        }
    }

    public class FishLikeGenerator : GeneratorBase
    {
        public FishLikeGenerator(ResourceTypeBase[] resourceTypeBases) : base(resourceTypeBases)
        {
        }

        public override bool Generate(World world)
        {
            return base.Generate(world);
        }
    }

    public class EmptyCaveLikeGenerator : GeneratorBase
    {
        public EmptyCaveLikeGenerator(ResourceTypeBase[] resourceTypeBases) : base(resourceTypeBases)
        {
        }

        public override bool Generate(World world)
        {
            return base.Generate(world);
        }
    }

    public class WitchHutLikeGenerator : GeneratorBase
    {
        public WitchHutLikeGenerator(ResourceTypeBase[] resourceTypeBases) : base(resourceTypeBases)
        {
        }

        public override bool Generate(World world)
        {
            return base.Generate(world);
        }
    }
    
    

    [HarmonyPatch(typeof(World), "GenLand")]
    class GenLand_Patch
    {
        /// <summary>
        /// Runs after map has been generated and adds every resource with a Generate() method
        /// </summary>
        /// <param name="__instance"></param>
        static void Postfix(ref World __instance)
        {
            KCModHelper helper = Main.Inst.Helper;
            
            helper.Log($"POSTFIXING \"GenLand\" with seed: {__instance.seed.ToString()}");

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