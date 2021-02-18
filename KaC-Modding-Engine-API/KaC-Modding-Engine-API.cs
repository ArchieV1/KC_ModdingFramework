using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Code;
using UnityEngine;
using VRTK.Examples;

namespace KaC_Modding_Engine_API
{
    public class Main : MonoBehaviour
    {
        public KCModHelper Helper { get; private set; }
        public static Main Inst;
        
        public const int numberDefaultResources = 9;
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
        private List<ResourceType> unassignedResourceTypes;
        private Dictionary<ResourceTypeData, ResourceType> assignedResourceTypes;

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
            Helper.Log("Starting KaC-Modding-Engine-API");
            Inst = this;

            unassignedResourceTypes = Enum.GetValues(typeof(ResourceType)).Cast<ResourceType>().ToList();

            // Mark the default values as assigned (ironDeposit, stoneDeposit etc) 
            // Ignore intelliSense it CANNOT be a foreach loop as it will edit the list while it goes
            for (int x = 0; x < unassignedResourceTypes.Count; x++)
            {
                ResourceType resourceType = unassignedResourceTypes[x];
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
            Helper.Log("Finished removing default ResourceTypes from unassignedResourceTypes");

            Helper.Log("Registering test mod:");
            //AssetBundle testModAssetBundle = KCModHelper.LoadAssetBundle(Helper.modPath, "TestModAssetBundle");
            //"C:\\Users\\green\\RiderProjects\\Kingdoms-and-Castles-Toolkit-master\\Assets\\CAssets\\Models\\GameModels (and some prefabs)"
            string wolfDenURI =
                "C:\\Users\\green\\RiderProjects\\Kingdoms-and-Castles-Toolkit-master\\Assets\\KCAssets\\Models\\GameModels (and some prefabs)\\wolfden.fbx";
            ModConfig testMod = new ModConfig
            {
                Author = "ArchieV1",
                ModName = "TestMod_Name",
                ResourceTypeDatas = new ResourceTypeData[]
                {
                    //new ResourceTypeData("Resource1", "/GoldDeposit/", testModAssetBundle),
                    new ResourceTypeData("Resource2", wolfDenURI),
                }
            };

            Helper.Log($"Test mod is registered: {testMod.Registered.ToString()}");
            RegisterMod(testMod);
            Helper.Log($"Test mod is registered: {testMod.Registered.ToString()}");
        }
        
        /// <summary>
        /// Registers the given mod. Changes "Registered" to true (Even if it failed to register the mod in its entirety
        /// </summary>
        /// <param name="modConfig"></param>
        /// <returns>Returns false if failed to register mod in its entirety</returns>
        public bool RegisterMod(ModConfig modConfig)
        {
            Main.Inst.Helper.Log($"Registering {modConfig.ModName} by {modConfig.Author}...");
            int numberResources = 0;

            // Assigns each ResourceTypeData an unassignedResourceType
            foreach (ResourceTypeData data in modConfig.ResourceTypeDatas)
            {
                bool success = AssignResourceTypeData(data, unassignedResourceTypes[0]);
                if (success)
                {
                    numberResources += 1;
                }
                else
                {
                    Main.Inst.Helper.Log($"WARNING. FAILED TO REGISTER {data.ResourceName}");
                }
            }
            
            
            
            modConfig.Registered = true;
            if (modConfig.ResourceTypeDatas.Length != numberResources)
            {
                Main.Inst.Helper.Log($"Registered {modConfig.ModName} by {modConfig.Author} successfully!\n" +
                                     $"Registered {numberResources} resources.\n" +
                                     $"Failed to register {modConfig.ResourceTypeDatas.Length - numberResources} resources.");
                return false;
            }
            
            Main.Inst.Helper.Log($"Failed to register {modConfig.ModName} by {modConfig.Author} successfully\n" +
                                 $"Registered {numberResources} resources.\n" +
                                 $"Failed to register {modConfig.ResourceTypeDatas.Length - numberResources} resources.");
            return true;
        }

        /// <summary>
        /// Adds data/resourceType to assignedResourceTypes and removes resourceType from unassignedResourceTypes
        /// </summary>
        /// <param name="resourceType"></param>
        /// <param name="data"></param>
        /// <returns>True if success</returns>
        private bool AssignResourceTypeData(ResourceTypeData data, ResourceType resourceType)
        {
            if (unassignedResourceTypes.Contains(resourceType)
                && !assignedResourceTypes.ContainsValue(resourceType))
            {
                unassignedResourceTypes.Remove(resourceType);
                assignedResourceTypes.Add(data, resourceType);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes resourceType from assignedResourceTypes and Adds resourceType to unassignedResourceTypes
        /// </summary>
        /// <param name="resourceType"></param>
        /// <param name="data"></param>
        /// <returns>True if success</returns>
        private bool UnassignResourceTypeData(ResourceTypeData data, ResourceType resourceType)
        {
            if (!unassignedResourceTypes.Contains(resourceType)
                && assignedResourceTypes.ContainsValue(resourceType))
            {
                unassignedResourceTypes.Add(resourceType);
                assignedResourceTypes.Remove(data);
                return true;
            }

            return false;
        }

        /// <summary>
        /// For removing default ResourceTypes from unassignedResourceTypes (But not adding it to assignedResourceTypes)
        /// </summary>
        /// <param name="resourceType"></param>
        /// <returns></returns>
        private bool MarkResourceTypeAssigned(ResourceType resourceType)
        {
            if (unassignedResourceTypes.Contains(resourceType))
            {
                unassignedResourceTypes.Remove(resourceType);
                return true;
            }

            return false;
        }
    }
    
    public class ModContext
    {
        public ModConfig Config { get; private set; }
        public string GameObject { get; private set; }

        
        public ModContext(ModConfig config, string gameObject)
        {
            Config = config;
            GameObject = gameObject;
        }
    }

    public class ModConfig
    {
        public string ModName;
        public string Author;
        public ResourceTypeData[] ResourceTypeDatas;
        /// <summary>
        /// Get this using:
        /// assetBundle = KCModHelper.LoadAssetBundle(Helper.modPath, "BUNDLE_NAME");
        /// </summary>
        public AssetBundle AssetBundle; // The asset bundle from Unity
        
        public bool Registered = false;
        
        public ModConfig(){}
    }

    public class ResourceTypeData
    {
        public string ResourceName; // For ease of logging
        public string ModelURI; // To get the mode
        public GameObject Model; // To appear on the map

        public ResourceTypeData(string resourceName,string modelUri, AssetBundle assetBundle)
        {
            this.ResourceName = resourceName;
            this.ModelURI = modelUri;
            
            Model = assetBundle.LoadAsset(ModelURI) as GameObject;
        }

        public ResourceTypeData(string resourceName, string modelUri)
        {
            this.ResourceName = resourceName;
            this.ModelURI = modelUri;
            
            Model = GameObject.Instantiate(Resources.Load(
                modelUri)) as GameObject;
        }
    }
}