using RimWorld;

namespace TKKN_NPS;

[DefOf]
public static class ElementSpawnDefOf
{

    //public static ElementSpawnDef TKKN_PlantBarnacles;
    public static ElementSpawnDef TKKN_SpawnSaltCrystals;
    public static ElementSpawnDef TKKN_SpawnHotSprings;
    public static ElementSpawnDef TKKN_SpawnColdSprings;
    public static ElementSpawnDef TKKN_SpawnDeadRedwoods;
    
    static ElementSpawnDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(ElementSpawnDefOf));
    }
}