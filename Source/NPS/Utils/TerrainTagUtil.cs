using System.Collections.Generic;
using Verse;

namespace TKKN_NPS;

public static class TerrainTagUtil
{
    public static readonly HashSet<TerrainDef> TKKN_Wet = [];
    public static readonly HashSet<TerrainDef> TKKN_Swim = [];
    public static readonly HashSet<TerrainDef> Lava = [];
    public static readonly HashSet<TerrainDef> TKKN_SwimOrLava = [];
    public static readonly HashSet<TerrainDef> TerrainHasModExtension = [];
    public static readonly HashSet<TerrainDef> HoldsFrost = [];
    public static readonly HashSet<TerrainDef> canBePacked = [];
    public static readonly Dictionary<TerrainDef, float> AmbientTempReaction = [];
    public static readonly Dictionary<TerrainDef, TerrainDef> DryTerrain = [];
    public static readonly Dictionary<TerrainDef, TerrainDef> WetTerrain = [];
    


    public static void IntializeTerrainTags() {
        List<TerrainDef> allTerrains = DefDatabase<TerrainDef>.AllDefsListForReading;
        canBePacked.Add(RimWorld.TerrainDefOf.Soil);
        canBePacked.Add(RimWorld.TerrainDefOf.Sand);
        
        foreach (var terrain in allTerrains) {
            if (terrain.HasTag("TKKN_Wet")) {
                TKKN_Wet.Add(terrain);
            }

            if (terrain.HasTag("TKKN_Swim")) {
                TKKN_Swim.Add(terrain);
                TKKN_SwimOrLava.Add(terrain);
            }

            if (terrain.HasTag("Lava")) {
                Lava.Add(terrain);
                TKKN_SwimOrLava.Add(terrain);
            }

            if (terrain.smoothedTerrain != null) {
                canBePacked.Add(terrain);
            }

            var weatherExtension = terrain.GetModExtension<TerrainWeatherReactions>();
            if (weatherExtension != null) {
                TerrainHasModExtension.Add(terrain);

                if (weatherExtension.temperatureAdjust != 0) {
                    AmbientTempReaction.Add(terrain, weatherExtension.temperatureAdjust);
                }

                if (weatherExtension.holdFrost) {
                    HoldsFrost.Add(terrain);
                }

                if (weatherExtension.dryTerrain != null) {
                    DryTerrain.Add(terrain, weatherExtension.dryTerrain);
                }

                if (weatherExtension.wetTerrain != null) {
                    WetTerrain.Add(terrain, weatherExtension.wetTerrain);
                }
                
                
            }
        }
    }
}