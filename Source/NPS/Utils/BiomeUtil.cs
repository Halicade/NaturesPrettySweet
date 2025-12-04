using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TKKN_NPS;

public class BiomeUtil
{
    public static readonly Dictionary<BiomeDef, List<WeatherCommonalityRecord>> OriginalBiomeWeather = [];

    public static readonly Dictionary<BiomeDef, List<PawnKindDef>> SpecialHerds = [];


    public static void InitializeDefaults() {
        List<BiomeDef> biomes = DefDatabase<BiomeDef>.AllDefsListForReading;

        foreach (var biome in biomes) {
            if (!biome.baseWeatherCommonalities.NullOrEmpty()) {
                List<WeatherCommonalityRecord> records = [];
                biome.baseWeatherCommonalities.CopyToList(records);
                OriginalBiomeWeather.Add(biome, records);
            }

            var seasonalSettingsExt = biome.GetModExtension<BiomeSeasonalSettings>();
            if (seasonalSettingsExt == null) {
                continue;
            }

            if (!seasonalSettingsExt.specialHerds.NullOrEmpty()) {
                SpecialHerds.Add(biome, seasonalSettingsExt.specialHerds);
            }
        }
    }
}