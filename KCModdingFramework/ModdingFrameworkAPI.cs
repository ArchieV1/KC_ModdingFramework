using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Assets.Code;
using Harmony;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

public class ModdingFrameworkNames
{
    public static class Methods
    {
        /// <summary>
        /// Send by mods to register themselves, menu returns a ModConfig with all values assigned (The ResourceType they have been assigned):
        /// Parameter: ModConfigMF
        /// Return value: ModConfigMF
        /// </summary>
        public const string RegisterMod = "RegisterMod";

        /// <summary>
        /// Get list of all mods loaded by the modding framework
        /// </summary>
        public const string GetLoadedModList = "GetLoadedModList";

        /// <summary>
        /// Returns list of all assigned resource types
        /// </summary>
        public const string GetAssignedResourceTypes = "GetAssignedResourceTypes";
    }
    public static class Objects
    {
        /// <summary>
        /// The name of the the modding framework GameObjet
        /// </summary>
        public const string ModdingFrameworkName = "ModdingFramework";
    }
}

#region Useful classes needed for the methods

public class ModConfigMF
{
    public string ModName;
    public string Author;
    public GeneratorBase[] Generators;
    public ModObject[] ModObjects;
    public AssetBundle AssetBundle;
    public bool Registered = false;
    
    public override string ToString()
    {
        string generatorNames = Generators.Aggregate("", (current, generator) => current + generator);

        return $"{ModName} by {Author}. Generators included are {generatorNames}";
    }
}

/// A class that can be passed to a ModConfigMF but does not have anything implemented.
/// It will be stored but not used so you must add functionality manually    
public class ModObject
{
}

public class ResourceTypeBase
{
    public bool DefaultResource = false;
        
    public string Name;
    
    public GameObject Model;
    public string AssetBundlePath;
    public string PrefabName;
    public AssetBundle AssetBundle;
    public bool ModelLoaded => Model != null;
    
    public ResourceType ResourceType = ResourceType.None; // ModdingFramework.RegisterResource() assigns this
    // TODO: CanBeDestroyedBy (Eg Rock destroyer, axe tool)
    
    /// <summary>
    /// UNKNOWN IF THIS WORKS PROPERLY
    /// The ResourceTypeBase to appear on the map.
    /// Generated using a Generator which is part of a ModConfigMF
    /// </summary>
    /// <param name="name">The name of the ResourceTypeBase</param>
    /// <param name="model">The model to be used on the map</param>
    public ResourceTypeBase(string name, GameObject model)
    {
        Name = name;
        Model = model;
    }

    public ResourceTypeBase(string name, string assetBundlePath, string prefabName)
    {
        Name = name;
        AssetBundlePath = assetBundlePath;
        PrefabName = prefabName;
    }

    /// <summary>
    /// Only use this constructor with default resources
    /// </summary>
    /// <param name="defaultResource"></param>
    public ResourceTypeBase()
    {
    }

    public override string ToString()
    {
        return $"{Name}\n" +
               $"Model loaded: {ModelLoaded}\n" +
               $"AssetBundlePath: {AssetBundlePath}\n" +
               $"ResourceType = {ResourceType}";
    }

    public void LoadAssetBundle(KCModHelper helper)
    {
        AssetBundle = KCModHelper.LoadAssetBundle(helper.modPath, AssetBundlePath);
    }

    public void LoadModel()
    {
        Model = AssetBundle.LoadAsset(AssetBundlePath + PrefabName) as GameObject;
    }
}

public class GeneratorBase
{
    /// <summary>
    /// A generator contains a collection of ResourceTypeBases and a method to edit the World map using these new
    /// ResourceTypeBases but also using built in ResourceTypes 
    /// </summary>

    public readonly string Name; // The name of this generator
    public readonly ResourceTypeBase[] Resources; // The resources to be used in .Generate()

