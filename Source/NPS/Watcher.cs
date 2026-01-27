using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace TKKN_NPS;

public class Watcher(Map map) : MapComponent(map)
{
    private const int HowManyRiverSteps = 5;
    private readonly int halfRiverSteps = (int)Math.Round((HowManyRiverSteps - 1M) / 2);
    private const int MaxRiverSteps = HowManyRiverSteps - 1;

    private const int HowManyTideSteps = 13;
    private readonly int halfTideSteps = (int)Math.Round((HowManyTideSteps - 1M) / 2);
    private const int MaxTideSteps = HowManyTideSteps - 1;

    //Every quarter hour
    private const int TideIntervalCheck = 625;

    private const int MaxPuddles = 1000;

    //used to save data about active springs.
    public Dictionary<int, springData> activeSprings = new();

    private BiomeSeasonalSettings biomeSettings;
    public Dictionary<IntVec3, cellData> cellWeatherAffects = new();
    private int cycleIndex;
    private bool doCoast = true; //false if no coast
    private List<List<IntVec3>> riverCellsList = [];

    private int floodLevel; // 0 - 3
    private int floodThreat;
    private int floodThreatIncrease;

    public FrostGrid frostGridComponent;
    private Vector2 location;

    private ModuleBase frostNoise;
    private float humidity;

    public float outdoorTemp;

    //used by weather
    private bool regenCellLists = true;

    private int ticks;

    //rebuild every save to keep file size down
    private List<List<IntVec3>> tideCellsList = [];
    private int tideLevel; // 0 - 13
    private int previousTideLevel;
    private int totalPuddles;

    private float wetPlantsValue;

    public bool dontRunAnything;
    private bool anyLavaTerrain;
    private bool doUnpacking;
    private bool noHurtPlants;
    private readonly Dictionary<Pawn, bool> validPawns = [];
    private float currentRainRate;
    private float currentSnowRate;
    private int mapArea; //Default area 62500
    private bool isRaining;
    private Rot4 coastRotation;
    private TerrainDef oceanTerrain;
    private TerrainDef beachTerrain;
    private TerrainDef shallowRiverTerrain;
    private bool doRiverFlooding;

    /* STANDARD STUFF */

    public override void FinalizeInit() {
        base.FinalizeInit();
        if (map.IsPocketMap) {
            //Log.Message("This is a pocket map. Nothing is running");
            dontRunAnything = true;
            return;
        }

        if (map.Biome.inVacuum) {
            //Log.Message("In the vacuum of space. Not running");
            dontRunAnything = true;
            return;
        }

        mapArea = map.Area;
        doCoast = map.TileInfo.IsCoastal;

        if (MapGenUtility.ShallowOceanWaterTerrainAt(new IntVec3(1, 0, 1), map) !=
            RimWorld.TerrainDefOf.WaterOceanShallow) {
            doCoast = false;
        }

        //oceanTerrain = MapGenUtility.ShallowOceanWaterTerrainAt(new IntVec3(1, 0, 1), map);
        oceanTerrain = TerrainDefOf.NPS_WaterOceanTide;
        beachTerrain = MapGenUtility.BeachTerrainAt(new IntVec3(1, 0, 1), map);
        if (doCoast) {
            coastRotation = Find.World.CoastDirectionAt(map.Tile);
            if (!coastRotation.IsValid) {
                Log.Error("[NPS] Tried to generate a coast but could not find coast rotation. This was on the biome " +
                          map.Biome + " From " + map.Biome.modContentPack?.Name);
                doCoast = false;
            }
        }

        if (beachTerrain == RimWorld.TerrainDefOf.Sand) {
            beachTerrain = TerrainDefOf.TKKN_SandBeachWetSalt;
        }

        shallowRiverTerrain = TerrainDefOf.NPS_WaterRiverFlood;

        Log.Message("Do coast? " + doCoast + " Direction? " + coastRotation);
        Log.Message("Beach terrain: " + beachTerrain + " Ocean terrain " + oceanTerrain);
        biomeSettings = map.Biome.GetModExtension<BiomeSeasonalSettings>();
        frostGridComponent = map.GetComponent<FrostGrid>();
        location = Find.WorldGrid.LongLatOf(map.Tile);
        UpdateBiomeSettings(true);

        frostNoise = new Perlin(0.039999999105930328, 2.0, 0.5, 5,
            Rand.Range(0, 651431), QualityMode.Medium);

        RebuildCellLists();
    }

