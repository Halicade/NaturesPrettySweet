using RimWorld;
using Verse;

namespace TKKN_NPS;


[DefOf]
public class PawnDefOf
{
    public static PawnKindDef TKKN_crab;
    
    static PawnDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(PawnDefOf));
    }
}