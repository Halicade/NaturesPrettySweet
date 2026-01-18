using RimWorld;
using TKKN_NPS;
using UnityEngine;
using Verse;
using HediffDefOf = TKKN_NPS.HediffDefOf;
using TerrainDefOf = TKKN_NPS.TerrainDefOf;
using ThingDefOf = TKKN_NPS.ThingDefOf;

namespace NPS;

public static class PawnChecks
{
    public static void checks(Pawn pawn, Map map, Watcher watcher, bool isRaining, int ticks) {
        if (!pawn.Spawned || pawn.Dead) {
            return;
        }

        bool doExtraChecks = (pawn.thingIDNumber + ticks) % 300 == 0;

        TerrainDef terrain = pawn.Position.GetTerrain(pawn.MapHeld);

        makePaths(pawn, watcher, map);
        makeBreath(pawn, map, doExtraChecks);
        makeWet(pawn, terrain, isRaining, map);
        drowningCheck(pawn, terrain, doExtraChecks);
        springCheck(pawn, terrain, doExtraChecks);
    }

    private static void springCheck(Pawn pawn, TerrainDef terrain, bool terrainChecks) {
        if (pawn.needs == null || !terrainChecks) {
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


            //Remove heatstroke if pawn is in cold spring
            Hediff heatstroke = pawn.health.hediffSet.GetFirstHediffOfDef(RimWorld.HediffDefOf.Heatstroke);
            if (heatstroke != null) {
                pawn.health.RemoveHediff(heatstroke);
            }


            var hediffDef = HediffDefOf.TKKN_coldspring_chill_out;
            if (pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef) != null) {
                return;
            }

            var hediff = HediffMaker.MakeHediff(hediffDef, pawn);
            pawn.health.AddHediff(hediff);
        }
    }

    private static void drowningCheck(Pawn pawn, TerrainDef terrain, bool drowningCheck) {
        //drowning == immobile and in water
        if (!Settings.allowPawnsDrowning && !drowningCheck) return;

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

        if (HarmonyMain.RimBrellasActive && HarmonyMain.HasUmbrella(pawn)) {
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

        if (Settings.showCold && watcher.outdoorTemp < 3) {
            watcher.frostGridComponent.AddDepth(pawn.Position, -.01f);
            map.snowGrid.AddDepth(pawn.Position, -.01f);
        }

        //pack down the soil only if the pawn is moving AND is in our colony
        if (pawn.IsColonist &&
            watcher.cellWeatherAffects.TryGetValue(pawn.Position, out var cell)) {
            cell.DoPack();
        }
    }

    private static readonly Vector3 BreathOffset = new(0f, 0f, -0.04f);

    private static void makeBreath(Pawn pawn, Map map, bool doBreathCheck) {
        if (!doBreathCheck && Settings.showCold &&
            pawn.Position.GetTemperature(map) < 3f &&
            (!ModsConfig.OdysseyActive ||
             pawn.GetStatValue(StatDefOf.VacuumResistance, cacheStaleAfterTicks: 60) < 1.0)) {
            return;
        }

        var head = pawn.Drawer.DrawPos + pawn.Drawer.renderer.BaseHeadOffsetAt(pawn.Rotation) +
                   pawn.Rotation.FacingCell.ToVector3() * 0.21f + BreathOffset;

        MoteThrown moteThrown = (MoteThrown)ThingMaker.MakeThing(ThingDefOf.TKKN_Mote_ColdBreath);
        moteThrown.Scale = Rand.Range(.5f, 1.5f);
        moteThrown.rotationRate = Rand.Range(-30f, 30f);
        moteThrown.exactPosition = head;
        moteThrown.SetVelocity(Rand.Range(20, 30), Rand.Range(0.5f, 0.7f));
        GenSpawn.Spawn(moteThrown, head.ToIntVec3(), map);
    }
}