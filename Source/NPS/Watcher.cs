using System;
using System.Collections.Generic;
using System.Linq;
using NPS;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace TKKN_NPS;

public class Watcher(Map map) : MapComponent(map)
{
    private const int HowManyFloodSteps = 5;
    private readonly int halfFloodSteps = (int)Math.Round((HowManyFloodSteps - 1M) / 2);
    private const int MaxFloodSteps = HowManyFloodSteps - 1;

    private const int HowManyTideSteps = 13;
    private readonly int halfTideSteps = (int)Math.Round((HowManyTideSteps - 1M) / 2);
    private const int MaxTideSteps = HowManyTideSteps - 1;

    private const int MaxPuddles = 50;

    //used to save data about active springs.
    public Dictionary<int, springData> activeSprings = new();

    private BiomeSeasonalSettings biomeSettings;
    public Dictionary<IntVec3, cellData> cellWeatherAffects = new();
    private int cycleIndex;
    private bool doCoast = true; //false if no coast
    private List<List<IntVec3>> floodCellsList = [];

    private int floodLevel; // 0 - 3
    private int floodThreat;
    private int floodThreatIncrease;

    public FrostGrid frostGridComponent;
    private Vector2 location;

    private ModuleBase frostNoise;
    private float humidity;

    public float outdoorTemp;

    //public HashSet<IntVec3> lavaCellsList = [];
    public Thing overlay;

    //used by weather
    private bool regenCellLists = true;
    public List<IntVec3> swimmingCellsList = [];

    private int ticks;

    //rebuild every save to keep file size down
    private List<List<IntVec3>> tideCellsList = [];
    private int tideLevel; // 0 - 13
    private int totalPuddles;
    private int totalSprings;

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

