using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TKKN_NPS;

public class cellData : IExposable
{
    private readonly int packAt = 750;
    public TerrainDef baseTerrain;
    public HashSet<int> floodLevel = [];
    public float frostLevel;

    public bool gettingWet = false;
    public int howPacked;
    public int howWet;
    public float howWetPlants = 60;
    public bool isFlooded;
    public bool isFrozen;
    public bool isMelt;
    public bool isThawed = true;
    public bool isWet;
    public IntVec3 location;
    public Map map;
    public TerrainDef originalTerrain;

    public string overrideType = "";
    public float temperature = -9999;

    public int tideLevel = -1;


    public TerrainWeatherReactions weather => baseTerrain.HasModExtension<TerrainWeatherReactions>()
        ? baseTerrain.GetModExtension<TerrainWeatherReactions>()
        : null;

    public TerrainDef currentTerrain => location.GetTerrain(map);


    public void ExposeData() {
        Scribe_Values.Look(ref tideLevel, "tideLevel", tideLevel, true);
        Scribe_Collections.Look(ref floodLevel, "floodLevel", LookMode.Value);
        Scribe_Values.Look(ref howPacked, "howPacked", howPacked, true);
        Scribe_Values.Look(ref howWet, "howWet", howWet, true);
        Scribe_Values.Look(ref howWetPlants, "howWetPlants", howWetPlants, true);
        Scribe_Values.Look(ref frostLevel, "frostLevel", frostLevel, true);
        Scribe_Values.Look(ref isWet, "isWet", isWet, true);
        Scribe_Values.Look(ref isFlooded, "isFlooded", isFlooded, true);
        Scribe_Values.Look(ref isMelt, "isMelt", isMelt, true);
        Scribe_Values.Look(ref overrideType, "overrideType", overrideType, true);

        Scribe_Values.Look(ref isThawed, "isThawed", isThawed, true);


        Scribe_Values.Look(ref location, "location", location, true);
        Scribe_Values.Look(ref temperature, "temperature", -999, true);
        Scribe_Defs.Look(ref baseTerrain, "baseTerrain");

        Scribe_Defs.Look(ref originalTerrain, "originalTerrain");
    }

    public void setTerrain(TerrainType type) {
        var thisTerrain = currentTerrain;
        //Make sure it hasn't been made a floor or a floor hasn't been removed.

        if (!TerrainTagUtil.TerrainHasModExtension.Contains(thisTerrain)) {
            baseTerrain = thisTerrain;
        }
        else if (baseTerrain != thisTerrain && !TerrainTagUtil.TerrainHasModExtension.Contains(baseTerrain)) {
            baseTerrain = thisTerrain;
        }

        if (weather == null) {
            return;
        }

        switch (type) {
            //change the terrain
            case TerrainType.Frozen:
                setFrozenTerrain(true);
                break;
            case TerrainType.Dry:
            case TerrainType.Wet:
                setWetTerrain();
                break;
            case TerrainType.Thaw when isFrozen:
                howWet = 1;
                setWetTerrain();
                isFrozen = false;
                break;
            case TerrainType.Thaw:
                setFrozenTerrain(false);
                break;
            case TerrainType.Flooded:
                setFloodedTerrain();
                break;
            case TerrainType.Tide:
                setTidesTerrain();
                break;
        }

        overrideType = "";
    }

    public void DoCellSteadyEffects() {
        if (howWetPlants < 0) {
            howWetPlants = 0;
        }
    }

    private void setWetTerrain() {
        if (!Settings.showRain) {
            return;
        }

        var thisTerrain = currentTerrain;

        if (weather.wetTerrain != null && thisTerrain != weather.wetTerrain && howWet > weather.wetAt) {
            changeTerrain(weather.wetTerrain);
            isWet = true;
            rainSpawns();
        }
        else {
            switch (howWet) {
                case 0 when thisTerrain != baseTerrain && isWet && !isFlooded:
                    changeTerrain(baseTerrain);
                    isWet = false;
                    howWet = -1;
                    break;
                case -1 when weather.dryTerrain != null && !isFlooded: {
                    if (thisTerrain == weather.dryTerrain && baseTerrain == weather.dryTerrain) {
                        return;
                    }

                    isWet = false;
                    baseTerrain = weather.dryTerrain;
                    changeTerrain(weather.dryTerrain);
                    break;
                }
            }
        }
    }

