using RimWorld;
using RimWorld.Planet;
using Verse;

namespace TKKN_NPS;

public class NPS_BiomeWorker_Redwoods : BiomeWorker
{
    public override float GetScore(BiomeDef biome, Tile tile, PlanetTile planetTile) {
        if (!BiomeSettings.allowRedwood) {
            return -100f;
        }

        if (tile.WaterCovered) {
            return -100f;
        }

        if (tile.temperature is < -10f or > 10f) {
            return 0f;
        }


        if (tile.rainfall < 1100f)
            return 0f;

        //MO uses the same logic for their dark forest biome
        //Generate random chance for which biome takes over
        if (BiomeSettings.MedievalOverhaulActive) {
            return Rand.ChanceSeeded(0.5f, planetTile.tileId) ? 40.5f : 39.5f;
        }

        return 40f;
    }
}