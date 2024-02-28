using System;
using System.Collections.Generic;
using UnityEngine;
using static KaC_Modding_Engine_API.Tools.PrivateFieldTools;

namespace KaC_Modding_Engine_API.Tools
{
    internal static class WorldTools
    {

        public static Cell[] GetCellData(World world)
        {
            return (Cell[])GetPrivateField(world, "cellData");
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
        /// Uses GetPrivateWorldField to get randomStoneState
        /// </summary>
        /// <param name="world">The instance of World randomStoneState will be read from</param>
        /// <returns>A copy of randomStoneState from the given world</returns>
        public static System.Random GetRandomStoneState(World world)
        {
            return (System.Random)GetPrivateWorldField(world, "randomStoneState");
        }

        /// <summary>
        /// Places a large stone feature on the given world.
        /// </summary>
        /// <param name="world">The world to place the large Stone Feature in.</param>
        /// <param name="cell">The central cell of the Stone Feature</param>
        /// <param name="primaryResourceType">The main ResourceType.</param>
        /// <param name="secondaryResourceType">The second ResourceType to surround the primary</param>
        /// <param name="chance1">Chance of growing the stoneGrowList</param>
        /// <param name="chance2">Chance of placing secondary resource after <see cref="chance1"/>. Chance of placing secondary resource is chance1 * chance2 per adjacent tile (4)y</param>

        /// <returns>TODO find out</returns>
        public static bool PlaceLargeStoneFeature(World world, Cell cell, ResourceType primaryResourceType, ResourceType secondaryResourceType = ResourceType.UnusableStone, int chance1 = 60, int chance2 = 35)
        {
            bool flag = false;
            flag |= PlaceSmallStoneFeature(world, cell, primaryResourceType);
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
                PlaceSmallStoneFeature(world, cell2, primaryResourceType, secondaryResourceType, chance1, chance2);
            }
            return flag;
        }

        /// <summary>
        /// Places a small stone featurein the given world.
        /// </summary>
        /// <param name="world">The world to place the large Stone Feature in.</param>
        /// <param name="cell">The central cell of the Stone Feature</param>
        /// <param name="primaryResourceType">The main ResourceType.</param>
        /// <param name="secondaryResourceType">The second ResourceType to surround the primary</param>
        /// <param name="chance1">Chance of growing the stoneGrowList</param>
        /// <param name="chance2">Chance of placing secondary resource after <see cref="chance1"/>. Chance of placing secondary resource is chance1 * chance2 per adjacent tile (4)y</param>
        /// <returns></returns>
        public static bool PlaceSmallStoneFeature(World world, Cell cell, ResourceType primaryResourceType, ResourceType secondaryResourceType = ResourceType.UnusableStone, int chance1 = 60, int chance2 = 35)
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

            bool result = world.PlaceStone(cell, primaryResourceType);
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
    }
}