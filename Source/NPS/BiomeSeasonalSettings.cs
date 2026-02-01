using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TKKN_NPS;

public class BiomeSeasonalSettings : DefModExtension
{
    //incident settings
    public List<ThingDef> bloomPlants;
    public bool diseaseCacheUpdated;

    //spring settings
    public int maxSprings;

    public List<PawnKindDef> specialHerds;

    //disease settings
    public List<BiomeDiseaseRecord> springDiseases;
    public List<BiomeDiseaseRecord> summerDiseases;
    public List<BiomeDiseaseRecord> fallDiseases;
    public List<BiomeDiseaseRecord> winterDiseases;

    public List<TKKN_IncidentCommonalityRecord> springEvents;
    public List<TKKN_IncidentCommonalityRecord> summerEvents;
    public List<TKKN_IncidentCommonalityRecord> fallEvents;
    public List<TKKN_IncidentCommonalityRecord> winterEvents;
    public float springSpawnChance;


    //weather settings
    public List<WeatherCommonalityRecord> springWeathers;
    public List<WeatherCommonalityRecord> summerWeathers;
    public List<WeatherCommonalityRecord> fallWeathers;
    public List<WeatherCommonalityRecord> winterWeathers;

    //unused settings
    public bool springsSurviveDrought;
    public bool springsSurviveSummer;
    public int wetPlantStart = 50;
    public bool plantCacheUpdated;
    public bool plantsAdded;
    public List<BiomePlantRecord> specialPlants;


    public void setWeatherBySeason(Map map, Season season, Quadrum quadrum) {
        if (!EffectSettings.seasonalWeather) {
            return;
        }

        if (springWeathers.NullOrEmpty() ||
            summerWeathers.NullOrEmpty() ||
            fallWeathers.NullOrEmpty() ||
            winterWeathers.NullOrEmpty()) {
            return;
        }
        // Quadrum is used if we're on a permanent summer/winter map.
        // This helps give the illusion of changing seasons

        switch (season) {
            case Season.Spring:
                setWeatherCommonalities(map.Biome.baseWeatherCommonalities, map.Biome, springWeathers);
                break;
            case Season.Summer:
                setWeatherCommonalities(map.Biome.baseWeatherCommonalities, map.Biome, summerWeathers);
                break;
            case Season.Fall:
                setWeatherCommonalities(map.Biome.baseWeatherCommonalities, map.Biome, fallWeathers);
                break;
            case Season.Winter:
                setWeatherCommonalities(map.Biome.baseWeatherCommonalities, map.Biome, winterWeathers);
                break;
            case Season.Undefined:
            case Season.PermanentSummer:
            case Season.PermanentWinter:
            default: {
                switch (quadrum) {
                    case Quadrum.Aprimay:
                        setWeatherCommonalities(map.Biome.baseWeatherCommonalities, map.Biome, springWeathers);
                        break;
                    case Quadrum.Decembary:
                        setWeatherCommonalities(map.Biome.baseWeatherCommonalities, map.Biome, winterWeathers);
                        break;
                    case Quadrum.Jugust:
                        setWeatherCommonalities(map.Biome.baseWeatherCommonalities, map.Biome, fallWeathers);
                        break;
                    case Quadrum.Septober:
                        setWeatherCommonalities(map.Biome.baseWeatherCommonalities, map.Biome, summerWeathers);
                        break;
                    case Quadrum.Undefined:
                    default:
                        Log.ErrorOnce(
                            $"NPS: Could not find an appropriate weather settings for biome: {map.Biome} from mod: {map.Biome.modContentPack?.Name} Season: {season} Quadrum: {quadrum}",
                            map.Biome.GetHashCode());
                        return;
                }

                break;
            }
        }

        if (map.Biome.baseWeatherCommonalities?.Any() == false) {
            map.Biome.baseWeatherCommonalities = [
                new WeatherCommonalityRecord { commonality = 1f, weather = WeatherDefOf.Clear }
            ];
        }
    }

    /// <summary>
    /// Set weather commonality without overriding any weathers that are patched in or otherwise originally available
    /// </summary>
    private static void setWeatherCommonalities(List<WeatherCommonalityRecord> baseWeatherCommonalities, BiomeDef biome,
        List<WeatherCommonalityRecord> newCommonalities) {
        if (!BiomeUtil.OriginalBiomeWeather.TryGetValue(biome, out List<WeatherCommonalityRecord> originalRecords))
            return;
        originalRecords.CopyToList(baseWeatherCommonalities);
        foreach (WeatherCommonalityRecord record in baseWeatherCommonalities) {
            foreach (WeatherCommonalityRecord newWeathers in newCommonalities) {
                if (record.weather == newWeathers.weather) {
                    record.commonality = newWeathers.commonality;
                    break;
                }
            }
        }
    }