    public override void MapComponentTick() {
        if (dontRunAnything) {
            return;
        }

        ticks++;
        base.MapComponentTick();

        //environmental changes
        if (Settings.doWeather) {
            //set up humidity
            outdoorTemp = map.mapTemperature.OutdoorTemp;
            currentRainRate = map.weatherManager.curWeather.rainRate;
            currentSnowRate = map.weatherManager.curWeather.snowRate;
            isRaining = currentRainRate > 0;
            var baseHumidity = (map.TileInfo.rainfall + 1) * (map.TileInfo.temperature + 1) *
                               (map.TileInfo.swampiness + 1);
            var currentHumidity =
                (1 + currentRainRate) * (1 + outdoorTemp);
            humidity = ((baseHumidity + currentHumidity) / 1000) + 18;
            wetPlantsValue = -1 * (outdoorTemp / humidity / 10);
            floodThreatIncrease = 1 + 2 * (int)Math.Round(currentRainRate);
            noHurtPlants = !Settings.allowPlantEffects || ticks % 150 != 0;
            doUnpacking = Settings.doDirtPath && !doUnpacking;
            DoTides();
            DoRiverModify();

            for (var i = 0; i < Settings.cellsPerTick; i++) {
                if (cycleIndex >= mapArea) {
                    cycleIndex = 0;
                }

                DoCellEnvironment(map.cellsInRandomOrder.Get(cycleIndex));
                cycleIndex++;
            }
        }


        IReadOnlyList<Pawn> allPawnsSpawned = map.mapPawns.AllPawnsSpawned;

        //Clear the list of valid pawns if it gets too large
        if (allPawnsSpawned.Count < (validPawns.Count - 100)) {
            validPawns.Clear();
        }

        for (int i = 0; i < allPawnsSpawned.Count; i++) {
            if (checkPawnHuman(allPawnsSpawned[i]))
                PawnChecks.checks(allPawnsSpawned[i], map, this, isRaining, ticks);
        }

        UpdateBiomeSettings();
    }

    private bool checkPawnHuman(Pawn pawn) {
        if (validPawns.TryGetValue(pawn, out var result)) {
            return result;
        }

        if (!pawn.RaceProps.Humanlike || pawn.IsShambler || pawn.IsMutant) {
            validPawns.Add(pawn, false);
            return false;
        }

        validPawns.Add(pawn, true);
        return true;
    }

    public override void ExposeData() {
        base.ExposeData();

        Scribe_Values.Look(ref regenCellLists, "regenCellLists", true, true);
        Scribe_Collections.Look(ref activeSprings, "TKKN_activeSprings", LookMode.Value, LookMode.Deep);
        Scribe_Collections.Look(ref cellWeatherAffects, "cellWeatherAffects", LookMode.Value, LookMode.Deep);
        Scribe_Values.Look(ref floodThreat, "floodThreat", 0, true);
        Scribe_Values.Look(ref tideLevel, "tideLevel", 0, true);
        Scribe_Values.Look(ref ticks, "ticks", 0, true);
        Scribe_Values.Look(ref totalPuddles, "totalPuddles", totalPuddles, true);
        Scribe_Values.Look(ref doRiverFlooding, "doRiverFlooding", doRiverFlooding, true);
        Scribe_Values.Look(ref anyLavaTerrain, "anyLavaTerrain", anyLavaTerrain, true);
    }


