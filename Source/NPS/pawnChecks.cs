using RimWorld;
using UnityEngine;
using Verse;

namespace TKKN_NPS;

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
        if (!EffectSettings.allowPawnsDrowning && !drowningCheck) return;

        if (!TerrainTagUtil.TKKN_Wet.Contains(terrain) || !pawn.health.Downed) {
            return;
        }

        //ignore if they're mostly vacuum resistant
        if (ModsConfig.OdysseyActive &&
            pawn.GetStatValue(StatDefOf.VacuumResistance, cacheStaleAfterTicks: 60) > 0.95)
            return;

        if (pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.TKKN_Drowning) != null)
            return;
            
        var hediff = HediffMaker.MakeHediff(HediffDefOf.TKKN_Drowning, pawn);
        hediff.Severity = 0.001f;
        pawn.health.AddHediff(hediff);
            
            
        string text = "TKKN_NPS_DrowningText".Translate();
        Messages.Message(text, MessageTypeDefOf.NegativeHealthEvent);
    }

    private static void makeWet(Pawn pawn, TerrainDef currentTerrain, bool isRaining, Map map) {
        if (!EffectSettings.allowPawnsToGetWet) {
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

        if (HarmonyWeatherEffects.RimBrellasActive && HarmonyWeatherEffects.HasUmbrella(pawn)) {
            return;
        }

        var hediff = HediffMaker.MakeHediff(hediffDef, pawn);
        hediff.Severity = 0;
        pawn.health.AddHediff(hediff);
    }


    private static void makePaths(Pawn pawn, Watcher watcher, Map map) {
        if (!EffectSettings.doDirtPath) {
            return;
        }

        if (!pawn.Position.InBounds(map) || !pawn.pather.MovingNow) {
            return;
        }

        if (!watcher.cellWeatherAffects.TryGetValue(pawn.Position, out var cell))
            return;

        if (EffectSettings.doWalkThroughSnow && watcher.outdoorTemp < 3) {
            watcher.frostGridComponent.AddDepth(cell, -.005f);
            map.snowGrid.AddDepth(pawn.Position, -.005f);
        }

        //pack down the soil only if the pawn is moving AND is in our colony
        if (pawn.IsColonist) {
            cell.DoPack();
        }
    }

    private static readonly Vector3 BreathOffset = new(0f, 0f, -0.04f);

    private static void makeBreath(Pawn pawn, Map map, bool doBreathCheck) {
        if (!doBreathCheck || !EffectSettings.doColdBreath)
            return;
        if (pawn.Position.GetTemperature(map) >= 3f ||
            (ModsConfig.OdysseyActive &&
             pawn.GetStatValue(StatDefOf.VacuumResistance, cacheStaleAfterTicks: 60) > 0.95)) {
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