using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TKKN_NPS;

public class NPS_BiomeWorker_Desert : BiomeWorker_Desert
{
    public override float GetScore(BiomeDef biome, Tile tile, PlanetTile planetTile) {
        if (!BiomeSettings.allowDesertSaltFlats) {
            return -100f;
        }

        if (base.GetScore(biome, tile, planetTile) <= 0) {
            return 0f;
        }

        if (Rand.ValueSeeded(planetTile.tileId ^ 0x1521ff00) > .006) {
            return 0f;
        }

        return tile.temperature + 15;
    }
}