    private void RebuildCellLists() {
        if (Settings.regenCells) {
            regenCellLists = Settings.regenCells;
        }

        if (regenCellLists) {
            //random so we can spawn plants and stuff in this step.
            IEnumerable<IntVec3> tmpTerrain = map.AllCells.InRandomOrder();
            cellWeatherAffects = new Dictionary<IntVec3, cellData>();
            foreach (var focusCell in tmpTerrain) {
                var terrain = focusCell.GetTerrain(map);

                if (!focusCell.InBounds(map)) {
                    continue;
                }

                if (terrain == TerrainDefOf.TKKN_Lava || terrain == TerrainDefOf.TKKN_LavaRock_RoughHewn) {
                    anyLavaTerrain = true;
                }

                if (isRiverTerrain(terrain)) {
                    doRiverFlooding = true;
                }

                var cell = new cellData { location = focusCell, baseTerrain = terrain, howWetPlants = 70 };

                var frostVal = frostNoise.GetValue(focusCell) + 1;
                frostVal += 1f;
                frostVal *= 0.5f;
                if (frostVal < 0.5f) {
                    frostVal = 0.5f;
                }

                cell.frostNoise = frostVal;

                if (terrain == RimWorld.TerrainDefOf.Sand ||
                    terrain == TerrainDefOf.TKKN_SandBeachWetSalt ||
                    terrain == beachTerrain) {
                    //get all the sand pieces that are touching the beach.
                    for (var j = 0; j < HowManyTideSteps; j++) {
                        // Checks to see if water is in the direction of the cell
                        // checks every cell up to HowManyTideSteps and will change the cell to TKKN_SandBeachWetSalt if this is true
                        var waterCheck = AdjustForRotation(focusCell, j);
                        if (!waterCheck.InBounds(map) ||
                            !isOceanicTerrain(waterCheck.GetTerrain(map))) {
                            continue;
                        }

                        if (terrain == RimWorld.TerrainDefOf.Sand ||
                            terrain == TerrainDefOf.TKKN_SandBeachWetSalt) {
                            map.terrainGrid.SetTerrain(focusCell, TerrainDefOf.TKKN_SandBeachWetSalt);
                        }

                        cell.tideLevel = j;
                        break;
                    }
                }
                else if (isRiverTerrain(terrain)) {
                    cell.riverLevel = 0;
                    for (var j = 0; j < HowManyRiverSteps; j++) {
                        var num = GenRadial.NumCellsInRadius(j);
                        for (var i = 0; i < num; i++) {
                            IntVec3 bankCheck = focusCell + GenRadial.RadialPattern[i];
                            if (!bankCheck.InBounds(map)) {
                                continue;
                            }

                            TerrainDef bankCheckTerrain = bankCheck.GetTerrain(map);
                            if (terrain == TerrainDefOf.TKKN_SandBeachWetSalt ||
                                TerrainTagUtil.TKKN_Wet.Contains(bankCheckTerrain)) {
                                continue;
                            }

                            cellData bankCell;
                            if (cellWeatherAffects.TryGetValue(bankCheck, out var affect))
                                bankCell = affect;
                            else
                                bankCell = new cellData { location = bankCheck, baseTerrain = bankCheckTerrain };

                            if (j <= bankCell.riverLevel) {
                                bankCell.riverLevel = j;
                                // If bankCell has already had a riverFocus assigned, and the distance is further away, ignore it.
                                // Otherwise, assign it focusCell 
                                if (bankCell.riverFocus != IntVec3.Invalid &&
                                    bankCheck.DistanceToSquared(bankCell.riverFocus) <
                                    bankCheck.DistanceToSquared(focusCell)) {
                                    continue;
                                }

                                bankCell.riverFocus = focusCell;
                            }
                        }
                    }
                }

                //Spawn special elements:
                SpawnSpecialPlants(focusCell);

                cellWeatherAffects[focusCell] = cell;
            }
        }


        //rebuild lookup lists.
        tideCellsList = [];
        riverCellsList = [];

        for (var k = 0; k < HowManyTideSteps; k++) {
            tideCellsList.Add([]);
        }

        for (var k = 0; k < HowManyRiverSteps; k++) {
            riverCellsList.Add([]);
        }

        foreach (KeyValuePair<IntVec3, cellData> thisCell in cellWeatherAffects) {
            cellWeatherAffects[thisCell.Key].map = map;
            if (TerrainTagUtil.HoldsFrost.Contains(thisCell.Value.currentTerrain)) {
                frostGridComponent.SetDepth(thisCell.Value.location, thisCell.Value.frostLevel);
            }

            if (thisCell.Value.tideLevel > -1) {
                tideCellsList[thisCell.Value.tideLevel].Add(thisCell.Key);
            }

            if (thisCell.Value.riverLevel != 999 &&
                thisCell.Value.riverLevel != 0) {
                riverCellsList[thisCell.Value.riverLevel].Add(thisCell.Key);
            }
        }
        
        // After calculating the initial river levels for all tiles we want to recalculate them.
        // This time, we try to get each cell to point to the lowest nearby level
        //Ignore the first row because that is empty.
        for (int i = 1; i < riverCellsList.Count; i++) {
            foreach (var riverLevel in riverCellsList[i]) {
                if (!cellWeatherAffects.TryGetValue(riverLevel, out var levelCell)) {
                    Log.Error("A cell that should have a value doesn't have a value");
                    continue;
                }

                foreach (var cellAround in GenAdjFast.AdjacentCells8Way(riverLevel).InRandomOrder()) {
                    if (!cellAround.IsValid)
                        continue;
                    if (!cellWeatherAffects.TryGetValue(cellAround, out var possiblePotentialCell)) {
                        continue;
                    }

                    if (levelCell.riverLevel >= possiblePotentialCell.riverLevel) {
                        levelCell.riverFocus = cellAround;
                    }
                    else if (isRiverTerrain(possiblePotentialCell.currentTerrain)) {
                        levelCell.riverFocus = cellAround;
                        break;
                    }

                    if (levelCell.riverLevel > possiblePotentialCell.riverLevel)
                        break;
                }
            }
        }

        if (!regenCellLists) {
            return;
        }

        SetUpTidesBanks();
        SetUpRiverLevel();
        regenCellLists = false;
    }