    public void setDiseaseBySeason(Map map, Season season, Quadrum quadrum) {
        if (!EffectSettings.seasonalDiseases) {
            return;
        }

        if (springDiseases.NullOrEmpty() ||
            summerDiseases.NullOrEmpty() ||
            fallDiseases.NullOrEmpty() ||
            winterDiseases.NullOrEmpty()) {
            return;
        }

        List<BiomeDiseaseRecord> seasonalDiseases;
        switch (season) {
            case Season.Spring when springDiseases != null:
                seasonalDiseases = springDiseases;
                break;
            case Season.Summer when summerDiseases != null:
                seasonalDiseases = summerDiseases;
                break;
            case Season.Fall when fallDiseases != null:
                seasonalDiseases = fallDiseases;
                break;
            case Season.Winter when winterDiseases != null:
                seasonalDiseases = winterDiseases;
                break;
            case Season.Undefined:
            case Season.PermanentSummer:
            case Season.PermanentWinter:
            default: {
                switch (quadrum) {
                    case Quadrum.Aprimay when springDiseases != null:
                        seasonalDiseases = springDiseases;
                        break;
                    case Quadrum.Decembary when winterDiseases != null:
                        seasonalDiseases = winterDiseases;
                        break;
                    case Quadrum.Jugust when summerDiseases != null:
                        seasonalDiseases = summerDiseases;
                        break;
                    case Quadrum.Septober when fallDiseases != null:
                        seasonalDiseases = fallDiseases;
                        break;
                    case Quadrum.Undefined:
                    default:
                        Log.ErrorOnce(
                            $"NPS: Could not find an appropriate weather settings for biome: {map.Biome} from mod: {map.Biome.modContentPack?.Name} Season: {season} Quadrum: {quadrum}",
                            map.Biome.GetHashCode());
                        return;
                }

                break;
            }
        }

        foreach (var diseaseRec in seasonalDiseases) {
            var disease = diseaseRec.diseaseInc;
            disease.baseChance = diseaseRec.commonality;
        }

        diseaseCacheUpdated = false;
    }

    public void setIncidentsBySeason(Map map, Season season, Quadrum quadrum) {
        if (!EffectSettings.seasonalIncidents) {
            return;
        }
        if (springEvents.NullOrEmpty() ||
            summerEvents.NullOrEmpty() ||
            fallEvents.NullOrEmpty() ||
            winterEvents.NullOrEmpty()) {
            return;
        }
        
        List<TKKN_IncidentCommonalityRecord> seasonalIncidents;
        switch (season) {
            case Season.Spring when springEvents != null:
                seasonalIncidents = springEvents;
                break;
            case Season.Summer when summerEvents != null:
                seasonalIncidents = summerEvents;
                break;
            case Season.Fall when fallEvents != null:
                seasonalIncidents = fallEvents;
                break;
            case Season.Winter when winterEvents != null:
                seasonalIncidents = winterEvents;
                break;
            case Season.Undefined:
            case Season.PermanentSummer:
            case Season.PermanentWinter:
            default: {
                switch (quadrum) {
                    case Quadrum.Aprimay when springEvents != null:
                        seasonalIncidents = springEvents;
                        break;
                    case Quadrum.Decembary when winterEvents != null:
                        seasonalIncidents = winterEvents;
                        break;
                    case Quadrum.Jugust when summerEvents != null:
                        seasonalIncidents = summerEvents;
                        break;
                    case Quadrum.Septober when fallEvents != null:
                        seasonalIncidents = fallEvents;
                        break;
                    case Quadrum.Undefined:
                    default:
                        Log.ErrorOnce(
                            $"NPS: Could not find an appropriate weather settings for biome: {map.Biome} from mod: {map.Biome.modContentPack?.Name} Season: {season} Quadrum: {quadrum}",
                            map.Biome.GetHashCode());
                        return;
                }

                break;
            }
        }

        foreach (var incidentRate in seasonalIncidents) {
            var incident = incidentRate.incident;
            incident.baseChance = incidentRate.commonality;
        }
    }
}