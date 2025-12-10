using RimWorld;

namespace TKKN_NPS;

[DefOf]
public class BiomeDefOf
{
    public static BiomeDef TKKN_Desert;
    public static BiomeDef TKKN_Oasis;
    public static BiomeDef TKKN_RedwoodForest;
    public static BiomeDef TKKN_Grasslands;
    public static BiomeDef TKKN_Savanna;
    public static BiomeDef TKKN_VolcanicFlow;
    
    public static BiomeDef AridShrubland;
    public static BiomeDef TemperateForest;

    static BiomeDefOf() {
        DefOfHelper.EnsureInitializedInCtor(typeof(BiomeDefOf));
    }
}