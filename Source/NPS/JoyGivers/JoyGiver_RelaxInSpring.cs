using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TKKN_NPS;

public class JoyGiver_RelaxInSpring : JoyGiver
{
    // Using Skygazing as reference
    public override Job TryGiveJob(Pawn pawn) {
        var map = pawn.Map;

        List<GameCondition> currentConditions = map.gameConditionManager.ActiveConditions;

        bool canBeOutside = map.weatherManager.curWeather.rainRate == 0f
                            && JoyUtility.EnjoyableOutsideNow(pawn);
        if (canBeOutside) {
            for (int i = 0; i < currentConditions.Count; i++) {
                GameCondition condition = currentConditions[i];
                if (condition.CanApplyOnMap(map) && !condition.AllowEnjoyableOutsideNow(map)) {
                    canBeOutside = false;
                }
            }
        }

        if (CellFinder.TryRandomClosewalkCellNear(
                root: pawn.Position,
                map: map,
                radius: 45,
                result: out var result,
                extraValidator: Validator)) {
            return JobMaker.MakeJob(def.jobDef, result);
        }

        return null;


        bool Validator(IntVec3 c) {
            if (c.Fogged(map)) {
                return false;
            }

            if (!c.Roofed(map)) {
                if (!canBeOutside) {
                    return false;
                }
            }

            if (c.GetTemperature(map) < 10) {
                return false;
            }

            var springTerrain = c.GetTerrain(map);
            return springTerrain == TerrainDefOf.TKKN_HotSpringsWater ||
                   springTerrain == TerrainDefOf.TKKN_ColdSpringsWater;
        }
    }
}