using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TKKN_NPS;

public class cellData : IExposable
{
    private const int PackAt = 750;
    private const int PackAtSmooth = PackAt * 10;
    private const int UnpackAt = PackAt / 2;
    //public TerrainDef baseTerrain;
    public int riverLevel = 999;
    public IntVec3 riverFocus = IntVec3.Invalid;
    public float frostLevel;
    public float frostNoise;

    public int howPacked;
    private bool packed = false;
    public int howWet;
    public float howWetPlants = 60;
    public bool isFlooded;
    public bool isFrozen;
    public bool isWet;
    public IntVec3 location;
    public Map map;
    private TerrainDef driedTerrain;

    public float temperature = -9999;

    public int tideLevel = -1;


    public TerrainWeatherReactions Weather => currentTerrain.GetModExtension<TerrainWeatherReactions>();

    public TerrainDef currentTerrain;


    public void ExposeData() {
        Scribe_Values.Look(ref tideLevel, "tideLevel", -1, false);
        Scribe_Values.Look(ref riverLevel, "riverLevel", 999, false);
        Scribe_Values.Look(ref riverFocus, "riverFocus", IntVec3.Invalid, false);
        Scribe_Values.Look(ref howPacked, "howPacked", 0, false);
        Scribe_Values.Look(ref packed, "packed");
        Scribe_Values.Look(ref howWet, "howWet", 0, false);
        Scribe_Values.Look(ref howWetPlants, "howWetPlants", 60, false);
        Scribe_Values.Look(ref frostLevel, "frostLevel", 0, false);
        Scribe_Values.Look(ref frostNoise, "frostNoise", 0, false);
        Scribe_Values.Look(ref isWet, "isWet", false, false);
        Scribe_Values.Look(ref isFlooded, "isFlooded", false, false);
        Scribe_Values.Look(ref location, "location", forceSave:true);
        Scribe_Defs.Look(ref driedTerrain, "driedTerrain");
    }

    public void setTerrainWet() {
        var thisTerrain = currentTerrain;
        //if terrain is temporary we don't want to affect it
        if (thisTerrain.temporary) {
            return;
        }

        if (!TerrainTagUtil.WetTerrain.TryGetValue(thisTerrain, out TerrainDef wetTerrain)) {
            return;
        }

        if (!TerrainTagUtil.TerrainWetAt.TryGetValue(thisTerrain, out var wetAt)) {
            return;
        }

        if (howWet > wetAt) {
            map.terrainGrid.SetTerrain(location, wetTerrain);
            driedTerrain = thisTerrain;
            isWet = true;
            rainSpawns();
        }
        else if (howWet == 0) {
            isWet = false;
            howWet = -1;
            driedTerrain = thisTerrain;
        }
    }

    public void trySetTerrainDry() {
        if (!isWet) {
            return;
        }

        var thisTerrain = currentTerrain;
/*
        //if terrain is temporary we don't want to affect it
        if (thisTerrain.temporary) {
            return;
        }*/

        if (!TerrainTagUtil.TerrainWetAt.TryGetValue(thisTerrain, out var wetAt))
            return;

        if (howWet <= wetAt) {
            map.terrainGrid.SetTerrain(location, driedTerrain);
            isWet = false;
            howWet = -1;
            driedTerrain = null;
        }
    }

    /// <summary>
    /// Yes this feels hideous, yes I hate it.
    /// Needs to reduce the amount of pieces that turn to ice per tick so now it has a 5% chance to happen
    /// If anyone has a better suggestion I'm all ears.
    /// </summary>
    public void SetTerrainFrozen() {
        if (isFrozen) {
            return;
        }

        TerrainDef thisTerrain = currentTerrain;
        if (!TerrainTagUtil.FreezeTerrain.TryGetValue(thisTerrain, out var frozenTerrain))
            return;

        if (temperature > frozenTerrain.freezeAt)
            return;
        if (!Rand.Chance(0.05f))
            return;

        map.terrainGrid.SetTempTerrain(location, frozenTerrain.terrain);
        isFrozen = true;
    }

    public void TrySetTerrainThawed() {
        if (!isFrozen) {
            return;
        }

        if (!currentTerrain.temporary) {
            return;
        }

        map.terrainGrid.RemoveTempTerrain(location, doLeavings: false, preventDestroyEffects: true);
        howWet = 4;
        isFrozen = false;
        setTerrainWet();
    }


    public void removeTempTerrain() {
        var thisTerrain = currentTerrain;
        if (!thisTerrain.temporary) {
            return;
        }

        map.terrainGrid.RemoveTempTerrain(location, doLeavings: false, preventDestroyEffects: true);
    }

    public void changeTide(TerrainDef tidalTerrain) {
        if (currentTerrain == tidalTerrain) {
            //Log.Message("Making beach terrain "+beachTerrain+" at "+location);
            decreaseTide();
        }
        else {
            //Log.Message("Making tidal terrain "+tidalTerrain+" at "+location);
            if (isFrozen)
                return;
            increaseTide(tidalTerrain);
        }
    }

    public void increaseTide(TerrainDef beachTerrain) {
        if (isFrozen) {
            return;
        }

        if (location.GetEdifice(map) == null) {
            map.terrainGrid.SetTempTerrain(location, beachTerrain);
            currentTerrain = beachTerrain;
            clearLoot();
        }
    }

    public void decreaseTide() {
        map.terrainGrid.RemoveTempTerrain(location);
        leaveLoot();
    }

