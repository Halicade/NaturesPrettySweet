using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TKKN_NPS;

//swap out plant graphics based on seasonal effects
//[HarmonyPatch(typeof(Plant), nameof(Plant.Graphic), MethodType.Getter)]
public static class Plant_Graphic
{
    private static Map cachedMap;
    private static Watcher watcher;
    private static Season season;

    //TODO think about immature plant growth levels
    
    //Giving higher priority to be before any mods that may modify texture
    [HarmonyPriority(Priority.High)]
    public static void Postfix(Plant __instance, ref Graphic __result) {
        if (cachedMap != __instance.Map) {
            cachedMap = __instance.Map;
            watcher = cachedMap.GetComponent<Watcher>();
            season = watcher.season;
        }

        if (watcher?.dontRunAnything != false) {
            return;
        }

        var graphics = __instance.def.GetModExtension<ThingWeatherReaction>();
        if (graphics?.hasGraphic != true) {
            return;
        }

        if (Find.TickManager.TicksAbs % 60000 == 0) {
            //Check every day for if the season changed
            season = watcher.season;
        }


        //get flowering or drought graphic if it's over 70
        if (__instance.AmbientTemperature > 21) {
            if (watcher.cellWeatherAffects.TryGetValue(__instance.Position, out var cell)) {
                if ((cell.howWetPlants > 60 && cachedMap.weatherManager.RainRate <= .001f || season == Season.Spring)
                    && graphics.floweringGraphicPath != null) {
                    __result = graphics.floweringGraphic;
                    return;
                }

                if (cell.howWetPlants < 20) {
                    if (graphics.droughtGraphicPath != null) {
                        __result = graphics.droughtGraphic;
                        return;
                    }

                    if (__instance.def.plant.leaflessGraphic != null) {
                        __result = __instance.def.plant.leaflessGraphic;
                        return;
                    }
                }
            }

            //There won't be snow if it's this warm so no need to check the below
            return;
        }

        if (!EffectSettings.showFrostGrid) return;

        if (graphics.frostGraphicPath != null && watcher.frostGridComponent.GetDepth(__instance.Position) >= 0.6f) {
            __result = graphics.frostGraphic;
            return;
        }

        //if it's leafless
        if (__instance.def.plant.leaflessGraphic == __result) {
            if (graphics.frostLeaflessGraphicPath != null) {
                __result = graphics.frostLeaflessGraphic;
                return;
            }
        } //TODO below throws errors at game load if there's snow. Something about editing snow when the game isn't loaded 
        /*else if (__instance.def.blockWind) {
            //make it so snow doesn't fall under the tree until it's leafless.
            cachedMap.snowGrid.AddDepth(__instance.Position, -.05f);
        }*/
    }
}