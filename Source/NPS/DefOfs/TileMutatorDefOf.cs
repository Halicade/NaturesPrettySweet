using RimWorld;

namespace TKKN_NPS;

[DefOf]
public class TileMutatorDefOf
{


        public static TileMutatorDef NPS_ColdSpringMutator;
        
        static TileMutatorDefOf()
        {
                DefOfHelper.EnsureInitializedInCtor(typeof(TileMutatorDefOf));
        }
}