    /// <summary>
    /// Verifies the current tile does not have anything on it.
    /// Then verifies that the tile it's focusing on doesn't have anything on it and is a water tile.
    /// If true set terrain to riverTerrain
    /// </summary>
    /// <param name="riverTerrain"></param>
    public void increaseRiver(TerrainDef riverTerrain) {
        if (isFrozen) {
            return;
        }
        // Verify the current tile does not have anything on it.
        // Then verify
        if (location.GetEdifice(map) != null) 
            return;
        if (riverFocus.GetEdifice(map) != null) 
            return;
        if (!riverFocus.GetTerrain(map).IsWater) 
            return;
        map.terrainGrid.SetTempTerrain(location, riverTerrain);
    }

    public void decreaseRiver() {
        map.terrainGrid.RemoveTempTerrain(location);
        if (EffectSettings.showRain) {
            howWet = 4;
            currentTerrain = location.GetTerrain(map);
            setTerrainWet();
        }
    }

    public void Unpack() {
        if (howPacked > 10000) {
            howPacked = 10000;
            return;
        }

        if (howPacked <= 0) {
            return;
        }
        howPacked--;
        if (!packed) {
            return;
        }
        
        if (howPacked <= UnpackAt) {
            if (currentTerrain == TerrainDefOf.TKKN_DirtPath) {
                map.terrainGrid.SetTerrain(location, RimWorld.TerrainDefOf.Soil);
                packed = false;
            }
            else if (currentTerrain == TerrainDefOf.TKKN_SandPath) {
                map.terrainGrid.SetTerrain(location, RimWorld.TerrainDefOf.Sand);
                packed = false;
            }
        }
    }

    /// <summary>
    /// This method currently has a weird thing where if you remove a packed path
    /// (You can, and we want the user to do the above)
    /// It will change back to a packed path after being stepped on
    /// Unsure how to address this part.
    /// </summary>
    public void DoPack() {
        var terrain = currentTerrain;
        if (!TerrainTagUtil.CanBePacked.Contains(terrain)) {
            return;
        }

        //don't pack if there's a growing zone.
        if (map.zoneManager.ZoneAt(location) is Zone_Growing) {
            return;
        }
        howPacked++;
        if (packed) {
            return;
        }

        if (howPacked <= PackAt) {
            return;
        }

        if (terrain == RimWorld.TerrainDefOf.Soil) {
            map.terrainGrid.SetTerrain(location, TerrainDefOf.TKKN_DirtPath);
            packed = true;
        }
        else if (terrain == RimWorld.TerrainDefOf.Sand) {
            map.terrainGrid.SetTerrain(location, TerrainDefOf.TKKN_SandPath);
            packed = true;
        }
        else if (terrain.smoothedTerrain != null && howPacked > PackAtSmooth) {
            map.terrainGrid.SetTerrain(location, terrain.smoothedTerrain);
            packed = true;
        }
    }

    private void rainSpawns() {
        //spawn special things when it rains.
        if (Rand.Value < .009) {
            if (currentTerrain == TerrainDefOf.TKKN_Lava) {
                GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.TKKN_LavaRock), location, map);
            }
            else if (currentTerrain == TerrainDefOf.TKKN_SandBeachWetSalt) {
                var crab = PawnGenerator.GeneratePawn(PawnDefOf.TKKN_crab);
                GenSpawn.Spawn(crab, location, map);
            }
            else {
                if (TerrainTagUtil.TKKN_Wet.Contains(currentTerrain)) {
                    FleckMaker.WaterSplash(location.ToVector3(), map, 1, 1);
                }
            }
        }
        else if (Rand.Value < .04 && TerrainTagUtil.Lava.Contains(currentTerrain)) {
            FleckMaker.ThrowSmoke(location.ToVector3(), map, 5);
        }
    }

    private void leaveLoot() {
        if (!EffectSettings.leaveStuff) {
            return;
        }

        var leaveSomething = Rand.Value;
        if (leaveSomething < 0.001f) {
            List<Thing> allowed = ThingSetMakerDefOf.TKKN_TidalLoot.root.Generate();

            if (allowed == null) {
                return;
            }

            for (int i = 0; i < allowed.Count; i++) {
                GenSpawn.Spawn(allowed[i], location, map);
            }
        }
        else if (leaveSomething < 0.002f && (location.GetPlant(map) == null && location.GetCover(map) == null)) {
            //grow water and shore plants:
            List<ThingDef> plants = map.Biome.AllWildPlants;
            for (var i = plants.Count - 1; i >= 0; i--) {
                //spawn some water plants:
                var plantDef = plants[i];
                if (!plantDef.CanEverPlantAt(location, map, checkMapTemperature: false))
                    continue;
                var plant = (Plant)ThingMaker.MakeThing(plantDef);
                plant.Growth = Rand.Range(0.07f, 1f);
                if (plant.def.plant.LimitedLifespan) {
                    plant.Age = Rand.Range(0, Mathf.Max(plant.def.plant.LifespanTicks - 50, 0));
                }

                GenSpawn.Spawn(plant, location, map);
                break;
            }
        }
    }

    private void clearLoot() {
        List<Thing> things = location.GetThingList(map);

        for (var i = things.Count - 1; i >= 0; i--) {
            if (things[i].def.category == ThingCategory.Item || things[i].def.category ==  ThingCategory.Plant) {
                things[i].Destroy();
            }
        }
    }
}