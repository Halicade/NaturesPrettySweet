using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TKKN_NPS;

public class EffectSettings : ModSettings
{
    public static bool leaveStuff = true;

    public static bool spawnLavaOnlyInBiome = true;
    public static bool allowLavaEruption = true;
    public static bool allowPlantEffects = true;

    public static bool doColdEffects = true;
    public static bool doIce = true;
    public static bool doColdBreath = true;
    public static bool showFrostGrid = true;
    public static bool doWalkThroughSnow = true;

    public static bool allowPawnEffects = true;
    public static bool pawnEffectsOnlyColonists = true;
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

    public static bool doFloods = true;
    public static float cellsPerTick = 5;
    public static bool useMapTemperature = false;

    public static bool seasonalDiseases = true;
    public static bool seasonalIncidents = true;
    public static bool seasonalWeather = true;

    public static bool terrainAffectTemperature = false;
    

    public static void DoWindowContents(Rect inRect) {
        Listing_Standard list = new Listing_Standard(GameFont.Small);

        list.Begin(inRect.LeftPart(0.49f));

        //Performance Settings
        list.CheckboxLabeled(
            "TKKN_doWeather_title".Translate(),
            ref doWeather,
            "TKKN_doWeather_text".Translate());
        if (doWeather) {
            cellsPerTick = (int)list.SliderLabeled(
                "NPS_weatherCellUpdateSpeed_title".Translate(cellsPerTick),
                cellsPerTick, 3, 75, 0.5f, "NPS_weatherCellUpdateSpeed_text".Translate());
        }


        list.CheckboxLabeled(
            "NPS_DoColdEffects_title".Translate(),
            ref doColdEffects,
            "NPS_DoColdEffects_text".Translate());

        if (doColdEffects) {
            if (!ModsConfig.OdysseyActive) {
                list.CheckboxLabeled(
                    "TKKN_doIce_title".Translate(),
                    ref doIce,
                    "TKKN_doIce_text".Translate());
            }

            list.CheckboxLabeled(
                "NPS_DoColdBreath_title".Translate(),
                ref doColdBreath,
                "NPS_DoColdBreath_text".Translate());

            list.CheckboxLabeled(
                "NPS_ShowFrostGrid_title".Translate(),
                ref showFrostGrid,
                "NPS_FrostGrid_text".Translate());
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
        }
        list.CheckboxLabeled(
            "TKKN_leaveStuff_title".Translate(),
            ref leaveStuff,
            "TKKN_leaveStuff_text".Translate());

        list.Gap();
        if (!HarmonyWeatherEffects.DesirePathsActive && allowPawnEffects) {
            list.CheckboxLabeled(
                "TKKN_doDirtPath_title".Translate(),
                ref doDirtPath,
                "TKKN_doDirtPath_text".Translate());
            if (doDirtPath) {
                list.CheckboxLabeled(
                    "NPS_doWalkThroughSnow_title".Translate(),
                    ref doWalkThroughSnow,
                    "NPS_doWalkThroughSnow_text".Translate());
            }
        }

        list.CheckboxLabeled(
            "NPS_doAmbientTemperature_title".Translate(),
            ref terrainAffectTemperature,
            "NPS_doAmbientTemperature_text".Translate());

        list.Gap();


        //Game Play Settings
        list.End();

        list.Begin(inRect.RightPart(0.49f));


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
            "NPS_useMapTemperature_title".Translate(),
            ref useMapTemperature,
            "NPS_useMapTemperature_text".Translate());
        
        
            
        
        list.CheckboxLabeled(
            "NPS_seasonalDiseases_title".Translate(),
            ref seasonalDiseases,
            "NPS_seasonalDiseases_text".Translate());
        list.CheckboxLabeled(
            "NPS_seasonalIncidents_title".Translate(),
            ref seasonalIncidents,
            "NPS_seasonalIncidents_text".Translate());
        list.CheckboxLabeled(
            "NPS_seasonalWeather_title".Translate(),
            ref seasonalWeather,
            "NPS_seasonalWeather_text".Translate());

        list.CheckboxLabeled(
            "NPS_allowPawnEffects_title".Translate(),
            ref allowPawnEffects,
            "NPS_allowPawnEffects_text".Translate());

        if (allowPawnEffects) {
            list.CheckboxLabeled(
                "NPS_pawnEffectsOnlyColonists_title".Translate(),
                ref pawnEffectsOnlyColonists,
                "NPS_pawnEffectsOnlyColonists_text".Translate());
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
        }


        //Development stuff
        list.Gap(30f);

        list.CheckboxLabeled(
            "TKKN_showTempReadout_title".Translate(),
            ref showDevReadout,
            "TKKN_showTempReadout_text".Translate());


       
        list.End();
    }

    public override void ExposeData() {
        base.ExposeData();

        Scribe_Values.Look(ref doWeather, "doWeather", true, true);
        Scribe_Values.Look(ref cellsPerTick, "cellsPerTick", 5f, true);
        Scribe_Values.Look(ref doDirtPath, "doDirtPath", true, true);
        Scribe_Values.Look(ref allowPlantEffects, "allowPlantEffects", true, true);
        Scribe_Values.Look(ref showRain, "showRain", true, true);
        Scribe_Values.Look(ref makePuddles, "makePuddles", true, true);
        Scribe_Values.Look(ref doTides, "doTides", true, true);
        Scribe_Values.Look(ref doFloods, "doFloods", true, true);
        Scribe_Values.Look(ref leaveStuff, "leaveStuff", true, true);

        Scribe_Values.Look(ref doColdEffects, "doColdEffects", true, true);
        Scribe_Values.Look(ref doIce, "doIce", true, true);
        Scribe_Values.Look(ref doColdBreath, "doColdBreath", true, true);
        Scribe_Values.Look(ref showFrostGrid, "showFrostGrid", true, true);
        Scribe_Values.Look(ref useMapTemperature, "useMapTemperature", false, true);
        Scribe_Values.Look(ref allowPawnEffects, "allowPawnEffects", true, true);
        Scribe_Values.Look(ref pawnEffectsOnlyColonists, "pawnEffectsOnlyColonists", true, true);
        Scribe_Values.Look(ref seasonalDiseases, "seasonalDiseases", true, true);
        Scribe_Values.Look(ref seasonalIncidents, "seasonalIncidents", true, true);
        Scribe_Values.Look(ref seasonalWeather, "seasonalWeather", true, true);

        Scribe_Values.Look(ref allowPawnsToGetWet, "allowPawnsToGetWet", true, true);
        Scribe_Values.Look(ref allowPawnsDrowning, "allowPawnsDrowning", true, true);
        Scribe_Values.Look(ref allowPawnsSwim, "allowPawnsSwim", true, true);
        Scribe_Values.Look(ref showDevReadout, "showDevReadout", false, true);
        Scribe_Values.Look(ref spawnLavaOnlyInBiome, "spawnLavaOnlyInBiome", false, true);
        Scribe_Values.Look(ref allowLavaEruption, "allowLavaEruption", true, true);
        Scribe_Values.Look(ref regenCells, "regenCells", false, true);
        Scribe_Values.Look(ref terrainAffectTemperature, "terrainAffectTemperature", false, true);
        
    }
}