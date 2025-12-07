using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TKKN_NPS.GenSteps;

public class GenStep_PostOasis : GenStep
{
    public override int SeedPart => 8561324;

    public TerrainDef soilTerrain;

    public List<ThingDef> validPlants = [];

    public override void Generate(Map map, GenStepParams parms) {
        foreach (IntVec3 item in map.cellsInRandomOrder.GetAll()) {
            if (!Rand.Chance(0.001f)) {
                genPlants(item, map);
            }
        }
    }


    private void genPlants(IntVec3 c, Map map) {
        if (c.GetEdifice(map) != null || c.GetCover(map) != null || c.GetTerrain(map) != soilTerrain) {
            return;
        }

        if (Rand.Chance(0.1f))
            return;

        if (validPlants.NullOrEmpty()) {
            foreach (var wildPlant in map.Biome.AllWildPlants) {
                if (wildPlant.CanEverPlantAt(c, map))
                    validPlants.Add(wildPlant);
            }
        }


        if (!validPlants.Any()) {
            return;
        }

        if (!validPlants.TryRandomElementByWeight(def => plantChoiceWeight(def, map), out var thingDef)) {
            return;
        }

        var plant = (Plant)ThingMaker.MakeThing(thingDef);
        plant.Growth = Rand.Range(0.07f, 1f);
        if (plant.def.plant.LimitedLifespan) {
            plant.Age = Rand.Range(0, Mathf.Max(plant.def.plant.LifespanTicks - 50, 0));
        }

        GenSpawn.Spawn(plant, c, map);
    }

    private static float plantChoiceWeight(ThingDef def, Map map) {
        return map.Biome.CommonalityOfPlant(def) * def.plant.wildClusterWeight;
    }
}