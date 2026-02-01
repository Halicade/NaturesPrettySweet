using Verse;

namespace TKKN_NPS;

public class Hediff_Drowning : HediffWithComps
{
    private Map map;

    private IntVec3 position;

    private float gearWeight;
    private int gearWeightTick;
    private int currentTicks;

    public override void PostAdd(DamageInfo? dinfo) {
        currentTicks = 60;
    }

    public override void Tick() {
        position = pawn.Position;

        if (!position.IsValid) {
            Severity = 0;
            return;
        }

        if (!pawn.Downed || pawn.CarriedBy != null) {
            Severity -= 0.001f;
            return;
        }

        map = pawn.MapHeld;
        if (map == null || !position.InBounds(map)) {
            Severity = 0;
            return;
        }

        TerrainDef terrain = position.GetTerrain(map);

        if (!TerrainTagUtil.TKKN_Wet.Contains(terrain)) {
            Severity -= 0.001f;
            return;
        }

        float severityChange = 0f;

        if (!pawn.health.capacities.CanBeAwake) {
            severityChange = 0.0001f;
        }

        severityChange += calculateApparelDamage();


        Severity += severityChange;
    }

    private float calculateApparelDamage() {
        currentTicks++;
        
        if (gearWeightTick > currentTicks - 60) {
            return gearWeight;
        }

        float weight = 1f;
        foreach (var specificApparel in pawn.apparel.WornApparel) {
            weight += (float)specificApparel.HitPoints / 10000;
        }

        gearWeightTick = currentTicks;
        gearWeight = weight / 5000;
        return gearWeight;
    }
}