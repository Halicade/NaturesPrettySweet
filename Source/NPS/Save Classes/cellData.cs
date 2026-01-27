using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TKKN_NPS;

public class cellData : IExposable
{
    private const int packAt = 750;
    private const int packAtSmooth = packAt * 10;
    private const int unpack = packAt / 2;
    public TerrainDef baseTerrain;
    public int riverLevel = 999;
    public IntVec3 riverFocus = IntVec3.Invalid;
    public float frostLevel;
    public float frostNoise;

    public int howPacked;
    public int howWet;
    public float howWetPlants = 60;
    public bool isFlooded;
    public bool isFrozen;
    public bool isWet;
    public IntVec3 location;
    public Map map;
    private TerrainDef driedTerrain;

    public TerrainType? terrainOverride;
    public float temperature = -9999;

    public int tideLevel = -1;


    public TerrainWeatherReactions Weather => baseTerrain.GetModExtension<TerrainWeatherReactions>();

    public TerrainDef currentTerrain => location.GetTerrain(map);


    public void ExposeData() {
        Scribe_Values.Look(ref tideLevel, "tideLevel", tideLevel, true);
        Scribe_Values.Look(ref riverLevel, "riverLevel", riverLevel, true);
        Scribe_Values.Look(ref riverFocus, "riverFocus", riverFocus, true);
        //Scribe_Collections.Look(ref floodLevel, "floodLevel", LookMode.Value);
        Scribe_Values.Look(ref howPacked, "howPacked", howPacked, true);
        Scribe_Values.Look(ref howWet, "howWet", howWet, true);
        Scribe_Values.Look(ref howWetPlants, "howWetPlants", howWetPlants, true);
        Scribe_Values.Look(ref frostLevel, "frostLevel", frostLevel, true);
        Scribe_Values.Look(ref frostNoise, "frostNoise", frostNoise, true);
        Scribe_Values.Look(ref isWet, "isWet", isWet, true);
        Scribe_Values.Look(ref isFlooded, "isFlooded", isFlooded, true);
        Scribe_Values.Look(ref terrainOverride, "overrideType", terrainOverride, true);
        Scribe_Values.Look(ref location, "location", location, true);
        Scribe_Values.Look(ref temperature, "temperature", -999, true);
        Scribe_Defs.Look(ref baseTerrain, "baseTerrain");
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

        map.terrainGrid.SetTerrain(location, frozenTerrain.terrain);
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
            map.terrainGrid.SetTerrain(location, beachTerrain);
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
        howWet = 4;
        setTerrainWet();
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
        if (howPacked <= unpack) {
            if (currentTerrain == TerrainDefOf.TKKN_DirtPath) {
                changeTerrain(RimWorld.TerrainDefOf.Soil);
            }
            else if (currentTerrain == TerrainDefOf.TKKN_SandPath) {
                changeTerrain(RimWorld.TerrainDefOf.Sand);
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
        if (howPacked <= packAt) {
            return;
        }

        if (terrain == RimWorld.TerrainDefOf.Soil) {
            changeTerrain(TerrainDefOf.TKKN_DirtPath);
        }
        else if (terrain == RimWorld.TerrainDefOf.Sand) {
            changeTerrain(TerrainDefOf.TKKN_SandPath);
        }
        else if (terrain.smoothedTerrain != null && howPacked > packAtSmooth) {
            changeTerrain(terrain.smoothedTerrain);
        }
    }

    private void changeTerrain(TerrainDef terrain) {
        if (terrain != null && terrain != currentTerrain) {
            map.terrainGrid.SetTerrain(location, terrain);
        }
    }

    private void rainSpawns() {
        //spawn special things when it rains.
        if (Rand.Value < .009) {
            if (baseTerrain == TerrainDefOf.TKKN_Lava) {
                GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.TKKN_LavaRock), location, map);
            }
            else if (baseTerrain == TerrainDefOf.TKKN_SandBeachWetSalt) {
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
        if (!Settings.leaveStuff) {
            return;
        }

        var leaveSomething = Rand.Value;
        switch (leaveSomething) {
            case < 0.001f: {
                List<Thing> allowed = ThingSetMakerDefOf.TKKN_TidalLoot.root.Generate();

                if (allowed == null) {
                    return;
                }

                for (int i = 0; i < allowed.Count; i++) {
                    GenSpawn.Spawn(allowed[i], location, map);
                }

                break;
            }
            //grow water and shore plants:
            case < 0.002f when location.GetPlant(map) == null && location.GetCover(map) == null: {
                List<ThingDef> plants = map.Biome.AllWildPlants;
                for (var i = plants.Count - 1; i >= 0; i--) {
                    //spawn some water plants:
                    var plantDef = plants[i];
                    if (!PlantReactionUtil.AllowedTerrains.TryGetValue(plantDef,
                            out List<TerrainDef> allowedTerrains)) {
                        continue;
                    }

                    if (!allowedTerrains.Contains<TerrainDef>(currentTerrain)) {
                        continue;
                    }

                    var plant = (Plant)ThingMaker.MakeThing(plantDef);
                    plant.Growth = Rand.Range(0.07f, 1f);
                    if (plant.def.plant.LimitedLifespan) {
                        plant.Age = Rand.Range(0, Mathf.Max(plant.def.plant.LifespanTicks - 50, 0));
                    }

                    GenSpawn.Spawn(plant, location, map);
                    break;
                }

                break;
            }
        }
    }

    private void clearLoot() {
        List<Thing> things = location.GetThingList(map);

        for (var i = things.Count - 1; i >= 0; i--) {
            if (things[i].def.category == ThingCategory.Item) {
                things[i].Destroy();
                continue;
            }

            //remove any plants that might've grown:

            if (things[i] is not Plant) {
                continue;
            }

            if (PlantReactionUtil.AllowedTerrains.TryGetValue(things[i].def, out var thingWeather)) {
                if (thingWeather.Contains(currentTerrain)) {
                    continue;
                }

                Log.Warning($"Destroying {things[i].def.defName} at {location} on {currentTerrain.defName}");
                things[i].Destroy();
            }
            else {
                things[i].Destroy();
            }
        }
    }
}