    private void SpawnSpecialPlants(IntVec3 c) {
        //salt crystals:
        var terrain = c.GetTerrain(map);
        if (terrain == TerrainDefOf.TKKN_SaltField || terrain == TerrainDefOf.TKKN_SandBeachWetSalt) {
            if (c.GetEdifice(map) == null && c.GetCover(map) == null && Rand.Value < .003f) {
                var plant = (Plant)ThingMaker.MakeThing(ThingDefOf.TKKN_SaltCrystal);
                plant.Growth = Rand.Range(0.07f, 1f);
                if (plant.def.plant.LimitedLifespan) {
                    plant.Age = Rand.Range(0, Mathf.Max(plant.def.plant.LifespanTicks - 50, 0));
                }

                GenSpawn.Spawn(plant, c, map);
            }
        }

        //barnacles and other ocean stuff
        if (terrain != TerrainDefOf.TKKN_SandBeachWetSalt) {
            return;
        }

        if (c.GetEdifice(map) != null || c.GetCover(map) != null || !(Rand.Value < .003f)) {
            return;
        }

        var barnaclePlant = (Plant)ThingMaker.MakeThing(ThingDefOf.TKKN_PlantBarnacles);
        barnaclePlant.Growth = Rand.Range(0.07f, 1f);
        if (barnaclePlant.def.plant.LimitedLifespan) {
            barnaclePlant.Age = Rand.Range(0, Mathf.Max(barnaclePlant.def.plant.LifespanTicks - 50, 0));
        }

        GenSpawn.Spawn(barnaclePlant, c, map);
    }

