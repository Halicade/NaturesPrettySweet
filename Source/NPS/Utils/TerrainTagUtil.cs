using System.Collections.Generic;
using Verse;

namespace TKKN_NPS;

public static class TerrainTagUtil
{
    public static readonly HashSet<TerrainDef> TKKN_Wet = [];
    public static readonly HashSet<TerrainDef> TKKN_Swim = [];
    public static readonly HashSet<TerrainDef> Lava = [];
    public static readonly HashSet<TerrainDef> TerrainHasModExtension = [];
    public static readonly HashSet<TerrainDef> HoldsFrost = [];
    public static readonly HashSet<TerrainDef> CanBePacked = [];
    public static readonly HashSet<TerrainDef> SaltTerrains = [];
    public static readonly Dictionary<TerrainDef, float> AmbientTempReaction = [];
    public static readonly Dictionary<TerrainDef, TerrainDef> DryTerrain = [];
    public static readonly Dictionary<TerrainDef, TerrainDef> WetTerrain = [];
    public static readonly Dictionary<TerrainDef, int> TerrainWetAt = [];
    public static readonly Dictionary<TerrainDef, freezeTerrain> FreezeTerrain = [];
    public static readonly Dictionary<TerrainDef, TerrainDef> FloodTerrain = [];


    public static void IntializeTerrainTags() {
        List<TerrainDef> allTerrains = DefDatabase<TerrainDef>.AllDefsListForReading;
        CanBePacked.Add(RimWorld.TerrainDefOf.Soil);
        CanBePacked.Add(RimWorld.TerrainDefOf.Sand);
        CanBePacked.Add(TerrainDefOf.TKKN_DirtPath);
        CanBePacked.Add(TerrainDefOf.TKKN_SandPath);

        foreach (var terrain in allTerrains) {
            if (terrain.HasTag("TKKN_Wet")) {
                TKKN_Wet.Add(terrain);
            }

            if (terrain.HasTag("TKKN_Swim")) {
                TKKN_Swim.Add(terrain);
            }

            if (terrain.HasTag("Lava") || terrain.HasTag("TKKN_Lava")) {
                Lava.Add(terrain);
            }

            if (terrain.smoothedTerrain != null) {
                CanBePacked.Add(terrain);
            }
            TerrainWetAt.SetOrAdd(terrain, 0);
            

            var weatherExtension = terrain.GetModExtension<TerrainWeatherReactions>();
            if (weatherExtension != null) {
                TerrainHasModExtension.Add(terrain);

                if (weatherExtension.temperatureAdjust != 0) {
                    AmbientTempReaction.Add(terrain, weatherExtension.temperatureAdjust);
                }

                if (terrain.holdSnowOrSand && weatherExtension.holdFrost) {
                    HoldsFrost.Add(terrain);
                }

                if (weatherExtension.wetTerrain != null) {
                    WetTerrain.Add(terrain, weatherExtension.wetTerrain);
                    TerrainWetAt.SetOrAdd(terrain, weatherExtension.wetAt);
                }

                if (weatherExtension.freezeTerrain?.terrain != null) {
                    FreezeTerrain.Add(terrain, weatherExtension.freezeTerrain);
                }

                if (weatherExtension.floodTerrain != null) {
                    FloodTerrain.Add(terrain, weatherExtension.floodTerrain);
                }

                if (weatherExtension.isSalty) {
                    SaltTerrains.Add(terrain);
                }
            }
        }
    }
}