    /// <summary>
    /// Create a ResourceTypeBase with multiple resources contained within it.
    /// Use this if generation code required multiple 
    /// </summary>
    /// <param name="resourceTypeBases">The list of resources this generatorBase will use</param>
    public GeneratorBase(ResourceTypeBase[] resourceTypeBases)
    {
        Debugging.Log($"Generation",$"Creating {GetType()}.\n" +
                                    $"It contains the resources:\n\t{resourceTypeBases.ToList().Join(null, "\n\t")}");

        Name = GetType().ToString(); // The name of the class derived from this
        Resources = resourceTypeBases;

        Debugging.Log($"Generation",$"Created {Name}");
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
        throw new NotImplementedException("Generate needs to be implemented!");
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
            Debugging.Log($"Generation", $"Placing {resourceTypeBase} at {cell.x}, {cell.z}");;
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
            Debugging.Log($"Generation","Placed");
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

//TODO (NOT IMPLEMENTED)
#region Default generators 
/// <summary>
/// Use this Generator if you wish your modded resource to spawn like vanilla Stone does. Requires two resourceTypes
/// </summary>
public class StoneLikeGenerator : GeneratorBase
{
    private ResourceTypeBase[] _resourceTypeBases;
    private bool _largeFeature = false;
    private int[] _mapSizeBiases;
    
    private struct MapFeatureDef
    {
        // Taken from `World.MapFeatureDef` though feature doesn't seem needed
        // public World.MapFeature feature; 
        public int x;
        public int z;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="resourceTypeBases"></param>
    /// <param name="largeFeature"></param>
    /// <param name="ironMode">If true changes mapSizeBiases default to follow the numbers for iron instead of stone. No effect with custom mapSizeBiases</param>
    /// <param name="mapSizeBiases">How many extra resource deposits to place per map size. Small, Medium, Large. Default {1, 2, 4}</param>
    public StoneLikeGenerator(ResourceTypeBase[] resourceTypeBases, bool largeFeature=false, bool ironMode=false, int[] mapSizeBiases=null) : base(resourceTypeBases)
    {
        // Assumes each ResourceTypeBase will not be malformed
        if (resourceTypeBases.Length == 2)
        {
            _resourceTypeBases = resourceTypeBases;
        }
        else
        {
            _resourceTypeBases = new ResourceTypeBase[]
            {
                resourceTypeBases[0], new ResourceTypeBase
                {
                    ResourceType = ResourceType.UnusableStone
                }
            };
        }

        _largeFeature = largeFeature;
        
        if (mapSizeBiases != null && mapSizeBiases.Length == 3)
        {
            _mapSizeBiases = mapSizeBiases;
        }
        else
        {
            _mapSizeBiases = ironMode ? new[] {1, 1, 2} : new[] {1, 2, 4};
        }
    }

    /// <summary>
    /// Stone generation requires two resources.
    /// A good resource and a dud resource. Can use vanilla resources for either. Just use their data in your ResourceTypeBase implementation
    /// </summary>
    /// <param name="world"></param>
    /// <returns></returns>
    public override bool Generate(World world)
    {
        // Edited version of `TryPlaceResource`: 2022/09/14
        // Recreating values of params
        // private void TryPlaceResource(World.MapFeature feature, ArrayExt<Cell> cellsToLandmassFiltered, List<World.MapFeatureDef> placedFeatures)
        
        // feature
        // The "feature" (Iron deposit, Fertile ground etc) to place
        // Not needed
        
        // placedFeatures
        // A list of a currently placed features. Seems to be empty each time it is given to `TryPlaceResource` though.
        // So the game only makes sure resources of the same type arent near each other? Different types can spawn next to each other?
        List<MapFeatureDef> placedFeatures = new List<MapFeatureDef>();
        

        // cellsToLandmassFiltered
        // ?? A list of cells comprising each island?
        ArrayExt<Cell>[] array = new ArrayExt<Cell>[world.NumLandMasses];
        for (int k = 0; k < world.NumLandMasses; k++)
        {
            Cell[] cellData = (Cell[]) Tools.GetPrivateWorldField(world, "cellData", fieldIsStatic: false);
            array[k] = new ArrayExt<Cell>(cellData.Length);
            for (int l = 0; l < world.cellsToLandmass[k].Count; l++)
            {
                if (world.cellsToLandmass[k].data[l].Type != ResourceType.Water)
                {
                    array[k].Add(world.cellsToLandmass[k].data[l]);
                }
            }
        }
        
        // For each island
        for (int num3 = 0; num3 < world.NumLandMasses; num3++)
        {
            ArrayExt<Cell> cellsToLandmassFiltered = array[num3];
            
            // Make different amounts of resources generate based on map size
            int mapSizeBias = 0;
            if (world.generatedMapsBias == World.MapBias.Land)
            {
                if (world.generatedMapSize == World.MapSize.Small)
                {
                    mapSizeBias = _mapSizeBiases[0];
                }
                else if (world.generatedMapSize == World.MapSize.Medium)
                {
                    mapSizeBias = _mapSizeBiases[1];
                }
                else if (world.generatedMapSize == World.MapSize.Large)
                {
                    mapSizeBias = _mapSizeBiases[2];
                }
            }

            int index2 = 0;  // Seems to just always be 0
            for (int depositNumber = 0; depositNumber < world.baseDensities[index2].stone + mapSizeBias; depositNumber++)
            {
                // this.TryPlaceResource(World.MapFeature.SmallStone, array[num3], placedFeatures);
                
                // Edited implementation of the above method. Does not have `placedFeatures` as that is not stored anywhere
                bool flag = false;
                int num = 50;
                while (!flag && num > 0)
                {
                    Cell cell = cellsToLandmassFiltered.RandomElement();
                    bool flag2 = false;
                    for (int i = 0; i < placedFeatures.Count; i++)
                    {
                        float xCoordDelta = Mathff.Abs(placedFeatures[i].x - cell.x);
                        float zCoordDelta = Mathff.Abs(placedFeatures[i].z - cell.z);
                        if (xCoordDelta < 4f && zCoordDelta < 4f)
                        {
                            flag2 = true;
                            break;
                        }
                    }

                    if (!flag2)
                    {
                        switch (_largeFeature)
                        {
                            case true:
                                flag = Tools.PlaceLargeStoneFeature(world, cell, _resourceTypeBases[0].ResourceType, _resourceTypeBases[1].ResourceType, 60, 35);
                                break;
                            default:
                                flag = Tools.PlaceSmallStoneFeature(world, cell, _resourceTypeBases[0].ResourceType, _resourceTypeBases[0].ResourceType, 60, 35);
                                break;
                        }

                        if (flag)
                        {
                            placedFeatures.Add(new MapFeatureDef
                            {
                                //feature = feature,
                                x = cell.x,
                                z = cell.z
                            });
                        }
                    }

                    num--;
                }
            }
        }

        return true;  // It worked. TODO Make this actually reflect if it worked or not
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
#endregion

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

public static class Tools
{
    public static class WorldEditor
    {
        
    }

    public static class JSON
    {
        
    }

    public static class Private
    {
        
    }
    
    public static Cell[] GetCellData(World world)
    {
        return (Cell[]) GetPrivateField(world, "cellData");
    }

    public static void SetCellData(World world, Cell cell, int i)
    {
        // Get a copy of cellData and edit it
        Cell[] newCellData = GetCellData(world);
        newCellData[i] = cell;
        
        SetPrivateField(world, "cellData", newCellData);
    }

    public static void SetCellData(World world, Cell[] newCellData)
    {
        SetPrivateField(world, "cellData", newCellData);
    }

    /// <summary>
    /// Sets a private field using reflection
    /// </summary>
    /// <param name="instance">The object containing the private field</param>
    /// <param name="fieldName">The name of the private field</param>
    /// <param name="newValue">The new value the field will hold</param>
    public static void SetPrivateField(object instance, string fieldName, object newValue)
    {
        Type type = instance.GetType();
        PropertyInfo cellDataProperty = type.GetProperty(fieldName);
        
        // Set fieldName's value to the NewValue
        cellDataProperty?.SetValue(type, newValue, null);
    }

    /// <summary>
    /// Uses GetPrivateWorldField to get randomStoneState
    /// </summary>
    /// <param name="world">The instance of World randomStoneState will be read from</param>
    /// <returns>A copy of randomStoneState from the given world</returns>
    public static System.Random GetRandomStoneState(World world)
    {
        return (System.Random) GetPrivateWorldField(world, "randomStoneState");
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

    /// <summary>
    /// Get value of a private field from an Instance
    /// </summary>
    /// <param name="instance">The instance that contains the private field</param>
    /// <param name="fieldName">The private field name</param>
    /// <param name="fieldIsStatic">Is the field static</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Thrown when fieldName is not found in instance</exception>
    public static object GetPrivateField(object instance, string fieldName, bool fieldIsStatic = false)
    {
        string exceptionString =
            $"{fieldName} does not correspond to a private {(fieldIsStatic ? "static" : "instance")} field in {instance}";
        object result;
        try
        {
            Type type = instance.GetType();

            FieldInfo fieldInfo = fieldIsStatic ? type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic) : type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            
            if (fieldInfo == null) throw new ArgumentException(exceptionString);
            
            result = fieldInfo.GetValue(instance);
        }
        catch (Exception e)
        {
            ULogger.Log(e);
            throw new ArgumentException(exceptionString);
        }

        return result;
    }
    
    /// <summary>
    /// Get value of a private field from Static Class
    /// </summary>
    /// <param name="type">The Class that contains the private field</param>
    /// <param name="fieldName">The private field name</param>
    /// <param name="fieldIsStatic">Is the field static</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Thrown when fieldName is not found in type</exception>
    public static object GetPrivateField(Type type, string fieldName, bool fieldIsStatic = false)
    {
        string exceptionString =
            $"{fieldName} does not correspond to a private {(fieldIsStatic ? "static" : "instance")} field in {type}";
        object result;

        try
        {
            FieldInfo fieldInfo = fieldIsStatic ? type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic) : type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            
            if (fieldInfo == null) throw new ArgumentException(exceptionString);
            
            result = fieldInfo.GetValue(type);
        }
        catch (Exception e)
        {
            ULogger.Log(e);
            throw new ArgumentException(exceptionString);
        }

        return result;
    }
    
    /// <summary>
    /// Returns an array of Methods above the currently called method in the method calling stack
    /// </summary>
    /// <returns></returns>
    [STAThread]
    public static IEnumerable<string> GetCallingMethods()
    {
        StackTrace stackTrace = new StackTrace();
        StackFrame[] stackFrames = stackTrace.GetFrames();

        var list = new List<string>();
        if (stackFrames == null) return list;
        list.AddRange(stackFrames.Select(frame => frame.GetMethod().Name));
        list.Remove(stackFrames[0].GetMethod().Name);
        
        return list;
    }

    public static string GetCallingMethodsAsString()
    {
        return string.Join(", ", GetCallingMethods().Where(u => u != "GetCallingMethodsAsString"));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="world"></param>
    /// <param name="cell"></param>
    /// <param name="resourceType"></param>
    /// <param name="chance1">Chance of growing the stoneGrowList</param>
    /// <param name="chance2">Chance of placing secondary resource after chance1. Chance of placing secondary resource is chance1 * chance2 per adjacent tile (4)y</param>
    /// <param name="secondaryResourceType"></param>
    /// <returns></returns>
    public static bool PlaceLargeStoneFeature(World world, Cell cell, ResourceType resourceType, ResourceType secondaryResourceType=ResourceType.UnusableStone, int chance1=60, int chance2=35)
    {
        bool flag = false;
        flag |= PlaceSmallStoneFeature(world, cell, resourceType);
        if (!flag)
        {
            return false;
        }
        int num = SRand.Range(1, 2);
        for (int i = 0; i < num; i++)
        {
            int num2 = SRand.Range(2, 3);
            int num3 = (SRand.Range(0, 100) > 50) ? 1 : -1;
            int num4 = (SRand.Range(0, 100) > 50) ? 1 : -1;
            Cell cell2 = world.GetCellData(cell.x + num2 * num3, cell.z + num2 * num4);
            PlaceSmallStoneFeature(world, cell2, resourceType, secondaryResourceType, chance1, chance2);
        }
        return flag;
    }

    public static bool PlaceSmallStoneFeature(World world, Cell cell, ResourceType resourceType, ResourceType secondaryResourceType=ResourceType.UnusableStone, int chance1=60, int chance2=35)
    {
        Cell[] scratch4 = new Cell[4];
        world.GetNeighborCells(cell, ref scratch4);
        for (int i = 0; i < 4; i++)
        {
            if (scratch4[i] != null && scratch4[i].Type == ResourceType.Water)
            {
                return false;
            }
        }

        // If first thing it does it clear the list it cant be that important? It is only used in this method and when it is defined with World.World() as so
        List<Cell> stoneGrowList = new List<Cell>();  
        stoneGrowList.Clear();
        
        bool result = world.PlaceStone(cell, resourceType);
        Vector3[] array = new Vector3[]
        {
            new Vector3(-1f, 0f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(0f, 0f, -1f),
            new Vector3(0f, 0f, 1f)
        };
        stoneGrowList.Add(cell);
        while (stoneGrowList.Count > 0)
        {
            int x = stoneGrowList[0].x;
            int z = stoneGrowList[0].z;
            stoneGrowList.RemoveAt(0);
            
            // If the zeroth position of stoneGrowList is the same as the Cell being passed num3=2 else num3=3
            // Num3 (+1) is the number of UnusableStone to place
            // First run (When placing around Resource) this WILL be true (3 stones)
            // Subsequent runs (When placing around an UnusableStone (21% chance) or adjacent to previous placement) will place up to 4 stones
            
            // End results:
            // In each direction:
            // 21% chance of placing UnusableStone + Adding that UnusableStoneCell to stoneGrowList
            // 60% chance add adding that UnusableStoneCell to stoneGrowList
            
            // Runs max 20 times cus of the `num -= 3;` 
            
            // This leads to the possible behaviour of a resource being placed and then nothing else for 20 tiles then 4 rocks around a point
            // The chance of this happening is:
            // ( {[3*0.40]*[1*(0.60*0.65)]} * {[3*0.43]*[1*(0.57*0.65)]} ... {[3*0.97]*[1*(0.03*0.65)]} ) * [4*0.03]
            // ( PROD_SUM (x=0.03, lim 0.6): [3*(0.40+x)]*[(0.60-x)*0.65] ) * [4*0.03]
            // ( PROD_SUM (x=1, lim 20): 1.95(0.40+x*0.03)(0.60-x*0.03) ) * [4*0.03]
            // = 1.29914E-10 * [4*0.04]
            // = 1.55879E-11
            // 1 in 100 000 000 000 
            // 1 in 100 billion chance
            // ....................U..
            // R..................U.U.
            // ....................U..
            int num3 = (x == cell.x && z == cell.z) ? 2 : 3;
            for (int j = 0; j < array.Length; j++)  // array.Length == 4
            {
                if (SRand.Range(0, 100) < chance1)  // 60% chance
                {
                    if (SRand.Range(0, 100) < chance2)  // 35% chance  ==> 21% chance of placing a stone. FOR EACH DIRECTION
                    {
                        Cell cell2 = world.GetCellData(x + (int)array[j].x, z + (int)array[j].z);
                        world.PlaceStone(cell2, secondaryResourceType);
                        num3--;
                    }
                    // This stops the for loop if placed all (2 : 3) stone.
                    // With how this is written it is not 2 or 3 stone it is 3 or 4 stones
                    if (num3 <= 0)
                    {
                        break;
                    }
                    // Add the unusable stone coords to the stone grow list
                    stoneGrowList.Add(world.GetCellData(x + (int)array[j].x, z + (int)array[j].z));
                }
            }
            // Lowers the first % chance by 3%. Max 20 runs in the while loop
            chance1 -= 3;
        }
        return result;
    }
    
    public static bool IsEncodable(object obj)
    {
        try
        {
            JsonConvert.SerializeObject(obj, IMCPort.serializerSettings);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsDecodable(string str)
    {
        if (str == null) return false;
        
        try
        {
            DecodeObject(str);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsJSONable(object obj)
    {
        try
        {
            // If Encode/Decode/Encode == Encode then it can be sent and received without issue
            return EncodeObject( DecodeObject(EncodeObject(obj))) ==
                   EncodeObject(obj);
        }
        catch
        {
            return false;
        }
    }
    
    public static string EncodeObject(object obj, JsonSerializerSettings settings = null)
    {
        if (settings == null)
        {
            settings = IMCPort.serializerSettings;
        }

        return JsonConvert.SerializeObject(obj, settings);
    }

    public static object DecodeObject(string str, JsonSerializerSettings settings = null)
    {
        if (settings == null)
        {
            settings = IMCPort.serializerSettings;
        }

        return JsonConvert.DeserializeObject(str, settings);
    }
}

public static class ULogger
{
    public static void Log(string category, string message)
    {
        Console.WriteLine("[ULOGGER|{2}] {0, 25} | {1}", category, message, DateTime.Now);
    }
    
    public static void Log(string message)
    {
        Log("", message);
    }
    
    public static void Log(object category, object message)
    {
        Log(category.ToString(), message.ToString());
    }

    public static void Log(object message)
    {
        Log("", message);
    }
}