    /// <summary>
    /// Takes the current cell and moves moveCount cells into coastRotations direction
    /// </summary>
    /// <param name="cell">Current location</param>
    /// <param name="moveCount">cells to move</param>
    /// <returns>new cell location</returns>
    private IntVec3 AdjustForRotation(IntVec3 cell, int moveCount) {
        var newDirection = new IntVec3(cell.x, cell.y, cell.z);
        if (coastRotation == Rot4.North) {
            newDirection.z += moveCount + 1;
        }
        else if (coastRotation == Rot4.South) {
            newDirection.z -= moveCount + 1;
        }
        else if (coastRotation == Rot4.East) {
            newDirection.x += moveCount + 1;
        }
        else if (coastRotation == Rot4.West) {
            newDirection.x -= moveCount + 1;
        }

        return newDirection;
    }

    private void SetUpTidesBanks() {
        //set up ocean tides for the first time:
        if (doCoast) {
            //set up for low tide
            previousTideLevel = 0;
            tideLevel = 0;

            for (var i = 0; i < HowManyTideSteps; i++) {
                List<IntVec3> makeSand = tideCellsList[i];
                foreach (var c in makeSand) {
                    if (!cellWeatherAffects.TryGetValue(c, out var cell)) {
                        continue;
                    }

                    if (beachTerrain == RimWorld.TerrainDefOf.Sand ||
                        beachTerrain == TerrainDefOf.TKKN_SandBeachWetSalt) {
                        cell.baseTerrain = TerrainDefOf.TKKN_SandBeachWetSalt;
                        map.terrainGrid.SetTerrain(c, TerrainDefOf.TKKN_SandBeachWetSalt);
                    }
                }
            }

            //bring to current tide levels
            FloodType level = GetTideLevel();
            int max = level switch {
                FloodType.Normal => halfTideSteps,
                FloodType.High => HowManyTideSteps - 1,
                _ => 0
            };

            for (var i = 0; i < max; i++) {
                List<IntVec3> makeSand = tideCellsList[i];
                foreach (var c in makeSand) {
                    if (!cellWeatherAffects.TryGetValue(c, out var cell)) {
                        continue;
                    }

                    cell.increaseTide(oceanTerrain);
                }
            }

            previousTideLevel = Math.Max(0, max - 1);
            tideLevel = max;
        }
    }

    private void SetUpRiverLevel() {
        if (!Settings.doFloods) return;

        for (int i = 0; i < HowManyRiverSteps; i++)
            DoRiverModify(force: true);
    }


    private Season season;
    private Quadrum quadrum;


    private void UpdateBiomeSettings(bool force = false) {
        if (!force) {
            if (Find.TickManager.TicksAbs % 30000 != 0) {
                // Check every 12 hours
                return;
            }
        }

        quadrum = GenDate.Quadrum(Find.TickManager.TicksAbs, location.x);

        season = GenDate.Season(Find.TickManager.TicksAbs, location);
        if (biomeSettings == null)
            return;

        biomeSettings.setWeatherBySeason(map, season, quadrum);
        biomeSettings.setDiseaseBySeason(season, quadrum);
        biomeSettings.setIncidentsBySeason(season, quadrum);
    }


