using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Assets.Code;
using Crosstales.FB.Util;
using Harmony;
using JetBrains.Annotations;
using Priority_Queue;
using Steamworks;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR.InteractionSystem;

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
            Helper.Log($"Starting KaC-Modding-Engine-API at {DateTime.Now}");
            
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
            ResourceTypeBase goldDeposit = new ResourceTypeBase("GoldDeposit", goldDepositModel);
            
            Helper.Log("Registering test mod: (Registering Generators and ResourceTypeBases)");
            ModConfig goldMinesMod = new ModConfig
            {
                Author = "ArchieV1",
                ModName = "GoldMines mod",
                Generators = new GeneratorBase[]
                {
                    new GoldDepositGenerator(new []{ goldDeposit }), 
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
            Helper.Log("Start");
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
            int succeeded = generator.Resources.Count(RegisterResource);

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

            return AssignResourceTypeBase(resourceTypeBase);
        }

        #region ResourceTypeAssigning

        /// <summary>
        /// Assigns given ResourceTypeBase an unassigned ResourceType
        /// </summary>
        /// <param name="resourceTypeBase"></param>
        /// <returns></returns>
        public bool AssignResourceTypeBase(ResourceTypeBase resourceTypeBase)
        {
            ResourceType resourceType = _unassignedResourceTypes[0];

            return AssignResourceTypeBase(resourceTypeBase, resourceType);
        }
        
        /// <summary>
        /// Adds resourceTypeBase/resourceType to _assignedResourceTypes and removes resourceType from _unassignedResourceTypes
        /// </summary>
        /// <param name="resourceType"></param>
        /// <param name="resourceTypeBase"></param>
        /// <returns>True if success</returns>
        public bool AssignResourceTypeBase(ResourceTypeBase resourceTypeBase, ResourceType resourceType)
        {
            if (!_unassignedResourceTypes.Contains(resourceType) ||
                _assignedResourceTypes.ContainsValue(resourceType)) return false;
            
            _unassignedResourceTypes.Remove(resourceType);
            _assignedResourceTypes.Add(resourceTypeBase, resourceType);

            resourceTypeBase.ResourceType = resourceType;
            
            return true;
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

        public ResourceTypeBase GetResourceTypeBase(ResourceType resourceType)
        {
            var key = _assignedResourceTypes.FirstOrDefault(x => x.Value == resourceType).Key;

            try
            {
                var result = _assignedResourceTypes[key];
                return key;
            }
            catch
            {
                throw new ArgumentException("There is no ResourceTypeBase for the given ResourceType");
            }
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
                Helper.Log($"{pair.Key.Name, 20} | {pair.Value, 4}");
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
    
    public class GoldDepositGenerator : GeneratorBase
    {
        public GoldDepositGenerator(ResourceTypeBase[] resourceTypeBases) : base(resourceTypeBases)
        {
        }

        public override bool Generate(World world)
        {
            KCModHelper helper = Main.Inst.Helper;
            Console.WriteLine("Generating GoldDeposit");
            Console.WriteLine($"{this}");
            Resources.ForEach(x => Console.WriteLine(x));
            System.Random randomStoneState = WorldTools.GetRandomStoneState(world);
            
            helper.Log("Populating list");
            // Populate list of cells to become GoldDeposits
            int numDeposits = world.GridWidth;
            Cell[] cells = new Cell[numDeposits];
            
            // for (int cell = 0; cell < cells.Length - 1; cell++)
            // {
            //     int x = SRand.Range(0, world.GridWidth, randomStoneState);
            //     int z = SRand.Range(0, world.GridHeight, randomStoneState);
            //     cells[cell] = world.GetCellData(x, z);
            // }

            for (int x = 0; x < world.GridWidth - 1; x++)
            {
                cells[x] = world.GetCellData(x, 0);
            }

            helper.Log("Applying to cells");
            for (int cell = 0; cell < cells.Length - 1; cell++)
            {
                helper.Log($"Doing cell {cell} / {cells.Length - 1}");
                Cell currentCell = cells[cell];

                helper.Log($"Clearing cell {currentCell.x}, {currentCell.z}");
                ClearCell(currentCell, clearCave:true);
                helper.Log($"Placing {Resources[0]} at {currentCell.x}, {currentCell.z}");
                try
                {
                    TryPlaceResource(currentCell, Resources[0], deleteTrees: true, storePostGenerationType: true);
                    helper.Log($"Placed");
                }
                catch (Exception e)
                {
                    helper.Log($"Not placed");
                    helper.Log(e.ToString());
                }
            }
            
            helper.Log("Finished generating GoldDeposit"); 
            Console.WriteLine("Finished generating GoldDeposit");
            return true;
        }
    }
    
    public class ResourceTypeBase
    {
        public string Name;
        public GameObject Model;
        public ResourceType ResourceType; // Main.RegisterResource() assigns this
        
        /// <summary>
        /// The ResourceTypeBase to appear on the map.
        /// Generated using a Generator which is part of a ModConfig
        /// </summary>
        /// <param name="name">The name of the ResourceTypeBase</param>
        /// <param name="model">The model to be used on the map</param>
        public ResourceTypeBase(string name, GameObject model)
        {
            Name = name;
            Model = model;
            Main.Inst.Helper.Log($"Model for {Name} loaded: {Model != null}");
        }

        public override string ToString()
        {
            return $"{Name}; Model loaded: {Model != null}; ResourceType = {ResourceType}";
        }
    }
    
    public class GeneratorBase
    {
        public readonly string Name; // The name of this generator
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

            Name = GetType().ToString(); // The name of the class derived from this
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
            return $"{Name}";
        }

        public string AllInfo()
        {
            StringBuilder str = new StringBuilder($"Generator: {Name}");
            str.Append("Resource Types:");
            foreach (ResourceTypeBase x in Resources)
            {
                str.Append($"{x}");
            }
            
            return str.ToString();
        }

        public static void TryPlaceResource(Cell cell, ResourceTypeBase resourceTypeBase,
            bool storePostGenerationType = false,
            bool deleteTrees = false,
            Vector3 localScale = new Vector3(),
            Vector3 rotate = new Vector3())
        {
            try
            {
                cell.Type = resourceTypeBase.ResourceType;
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(resourceTypeBase.Model);

                if (storePostGenerationType) cell.StorePostGenerationType();
                if (deleteTrees) TreeSystem.inst.DeleteTreesAt(cell);

                gameObject.transform.position = cell.Position;
                if (localScale != new Vector3()) gameObject.transform.localScale = localScale;
                if (rotate != new Vector3()) gameObject.transform.Rotate(rotate); // 0 or 180 for Z axis

                if (cell.Models == null)
                {
                    cell.Models = new List<GameObject>();
                }

                cell.Models.Add(gameObject);
            }
            catch (Exception e)
            {
                throw new PlacementFailedException(e.ToString());
            }
        }

        protected static bool ClearCell(Cell cell, bool clearCave = true)
        {
            if (cell.Models != null)
            {
                foreach (GameObject model in cell.Models)
                {
                    UnityEngine.Object.Destroy(model.gameObject);
                }

                cell.Models.Clear();
            }
            
            if (clearCave) ClearCaveAtCell(cell);
            
            return true;
        }

        protected static bool ClearCaveAtCell(Cell cell)
        {
            GameObject caveAt = World.inst.GetCaveAt(cell);
            if (caveAt != null)
            {
                UnityEngine.Object.Destroy(caveAt);
            }

            return true;
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
    
    #region Default generators (NOT IMPLEMENTED)
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
    
    #endregion
    
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
            if (helper == null) return;
            
            helper.Log($"POSTFIXING \"GenLand\" with seed: {__instance.seed.ToString()}");

            if (Main.Inst.ModConfigs == null) return;
            foreach (ModConfig modConfig in Main.Inst.ModConfigs)
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
        static void Postfix(ref World __result)
        {
            try
            {
                //TODO Determine if this is needed
                KCModHelper helper = Main.Inst.Helper;
                World world = __result;

                helper.Log($"Patching \"Unpack\" with seed {__result.seed}");

                // Create generator to be able to use it's methods
                ResourceTypeBase[]
                    resourceTypeBases = new ResourceTypeBase[256]; // 256 is max KaC modding engine installer does
                foreach (var generatorBase in Main.Inst.ModConfigs.SelectMany(modConfig => modConfig.Generators))
                {
                    resourceTypeBases.AddRangeToArray(generatorBase.Resources);
                }

                GeneratorBase generator = new GeneratorBase(resourceTypeBases);


                Cell[] cellData = WorldTools.GetCellData(world);
                Cell.CellSaveData[] cellSaveData =
                    (Cell.CellSaveData[]) WorldTools.GetPrivateWorldField(world, "cellSaveData");
                bool hasDeepWater = false;

                for (int i = 0; i < cellData.Length - 1; i++)
                {
                    Cell cell = cellData[i];
                    int x = i % world.GridWidth;
                    int z = i / world.GridWidth;

                    // Setting the cell.type was done in the method before it this Postfix
                    // It was set if needed for default resources in 
                    if (Main.Inst.listOfDefaultResources.Contains(cell.Type)) continue;

                    cell.Type = cellSaveData[i].type;

                    helper.Log($"Cell {cell.x}, {cell.z} is: {cell.Type}");

                    // This is the equivalent of World.SetupStoneForCell
                    ResourceTypeBase currentResourceTypeBase = Main.Inst.GetResourceTypeBase(cell.Type);
                    GeneratorBase.TryPlaceResource(cell, currentResourceTypeBase, deleteTrees: false,
                        storePostGenerationType: true);

                    helper.Log($"Set Cell to {currentResourceTypeBase}");
                }

                WorldTools.SetCellData(world, cellData);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    public static class WorldTools
    {
        public static Cell[] GetCellData(World world)
        {
            /*
             * Emulates this from `World.Unpack(World w)`
             * for (int i = 0; i < w.cellData.Length; i++)
			 * {
			 *     Cell cell = w.cellData[i];
             * }
             */
            
            Type type = typeof(World);
            //object lateBound = Activator.CreateInstance(type); // late-binding??
 
            PropertyInfo cellDataProperty = type.GetProperty("cellData");
            return (Cell[]) cellDataProperty.GetValue(world, null);
        }

        public static void SetCellData(World world, Cell cell, int i)
        {
            Type type = typeof(World);
            PropertyInfo cellDataProperty = type.GetProperty("cellData");

            // Get a copy of cellData and edit it
            Cell[] newCellData = GetCellData(world);
            newCellData[i] = cell;
            
            // Set cellData to be the edited version
            cellDataProperty.SetValue(world, cell, null);
        }

        public static void SetCellData(World world, Cell[] newCellData)
        {
            Type type = typeof(World);
            PropertyInfo cellDataProperty = type.GetProperty("cellData");
            
            // Set cellData to be the edited version
            cellDataProperty.SetValue(world, newCellData, null);
        }

        /// <summary>
        /// Uses GetPrivateWorldField to get randomStoneState from world
        /// </summary>
        /// <param name="world"></param>
        /// <returns>A copy of randomStoneState from the given world</returns>
        public static System.Random GetRandomStoneState(World world)
        {
            Console.WriteLine("Starting GetRandomStoneState");
            // Pretty sure this is always `new System.Random(1234567);`
            System.Random result = (System.Random) GetPrivateWorldField(world, "randomStoneState");
            Console.WriteLine("Got randomStoneState");
            return result;
            }

        /// <summary>
        /// Gets a private field from a given World instance with the given FieldName
        /// </summary>
        /// <param name="world"></param>
        /// <param name="fieldName"></param>
        /// <param name="fieldIsStatic"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">Thrown if fieldName does not exist in the given context (Static/Instance)</exception>
        public static object GetPrivateWorldField(World world, string fieldName, bool fieldIsStatic = false)
        {
            return GetPrivateField(world, fieldName, fieldIsStatic);
        }

        public static object GetPrivateField(object world, string fieldName, bool fieldIsStatic = false)
        {
            string exceptionString =
                $"{fieldName} does not correspond to a private {(fieldIsStatic ? "static" : "instance")} field in {world}";
            object result;
            try
            {
                Type type = world.GetType();

                FieldInfo fieldInfo = fieldIsStatic ? type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic) : type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                
                if (fieldInfo == null) throw new ArgumentException(exceptionString);
                
                result = fieldInfo.GetValue(world);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new ArgumentException(exceptionString);
            }

            return result;
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

    public class PlacementFailedException : Exception
    {
        public PlacementFailedException()
        {
        }

        public PlacementFailedException(string message) : base(message)
        {
        }

        public PlacementFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PlacementFailedException([NotNull] SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}