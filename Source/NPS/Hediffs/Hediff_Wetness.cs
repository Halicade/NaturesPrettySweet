using RimWorld;
using Verse;

namespace TKKN_NPS;

public class Hediff_Wetness : HediffWithComps
{
    private Map map;

    private IntVec3 position;
    private int timeDrying;

    public override void ExposeData() {  
        base.ExposeData();
        Scribe_Values.Look(ref timeDrying, "timeDrying");
    }

    public override void PreRemoved() {
        base.PreRemoved();
        pawn.needs?.mood?.thoughts?.memories?.RemoveMemoriesOfDef(ThoughtDefOf.SoakingWet);
    }

    public override void Tick() {
        position = pawn.Position;

        if (!position.IsValid) {
            Severity = 0;
            return;
        }

        map = pawn.MapHeld;
        if (map == null || !position.InBounds(map)) {
            return;
        }

        var wetness = wetnessRate();
        if (wetness > 0) {
            Severity += wetness / 1000;

            if (!(Severity > .62) || ageTicks % 1000 != 0) {
                return;
            }

            if(FilthMaker.TryMakeFilth(position, map, ThingDefOf.TKKN_FilthPuddle))
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
        else {
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

        // This is such a niche case, and pawns dry relatively quick.
        // If it's a performance issue I can remove it but it doesn't seem like it should be
        foreach (var c in GenAdj.CellsAdjacentCardinal(pawn))
        {
            if (!c.InBounds(map) || !c.IsValid)
            {
                continue;
            }

            foreach (var thing in c.GetThingList(map))
            {
                if (ThingUtil.heatThings.TryGetValue(thing.def, out var heat)) {
                    rate -= heat;
                }
            }
        }
        return rate - (float)timeDrying / 250;
    }
}