    private void DoCellEnvironment(IntVec3 c) {
        if (!cellWeatherAffects.TryGetValue(c, out var cell)) {
            return;
        }

        //This sets cell.howwetplants to 0. But that is apparently never even a possibility
        //cell.DoCellSteadyEffects();

        if (doUnpacking) {
            cell.Unpack();
        }

        var currentTerrain = c.GetTerrain(map);
        var roofed = map.roofGrid.Roofed(c);

        var gettingWet = false;

        //check if the terrain has been floored
        if (currentTerrain.designationCategory == DesignationCategoryDefOf.Floors) {
            cell.baseTerrain = currentTerrain;
        }

        //spawn special things
        if (anyLavaTerrain) {
            LavaRockSpecials(c, currentTerrain);
        }

        if (Settings.showRain && !roofed) {
            //if it's raining in this cell:
            if (currentRainRate > .0001f) {
                if (floodThreat < 1090000) {
                    floodThreat += floodThreatIncrease;
                }

                gettingWet = true;
                cell.setTerrainWet();
            }
            else if (currentSnowRate > .001f) {
                gettingWet = true;
                cell.setTerrainWet();
            }
            else {
                if (currentRainRate == 0) {
                    floodThreat--;
                }

                //DRY GROUND
                cell.trySetTerrainDry();
            }
        }

        cell.temperature = cell.location.GetTemperature(map);

        if (cell.temperature <= 1) {
            if (Settings.doIce) {
                cell.SetTerrainFrozen();
            }

            if (TerrainTagUtil.HoldsFrost.Contains(currentTerrain)) {
                //handle frost based on snowing
                if (!roofed && currentSnowRate > 0.001f) {
                    frostGridComponent.AddDepth(cell, currentSnowRate * -.01f);
                }
                else {
                    CreepFrostAt(cell, 0.46f * .3f);
                }
            }
        }
        else {
            cell.TrySetTerrainThawed();
            if (TerrainTagUtil.HoldsFrost.Contains(currentTerrain)) {
                var frosty = cell.temperature * -.025f;
                frostGridComponent.AddDepth(cell, frosty);
            }
        }


        //HANDLE PLANT DAMAGES:
        if (gettingWet) {
            //note - removed ismelt because the dirt shouldn't dry out in winter, and snow wets the ground then.
            if (cell.howWetPlants < 100) {
                if (currentRainRate > 0) {
                    cell.howWetPlants += currentRainRate * 2;
                }
                else if (currentSnowRate > 0) {
                    cell.howWetPlants += currentSnowRate * 2;
                }
            }
        }
        else {
            if (outdoorTemp > 20) {
                cell.howWetPlants += wetPlantsValue;
                if (cell.howWetPlants <= 0) {
                    if (TerrainTagUtil.TerrainHasModExtension.Contains(currentTerrain)) {
                        if (!TerrainTagUtil.DryTerrain.ContainsKey(currentTerrain)) {
                            HurtPlants(c, false, true);
                        }
                    }
                    else {
                        HurtPlants(c, false, true);
                    }
                }
            }
        }

        if (Settings.showRain) {
            if (cell.howWet < 3 && gettingWet) {
                cell.howWet += 2;
            }
            else if (cell.howWet > -1) {
                cell.howWet--;
            }
        }

        if (Settings.makePuddles) {
            if (cell.howWet == 3 && (outdoorTemp > 2 && MaxPuddles > totalPuddles &&
                                     currentTerrain != TerrainDefOf.TKKN_SandBeachWetSalt)) {
                FilthMaker.TryMakeFilth(c, map, ThingDefOf.TKKN_FilthPuddle);
                totalPuddles++;
            }
        }

        //cellWeatherAffects[c] = cell;
    }

    private void LavaRockSpecials(IntVec3 c, TerrainDef currentTerrain) {
        if (Rand.Value < .0001f) {
            if (c.InBounds(map)) {
                if (currentTerrain == TerrainDefOf.TKKN_Lava) {
                    var thing = ThingMaker.MakeThing(ThingDefOf.TKKN_LavaRock);
                    GenSpawn.Spawn(thing, c, map);
                }
                else if (currentTerrain == TerrainDefOf.TKKN_LavaRock_RoughHewn &&
                         map.Biome == BiomeDefOf.TKKN_VolcanicFlow &&
                         map.listerThings.ThingsOfDef(ThingDefOf.TKKN_SteamVent).Count < 10) {
                    var thing = ThingMaker.MakeThing(ThingDefOf.TKKN_SteamVent);
                    GenSpawn.Spawn(thing, c, map);
                }
            }
        }
    }

