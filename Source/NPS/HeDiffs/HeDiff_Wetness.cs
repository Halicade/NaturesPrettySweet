using RimWorld;
using Verse;

namespace TKKN_NPS;

public class Hediff_Wetness : HediffWithComps
{
    private Map map;

    private IntVec3 position;
    private int timeDrying;
    private float wetnessLevel;

    public override void ExposeData() {
        base.ExposeData();
        Scribe_Values.Look(ref wetnessLevel, "wetnessLevel");
        Scribe_Values.Look(ref timeDrying, "timeDrying");
    }

    public override void Tick() {
        base.Tick();
        position = pawn.Position;

        if (!position.IsValid) {
            return;
        }

        map = pawn.MapHeld;
        if (map == null || !position.InBounds(map)) {
            return;
        }

        var wetness = wetnessRate();
        if (wetness > 0) {
            Severity += wetness / 1000;
            wetnessLevel += wetness;
            if (wetnessLevel < 0) {
                wetnessLevel = 0;
            }

            if (!(Severity > .62) || ageTicks % 1000 != 0) {
                return;
            }

            FilthMaker.TryMakeFilth(position, map, ThingDefOf.TKKN_FilthPuddle);
            Severity -= .3f;
        }
        else {
            Severity += wetness / 1000;
        }
    }

    private float wetnessRate() {
        var rate = 0f;
        //check if the pawn is in water
        var terrain = position.GetTerrain(map);
        if (terrain != null && TerrainTagUtil.TKKN_Wet.Contains(terrain)) {
            //deep water gets them soaked.
            if (TerrainTagUtil.TKKN_Swim.Contains(terrain)) {
                if (Severity < .65f) {
                    Severity = .65f;
                }

                return 0.3f;
            }

            rate = .05f;
        }


        //check if the pawn is wet from the weather
        if (!map.roofGrid.Roofed(position)) {
            var weatherManager = map.weatherManager.curWeather;
            if (weatherManager.rainRate > .001f) {
                rate = weatherManager.rainRate / 10;
            }
            else if (weatherManager.snowRate > .001f) {
                rate = weatherManager.snowRate / 100;
            }
        }

        if (rate != 0f) {
            timeDrying = 0;
            return rate;
        }

        timeDrying++;

        //dry the pawn.
        var ambientTemp = pawn.AmbientTemperature;
        if (ambientTemp > 0) {
            rate -= ambientTemp / 200;
        }

        /*
         This is such a niche case it really isn't worth calculating
        //check if the pawn is near a heat source
        foreach (var c in GenAdj.CellsAdjacentCardinal(pawn))
        {
            if (!c.InBounds(map) || !c.IsValid)
            {
                continue;
            }

            var things = c.GetThingList(map);
            foreach (var thing in things)
            {
                var heater = thing.TryGetComp<CompHeatPusher>();
                if (heater != null)
                {
                    rate -= heater.Props.heatPerSecond / 5000;
                }
            }
        }*/
        return rate - (float)timeDrying / 250;
    }
}