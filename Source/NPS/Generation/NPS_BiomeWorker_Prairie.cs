using RimWorld;
using RimWorld.Planet;

namespace TKKN_NPS;

public class NPS_BiomeWorker_Prairie : BiomeWorker
{
    public override float GetScore(BiomeDef biome, Tile tile, PlanetTile planetTile) {
        if (!Settings.allowTallGrassPrairie) {
            return -100f;
        }

        if (tile.WaterCovered) {
            return -100f;
        }

        if (tile.temperature is < -10f or > 22) {
            return 0f;
        }

        if (tile.rainfall is < 900f or >= 1300f) {
            return 0f;
        }

        if (tile.hilliness != Hilliness.Flat) {
            return 0f;
        }

        return 22.5f + ((tile.temperature - 20f) * 6.2f) + ((tile.rainfall - 0f) / 100f);
    }
}