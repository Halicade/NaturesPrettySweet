using UnityEngine;
using Verse;

namespace TKKN_NPS;

public class BiomeSettings : ModSettings
{
    //Biomes
    public static bool allowDesertSaltFlats = true;
    public static bool allowDesertOasis = true;
    public static bool allowRedwood = true;
    public static bool allowTallGrassPrairie = true;
    public static bool allowSavanna = true;
    public static bool allowVolcanicFields = true;
    public static bool modifyAridShrubland = true;
    public static bool modifyTemperateForest = true;
    public static bool lavaReplace = false;
    
    //Plant changes
    private static bool dandelionChanges = true;
    private static bool wildVegetables = true;
    private static bool grassTexture = true;
    
    public static bool GetActiveSettings(string settingName) {
        switch (settingName) {
            case "Dandelions":
                return dandelionChanges;
            case "WildVegetables":
                return wildVegetables;
            case "GrassTexture":
                return grassTexture;
            case  "LavaReplace":
                return lavaReplace;
            default:
                Log.Error($"NPS: Error trying to perform operation. Could not find setting named {settingName}");
                return false;
        }
    }

    public static void DoWindowContents(Rect inRect) {
        Listing_Standard list = new Listing_Standard(GameFont.Small);

        list.Begin(inRect.LeftHalf());

        list.CheckboxLabeled(
            "NPS_AllowBiome_Title".Translate(BiomeDefOf.TKKN_Desert.LabelCap),
            ref allowDesertSaltFlats, tooltip: "NPS_AllowBiomes_Text".Translate(BiomeDefOf.TKKN_Desert.LabelCap));
        list.CheckboxLabeled(
            "NPS_AllowBiome_Title".Translate(BiomeDefOf.TKKN_Oasis.LabelCap),
            ref allowDesertOasis, tooltip: "NPS_AllowBiomes_Text".Translate(BiomeDefOf.TKKN_Oasis.LabelCap));
        list.CheckboxLabeled(
            "NPS_AllowBiome_Title".Translate(BiomeDefOf.TKKN_RedwoodForest.LabelCap),
            ref allowRedwood, tooltip: "NPS_AllowBiomes_Text".Translate(BiomeDefOf.TKKN_RedwoodForest.LabelCap));
        list.CheckboxLabeled(
            "NPS_AllowBiome_Title".Translate(BiomeDefOf.TKKN_Grasslands.LabelCap),
            ref allowTallGrassPrairie, tooltip: "NPS_AllowBiomes_Text".Translate(BiomeDefOf.TKKN_Grasslands.LabelCap));
        list.CheckboxLabeled(
            "NPS_AllowBiome_Title".Translate(BiomeDefOf.TKKN_Savanna.LabelCap),
            ref allowSavanna, tooltip: "NPS_AllowBiomes_Text".Translate(BiomeDefOf.TKKN_Savanna.LabelCap));
        list.CheckboxLabeled(
            "NPS_AllowBiome_Title".Translate(BiomeDefOf.TKKN_VolcanicFlow.LabelCap),
            ref allowVolcanicFields, tooltip: "NPS_AllowBiomes_Text".Translate(BiomeDefOf.TKKN_VolcanicFlow.LabelCap));
        list.CheckboxLabeled(
            "NPS_ModifyBiome_Title".Translate(BiomeDefOf.AridShrubland.LabelCap),
            ref modifyAridShrubland, tooltip: "NPS_ModifyBiome_Text".Translate(BiomeDefOf.AridShrubland.LabelCap));
        list.CheckboxLabeled(
            "NPS_ModifyBiome_Title".Translate(BiomeDefOf.TemperateForest.LabelCap),
            ref modifyTemperateForest, tooltip: "NPS_ModifyBiome_Text".Translate(BiomeDefOf.TemperateForest.LabelCap));

        //ToggleablePatches
        list.Gap();
        list.CheckboxLabeled(
            "NPS_Dandelions_Title".Translate(),
            ref dandelionChanges,
            "NPS_Dandelions_Text".Translate());
        list.CheckboxLabeled(
            "NPS_WildVegetables_Title".Translate(),
            ref wildVegetables,
            "NPS_WildVegetables_Text".Translate());
        list.CheckboxLabeled(
            "NPS_GrassTexture_Title".Translate(),
            ref grassTexture,
            "NPS_GrassTexture_Text".Translate());
        if (ModsConfig.OdysseyActive) {
            list.CheckboxLabeled(
                "NPS_LavaReplace_Title".Translate(),
                ref lavaReplace,
                "NPS_LavaReplace_Text".Translate());
        }

        list.End();
    }

    public override void ExposeData() {
        base.ExposeData();
        
        Scribe_Values.Look(ref allowDesertSaltFlats, "allowDesertSaltFlats", true, true);
        Scribe_Values.Look(ref allowDesertOasis, "allowDesertOasis", true, true);
        Scribe_Values.Look(ref allowRedwood, "allowRedwood", true, true);
        Scribe_Values.Look(ref allowTallGrassPrairie, "allowTallGrassPrairie", true, true);
        Scribe_Values.Look(ref allowSavanna, "allowSavanna", true, true);
        Scribe_Values.Look(ref allowVolcanicFields, "allowVolcanicFields", true, true);
        Scribe_Values.Look(ref modifyAridShrubland, "modifyAridShrubland", true, true);
        Scribe_Values.Look(ref modifyTemperateForest, "modifyTemperateForest", true, true);
        Scribe_Values.Look(ref dandelionChanges, "dandelionChanges", true, true);
        Scribe_Values.Look(ref wildVegetables, "wildVegetables", true, true);
        Scribe_Values.Look(ref grassTexture, "grassTexture", true, true);
        Scribe_Values.Look(ref lavaReplace, "lavaReplace", false, true);
    }
}