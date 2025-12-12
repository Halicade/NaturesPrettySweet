using HarmonyLib;
using RimWorld;
using Verse;

namespace TKKN_NPS;

//[HarmonyPatch(typeof(Pawn), "Tick")]
public static class Pawn_Tick
{
    public static void Postfix(Pawn __instance, Map map, Watcher watcher, bool isRaining) {
        if (!__instance.Spawned || __instance.Dead) {
            return;
        }

        var terrain = __instance.Position.GetTerrain(__instance.MapHeld);
        makePaths(__instance, watcher, map);
        makeBreath(__instance, watcher, map);
        makeWet(__instance, terrain, isRaining, map);
        drowningCheck(__instance, terrain);
        springCheck(__instance, terrain);
    }

    private static void springCheck(Pawn pawn, TerrainDef terrain) {
        if (pawn.needs == null) {
            return;
        }

        if (terrain == TerrainDefOf.TKKN_HotSpringsWater) {
            if (pawn.needs.comfort != null) {
                pawn.needs.comfort.lastComfortUseTick--;
            }

            var hediffDef = HediffDefOf.TKKN_hotspring_chill_out;
            if (pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef) != null) {
                return;
            }

            var hediff = HediffMaker.MakeHediff(hediffDef, pawn);
            pawn.health.AddHediff(hediff);
        }
        else if (terrain == TerrainDefOf.TKKN_ColdSpringsWater) {
            pawn.needs.rest?.TickResting(.05f);

            if (Find.TickManager.TicksAbs % 300 == 0) {
                //Remove heatstroke if pawn is in cold spring
                Hediff heatstroke = pawn.health.hediffSet.GetFirstHediffOfDef(RimWorld.HediffDefOf.Heatstroke);
                if (heatstroke != null) {
                    pawn.health.RemoveHediff(heatstroke);
                }
            }

            var hediffDef = HediffDefOf.TKKN_coldspring_chill_out;
            if (pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef) != null) {
                return;
            }

            var hediff = HediffMaker.MakeHediff(hediffDef, pawn);
            pawn.health.AddHediff(hediff);
        }
    }

    private static void drowningCheck(Pawn pawn, TerrainDef terrain) {
        //drowning == immobile and in water
        if (!Settings.allowPawnsDrowning) return;

        if (pawn.health.Downed && TerrainTagUtil.TKKN_Wet.Contains(terrain)) {
            var damage = .0005f;
            //if they're awake, take less damage
            if (!pawn.health.capacities.CanBeAwake) {
                if (TerrainTagUtil.TKKN_Swim.Contains(terrain)) {
                    damage = .0001f;
                }
                else {
                    return;
                }
            }

            //heavier clothing hurts them more
            var apparel = pawn.apparel.WornApparel;
            var weight = 0f;
            foreach (var apparel1 in apparel) {
                weight += (float)apparel1.HitPoints / 10000;
            }

            damage += weight / 5000;
            HealthUtility.AdjustSeverity(pawn, HediffDefOf.TKKN_Drowning, damage);

            var hediffDef = HediffDefOf.TKKN_Drowning;
            if (pawn.Faction is not { IsPlayer: true } ||
                pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef) != null) {
                return;
            }

            string text = "TKKN_NPS_DrowningText".Translate();
            Messages.Message(text, MessageTypeDefOf.NeutralEvent);
            return;
        }

        var drowning = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.TKKN_Drowning);
        if (drowning != null) {
            pawn.health.RemoveHediff(drowning);
        }
    }

    private static void makeWet(Pawn pawn, TerrainDef currentTerrain, bool isRaining, Map map) {
        if (!Settings.allowPawnsToGetWet) {
            return;
        }

        var hediffDef = HediffDefOf.TKKN_Wetness;
        if (pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef) != null) {
            return;
        }

        var c = pawn.Position;
        if (!c.IsValid) {
            return;
        }

        var isWet = false;
        if (isRaining) {
            var roofed = map.roofGrid.Roofed(c);
            if (!roofed) {
                isWet = true;
            }
        }
        else {
            if (TerrainTagUtil.TKKN_Wet.Contains(currentTerrain)) {
                isWet = true;
            }
        }

        if (!isWet) {
            return;
        }

        if (HarmonyMain.RimBrellasActive && (bool)HarmonyMain.HasUmbrella.Invoke(pawn, [pawn])) {
            return;
        }

        var hediff = HediffMaker.MakeHediff(hediffDef, pawn);
        hediff.Severity = 0;
        pawn.health.AddHediff(hediff);
    }


    private static void makePaths(Pawn pawn, Watcher watcher, Map map) {
        if (!Settings.doDirtPath) {
            return;
        }

        if (!pawn.Position.InBounds(map) || !pawn.pather.MovingNow) {
            return;
        }

        //damage plants and remove snow/frost where they are. This will hopefully generate paths as pawns walk :)
        if (Settings.showCold && watcher.CheckIfCold(pawn.Position)) {
            watcher.frostGridComponent.AddDepth(pawn.Position, -.05f);
            map.snowGrid.AddDepth(pawn.Position, -.05f);
        }

        //pack down the soil only if the pawn is moving AND is in our colony
        if (pawn.IsColonist &&
            watcher.cellWeatherAffects.TryGetValue(pawn.Position, out var cell)) {
            cell.DoPack();
        }
    }

    private static void makeBreath(Pawn pawn, Watcher watcher, Map map) {
        if (!Settings.showCold) {
            return;
        }

        if (Find.TickManager.TicksGame % 200 != 0) {
            return;
        }

        var isCold = watcher.CheckIfCold(pawn.Position);
        if (!isCold) {
            return;
        }

        var head = pawn.Position;
        head.z += 1;
        if (!head.ShouldSpawnMotesAt(map) || map.moteCounter.SaturatedLowPriority) {
            return;
        }

        var moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDefOf.TKKN_Mote_ColdBreath);
        moteThrown.airTimeLeft = 99999f;
        moteThrown.Scale = Rand.Range(.5f, 1.5f);
        moteThrown.rotationRate = Rand.Range(-30f, 30f);
        moteThrown.exactPosition = head.ToVector3();
        moteThrown.SetVelocity(Rand.Range(20, 30), Rand.Range(0.5f, 0.7f));
        GenSpawn.Spawn(moteThrown, head, map);
    }
}