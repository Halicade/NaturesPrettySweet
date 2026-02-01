using Verse;

namespace TKKN_NPS.GenSteps;

public class GenStep_LavaPathing : GenStep
{
    public override int SeedPart => 418947;

    //Makes regular lava deep lava if surrounded by other lava
    public override void Generate(Map map, GenStepParams parms) {
        //Not running if we replace the lava terrain
        if (BiomeSettings.lavaReplace)
            return;

        foreach (IntVec3 c in map.AllCells) {
            if (c.GetTerrain(map) != TerrainDefOf.TKKN_Lava)
                continue;

            if (c.OnEdge(map)) {
                map.terrainGrid.SetTerrain(c, TerrainDefOf.TKKN_LavaDeep);
                continue;
            }

            var edgeLava = false;
            //Make lava on the edge of the map deep lava so it can't be walked on
            //Need to check all 4 edges to see if any are out of bounds

            foreach (var lavaCheck in GenAdjFast.AdjacentCellsCardinal(c)) {
                TerrainDef lavaCheckTerrain = lavaCheck.GetTerrain(map);
                if (lavaCheckTerrain != TerrainDefOf.TKKN_Lava &&
                    lavaCheckTerrain != TerrainDefOf.TKKN_LavaDeep) {
                    edgeLava = true;
                    break;
                }
            }

            if (!edgeLava) {
                map.terrainGrid.SetTerrain(c, TerrainDefOf.TKKN_LavaDeep);
            }
        }
    }
}