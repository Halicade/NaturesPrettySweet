using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TKKN_NPS;

public class NPS_BiomeWorker_LavaFields : BiomeWorker_TropicalRainforest
{
    public override float GetScore(BiomeDef biome, Tile tile, PlanetTile planetTile) {
        if (!BiomeSettings.allowVolcanicFields) {
            return -100f;
        }

        if (base.GetScore(biome, tile, planetTile) < 0) {
            return 0f;
        }

        if (Rand.ValueSeeded(planetTile.tileId ^ 0x1198291d) > .009) {
            return 0f;
        }

        return (float)(32.0 + ((tile.temperature - 20.0) * 3.5) + ((tile.rainfall - 600.0) / 165.0));
    }
}