    private void setFrozenTerrain(bool frozen) {
        if (frozen) {
            if (!(temperature < 0) || !(temperature < weather.freezeAt) || weather.freezeTerrain == null) {
                return;
            }

            var thisTerrain = currentTerrain;

            if (isFlooded && weather.freezeTerrain != thisTerrain) {
                if (TerrainTagUtil.TerrainHasModExtension.Contains(thisTerrain)) {
                    var curWeather = thisTerrain.GetModExtension<TerrainWeatherReactions>();
                    changeTerrain(curWeather.freezeTerrain);
                }
            }
            else if (!isFrozen) {
                changeTerrain(weather.freezeTerrain);
            }

            isFrozen = true;
            isThawed = false;
            return;
        }

        if (isThawed) {
            return;
        }

        isFrozen = false;
        isThawed = true;
        changeTerrain(baseTerrain);
    }

    private void setFloodedTerrain() {
        if (!Settings.showRain || !Settings.doTides) {
            return;
        }

        var thisTerrain = currentTerrain;
        var floodTerrain = weather.floodTerrain;
        if (isFrozen) {
            var currWeather = thisTerrain.GetModExtension<TerrainWeatherReactions>();
            var frozenTerrain = currWeather.freezeTerrain;
            if (frozenTerrain != null) {
                changeTerrain(frozenTerrain);
            }
        }
        else if (overrideType == "dry") {
            howWetPlants = 100;
            floodTerrain = baseTerrain;
            changeTerrain(floodTerrain);
        }
        else if (floodTerrain != null && thisTerrain != floodTerrain) {
            changeTerrain(floodTerrain);

            isFlooded = true;
            if (!floodTerrain.IsWater) {
                isFlooded = false;
                howWetPlants = 100;
                leaveLoot();
            }
            else {
                clearLoot();
            }
        }
    }

    private void setTidesTerrain() {
        if (!Settings.doTides) {
            return;
        }

        var thisTerrain = currentTerrain;
        switch (overrideType) {
            case "dry":
                changeTerrain(baseTerrain);
                break;
            case "wet":
                changeTerrain(weather.tideTerrain);
                break;
            default: {
                changeTerrain(thisTerrain != baseTerrain ? baseTerrain : weather.tideTerrain);
                break;
            }
        }

        if (weather.tideTerrain == null) {
            return;
        }

        if (TerrainTagUtil.TKKN_Wet.Contains(thisTerrain)) {
            clearLoot();
        }
        else {
            leaveLoot();
        }
    }

    public void doFrostOverlay(string action) {
        if (!location.InBounds(map)) {
            return;
        }

        //KEEPING TO REMOVE OLD WAY OF DOING FROST
        var overlayIce = (from t in location.GetThingList(map)
            where t.def == ThingDefOf.TKKN_IceOverlay
            select t).FirstOrDefault();
        if (overlayIce == null) {
            return;
        }

        if (isFrozen) {
            isMelt = true;
        }

        overlayIce.Destroy();
    }


    public void Unpack() {
        if (!Settings.doDirtPath) {
            return;
        }

        if (howPacked > 0) {
            howPacked--;
        }

        if (howPacked <= packAt / 2) {
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
    /// (You can, and we want the user to do this) it will change back to a packed path after being stepped on
    /// </summary>
    public void DoPack() {
        if (!Settings.doDirtPath) {
            return;
        }

        if (!TerrainTagUtil.canBePacked.Contains(baseTerrain)) {
            return;
        }
        
        howPacked++;
        

        //don't pack if there's a growing zone.
        if (map.zoneManager.ZoneAt(location) is Zone_Growing) {
            return;
        }


        if (howPacked > packAt) {
            if (baseTerrain == RimWorld.TerrainDefOf.Soil) {
                changeTerrain(TerrainDefOf.TKKN_DirtPath);
                baseTerrain = TerrainDefOf.TKKN_DirtPath;
            }
            else if (baseTerrain == RimWorld.TerrainDefOf.Sand) {
                changeTerrain(TerrainDefOf.TKKN_SandPath);
                baseTerrain = TerrainDefOf.TKKN_SandPath;
            }
            else if (baseTerrain.smoothedTerrain != null && howPacked > packAt * 10) {
                changeTerrain(baseTerrain.smoothedTerrain);
                baseTerrain = baseTerrain.smoothedTerrain;
            }
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
                GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.TKKN_crab), location, map);
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
                List<Thing> allowed=ThingSetMakerDefOf.TKKN_TidalLoot.root.Generate();
                
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
        if (!location.IsValid) {
            return;
        }

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