using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using UnityEngine;

namespace KaC_Modding_Engine_API
{
    public static class API
    {
        public static string PortObjectName { get; } = "ModdingEngineAPI";
        private static IMCPort _port;

        /*
         * Data Structures:
         * CellType (struct):
            * 
            * 
            * 
            * 
         * 
         *  
         * Commands:
         * 
         * int Register(CellType type)
         * 
         */

        static API()
        {
            Main.Init += Init;
        }

        private static void Init()
        {
            _port = new GameObject(PortObjectName).AddComponent<IMCPort>();

            _port.RegisterReceiveListener<ModConfigME>("Register", r_Register);
        }

        #region Callbacks

        private static void r_Register(IRequestHandler handler, string source, ModConfigME type)
        {
            ModConfigME result = Main.Inst.RegisterMod(type);
            handler.SendResponse<ModConfigME>(PortObjectName, result);
        }

        #endregion

        public class ResourceTypeBase
    {
        public string Name;
        public GameObject Model;
        public ResourceType ResourceType; // Main.RegisterResource() assigns this
        // TODO: CanBeDestroyedBy (Eg Rock destroyer, axe tool)
        
        /// <summary>
        /// The ResourceTypeBase to appear on the map.
        /// Generated using a Generator which is part of a ModConfigME
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
            Main.Inst.Helper.Log($"Creating {GetType()}.\n" +
                                 $"It contains the resources:\n\t{resourceTypeBases.ToList().Join(null, "\n\t")}");

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
                Main.Inst.Helper.Log($"Placing {resourceTypeBase} at {cell.x}, {cell.z}");;
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
                Main.Inst.Helper.Log("Placed");
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
    }
}