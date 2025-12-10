using RimWorld;
using RimWorld.Planet;

namespace TKKN_NPS;

internal class NPS_BiomeWorker_Redwoods : BiomeWorker
{
    public override float GetScore(BiomeDef biome, Tile tile, PlanetTile planetTile)
    {
        if (!Settings.allowRedwood) {
            return -100f;
        }
        if (tile.WaterCovered)
        {
            return -100f;
        }

        if (tile.temperature is < -10f or > 10f)
        {
            return 0f;
        }

        return tile.rainfall < 1100f ? 0f : 40f;
    }
}