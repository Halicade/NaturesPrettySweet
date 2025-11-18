using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TKKN_NPS;

//swap out plant graphics based on seasonal effects
[HarmonyPatch(typeof(Plant), nameof(Plant.Graphic), MethodType.Getter)]
public static class Plant_Graphic
{
    private static Map cachedMap;
    private static int cachedTicks;
    private static Watcher watcher;
    private static Vector2 location;
    private static Season season;

    public static void Postfix(Plant __instance, ref Graphic __result) {
        if (cachedMap != __instance.Map) {
            cachedMap = __instance.Map;
            watcher = cachedMap.GetComponent<Watcher>();
        }

        if (watcher == null) {
            return;
        }

        if (!PlantReactionUtil.HasGraphic.TryGetValue(__instance.def, out ThingWeatherReaction graphics)) {
            return;
        }


        //get flowering or drought graphic if it's over 70
        if (__instance.AmbientTemperature > 21) {
            if (watcher.cellWeatherAffects.TryGetValue(__instance.Position, out var cell)) {
                if (cachedTicks != Find.TickManager.TicksAbs) {
                    cachedTicks = Find.TickManager.TicksAbs;
                    location = Find.WorldGrid.LongLatOf(__instance.MapHeld.Tile);
                    season = GenDate.Season(Find.TickManager.TicksAbs, location);
                }

                if (cell.howWetPlants > 60 && cachedMap.weatherManager.RainRate <= .001f || season == Season.Spring) {
                    if (!graphics.floweringGraphicPath.NullOrEmpty()) {
                        __result = graphics.floweringGraphic;
                        return;
                    }
                }

                if (cell.howWetPlants < 20) {
                    if (!graphics.droughtGraphicPath.NullOrEmpty()) {
                        __result = graphics.droughtGraphic;
                        return;
                    }
                }
                else if (__instance.def.plant.leaflessGraphic != null && cell.howWetPlants < 20) {
                    __result = __instance.def.plant.leaflessGraphic;
                    return;
                }
            }
        }

        if (Settings.showCold) {
            //get snow graphic
            if (cachedMap.snowGrid.GetDepth(__instance.Position) >= 0.5f) {
                if (!graphics.snowGraphicPath.NullOrEmpty()) {
                    __result = graphics.snowGraphic;
                    return;
                }
            }

            if (watcher.frostGridComponent.GetDepth(__instance.Position) >= 0.6f) {
                if (!graphics.frostGraphicPath.NullOrEmpty()) {
                    __result = graphics.frostGraphic;
                    return;
                }
            }

            //if it's leafless
            if (__instance.def.plant.leaflessGraphic == __result) {
                if (!graphics.frostLeaflessGraphicPath.NullOrEmpty()) {
                    __result = graphics.frostLeaflessGraphic;
                    return;
                }
            } //TODO move this somewhere else probably
            else if (__instance.def.blockWind) {
                //make it so snow doesn't fall under the tree until it's leafless.
                cachedMap.snowGrid.AddDepth(__instance.Position, -.05f);
            }
        }
    }
}