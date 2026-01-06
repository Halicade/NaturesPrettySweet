using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TKKN_NPS;
/*
 //Biome was never implemented
public class NPS_BiomeWorker_Sequioa : BiomeWorker
{
    public override float GetScore(BiomeDef biome, Tile tile, PlanetTile planetTile) {
        if (tile.WaterCovered) {
            return -100f;
        }

        if (tile.temperature < -10f) {
            return 0f;
        }

        if (tile.rainfall < 600f) {
            return 0f;
        }

        if (Rand.Value > .0001f) {
            return 0f;
        }

        return (float)(16.0 + (tile.temperature - 7.0) + (tile.rainfall - 600.0) / 180.0);
    }
}
*/