using RimWorld;
using Verse;

namespace TKKN_NPS.GenSteps;

public class GenStep_PostSpringPlants : GenStep
{
    public override int SeedPart => 564897;
    
    public override void Generate(Map map, GenStepParams parms) {
        
        float currentPlantDensityFactor = map.wildPlantSpawner.CurrentPlantDensityFactor+1;
        float currentWholeMapNumDesiredPlants = map.wildPlantSpawner.CurrentWholeMapNumDesiredPlants+50;
        
        Log.Message("currentPlantDensityFactor "+currentPlantDensityFactor);
        Log.Message("currentWholeMapNumDesiredPlants "+currentWholeMapNumDesiredPlants);
        
        foreach (var soilCell in ValidOasisSoils.ValidSoils) {
            map.wildPlantSpawner.CheckSpawnWildPlantAt(soilCell, currentPlantDensityFactor, currentWholeMapNumDesiredPlants, true);


        }
        
        ValidOasisSoils.ValidSoils.Clear();
    }
    
    
}