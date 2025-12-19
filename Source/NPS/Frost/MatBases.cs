using UnityEngine;
using Verse;

namespace TKKN_NPS;

[StaticConstructorOnStartup]
public static class MatBases
{
    private static readonly Texture frostTexture = ContentFinder<Texture2D>.Get("TKKN_NPS/Temperature/Frost");
    
//    public static readonly Material Frost = MatLoader.LoadMat("TKKN_NPS/Temperature/Frost");

    private static Material cachedFrost;

    public static Material Frost
    {
        get
        {
            if (cachedFrost == null) {

                cachedFrost = new Material(Verse.MatBases.Snow) { mainTexture = frostTexture };
            }

            return cachedFrost;
        }
    }
}