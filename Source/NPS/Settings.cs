using UnityEngine;
using Verse;

namespace TKKN_NPS;

public class Settings : ModSettings
{
    public static bool leaveStuff = true;

    public static bool spawnLavaOnlyInBiome = true;
    public static bool allowLavaEruption = true;
    public static bool allowPlantEffects = true;

    public static bool showCold = true;

    //private static bool showHot = true;
    public static bool allowPawnsToGetWet = true;
    public static bool allowPawnsDrowning = true;
    public static bool allowPawnsSwim = true;
    public static bool showRain = true;
    public static bool makePuddles = true;
    public static bool doWeather = true;
    public static bool doDirtPath = true;
    public static bool regenCells;
    public static bool doTides = true;
    public static bool showDevReadout;

    //private static bool showUpdateNotes = true;
    public static bool doFloods = true;
    public static float weatherCellUpdateSpeed = 0.0002f;
    public static bool doIce = showCold;
    public static bool doSprings = true;

    public static bool terrainAffectTemperature = false;

    //Biomes
    public static bool allowDesertSaltFlats = true;
    public static bool allowDesertOasis = true;
    public static bool allowRedwood = true;
    public static bool allowTallGrassPrairie = true;
    public static bool allowSavanna = true;
    public static bool allowVolcanicFields = true;
    public static bool modifyAridShrubland = true;
    public static bool modifyTemperateForest = true;

    static Settings() { }

    public static void DoWindowContents(Rect inRect) {
        Listing_Standard list = new Listing_Standard(GameFont.Small);

        list.Begin(inRect.LeftHalf());

        //Performance Settings
        list.CheckboxLabeled(
            "TKKN_doWeather_title".Translate(),
            ref doWeather,
            "TKKN_doWeather_text".Translate());
        if (doWeather) {
            weatherCellUpdateSpeed = list.SliderLabeled(
                "TKKN_weatherCellUpdateSpeed_title".Translate(weatherCellUpdateSpeed * 10000),
                weatherCellUpdateSpeed, 0.0001f, 0.002f, 0.5f, "TKKN_weatherCellUpdateSpeed_text".Translate());
        }
        
        list.CheckboxLabeled(
            "TKKN_showCold_title".Translate(),
            ref showCold,
            "TKKN_showCold_text".Translate());
        if (showCold && !ModsConfig.OdysseyActive) {
            list.CheckboxLabeled(
                "TKKN_doIce_title".Translate(),
                ref doIce,
                "TKKN_doIce_text".Translate());
        }

        list.CheckboxLabeled(
            "TKKN_showRain_title".Translate(),
            ref showRain,
            "TKKN_showRain_text".Translate());
        list.CheckboxLabeled(
            "NPS_makePuddles_title".Translate(),
            ref makePuddles,
            "NPS_makePuddles_text".Translate());
        list.CheckboxLabeled(
            "TKKN_doTides_title".Translate(),
            ref doTides,
            "TKKN_doTides_text".Translate());
        if (!ModsConfig.OdysseyActive) {
            list.CheckboxLabeled(
                "TKKN_doFloods_title".Translate(),
                ref doFloods,
                "TKKN_doFloods_text".Translate());
            list.CheckboxLabeled(
                "TKKN_leaveStuff_title".Translate(),
                ref leaveStuff,
                "TKKN_leaveStuff_text".Translate());
        }

        list.CheckboxLabeled(
            "TKKN_doSprings_title".Translate(),
            ref doSprings,
            "TKKN_doSprings_text".Translate());
        list.Gap();
        if (!HarmonyMain.DesirePathsActive) {
            list.CheckboxLabeled(
                "TKKN_doDirtPath_title".Translate(),
                ref doDirtPath,
                "TKKN_doDirtPath_text".Translate());
        }

        list.CheckboxLabeled(
            "NPS_doAmbientTemperature_title".Translate(),
            ref terrainAffectTemperature,
            "NPS_doAmbientTemperature_text".Translate());

        list.Gap();


        //Game Play Settings


        list.CheckboxLabeled(
            "TKKN_allowLavaEruption_title".Translate(),
            ref allowLavaEruption,
            "TKKN_allowLavaEruption_text".Translate());
        list.CheckboxLabeled(
            "TKKN_spawnLavaOnlyInBiome_title".Translate(),
            ref spawnLavaOnlyInBiome,
            "TKKN_spawnLavaOnlyInBiome_text".Translate());
        list.CheckboxLabeled(
            "TKKN_allowPlantEffects_title".Translate(),
            ref allowPlantEffects,
            "TKKN_allowPlantEffects_text".Translate());
        list.CheckboxLabeled(
            "TKKN_allowPawnsToGetWet_title".Translate(),
            ref allowPawnsToGetWet,
            "TKKN_allowPawnsToGetWet_text".Translate());
        list.CheckboxLabeled(
            "NPS_allowPawnsToDrown_title".Translate(),
            ref allowPawnsDrowning,
            "NPS_allowPawnsToDrown_text".Translate());
        if (!ModsConfig.OdysseyActive) {
            list.CheckboxLabeled(
                "TKKN_allowPawnsSwim_title".Translate(),
                ref allowPawnsSwim,
                "TKKN_allowPawnsToSwim_text".Translate());
        }


        //Development stuff
        list.Gap(30f);

        list.CheckboxLabeled(
            "TKKN_showTempReadout_title".Translate(),
            ref showDevReadout,
            "TKKN_showTempReadout_text".Translate());

        list.End();

        list.Begin(inRect.RightHalf());
        list.CheckboxLabeled(
            "NPS_allowBiome_title".Translate(BiomeDefOf.TKKN_Desert.LabelCap),
            ref allowDesertSaltFlats, tooltip: "NPS_AllowBiomesText".Translate(BiomeDefOf.TKKN_Desert.LabelCap));
        list.CheckboxLabeled(
            "NPS_allowBiome_title".Translate(BiomeDefOf.TKKN_Oasis.LabelCap),
            ref allowDesertOasis, tooltip: "NPS_AllowBiomesText".Translate(BiomeDefOf.TKKN_Oasis.LabelCap));
        list.CheckboxLabeled(
            "NPS_allowBiome_title".Translate(BiomeDefOf.TKKN_RedwoodForest.LabelCap),
            ref allowRedwood, tooltip: "NPS_AllowBiomesText".Translate(BiomeDefOf.TKKN_RedwoodForest.LabelCap));
        list.CheckboxLabeled(
            "NPS_allowBiome_title".Translate(BiomeDefOf.TKKN_Grasslands.LabelCap),
            ref allowTallGrassPrairie, tooltip: "NPS_AllowBiomesText".Translate(BiomeDefOf.TKKN_Grasslands.LabelCap));
        list.CheckboxLabeled(
            "NPS_allowBiome_title".Translate(BiomeDefOf.TKKN_Savanna.LabelCap),
            ref allowSavanna, tooltip: "NPS_AllowBiomesText".Translate(BiomeDefOf.TKKN_Savanna.LabelCap));
        list.CheckboxLabeled(
            "NPS_allowBiome_title".Translate(BiomeDefOf.TKKN_VolcanicFlow.LabelCap),
            ref allowVolcanicFields, tooltip: "NPS_AllowBiomesText".Translate(BiomeDefOf.TKKN_VolcanicFlow.LabelCap));
        list.CheckboxLabeled(
            "NPS_ModifyBiome_title".Translate(BiomeDefOf.AridShrubland.LabelCap),
            ref modifyAridShrubland, tooltip: "NPS_ModifyBiome_desc".Translate(BiomeDefOf.AridShrubland.LabelCap));
        list.CheckboxLabeled(
            "NPS_ModifyBiome_title".Translate(BiomeDefOf.TemperateForest.LabelCap),
            ref modifyTemperateForest, tooltip: "NPS_ModifyBiome_desc".Translate(BiomeDefOf.TemperateForest.LabelCap));
        list.End();
    }

