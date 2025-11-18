using System.Collections.Generic;
using Verse;

namespace TKKN_NPS;

public static class PlantReactionUtil
{
    public static readonly Dictionary<ThingDef, List<TerrainDef>> AllowedTerrains = [];

    public static readonly Dictionary<ThingDef, ThingWeatherReaction> HasGraphic = [];


    public static void InitializePlantGraphics() {
        List<ThingDef> allPlants = DefDatabase<ThingDef>.AllDefsListForReading;

        foreach (var plant in allPlants) {
            if (plant.plant == null)
                continue;

            ThingWeatherReaction modExtension = plant.GetModExtension<ThingWeatherReaction>();
            if (modExtension == null) continue;

            if (modExtension.allowedTerrains != null) {
                AllowedTerrains.Add(plant, modExtension.allowedTerrains);
            }

            if (modExtension.initializeGraphics(plant)) {
                HasGraphic.Add(plant, modExtension);
            }
        }
    }
}