    private void CreepFrostAt(cellData c, float baseAmount) {
        var depthToAdd = baseAmount * c.frostNoise;
        frostGridComponent.AddDepth(c, depthToAdd);
    }

    public FloodType GetRiverLevel() {
        var flood = FloodType.Normal;
        if (floodThreat > 1000000 || season == Season.Spring) {
            flood = FloodType.High;
        }
        else if (season == Season.Fall) {
            flood = FloodType.Low;
        }

        var isDrought = map.gameConditionManager.GetActiveCondition<GameCondition_Drought>();
        if (isDrought != null) {
            flood = isDrought.floodOverride;
        }

        return flood;
    }

    private void DoRiverModify(bool force = false) {
        if (!force) {
            if (!Settings.doFloods || !doRiverFlooding || ticks % TideIntervalCheck != 0) {
                return;
            }
        }

        FloodType riverLevel = GetRiverLevel();

        bool increaseFlood;
        if (riverLevel == FloodType.High && floodLevel < HowManyRiverSteps)
            increaseFlood = true;
        else if (riverLevel == FloodType.Low && floodLevel > 0)
            increaseFlood = false;
        else if (riverLevel == FloodType.Normal && floodLevel < halfRiverSteps)
            increaseFlood = true;
        else if (riverLevel == FloodType.Normal && floodLevel > halfRiverSteps)
            increaseFlood = false;
        else
            return;

        if ((riverLevel == FloodType.High && floodLevel == HowManyRiverSteps) ||
            (riverLevel == FloodType.Low && floodLevel == 0) ||
            (riverLevel == FloodType.Normal && floodLevel == halfRiverSteps)) {
            return;
        }

        List<IntVec3> cellsToChange = riverCellsList[floodLevel];
        foreach (var c in cellsToChange.InRandomOrder()) {
            if (!cellWeatherAffects.TryGetValue(c, out var cell)) {
                continue;
            }

            if (increaseFlood)
                cell.increaseRiver(shallowRiverTerrain);
            else {
                cell.decreaseRiver();
            }
        }

        if (riverLevel == FloodType.High && floodLevel < MaxRiverSteps)
            floodLevel++;
        else if (riverLevel == FloodType.Low && floodLevel > 0)
            floodLevel--;
        else if (riverLevel == FloodType.Normal && floodLevel < halfRiverSteps)
            floodLevel++;
        else if (riverLevel == FloodType.Normal && floodLevel > halfRiverSteps)
            floodLevel--;
    }

    private FloodType GetTideLevel() {
        if (map.gameConditionManager.ConditionIsActive(GameConditionDefOf.Eclipse)) {
            return FloodType.High;
        }

        return GenLocalDate.HourOfDay(map) switch {
            > 4 and < 8 => FloodType.Low,
            > 15 and < 20 => FloodType.High,
            _ => FloodType.Normal
        };
    }

