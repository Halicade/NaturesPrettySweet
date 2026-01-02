using System.Collections.Generic;
using Verse;

namespace TKKN_NPS;

public class ThingUtil
{
    public static readonly Dictionary<ThingDef, float> heatThings = [];

    public static void InitializeThingUtil() {
        List<ThingDef> thingList = DefDatabase<ThingDef>.AllDefsListForReading;

        foreach (ThingDef thingDef in thingList) {
            var heater = thingDef.GetCompProperties<CompProperties_HeatPusher>();

            if (heater == null) {
                continue;
            }

            if (heater.heatPerSecond != 0) {
                heatThings.Add(thingDef, heater.heatPerSecond / 400);
            }
        }
    }
}