    public override void ExposeData() {
        base.ExposeData();

        Scribe_Values.Look(ref doWeather, "doWeather", true, true);
        Scribe_Values.Look(ref weatherCellUpdateSpeed, "weatherCellUpdateSpeed", 0.0006f, true);
        Scribe_Values.Look(ref doDirtPath, "doDirtPath", true, true);
        Scribe_Values.Look(ref showCold, "showCold", true, true);
        Scribe_Values.Look(ref allowPlantEffects, "allowPlantEffects", true, true);
        Scribe_Values.Look(ref showRain, "showRain", true, true);
        Scribe_Values.Look(ref makePuddles, "makePuddles", true, true);
        Scribe_Values.Look(ref doTides, "doTides", true, true);
        Scribe_Values.Look(ref doFloods, "doFloods", true, true);
        Scribe_Values.Look(ref leaveStuff, "leaveStuff", true, true);
        Scribe_Values.Look(ref doSprings, "doSprings", true, true);
        Scribe_Values.Look(ref doIce, "doIce", showCold, true);
        Scribe_Values.Look(ref allowPawnsToGetWet, "allowPawnsToGetWet", true, true);
        Scribe_Values.Look(ref allowPawnsDrowning, "allowPawnsDrowning", true, true);
        Scribe_Values.Look(ref allowPawnsSwim, "allowPawnsSwim", true, true);
        Scribe_Values.Look(ref showDevReadout, "showDevReadout", false, true);
        Scribe_Values.Look(ref spawnLavaOnlyInBiome, "spawnLavaOnlyInBiome", false, true);
        Scribe_Values.Look(ref allowLavaEruption, "allowLavaEruption", true, true);
        Scribe_Values.Look(ref regenCells, "regenCells", false, true);
        Scribe_Values.Look(ref terrainAffectTemperature, "terrainAffectTemperature", false, true);
        Scribe_Values.Look(ref allowDesertSaltFlats, "allowDesertSaltFlats", true, true);
        Scribe_Values.Look(ref allowDesertOasis, "allowDesertOasis", true, true);
        Scribe_Values.Look(ref allowRedwood, "allowRedwood", true, true);
        Scribe_Values.Look(ref allowTallGrassPrairie, "allowTallGrassPrairie", true, true);
        Scribe_Values.Look(ref allowSavanna, "allowSavanna", true, true);
        Scribe_Values.Look(ref allowVolcanicFields, "allowVolcanicFields", true, true);
        Scribe_Values.Look(ref modifyAridShrubland, "modifyAridShrubland", true, true);
        Scribe_Values.Look(ref modifyTemperateForest, "modifyTemperateForest", true, true);
        
    }
}