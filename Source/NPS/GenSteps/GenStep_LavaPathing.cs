using Verse;

namespace TKKN_NPS.GenSteps;

public class GenStep_LavaPathing : GenStep
{
    public override int SeedPart => 418947;

    //Makes regular lava deep lava if surrounded by other lava
    public override void Generate(Map map, GenStepParams parms) {
        //Not running if we replace the lava terrain
        if (Settings.lavaReplace)
            return;
        
        foreach (IntVec3 c in map.cellsInRandomOrder.GetAll()) {
            if (c.GetTerrain(map) != TerrainDefOf.TKKN_Lava) 
                continue;
            
            var edgeLava = false;
            var num = GenRadial.NumCellsInRadius(1);
            for (var i = 0; i < num; i++) {
                var lavaCheck = c + GenRadial.RadialPattern[i];
                if (!lavaCheck.InBounds(map)) {
                    continue;
                }

                var lavaCheckTerrain = lavaCheck.GetTerrain(map);
                if (lavaCheckTerrain == TerrainDefOf.TKKN_Lava ||
                    lavaCheckTerrain == TerrainDefOf.TKKN_LavaDeep) {
                    edgeLava = true;
                }
            }

            if (!edgeLava) {
                map.terrainGrid.SetTerrain(c, TerrainDefOf.TKKN_LavaDeep);
            }
        }
    }
}