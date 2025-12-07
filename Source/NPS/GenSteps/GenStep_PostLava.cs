using Verse;

namespace TKKN_NPS.GenSteps;

public class GenStep_PostLava : GenStep
{
    public override int SeedPart => 2315674;

    public TerrainDef terrainOverwrite;


    public override void Generate(Map map, GenStepParams parms) {
        var centerSpot = MapGenerator.PlayerStartSpot;
        foreach (var cell in GenRadial.RadialCellsAround(centerSpot, 23, true)) {
            if (TerrainTagUtil.Lava.Contains(cell.GetTerrain(map))) {
                map.terrainGrid.SetTerrain(cell, terrainOverwrite);
            }
        }

        
    }
}