    /* STANDARD STUFF */

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
            currentSnowRate =  map.weatherManager.curWeather.snowRate;
            isRaining = currentRainRate > 0;
            var baseHumidity = (map.TileInfo.rainfall + 1) * (map.TileInfo.temperature + 1) *
                               (map.TileInfo.swampiness + 1);
            var currentHumidity =
                (1 + currentRainRate) * (1 + outdoorTemp);
            humidity = ((baseHumidity + currentHumidity) / 1000) + 18;
            wetPlantsValue = -1 * (outdoorTemp / humidity / 10);
            floodThreatIncrease = 1 + 2 * (int)Math.Round(currentRainRate);
            noHurtPlants = !Settings.allowPlantEffects || ticks % 150 != 0;
            doUnpacking = !doUnpacking;
            DoTides();
            DoFloods();

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
    }

    public override void FinalizeInit() {
        base.FinalizeInit();
        if (map.IsPocketMap) {
            Log.Message("This is a pocket map. Nothing is running");
            dontRunAnything = true;
            return;
        }

        if (map.Biome.inVacuum) {
            Log.Message("In the vacuum of space. Not running");
            dontRunAnything = true;
            return;
        }

        mapArea = map.Area;
        doCoast = map.Tile.Tile.IsCoastal;
        biomeSettings = map.Biome.GetModExtension<BiomeSeasonalSettings>();
        frostGridComponent = map.GetComponent<FrostGrid>();
        location = Find.WorldGrid.LongLatOf(map.Tile);
        UpdateBiomeSettings(true);

        frostNoise = new Perlin(0.039999999105930328, 2.0, 0.5, 5,
            Rand.Range(0, 651431), QualityMode.Medium);
        

        RebuildCellLists();
    }


    private void RebuildCellLists() {
        if (Settings.regenCells) {
            regenCellLists = Settings.regenCells;
        }


        /*
        #region devonly
        this.regenCellLists = true;
        Log.Error("DEV STUFF IS ON");
        this.cellWeatherAffects = new Dictionary<IntVec3, cellData>();
        #endregion
        */

        if (regenCellLists) {

            Rot4 rot = Find.World.CoastDirectionAt(map.Tile);

            IEnumerable<IntVec3>
                tmpTerrain = map.AllCells.InRandomOrder(); //random so we can spawn plants and stuff in this step.
            cellWeatherAffects = new Dictionary<IntVec3, cellData>();
            foreach (var c in tmpTerrain) {
                var terrain = c.GetTerrain(map);

                if (!c.InBounds(map)) {
                    continue;
                }

                if (terrain == TerrainDefOf.TKKN_Lava || terrain == TerrainDefOf.TKKN_LavaRock_RoughHewn) {
                    anyLavaTerrain = true;
                }

                var cell = new cellData { location = c, baseTerrain = terrain, howWetPlants = 70 };

                if (cell.originalTerrain != null) {
                    cell.originalTerrain = terrain;
                }

                if (rot.IsValid && terrain == RimWorld.TerrainDefOf.Sand ||
                    terrain == TerrainDefOf.TKKN_SandBeachWetSalt) {
                    //get all the sand pieces that are touching water.
                    for (var j = 0; j < HowManyTideSteps; j++) {
                        var waterCheck = AdjustForRotation(rot, c, j);
                        if (!waterCheck.InBounds(map) ||
                            waterCheck.GetTerrain(map) != RimWorld.TerrainDefOf.WaterOceanShallow) {
                            continue;
                        }

                        map.terrainGrid.SetTerrain(c, TerrainDefOf.TKKN_SandBeachWetSalt);
                        cell.tideLevel = j;
                        break;
                    }
                }
                else if (terrain != RimWorld.TerrainDefOf.WaterOceanShallow &&
                         terrain != RimWorld.TerrainDefOf.WaterOceanDeep &&
                         TerrainTagUtil.TKKN_Wet.Contains(terrain)) {
                    for (var j = 0; j < HowManyFloodSteps; j++) {
                        var num = GenRadial.NumCellsInRadius(j);
                        for (var i = 0; i < num; i++) {
                            var bankCheck = c + GenRadial.RadialPattern[i];
                            if (!bankCheck.InBounds(map)) {
                                continue;
                            }

                            var bankCheckTerrain = bankCheck.GetTerrain(map);
                            if (terrain == TerrainDefOf.TKKN_SandBeachWetSalt ||
                                TerrainTagUtil.TKKN_Wet.Contains(bankCheckTerrain)) {
                                continue;
                            }

                            //see if this cell has already been done, because we can have each cell in multiple flood levels.
                            var bankCell = cellWeatherAffects.TryGetValue(bankCheck, out var affect)
                                ? affect
                                : new cellData { location = bankCheck, baseTerrain = bankCheckTerrain };

                            bankCell.floodLevel.Add(j);
                        }
                    }
                }

                //Spawn special elements:
                SpawnSpecialElements(c);
                SpawnSpecialPlants(c);

                cellWeatherAffects[c] = cell;
            }
        }


        //rebuild lookup lists.
        swimmingCellsList = [];
        tideCellsList = [];
        floodCellsList = [];

        for (var k = 0; k < HowManyTideSteps; k++) {
            tideCellsList.Add([]);
        }

        for (var k = 0; k < HowManyFloodSteps; k++) {
            floodCellsList.Add([]);
        }

        foreach (KeyValuePair<IntVec3, cellData> thisCell in cellWeatherAffects) {
            cellWeatherAffects[thisCell.Key].map = map;

            frostGridComponent.SetDepth(thisCell.Value.location, thisCell.Value.frostLevel);
            if (thisCell.Value.tideLevel > -1) {
                tideCellsList[thisCell.Value.tideLevel].Add(thisCell.Key);
            }

            if (thisCell.Value.floodLevel.Count != 0) {
                foreach (var level in thisCell.Value.floodLevel) {
                    floodCellsList[level].Add(thisCell.Key);
                }
            }

            if (TerrainTagUtil.TKKN_Swim.Contains(thisCell.Value.baseTerrain)) {
                swimmingCellsList.Add(thisCell.Key);
            }
        }

        if (!regenCellLists) {
            return;
        }

        SetUpTidesBanks();
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

    private void SpawnSpecialElements(IntVec3 c) {
        
        //defaults
        var maxSprings = 3;
        var springSpawnChance = .8f;

        if (biomeSettings != null) {
            maxSprings = biomeSettings.maxSprings;
            springSpawnChance = biomeSettings.springSpawnChance;
        }

        if (!Settings.doSprings) {
            maxSprings = 0;
            springSpawnChance = 0;
        }

        foreach (var element in DefDatabase<ElementSpawnDef>.AllDefs) {
            var isSpring = element.thingDef.defName.Contains("Spring");

            if (isSpring && maxSprings <= totalSprings) {
                continue;
            }

            if (element.forbiddenBiomes.Contains(map.Biome)) {
                continue;
            }

            if (!element.allowedBiomes.Contains(map.Biome)) {
                continue;
            }


            if (isSpring && Rand.Value < springSpawnChance) {
                var thing = ThingMaker.MakeThing(element.thingDef);
                GenSpawn.Spawn(thing, c, map);
                totalSprings++;
            }

            if (isSpring || !(Rand.Value < .0001f)) {
                continue;
            }

            var elementThing = ThingMaker.MakeThing(element.thingDef);
            GenSpawn.Spawn(elementThing, c, map);
        }
    }

    private static IntVec3 AdjustForRotation(Rot4 rot, IntVec3 cell, int j) {
        var newDirection = new IntVec3(cell.x, cell.y, cell.z);
        if (rot == Rot4.North) {
            newDirection.z += j + 1;
        }
        else if (rot == Rot4.South) {
            newDirection.z -= j - 1;
        }
        else if (rot == Rot4.East) {
            newDirection.x += j + 1;
        }
        else if (rot == Rot4.West) {
            newDirection.x -= j - 1;
        }

        return newDirection;
    }

    private void SetUpTidesBanks() {
        //set up tides and river banks for the first time:
        if (doCoast) {
            //set up for low tide
            tideLevel = 0;

            for (var i = 0; i < HowManyTideSteps; i++) {
                List<IntVec3> makeSand = tideCellsList[i];
                foreach (var c in makeSand) {
                    if (!cellWeatherAffects.TryGetValue(c, out var cell)) {
                        continue;
                    }

                    cell.baseTerrain = TerrainDefOf.TKKN_SandBeachWetSalt;
                    map.terrainGrid.SetTerrain(c, TerrainDefOf.TKKN_SandBeachWetSalt);
                }
            }

            //bring to current tide levels
            FloodType level = GetTideLevel();
            int max = level switch {
                FloodType.Normal => (int)Math.Floor((HowManyTideSteps - 1) / 2M),
                FloodType.High => HowManyTideSteps - 1,
                _ => 0
            };

            for (var i = 0; i < max; i++) {
                List<IntVec3> makeSand = tideCellsList[i];
                foreach (var c in makeSand) {
                    if (!cellWeatherAffects.TryGetValue(c, out var cell)) {
                        continue;
                    }

                    cell.setTerrain(TerrainType.Tide);
                }
            }

            tideLevel = max;
        }

        FloodType flood = GetFloodType();

        for (var i = 0; i < HowManyFloodSteps; i++) {
            List<IntVec3> makeWater = floodCellsList[i];
            foreach (var c in makeWater) {
                if (!cellWeatherAffects.TryGetValue(c, out var cell)) {
                    continue;
                }

                if (!TerrainTagUtil.TKKN_Wet.Contains(cell.baseTerrain)) {
                    cell.baseTerrain = RimWorld.TerrainDefOf.Riverbank;
                }

                if (flood == FloodType.Low) {
                    cell.terrainOverride = TerrainType.Dry;
                }
                else if(flood !=FloodType.High) {
                    if (i >= HowManyFloodSteps / 2) {
                        cell.terrainOverride = TerrainType.Dry;
                    }
                }

                cell.setTerrain(TerrainType.Flooded);
            }
        }
    }

    private Season season;
    private Quadrum quadrum;


    private void UpdateBiomeSettings(bool force = false) {
        if (biomeSettings == null) {
            return;
        }

        if (force) {
            quadrum = GenDate.Quadrum(Find.TickManager.TicksAbs, location.x);
        }
        else {
            if (Find.TickManager.TicksAbs % 30000 != 0) {
                // Check every 12 hours
                return;
            }

            quadrum = GenDate.Quadrum(Find.TickManager.TicksAbs, location.x);

            if (biomeSettings.lastChangedQ == quadrum) {
                return;
            }
        }

        season = GenDate.Season(Find.TickManager.TicksAbs, location);

        biomeSettings.setWeatherBySeason(map, season, quadrum);
        biomeSettings.setDiseaseBySeason(season, quadrum);
        biomeSettings.setIncidentsBySeason(season, quadrum);
        biomeSettings.lastChanged = season;
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
        LavaRockSpecials(c, currentTerrain);

        if (Settings.showRain && !roofed && !TerrainTagUtil.TKKN_Wet.Contains(currentTerrain)) {
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

            //handle frost based on snowing
            if (!roofed && currentSnowRate > 0.001f) {
                frostGridComponent.AddDepth(c, currentSnowRate * -.01f);
            }
            else {
                CreepFrostAt(c, 0.46f * .3f);
            }
        }
        else {
            cell.TrySetTerrainThawed();
            var frosty = cell.temperature * -.025f;
            frostGridComponent.AddDepth(c, frosty);
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
            if (currentTerrain.IsWater) {
                cell.howWet = 4;
            }
            else if (cell.howWet < 3 && gettingWet) {
                cell.howWet += 2;
            }
            else if (cell.howWet > -1) {
                cell.howWet--;
            }
        }

        if (Settings.makePuddles) {
            if (cell.howWet == 3 && (outdoorTemp>2 && MaxPuddles > totalPuddles &&
                                     currentTerrain != TerrainDefOf.TKKN_SandBeachWetSalt)) {
                FilthMaker.TryMakeFilth(c, map, ThingDefOf.TKKN_FilthPuddle);
                totalPuddles++;
            }
        }

        //cellWeatherAffects[c] = cell;
    }

    private void LavaRockSpecials(IntVec3 c, TerrainDef currentTerrain) {
        if (!anyLavaTerrain) return;
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

    private void CreepFrostAt(IntVec3 c, float baseAmount) {
        var num = frostNoise.GetValue(c)+1;
        num += 1f;
        num *= 0.5f;
        if (num < 0.5f) {
            num = 0.5f;
        }

        var depthToAdd = baseAmount * num;

        frostGridComponent.AddDepth(c, depthToAdd);
    }

    private FloodType GetFloodType() {
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

    private void DoFloods() {
        if (!Settings.doFloods || ticks % 300 != 0) {
            return;
        }


        FloodType flood = GetFloodType();

        TerrainType? overrideType = flood switch {
            FloodType.High when floodLevel < MaxFloodSteps => TerrainType.Wet,
            FloodType.Low when floodLevel > 0 => TerrainType.Dry,
            FloodType.Normal when floodLevel < halfFloodSteps => TerrainType.Wet,
            FloodType.Normal when floodLevel > halfFloodSteps => TerrainType.Dry,
            _ => null
        };

        if ((flood == FloodType.High && floodLevel == HowManyFloodSteps) ||
            (flood == FloodType.Low && floodLevel == 0) ||
            (flood == FloodType.Normal && floodLevel == halfFloodSteps))
            return;

        List<IntVec3> cellsToChange = floodCellsList[floodLevel];
        foreach (var c in cellsToChange) {
            if (!cellWeatherAffects.TryGetValue(c, out var cell)) {
                continue;
            }

            if (overrideType != null) {
                cell.terrainOverride = overrideType;
            }

            cell.setTerrain(TerrainType.Flooded);
        }

        switch (flood) {
            case FloodType.High when floodLevel < MaxFloodSteps:
                floodLevel++;
                break;
            case FloodType.Low when floodLevel > 0:
                floodLevel--;
                break;
            case FloodType.Normal when floodLevel < halfFloodSteps:
                floodLevel++;
                break;
            case FloodType.Normal when floodLevel > halfFloodSteps:
                floodLevel--;
                break;
        }
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
        if (!doCoast || !Settings.doTides || ticks % 100 != 0) {
            return;
        }

        var tideType = GetTideLevel();

        switch (tideType) {
            case FloodType.Normal when tideLevel == halfTideSteps:
            case FloodType.High when tideLevel == MaxTideSteps:
            case FloodType.Low when tideLevel == 0:
                return;
            case FloodType.Normal when tideLevel == MaxTideSteps:
                tideLevel--;
                return;
        }

        List<IntVec3> cellsToChange = tideCellsList[tideLevel];
        foreach (var c in cellsToChange) {
            if (!cellWeatherAffects.TryGetValue(c, out var cell)) {
                continue;
            }

            switch (tideType) {
                case FloodType.High:
                    cell.terrainOverride = TerrainType.Wet;
                    break;
                case FloodType.Low:
                    cell.terrainOverride = TerrainType.Dry;
                    break;
            }

            cell.setTerrain(TerrainType.Tide);
        }

        switch (tideType) {
            case FloodType.High: {
                if (tideLevel < MaxTideSteps) {
                    tideLevel++;
                }

                break;
            }
            case FloodType.Low: {
                if (tideLevel > 0) {
                    tideLevel--;
                }

                break;
            }
            case FloodType.Normal when tideLevel > halfTideSteps:
                tideLevel--;
                break;
            case FloodType.Normal: {
                if (tideLevel < halfTideSteps) {
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
}