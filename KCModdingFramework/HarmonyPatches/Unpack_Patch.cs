using HarmonyLib;
using KaC_Modding_Engine_API.Objects.Generators;
using KaC_Modding_Engine_API.Objects.Resources;
using KaC_Modding_Engine_API.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using static KaC_Modding_Engine_API.Tools.Tools;

namespace KaC_Modding_Engine_API.HarmonyPatches
{
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
                List<ModdedResourceType> moddedResourceTypes = new List<ModdedResourceType>();
                foreach (GeneratorBase generatorBase in ModdingFramework.Inst.Generators)
                {
                    moddedResourceTypes.AddRange(generatorBase.Resources);
                }
                if (moddedResourceTypes.Count() == 0) return;

                Cell[] cellData = WorldExtension.GetCellData(world);
                Cell.CellSaveData[] cellSaveData =
                    (Cell.CellSaveData[])WorldExtension.GetPrivateField(world, "cellSaveData");
                bool hasDeepWater = false;

                for (int i = 0; i < cellData.Length - 1; i++)
                {
                    Cell cell = cellData[i];
                    int x = i % world.GridWidth;
                    int z = i / world.GridWidth;

                    // Setting the cell.type was done in the method before it this Postfix
                    // It was set if needed for default resources in 
                    if (VanillaResourceType.Contains(cell.Type)) continue;

                    cell.Type = cellSaveData[i].type;

                    helper.Log($"Cell {cell.x}, {cell.z} is: {cell.Type}");

                    // This is the equivalent of World.SetupStoneForCell
                    ModdedResourceType currentModdedResourceType = ModdingFramework.Inst.GetModdedResourceType(cell.Type);
                    GeneratorBase.TryPlaceResource(cell, currentModdedResourceType, deleteTrees: false,
                        storePostGenerationType: true);

                    helper.Log($"Set Cell to {currentModdedResourceType}");
                }

                WorldExtension.
                                SetCellData(world, cellData);
                helper.Log("Finished patching Unpack");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
