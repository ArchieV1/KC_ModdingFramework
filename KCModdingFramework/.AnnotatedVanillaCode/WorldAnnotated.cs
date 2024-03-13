using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace KaC_Modding_Engine_API
{
    /// <summary>
    /// Select methods from World that have been annotated
    /// </summary>
    internal class WorldAnnotated : World
    {
        #region Variables
        public delegate void wolfDenClear(int landmassIdx, int teamID);

        public delegate List<Cell> PathingFunction(Cell start, Cell end);

        [Serializable]
        public class Livery {
            public Texture banners;

            public Material bannerMaterial;

            public Color bannerColor;

            public Color mapColor;

            public Material uniMaterial;

            public Material buildingMaterial;

            public Material armyMaterial;

            public Material armyMaterialUnlit;

            public Material buildUIMaterial;

            public Material flagMaterial;

            public Material uniMaterialCracked;

            public Material uniMaterialFogClip;

            public Material[] headMaterials;

            public Sprite knightSprite;

            public Sprite archerSprite;

            public Sprite catapultSprite;

            public Sprite settlerSprite;

            public Sprite transportSprite;

            public Sprite seedSprite;

            public Sprite envoySprite;
        }

        public enum MapSize
        {
            Small,
            Medium,
            Large,
            Random
        }

        [Serializable]
        public struct MapSizeDef
        {
            public int GridMin;

            public int GridMax;

            public int DesiredTiles;
        }

        [Serializable]
        public class WaterGenSettings
        {
            public float waterFreqSmall = 5f;

            public float waterFreqMedium = 5f;

            public float waterFreqLarge = 5f;

            public float waterThresholdSmall = 0.3f;

            public float waterThresholdMedium = 0.3f;

            public float waterThresholdLarge = 0.3f;

            public int borderGrowth = 3;
        }

        public enum MapBias
        {
            Land,
            Island,
            Random
        }

        public enum MapRiverLakes
        {
            None,
            Some,
            Random
        }

        private enum MapFeature
        {
            HighFertility,
            NormalFertility,
            Barren,
            LargeStone,
            SmallStone,
            SmallIron,
            LargeIron,
            Nothing,
            NumFeatures
        }

        [Serializable]
        public class IslandSignature
        {
            public int stone;

            public int iron;

            public int largeStone;

            public int largeIron;

            public List<TerrainGen.FertilitySplotData> fertileSplots;

            public List<TreeGrowth.TreeSplotData> treeSplots;

            public List<FishSystem.FishSplotData> fishSplots;
        }

        private struct MapFeatureDef
        {
            public MapFeature feature;

            public int x;

            public int z;
        }

        [Serializable]
        public struct SizeToDepostsInfo
        {
            public int numIron;

            public int numSmallStone;

            public int numLargeStone;
        }

        [Serializable]
        public class KingdomHeadInfo
        {
            public GameObject headPrefabs;

            public bool maleHonorific;
        }

        private class LandmassStartInfo
        {
            public int numTreeTiles;

            public int numFertileTiles;

            public int numStones;

            public int numIrons;

            public int size;

            public int landmass;
        }

        private class ScreenshotData
        {
            public string path;

            public Func<int, int, Texture2D> screenshotFunction;

            public UnityAction onComplete;

            public UnityAction<Texture2D> onCompleteWithTexture;
        }

        public delegate void EvaluateCell(int x, int z, Cell cell);

        public delegate bool EvaluateIsValidCell(int x, int z, Cell cell);

        public enum Relations
        {
            Neutral,
            Allies,
            Enemy
        }

        [Serializable]
        public class WorldSaveData : SaveClass<World, WorldSaveData>
        {
            [OptionalField(VersionAdded = 2)]
            public int seed;

            public int gridWidth;

            public int gridHeight;

            public Cell.CellSaveData[] cellSaveData;

            [OptionalField(VersionAdded = 3)]
            public List<WolfDen.WolfDenSaveData> wolfDens = new List<WolfDen.WolfDenSaveData>();

            [OptionalField(VersionAdded = 3)]
            public List<WitchHut.WitchHutSaveData> witchHuts = new List<WitchHut.WitchHutSaveData>();

            [OptionalField(VersionAdded = 4)]
            public bool placedCavesWitches;

            [OptionalField(VersionAdded = 5)]
            public bool placedFish;

            [OptionalField(VersionAdded = 5)]
            public bool hasStoneUI;

            public List<ResourceTracker.ResourceTrackerSaveData> resourceTrackers = new List<ResourceTracker.ResourceTrackerSaveData>();

            [OptionalField(VersionAdded = 6)]
            private Relations[,] hostility;

            [OptionalField(VersionAdded = 7)]
            private List<TerrainGen.PlacedSplots>[] placedFertilitySplots;

            [OnDeserializing]
            private void SetDefaults(StreamingContext sc)
            {
                wolfDens = new List<WolfDen.WolfDenSaveData>();
                witchHuts = new List<WitchHut.WitchHutSaveData>();
                placedCavesWitches = false;
                placedFish = false;
                hasStoneUI = false;
                resourceTrackers = new List<ResourceTracker.ResourceTrackerSaveData>();
            }

            public override WorldSaveData Pack(World w)
            {
                gridWidth = w.GridWidth;
                gridHeight = w.GridHeight;
                seed = w.seed;
                cellSaveData = new Cell.CellSaveData[gridWidth * gridHeight];
                for (int i = 0; i < cellSaveData.Length; i++)
                {
                    cellSaveData[i] = new Cell.CellSaveData();
                    cellSaveData[i].CopyFrom(w.cellData[i]);
                }
                for (int j = 0; j < w.caveContainer.transform.childCount; j++)
                {
                    WolfDen component = w.caveContainer.transform.GetChild(j).GetComponent<WolfDen>();
                    if (component != null)
                    {
                        WolfDen.WolfDenSaveData item = new WolfDen.WolfDenSaveData().Pack(component);
                        wolfDens.Add(item);
                    }
                    WitchHut component2 = w.caveContainer.transform.GetChild(j).GetComponent<WitchHut>();
                    if (component2 != null)
                    {
                        WitchHut.WitchHutSaveData item2 = new WitchHut.WitchHutSaveData().Pack(component2);
                        witchHuts.Add(item2);
                    }
                }
                for (int k = 0; k < w.resourceTrackers.Count; k++)
                {
                    ResourceTracker resourceTracker = w.resourceTrackers[k];
                    if (resourceTracker != null)
                    {
                        ResourceTracker.ResourceTrackerSaveData item3 = new ResourceTracker.ResourceTrackerSaveData().Pack(resourceTracker);
                        resourceTrackers.Add(item3);
                    }
                }
                placedCavesWitches = w.placedCavesWitches;
                placedFish = w.placedFish;
                hasStoneUI = w.hasStoneUI;
                hostility = w.hostility;
                placedFertilitySplots = new List<TerrainGen.PlacedSplots>[TerrainGen.inst.fertilitySplots.Count];
                for (int l = 0; l < TerrainGen.inst.fertilitySplots.Count; l++)
                {
                    placedFertilitySplots[l] = TerrainGen.inst.fertilitySplots.data[l];
                }
                return this;
            }

            public override World Unpack(World w)
            {
                w.Setup(gridWidth, gridHeight);
                w.seed = seed;
                bool hasDeepWater = false;
                for (int i = 0; i < w.cellData.Length; i++)
                {
                    Cell cell = w.cellData[i];
                    int x = i % w.GridWidth;
                    int z = i / w.GridWidth;
                    if (cellSaveData[i].type == ResourceType.Water)
                    {
                        TerrainGen.inst.SetWaterTile(x, z);
                    }
                    cell.saltWater = cellSaveData[i].saltWater;
                    cell.deepWater = cellSaveData[i].deepWater;
                    if (cell.deepWater)
                    {
                        hasDeepWater = true;
                    }
                    if (!cell.deepWater && cellSaveData[i].type == ResourceType.Water)
                    {
                        TerrainGen.inst.SetTileHeight(cell, TerrainGen.waterHeightShallow - SRand.value * 0.1f);
                    }
                    if (cellSaveData[i].type == ResourceType.Stone || cellSaveData[i].type == ResourceType.UnusableStone || cellSaveData[i].type == ResourceType.IronDeposit)
                    {
                        cell.Type = cellSaveData[i].type;
                        w.SetupStoneForCell(cell);
                    }
                    else if (cellSaveData[i].type == ResourceType.EmptyCave)
                    {
                        w.AddEmptyCave(x, z);
                    }
                    else if (cellSaveData[i].type != ResourceType.WolfDen)
                    {
                        _ = cellSaveData[i].type;
                        _ = 8;
                    }
                    for (int j = 0; j < cellSaveData[i].amount; j++)
                    {
                        TreeSystem.inst.PlaceTree(cell);
                    }
                    TerrainGen.inst.SetFertileTile(x, z, cellSaveData[i].fertile);
                    if (cell.Type == ResourceType.Water)
                    {
                        cell.fertile = 0;
                    }
                    cell.StorePostGenerationType();
                }
                for (int k = 0; k < wolfDens.Count; k++)
                {
                    WolfDen obj = w.AddWolfDen(wolfDens[k].x, wolfDens[k].z);
                    wolfDens[k].Unpack(obj);
                }
                for (int l = 0; l < witchHuts.Count; l++)
                {
                    WitchHut obj2 = w.AddWitchHut(witchHuts[l].x, witchHuts[l].z);
                    witchHuts[l].Unpack(obj2);
                }
                if (w.resourceTrackers == null)
                {
                    w.resourceTrackers = new List<ResourceTracker>();
                }
                else
                {
                    w.resourceTrackers.Clear();
                }
                for (int m = 0; m < resourceTrackers.Count; m++)
                {
                    ResourceTracker obj3 = new ResourceTracker();
                    obj3 = resourceTrackers[m].Unpack(obj3);
                    w.resourceTrackers.Add(obj3);
                }
                w.placedCavesWitches = placedCavesWitches;
                w.placedFish = placedFish;
                w.DestroyStoneUIs();
                if (hasStoneUI)
                {
                    w.GenerateStoneUIs();
                }
                w.UpdateWaterTable(hasDeepWater);
                TerrainGen.inst.AddNoise();
                TerrainGen.inst.FinalizeChanges();
                Water.inst.UpdateWaterTexture();
                w.CombineStone();
                w.regenRimCells = true;
                w.hostility = hostility;
                if (w.hostility == null)
                {
                    w.ClearHostility();
                }
                w.GenerateLandMassIds(prune1TileMasses: false);
                JobSystem.inst.InitJobList();
                w.FindValidStartLandmasses();
                if (placedFertilitySplots != null)
                {
                    TerrainGen.inst.ClearSplots();
                    for (int n = 0; n < placedFertilitySplots.Length; n++)
                    {
                        TerrainGen.inst.fertilitySplots.Add(placedFertilitySplots[n]);
                    }
                }
                return w;
            }
        }

        private const int MAX_DROP_AMOUNT = 30;

        public Villager personModel;

        public GameObject lightStoneModel;

        public GameObject darkStoneModel;

        public GameObject ironStoneModel;

        public Material uniMaterialFog;

        public Material uniMaterialNoFog;

        public Material uniMaterialNoFogNoSnow;

        public List<ResourceTracker> resourceTrackers;

        private CellInfluence[] cellInfluenceData;

        private Cell[] cellData;

        private PathCell[] pathCellData;

        [NonSerialized]
        public VillagerGrid[] villagerGrid;

        public List<Texture2D> screenshotTextures;

        private int gridWidth;

        private int gridHeight;

        [HideInInspector]
        public GameObject resourceContainer;

        [HideInInspector]
        public GameObject caveContainer;

        public GameObject Table;

        public int seed;

        public bool Inited;

        public static World inst;

        public static string aqueductName = "aqueduct";

        public static string archerSchoolName = "archerschool";

        public static string archerTowerName = "archer";

        public static string greekFireName = "greekfire";

        public static string bakerName = "baker";

        public static string ballistaTowerName = "ballista";

        public static string barracksName = "barracks";

        public static string bathhouseName = "bathhouse";

        public static string blacksmithName = "blacksmith";

        public static string bridgeName = "bridge";

        public static string butcherName = "butcher";

        public static string castleblockName = "castleblock";

        public static string castlestairsName = "castlestairs";

        public static string cathedralName = "cathedral";

        public static string cemetery3x3Name = "cemetery";

        public static string cemeteryDummyName = "cemeterydummy";

        public static string parkDummyName = "parkdummy";

        public static string cemetery4x4Name = "cemetery44";

        public static string cemeteryCircleName = "cemeteryCircle";

        public static string cemeteryDiamondName = "cemeteryDiamond";

        public static string cemeteryKeeperName = "cemeterykeeper";

        public static string chamberOfWarName = "chamberofwar";

        public static string charcoalMakerName = "charcoalmaker";

        public static string largeCharcoalMakerName = "largecharcoalmaker";

        public static string chapelName = "chapel";

        public static string churchName = "church";

        public static string clinicName = "clinic";

        public static string statueDummyName = "statuedummy";

        public static string stoneRemovalName = "destructioncrew";

        public static string dockName = "dock";

        public static string drawBridgeName = "drawbridge";

        public static string farmName = "farm";

        public static string fireHouseName = "firehouse";

        public static string fishinghutName = "fishinghut";

        public static string fishMongerName = "fishmonger";

        public static string foresterName = "forester";

        public static string fountainName = "fountain";

        public static string gardenName = "garden";

        public static string gateName = "gate";

        public static string granaryName = "largegranary";

        public static string greatHallName = "greathall";

        public static string greatLibraryName = "greatlibrary";

        public static string hospitalName = "hospital";

        public static string smallHouseName = "smallhouse";

        public static string ironMineName = "ironmine";

        public static string largeIronMine = "largeironmine";

        public static string joustingArenaName = "joustingarena";

        public static string keepName = "keep";

        public static string largefountainName = "largefountain";

        public static string largeHouseName = "largehouse";

        public static string libraryName = "library";

        public static string manorHouseName = "manorhouse";

        public static string marketName = "market";

        public static string masonName = "Mason";

        public static string moatName = "moat";

        public static string noriaName = "noria";

        public static string orchardName = "orchard";

        public static string outpostName = "outpost";

        public static string pathName = "path";

        public static string pierName = "pier";

        public static string produceStandName = "producestand";

        public static string largeProduceStandName = "largeproducestand";

        public static string projectileTopper = "projectiletopper";

        public static string quarryName = "quarry";

        public static string largeQuarryName = "largequarry";

        public static string reservoirName = "reservoir";

        public static string largeReservoirName = "largereservoir";

        public static string roadName = "road";

        public static string rubbleName = "rubble";

        public static string smallGranaryName = "smallgranary";

        public static string smallMarketName = "smallmarket";

        public static string smallstockpileName = "smallstockpile";

        public static string statueBarbaraName = "statue_barbara";

        public static string statueLeviName = "statue_levi";

        public static string statueSamName = "statue_sam";

        public static string statueName = "statue";

        public static string largeStockpileName = "largestockpile";

        public static string seaGate = "seagate";

        public static string seedShipName = "seedship";

        public static string siegeWorkshopName = "siegeworkshop";

        public static string stoneBridgeName = "stonebridge";

        public static string stoneRoadName = "stoneroad";

        public static string swineherdName = "swineherd";

        public static string tavernName = "tavern";

        public static string largeTavernName = "largetavern";

        public static string throneRoomName = "throneroom";

        public static string largeThroneRoomName = "largethroneroom";

        public static string hallOfDiplomacyName = "hallofdiplomacy";

        public static string theaterName = "theater";

        public static string transportCartName = "transportcart";

        public static string townsquareName = "townsquare";

        public static string transportShipName = "transportship";

        public static string troopTransportShipName = "trooptransportship";

        public static string playerMerchantShipName = "playermerchant";

        public static string wellName = "well";

        public static string windmillName = "windmill";

        public static string woodcastleblockName = "woodcastleblock";

        public static string woodengateName = "woodengate";

        public static string foreignMinistryName = "hallofdiplomacy";

        public static string houseCategoryName = "house";

        public static string uHouseName = "uhouse";

        public static string statueSlabName = "statue_slab";

        public static int charcoalmakerHash = charcoalMakerName.GetHashCode();

        public static int largeCharcoalmakerHash = largeCharcoalMakerName.GetHashCode();

        public static int manorhouseHash = manorHouseName.GetHashCode();

        public static int smallHouseHash = smallHouseName.GetHashCode();

        public static int largehouseHash = largeHouseName.GetHashCode();

        public static int keepHash = keepName.GetHashCode();

        public static int farmHash = farmName.GetHashCode();

        public static int orchardHash = orchardName.GetHashCode();

        public static int castleblockHash = castleblockName.GetHashCode();

        public static int woodcastleblockHash = woodcastleblockName.GetHashCode();

        public static int castlestairsHash = castlestairsName.GetHashCode();

        public static int pathHash = pathName.GetHashCode();

        public static int projectileTopperHash = projectileTopper.GetHashCode();

        public static int townsquareHash = townsquareName.GetHashCode();

        public static int pierHash = pierName.GetHashCode();

        public static int stoneBridgeHash = stoneBridgeName.GetHashCode();

        public static int moatHash = moatName.GetHashCode();

        public static int gateHash = gateName.GetHashCode();

        public static int woodengateHash = woodengateName.GetHashCode();

        public static int rubbleHash = rubbleName.GetHashCode();

        public static int statueLeviHash = statueLeviName.GetHashCode();

        public static int noriaHash = noriaName.GetHashCode();

        public static int aqueductHash = aqueductName.GetHashCode();

        public static int roadHash = roadName.GetHashCode();

        public static int stoneRoadHash = stoneRoadName.GetHashCode();

        public static int gardenHash = gardenName.GetHashCode();

        public static int reservoirHash = reservoirName.GetHashCode();

        public static int largeReservoirHash = largeReservoirName.GetHashCode();

        public static int throneRoomHash = throneRoomName.GetHashCode();

        public static int largeThroneRoomHash = largeThroneRoomName.GetHashCode();

        public static int statueHash = statueName.GetHashCode();

        public static int wellHash = wellName.GetHashCode();

        public static int dockHash = dockName.GetHashCode();

        public static int drawBridgeHash = drawBridgeName.GetHashCode();

        public static int fishinghutHash = fishinghutName.GetHashCode();

        public static int outpostHash = outpostName.GetHashCode();

        public static int cemeteryHash = cemetery3x3Name.GetHashCode();

        public static int cemeteryKeeperHash = cemeteryKeeperName.GetHashCode();

        public static int fountainHash = fountainName.GetHashCode();

        public static int largefountainHash = largefountainName.GetHashCode();

        public static int foresterHash = foresterName.GetHashCode();

        public static int quarryHash = quarryName.GetHashCode();

        public static int largeQuarryHash = largeQuarryName.GetHashCode();

        public static int clinicHash = clinicName.GetHashCode();

        public static int hospitalHash = hospitalName.GetHashCode();

        public static int tavernHash = tavernName.GetHashCode();

        public static int largeTavernHash = largeTavernName.GetHashCode();

        public static int chapelHash = chapelName.GetHashCode();

        public static int churchHash = churchName.GetHashCode();

        public static int smallstockpileHash = smallstockpileName.GetHashCode();

        public static int largeStockpileHash = largeStockpileName.GetHashCode();

        public static int swineherdHash = swineherdName.GetHashCode();

        public static int archerTowerHash = archerTowerName.GetHashCode();

        public static int greekFireHash = greekFireName.GetHashCode();

        public static int ballistaTowerHash = ballistaTowerName.GetHashCode();

        public static int ironMineHash = ironMineName.GetHashCode();

        public static int largeIronMineHash = largeIronMine.GetHashCode();

        public static int joustingArenaHash = joustingArenaName.GetHashCode();

        public static int hallofdiplomacyHash = hallOfDiplomacyName.GetHashCode();

        public static int theaterHash = theaterName.GetHashCode();

        public static int bathhouseHash = bathhouseName.GetHashCode();

        public static int transportCartHash = transportCartName.GetHashCode();

        public static int blacksmithHash = blacksmithName.GetHashCode();

        public static int marketHash = marketName.GetHashCode();

        public static int produceStandHash = produceStandName.GetHashCode();

        public static int largeProduceStandHash = produceStandName.GetHashCode();

        public static int libraryHash = libraryName.GetHashCode();

        public static int fishMongerHash = fishMongerName.GetHashCode();

        public static int barracksHash = barracksName.GetHashCode();

        public static int archerSchoolHash = archerSchoolName.GetHashCode();

        public static int chamberOfWarHash = chamberOfWarName.GetHashCode();

        public static int foreignMinistryHash = foreignMinistryName.GetHashCode();

        public static int smallGranaryHash = smallGranaryName.GetHashCode();

        public static int granaryHash = granaryName.GetHashCode();

        public static int granaryCategoryHash = "granary".GetHashCode();

        public static int masonHash = masonName.GetHashCode();

        public static int butcherHash = butcherName.GetHashCode();

        public static int cathedralHash = cathedralName.GetHashCode();

        public static int greatLibraryHash = greatLibraryName.GetHashCode();

        public static int houseCategoryHash = houseCategoryName.GetHashCode();

        public static int smallMarketHash = smallMarketName.GetHashCode();

        public static int windmillHash = windmillName.GetHashCode();

        public static int transportShipHash = transportShipName.GetHashCode();

        public static int playerMerchantShipHash = playerMerchantShipName.GetHashCode();

        public static int troopTransportShipHash = troopTransportShipName.GetHashCode();

        public static int uHouseHash = uHouseName.GetHashCode();

        public static int bakerHash = bakerName.GetHashCode();

        public static int statueSlabHash = statueSlabName.GetHashCode();

        public static int greatHallHash = greatHallName.GetHashCode();

        public static int stoneRemovalHash = stoneRemovalName.GetHashCode();

        public static int siegeWorkshopHash = siegeWorkshopName.GetHashCode();

        public static int bridgeHash = "bridge".GetHashCode();

        public static int seagateHash = "seagate".GetHashCode();

        public static int shipHash = "ship".GetHashCode();

        public List<Livery> liverySets;

        public Sprite icon_viking;

        public Sprite icon_ogre;

        public Sprite icon_troopTransportShip_vikings;

        public Sprite icon_troopTransportShip_ogre;

        private System.Random randomStoneState;

        public Whale whale;

        [NonSerialized]
        public Pathfinder pather;

        [NonSerialized]
        public ThreadedPathing threadedPather = new ThreadedPathing();

        private int frameToWaitBeforeSeen = 5;

        private int framesAfterGenerate;

        public bool hasStoneUI;

        public GameObject stoneUIPrefab;

        public GameObject stoneUIContainer;

        public GameObject stoneUITip;

        private List<GameObject> stoneList = new List<GameObject>();

        private List<CombineInstance> combineList = new List<CombineInstance>();

        [NonSerialized]
        public MapSize generatedMapSize = MapSize.Random;

        [NonSerialized]
        public MapSize mapSize = MapSize.Medium;

        public List<MapSizeDef> mapSizeDefs = new List<MapSizeDef>();

        public WaterGenSettings[] settings = new WaterGenSettings[2];

        public MapBias generatedMapsBias = MapBias.Random;

        public MapBias mapBias = MapBias.Random;

        public MapRiverLakes generatedRiverLakes = MapRiverLakes.Random;

        public MapRiverLakes mapRiverLakes = MapRiverLakes.Random;

        public List<IslandSignature> smallIslandSignatures = new List<IslandSignature>();

        public List<IslandSignature> islandSignatures = new List<IslandSignature>();

        public List<IslandSignature> largeIslandSignatures = new List<IslandSignature>();

        public List<IslandSignature> baseDensities = new List<IslandSignature>();

        private List<Cell> stoneGrowList = new List<Cell>();

        public SizeToDepostsInfo smallMapDepositInfo;

        public SizeToDepostsInfo mediumMapDepositInfo;

        public SizeToDepostsInfo largeMapDepositInfo;

        [NonSerialized]
        public ArrayExt<Cell>[] cellsToLandmass;

        public int minDistForRandomFeatures = 7;

        public KingdomHeadInfo[] kingdomHeadInfo;

        public UnityEvent worldGenerationComplete;

        [NonSerialized]
        public bool placedFish;

        private bool placedCavesWitches;

        public GameObject emptyCavePrefab;

        public GameObject wolfDenPrefab;

        public GameObject witchHutPrefab;

        public int caveSpawnTreeNeighborThreshold = 4;

        private List<LandmassStartInfo> landmassStartInfo = new List<LandmassStartInfo>();

        [NonSerialized]
        public int validStartLandmassCount;

        [NonSerialized]
        public int validStartLandmassForAICount;

        public Vector3 bigRockSizeMin = new Vector3(0.35f, 0.35f, 0.35f);

        public Vector3 bigRockSizeMax = new Vector3(1.35f, 1.35f, 1.35f);

        public static ArrayExt<LandmassOwner> LandMassOwners = new ArrayExt<LandmassOwner>(5);

        public static LandmassOwner[] LandMassOwnerLookup;

        public Cell[] rimCells;

        private ArrayExt<Villager> emptyVillagerList = new ArrayExt<Villager>(0);

        private ArrayExt<ArrayExt<Villager>> villagersPerLandMass = new ArrayExt<ArrayExt<Villager>>(10);

        private bool regenRimCells;

        public List<Cell> rebakeCells = new List<Cell>();

        [NonSerialized]
        public int pathGridId;

        private List<ScreenshotData> pendingScreenshots = new List<ScreenshotData>();

        private Cell[] scratch4 = new Cell[4];

        public int RoadBuildRadius = 2;

        public int PierBuildRadius = 1;

        public int BuildingCount;

        private Vector3[] cardinalOffsets = new Vector3[4]
        {
        new Vector3(1f, 0f, 0f),
        new Vector3(-1f, 0f, 0f),
        new Vector3(0f, 0f, 1f),
        new Vector3(0f, 0f, -1f)
        };

        private List<Cell> cellCollection = new List<Cell>();

        private Cell[] scratchNeighbors = new Cell[8];

        public float timeLastPlaced = -999f;

        public static int stonebridgeHash = "stonebridge".GetHashCode();

        public static int stockpileHash = "stockpile".GetHashCode();

        private Vector3[] cellBoundOffsets = new Vector3[4]
        {
        Vector3.zero,
        Vector3.forward,
        new Vector3(1f, 0f, 1f),
        Vector3.right
        };

        private TreeGrowth treeGrowth;

        private int numLandMasses;

        public Transform table;

        private Relations[,] hostility = new Relations[5, 5];

        public int ScreenshotWidth => 800;

        public int ScreenshotHeight => ScreenshotWidth / 4 * 3;

        public int GridWidth => gridWidth;

        public int GridHeight => gridHeight;

        public int MaxTrees => treeGrowth.MaxTreesOnMap;

        public int NumLandMasses => numLandMasses;

        public event wolfDenClear OnWolfDenCleared;

        // Map sizes vary between MapSizeDef.GridMin and MapSizeDef.GridMax
        // Two maps of the same size selected upon creation will have different sizes
        // Cannot find out what this is set to upon World.#ctor() though for some reason
        // public List<MapSizeDef> mapSizeDefs = new List<MapSizeDef>();
        #endregion

        private int GetDefaultWolfCaves()
        {
            return gridWidth * gridHeight / 300;
        }

        private void PlaceCavesWitches()
        {
            placedCavesWitches = true;
            bool placedWitchHut = false;
            int numWolfCaves = this.GetDefaultWolfCaves();
            for (int i = 2; i < gridWidth; i++)
            {
                for (int j = 2; j < gridHeight; j++)
                {
                    Cell cellDataUnsafe = GetCellDataUnsafe(i, j);
                    DoPlaceCave(cellDataUnsafe, ref numWolfCaves, ref placedWitchHut);
                }
            }
        }

        /// <summary>
        /// Places either a Witch Hut or Cave given:
        /// 
        /// Places a WitchHut if: (50% chance)
        /// IF <paramref name="placedWitchHut"/> IS FALSE
        /// No EmptyCave within 5
        /// No Structure within 5
        /// More than 4 trees within 1
        /// No Iron within 2
        /// No Stone within 2
        /// 
        /// Places Cave: (Less than number of Caves placed [Bit random, Determned by number of Map Cells)
        /// IF DID NOT PLACE WITCH HUT THIS TIME
        /// No EmptyCave within 5
        /// No Structure within 5
        /// More than 4 trees within 1
        /// No Iron within 2
        /// No Stone within 2
        /// </summary>
        /// <param name="cell">The cell to place the Cave/Witch hut in</param>
        /// <param name="numWolfCaves">maxNumber * k of WolfCaves to place (k = constant)</param>
        /// <param name="placedWitchHut">Does map currently have a Witch Hut?</param>
        private void DoPlaceCave(Cell cell, ref int numWolfCaves, ref bool placedWitchHut)
        {
            if (cell.Type != ResourceType.None || cell.TreeAmount > 0)
            {
                return;
            }
            int x = cell.x;
            int z = cell.z;

            #region Calculates the number of surrounding cells with trees
            int numSurroundingCellsWithTrees = 0;
            // Makes sure x and z (+-1) are between 0 and gridwidth/gridheight
            // Makes sure (+-) x, z are inside the map
            int xPosLower = Mathff.Clamp(x - 1, 0, gridWidth - 1);
            int zPosLower = Mathff.Clamp(z - 1, 0, gridHeight - 1);
            int xPostUpper = Mathff.Clamp(x + 1, 0, gridWidth - 1);
            int zPosUpper = Mathff.Clamp(z + 1, 0, gridHeight - 1);
            // The clamping means that loop will be any of:
            // [cellZ-1, cellZ, cellZ+1]
            // [cellZ, cellZ+1]
            // [cellZ-1, cellZ]
            // Theoretical [cellZ] but that would need a 1x1 size map
            for (int zPos = zPosLower; zPos <= zPosUpper; zPos++)
            {
                // For [cellZ-1, cellZ, cellZ+1]
                // Clamped though so cellZ-1 and cellZ+1 will be valid for sure
                for (int xPos = xPosLower; xPos <= xPostUpper; xPos++)
                {
                    // For [cellX-1, cellX, cellX+1]
                    // Clamped though so cellX-1 and cellX+1 will be valid for sure
                    if (cellData[zPos * gridWidth + xPos].TreeAmount > 0)
                    {
                        numSurroundingCellsWithTrees++;
                    }
                }
            }
            #endregion

            #region Is there a stone/iron cells within 2 tiles of cell?
            bool foundStoneIronCellWithin2 = false;
            int territory = WolfDen.territory; // 2
            int xPosLowerWolf = Mathff.Clamp(x - territory, 0, gridWidth - 1);
            int zPosLowerWolf = Mathff.Clamp(z - territory, 0, gridHeight - 1);
            int xPosUpperWolf = Mathff.Clamp(x + territory, 0, gridWidth - 1);
            int zPosUppeWolf = Mathff.Clamp(z + territory, 0, gridHeight - 1);
            // Will iterate through:
            // [zPos-2, zPos-1, zPos, zPos+1, zPos+2]
            // Clamping means it will be same as above where it could be less than 5 values if some values go off of the map edge
            for (int zPos = zPosLowerWolf; zPos <= zPosUppeWolf; zPos++)
            {
                int xPosLowerWolfCounter = xPosLowerWolf;
                while (xPosLowerWolfCounter <= xPosUpperWolf)
                {
                    // If a non-stone cell then increment xPosLowerWolfCounter
                    Cell cell2 = cellData[zPos * gridWidth + xPosLowerWolfCounter];
                    if (cell2.Type != ResourceType.IronDeposit && cell2.Type != ResourceType.Stone)
                    {
                        xPosLowerWolfCounter++;
                        continue;
                    }
                    goto IL_0137;
                }
                continue;
            IL_0137:
                foundStoneIronCellWithin2 = true;
                break;
            }
            #endregion

            #region Is there an EmptyCave/OccupyingStructure within 5 tiles of cell?
            int largerTerritory = WolfDen.territory * 2 + 1; // 5
            int xPosLowerWolfLarger = Mathff.Clamp(x - largerTerritory, 0, gridWidth - 1);
            int zPosLowerWolfLarger = Mathff.Clamp(z - largerTerritory, 0, gridHeight - 1);
            int xPosUpperWolfLarger = Mathff.Clamp(x + largerTerritory, 0, gridWidth - 1);
            int zPosUpperWolfLarger = Mathff.Clamp(z + largerTerritory, 0, gridHeight - 1);
            bool foundEmptyCaveOrStructureCellWithin5 = false;
            for (int zPos = zPosLowerWolfLarger; zPos <= zPosUpperWolfLarger; zPos++)
            {
                int num16 = xPosLowerWolfLarger;
                while (num16 <= xPosUpperWolfLarger)
                {
                    Cell cell3 = cellData[zPos * gridWidth + num16];
                    if (cell3.Type != ResourceType.EmptyCave && cell3.OccupyingStructure.Count <= 0)
                    {
                        num16++;
                        continue;
                    }
                    goto IL_01e7;
                }
                continue;
            IL_01e7:
                foundEmptyCaveOrStructureCellWithin5 = true;
                break;
            }
            #endregion

            // If:
            // No EmptyCave within 5
            // No Structure within 5
            // More than 4 trees within 1 (Out of possible 8)
            // No Iron within 2
            // No Stone within 2
            if (!foundEmptyCaveOrStructureCellWithin5 && numSurroundingCellsWithTrees >= caveSpawnTreeNeighborThreshold && !foundStoneIronCellWithin2)
            {
                bool placedWitchHutThisTime = false;
                // If:
                // Method called being told NO Witch Hut has been placed on the map
                // 50% chance
                if (!placedWitchHut && SRand.Range(0, 100) < 50)
                {
                    // Place a WitchHut
                    // Update calling var to say there is now a Witch Hut existing on the map
                    AddWitchHut(x, z);
                    placedWitchHut = true;
                    placedWitchHutThisTime = true;
                }

                // If:
                // Did not place a Witch Hut this time
                // Rand(0-100) < numberWolfCaves left to place [This number is determined by size of map. {X * Z /300}]
                if (!placedWitchHutThisTime && SRand.Range(0, 100) < numWolfCaves)
                {
                    // Place an EmptyCave
                    // (Empty cave can be replaced by the initial cave placement)
                    AddEmptyCave(x, z).replaceOnKeepPlacement = true;
                    numWolfCaves--;
                }
            }
        }

        private void GenLand()
        {
            bool hasLooped = false;
            while (true)
            {
                #region Set up world size
                int index = (int)generatedMapSize;
                int mapSizeIncreaser = 0;
                if (generatedMapsBias == MapBias.Island)
                {
                    mapSizeIncreaser = 7;
                }
                int width = SRand.Range(mapSizeDefs[index].GridMin + mapSizeIncreaser, mapSizeDefs[index].GridMax + mapSizeIncreaser);
                int height = SRand.Range(mapSizeDefs[index].GridMin + mapSizeIncreaser, mapSizeDefs[index].GridMax + mapSizeIncreaser);
                #endregion

                Setup(width, height);
                // If running inside of unity editor WHILE holding down space (Moved from line below UpdateWaterTable())
                bool devModeOverride = Application.isEditor && Input.GetKey(KeyCode.Space);

                #region Set up water
                TerrainGen.inst.GenerateWater(generatedMapSize);
                for (int i = 0; i < cellData.Length; i++)
                {
                    cellData[i].deepWater = cellData[i].Type == ResourceType.Water;
                }

                Water.inst.SetupSaltwater();
                Water.inst.SetupShallowWater();
                Water.inst.UpdateWaterTexture();
                UpdateWaterTable(hasDeepWater: true);
                #endregion

                for (int j = 0; j < gridWidth * gridHeight; j++)
                {
                    cellData[j].landMassIdx = -1;
                }
                // Original:
                //int j = 0;
                //for (int num2 = gridWidth * gridHeight; j < num2; j++)
                //{
                //    cellData[j].landMassIdx = -1;
                //}

                GenerateLandMassIds();
                List<MapFeatureDef> placedFeatures = new List<MapFeatureDef>();
                ArrayExt<Cell>[] landMassContainer = new ArrayExt<Cell>[NumLandMasses];
                ArrayExt<Cell>[] waterTiles = new ArrayExt<Cell>[NumLandMasses];
                for (int k = 0; k < NumLandMasses; k++)
                {
                    landMassContainer[k] = new ArrayExt<Cell>(cellData.Length);
                    waterTiles[k] = new ArrayExt<Cell>(cellData.Length);
                    for (int l = 0; l < cellsToLandmass[k].Count; l++)
                    {
                        if (cellsToLandmass[k].data[l].Type != ResourceType.Water)
                        {
                            landMassContainer[k].Add(cellsToLandmass[k].data[l]);
                        }
                        else
                        {
                            waterTiles[k].Add(cellsToLandmass[k].data[l]);
                        }
                    }
                }

                TerrainGen.inst.ClearSplots();
                if (devModeOverride)
                {
                    // If devmode fill whole map with Fertile Tiles
                    for (int m = 0; m < gridWidth; m++)
                    {
                        for (int n = 0; n < gridHeight; n++)
                        {
                            TerrainGen.inst.SetFertileTile(m, n, 2);
                        }
                    }
                }
                else
                {
                    GetComponent<TreeGrowth>().GenerateTrees();
                    TerrainGen.inst.GenerateFertileTiles();
                    int index2 = 0;

                    // For each land mass
                    for (int landMassInd = 0; landMassInd < NumLandMasses; landMassInd++)
                    {
                        // Unsure how it saves the splot data to baseDensities
                        GetComponent<TreeGrowth>().PlaceSplots(landMassInd, baseDensities[index2].treeSplots); // Place tree areas
                        TerrainGen.inst.PlaceSplots(landMassInd, baseDensities[index2].fertileSplots); // Place fertile areas
                        FishSystem.inst.PlaceSplots(landMassInd, baseDensities[index2].fishSplots, waterTiles[landMassInd]); // Place fish areas
                        int smallStoneWeighting = 0;
                        int smallIronWeighting = 0;
                        if (generatedMapsBias == MapBias.Land)
                        {
                            if (generatedMapSize == MapSize.Small)
                            {
                                smallStoneWeighting = 1;
                                smallIronWeighting = 1;
                            }
                            else if (generatedMapSize == MapSize.Medium)
                            {
                                smallStoneWeighting = 2;
                                smallIronWeighting = 1;
                            }
                            else if (generatedMapSize == MapSize.Large)
                            {
                                smallStoneWeighting = 4;
                                smallIronWeighting = 2;
                            }
                        }

                        // Place small stone/iron depending on densities (And map size)
                        for (int smallStoneInd = 0; smallStoneInd < baseDensities[index2].stone + smallStoneWeighting; smallStoneInd++)
                        {
                            TryPlaceResource(MapFeature.SmallStone, landMassContainer[landMassInd], placedFeatures);
                        }
                        for (int smallIronInd = 0; smallIronInd < baseDensities[index2].iron + smallIronWeighting; smallIronInd++)
                        {
                            TryPlaceResource(MapFeature.SmallIron, landMassContainer[landMassInd], placedFeatures);
                        }
                    }

                    // Sets islandSignatures depending on map size (Neither Small/Large is default)
                    List<IslandSignature> listIslandSignatures = islandSignatures;
                    if (generatedMapSize == MapSize.Small)
                    {
                        listIslandSignatures = smallIslandSignatures;
                    }
                    else if (generatedMapSize == MapSize.Large)
                    {
                        listIslandSignatures = largeIslandSignatures;
                    }

                    // For each island places features based on where the splots end up

                    // These two vars used to make sure no numbers go negative or anything
                    int landMassInd = 0; // Used to keep in range (0, NumLandMasses)
                    int islandSignatureInd = 0; // Used to keep in range (0, listIslandSignatures.Count)
                    for (int loopNumber = 0; loopNumber < listIslandSignatures.Count; loopNumber++)
                    {
                        IslandSignature islandSignature = listIslandSignatures[islandSignatureInd % listIslandSignatures.Count];
                        islandSignatureInd++;

                        GetComponent<TreeGrowth>().PlaceSplots(landMassInd, islandSignature.treeSplots);
                        TerrainGen.inst.PlaceSplots(landMassInd, islandSignature.fertileSplots);
                        FishSystem.inst.PlaceSplots(landMassInd, islandSignature.fishSplots, waterTiles[landMassInd]);

                        // Place resource areas
                        for (int smallStoneInd = 0; smallStoneInd < islandSignature.stone; smallStoneInd++)
                        {
                            TryPlaceResource(MapFeature.SmallStone, landMassContainer[landMassInd], placedFeatures);
                        }
                        for (int smallIronInd = 0; smallIronInd < islandSignature.iron; smallIronInd++)
                        {
                            TryPlaceResource(MapFeature.SmallIron, landMassContainer[landMassInd], placedFeatures);
                        }
                        for (int largeStoneInd = 0; largeStoneInd < islandSignature.largeStone; largeStoneInd++)
                        {
                            TryPlaceResource(MapFeature.LargeStone, landMassContainer[landMassInd], placedFeatures);
                        }
                        for (int largeIronInd = 0; largeIronInd < islandSignature.largeIron; largeIronInd++)
                        {
                            TryPlaceResource(MapFeature.LargeIron, landMassContainer[landMassInd], placedFeatures);
                        }

                        landMassInd++;
                        landMassInd %= NumLandMasses;
                    }

                    bool placedWitchHut = false;
                    int numWolfCaves = GetDefaultWolfCaves();
                    for (int landMassIndex = 0; landMassIndex < landMassContainer.Length; landMassIndex++)
                    {
                        // Only place Wolves/Witches if more than 200 land mass tiles on this island 
                        // (though only 1 witch hut PER MAP)

                        // TODO note somewhere: ArrayExt .Length = number of arrays, .Count = Length of one of the arrays
                        // ArrayExt seems to act as an array of arrays
                        if (landMassContainer[landMassIndex].Count >= 200)
                        {
                            // For each LandMassTile, For each LandMassTile
                            for (int landMassTile = 0; landMassTile < landMassContainer[landMassIndex].Count; landMassTile++)
                            {
                                Cell cell = landMassContainer[landMassIndex].data[landMassTile];
                                // Has a change to place a wolf cave (Depending on numWolfWaves)
                                // Its not a hard counter though there is chance involved
                                // CONCLUSION: It will run DoPlaceCave for every landMassTile on every island
                                DoPlaceCave(cell, ref numWolfCaves, ref placedWitchHut);
                            }
                        }
                    }
                    _ = new bool[gridWidth * gridHeight];
                    for (int num17 = 0; num17 < cellData.Length; num17++)
                    {
                        if (cellData[num17].Type == ResourceType.IronDeposit || cellData[num17].Type == ResourceType.Stone)
                        {
                            int x = cellData[num17].x;
                            int z = cellData[num17].z;
                            _ = cellData[num17];
                            int num18 = x - 2;
                            int num19 = z - 2;
                            for (int num20 = num18; num20 < x; num20++)
                            {
                                RemoveStone(GetCellData(num20, z), doRecombine: false);
                            }
                            for (int num21 = num19; num21 < z; num21++)
                            {
                                RemoveStone(GetCellData(x, num21), doRecombine: false);
                            }
                        }
                    }
                }

                if (FindValidStartLandmasses() || hasLooped)
                {
                    break;
                }
                hasLooped = true;
            }
            FishSystem.inst.AddInitialFish();
            TerrainGen.inst.AddShape();
            TerrainGen.inst.AddNoise();
            TerrainGen.inst.FinalizeChanges();
            TerrainGen.inst.SetSnowFade(0f);
            TerrainGen.inst.snowOn = false;
            TerrainGen.inst.ClearOverlay(updateToo: false);
            for (int num22 = 0; num22 < gridWidth * gridHeight; num22++)
            {
                AIKingdom kingdomByLandmass = AIKingdom.GetKingdomByLandmass(GetCellDataUnsafe(num22).landMassIdx);
                if (kingdomByLandmass != null)
                {
                    TerrainGen.inst.SetOverlayPixelColor(num22 % gridWidth, num22 / gridWidth, kingdomByLandmass.GetColor());
                }
            }
            TerrainGen.inst.UpdateOverlayTextures();
            TerrainGen.inst.FadeOverlay(1f);
            Player.inst.ResetPerLandMassData();
            worldGenerationComplete.Invoke();
        }

    }
}