    private void DoTides() {
        //notes to future me: use this.howManyTideSteps - 1, so we always have a little bit of wet sand, or else it looks stupid.
        if (!doCoast || !Settings.doTides || ticks % TideIntervalCheck != 0) {
            return;
        }

        var tideType = GetTideLevel();

        if ((tideType == FloodType.Normal && tideLevel == halfTideSteps) ||
            (tideType == FloodType.High && tideLevel == MaxTideSteps) ||
            (tideType == FloodType.Low && tideLevel == 0))
            return;

        if (tideType == FloodType.Normal && tideLevel == MaxTideSteps) {
            previousTideLevel = tideLevel;
            tideLevel--;
            return;
        }

        List<IntVec3> cellsToChange = tideCellsList[tideLevel];
        foreach (var c in cellsToChange) {
            if (!cellWeatherAffects.TryGetValue(c, out var cell)) {
                continue;
            }

            var waterCheck = AdjustForRotation(c, 0);
            if (!waterCheck.InBounds(map)) {
                continue;
            }

            // Check if the previous tile is an ocean tile
            // If it isn't, check if the current tile is and remove it if so
            if (!isOceanicTerrain(waterCheck.GetTerrain(map))) {
                if (isOceanicTerrain(c.GetTerrain(map))) {
                    cell.decreaseTide();
                }

                continue;
            }

            switch (tideType) {
                case FloodType.High:
                    //cell.terrainOverride = TerrainType.Wet;
                    //cell.changeTide(TerrainType.Wet, oceanTerrain, beachTerrain);
                    cell.increaseTide(oceanTerrain);
                    break;
                case FloodType.Low:
                    //cell.terrainOverride = TerrainType.Dry;
                    //cell.changeTide(TerrainType.Dry, oceanTerrain, beachTerrain);
                    cell.decreaseTide();
                    break;
                case FloodType.Normal:
                    if (tideLevel < halfTideSteps) {
                        //cell.changeTide(TerrainType.Wet, oceanTerrain, beachTerrain);
                        cell.increaseTide(oceanTerrain);
                    }
                    else if (tideLevel > halfTideSteps) {
                        //cell.changeTide(TerrainType.Dry, oceanTerrain, beachTerrain);
                        cell.decreaseTide();
                    }
                    else if (previousTideLevel < tideLevel) {
                        //cell.changeTide(TerrainType.Wet, oceanTerrain, beachTerrain);
                        cell.increaseTide(oceanTerrain);
                    }
                    else if (previousTideLevel > tideLevel) {
                        //cell.changeTide(TerrainType.Dry, oceanTerrain, beachTerrain);
                    }
                    else {
                        cell.changeTide(oceanTerrain);
                    }

                    break;
            }
        }

        switch (tideType) {
            case FloodType.High: {
                if (tideLevel < MaxTideSteps) {
                    previousTideLevel = tideLevel;
                    tideLevel++;
                }

                break;
            }
            case FloodType.Low: {
                if (tideLevel > 0) {
                    previousTideLevel = tideLevel;
                    tideLevel--;
                }

                break;
            }
            case FloodType.Normal when tideLevel > halfTideSteps:
                previousTideLevel = tideLevel;
                tideLevel--;
                break;
            case FloodType.Normal: {
                if (tideLevel < halfTideSteps) {
                    previousTideLevel = tideLevel;
                    tideLevel++;
                }

                break;
            }
        }
    }

    private void HurtPlants(IntVec3 c, bool onlyLow, bool saveHarvest) {
        if (noHurtPlants) {
            return;
        }

        //don't hurt things in growing zone
        if (map.zoneManager.ZoneAt(c) is Zone_Growing) {
            return;
        }

        List<Thing> things = c.GetThingList(map);
        foreach (var thing in things.ToList()) {
            if (thing is not Plant) {
                continue;
            }

            var isLow = true;
            if (onlyLow) {
                isLow = thing.def.altitudeLayer == AltitudeLayer.LowPlant;
            }

            var isHarvestable = true;
            if (saveHarvest) {
                isHarvestable = thing.def.plant.harvestTag != "Standard";
            }

            if (thing.def.category != ThingCategory.Plant || !isLow || !isHarvestable) {
                continue;
            }

            var damage = -.001f;
            damage *= thing.def.plant.fertilityMin;
            thing.TakeDamage(new DamageInfo(DamageDefOf.Rotting, damage, 0, 0));
        }
    }

    private bool isOceanicTerrain(TerrainDef terrain) {
        return terrain == RimWorld.TerrainDefOf.WaterOceanShallow ||
               terrain == TerrainDefOf.NPS_WaterOceanTide ||
               terrain == oceanTerrain;
    }

    private bool isRiverTerrain(TerrainDef terrain) {
        return terrain == RimWorld.TerrainDefOf.WaterMovingShallow ||
               terrain == TerrainDefOf.NPS_WaterRiverFlood ||
               terrain == RimWorld.TerrainDefOf.WaterMovingChestDeep;
    }
}