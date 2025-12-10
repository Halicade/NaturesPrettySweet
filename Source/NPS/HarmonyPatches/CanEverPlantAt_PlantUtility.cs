using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TKKN_NPS;

//[HarmonyPatch(typeof(PlantUtility), nameof(PlantUtility.CanEverPlantAt), typeof(ThingDef), typeof(IntVec3), typeof(Map),typeof(bool), typeof(bool))]
public static class CanEverPlantAt_PlantUtility
{
    public static void Postfix(ThingDef plantDef, IntVec3 c, Map map, ref bool __result) {
        if (!__result) {
            return;
        }

        //verify that the plant can grow on this terrain.
        if (!PlantReactionUtil.AllowedTerrains.TryGetValue(plantDef, out List<TerrainDef> allowed)) {
            return;
        }

        var terrain = c.GetTerrain(map);
        if (!allowed.Contains(terrain)) {
            __result = false;
        }
    }
}