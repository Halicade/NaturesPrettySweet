using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TKKN_NPS;

[StaticConstructorOnStartup]
public class HarmonyWeatherEffects
{
    public static readonly bool RimBrellasActive;
    public static readonly bool DesirePathsActive;

    public delegate bool HasUmbrellaDelegate(Pawn pawn);

    public static HasUmbrellaDelegate HasUmbrella;
    
    

    static HarmonyWeatherEffects() {
        TerrainTagUtil.IntializeTerrainTags();
        ThingUtil.InitializeThingUtil();
        PlantReactionUtil.InitializePlantGraphics();
        BiomeUtil.InitializeDefaults();

        DesirePathsActive = ModLister.GetActiveModWithIdentifier("mlie.desirepaths") != null;
        RimBrellasActive = ModLister.GetActiveModWithIdentifier("battlemage64.Rimbrellas", true) != null;

        if (ModsConfig.OdysseyActive) {
            EffectSettings.doIce = false;
            EffectSettings.doFloods = false;
            EffectSettings.allowPawnsSwim = false;
        }

        if (DesirePathsActive) {
            EffectSettings.doDirtPath = false;
        }


        var harmony = new Harmony("Hali.NPS_WeatherEffects");

        harmony.Patch(AccessTools.Method(typeof(BiomeDef), nameof(BiomeDef.CommonalityOfDisease)),
            prefix: new HarmonyMethod(typeof(BiomeDef_CommonalityOfDisease),
                nameof(BiomeDef_CommonalityOfDisease.Prefix)));

        harmony.Patch(
            AccessTools.Method(typeof(GenSpawn), nameof(GenSpawn.Spawn),
            [
                typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool), typeof(bool)
            ]),
            postfix: new HarmonyMethod(typeof(GenSpawn_Spawn), nameof(GenSpawn_Spawn.Postfix)));


        harmony.Patch(
            AccessTools.Method(typeof(Graphic_Shadow), nameof(Graphic_Shadow.DrawWorker)),
            prefix: new HarmonyMethod(typeof(Graphic_Shadow_DrawWorker),
                nameof(Graphic_Shadow_DrawWorker.Prefix)));

        harmony.Patch(AccessTools.Method(typeof(MouseoverReadout), nameof(MouseoverReadout.MouseoverReadoutOnGUI)),
            postfix: new HarmonyMethod(typeof(MouseoverReadout_MouseoverReadoutOnGUI),
                nameof(MouseoverReadout_MouseoverReadoutOnGUI.Postfix)));

        /*
        //Removed these patches cause this mod shouldn't be the one to do it
        harmony.Patch(AccessTools.Method(typeof(CellFinder), nameof(CellFinder.TryRandomClosewalkCellNear)),
            prefix: new HarmonyMethod(typeof(CellFinder_TryRandomClosewalkCellNear),
                nameof(CellFinder_TryRandomClosewalkCellNear.Prefix)));
        harmony.Patch(AccessTools.Method(typeof(Pawn_PathFollower), nameof(Pawn_PathFollower.StartPath)),
            prefix: new HarmonyMethod(typeof(Pawn_PathFollower_StartPath),
                nameof(Pawn_PathFollower_StartPath.Prefix)));

        harmony.Patch(AccessTools.Method(typeof(Reachability), nameof(Reachability.CanReach), [
                typeof(IntVec3), typeof(LocalTargetInfo),
                typeof(PathEndMode),
                typeof(TraverseParms)
            ]),
            postfix: new HarmonyMethod(typeof(Reachability_CanReach),
                nameof(Reachability_CanReach.Postfix)));
        */

        harmony.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.SpawnSetup)),
            postfix: new HarmonyMethod(typeof(Pawn_SpawnSetup),
                nameof(Pawn_SpawnSetup.Postfix)));

        harmony.Patch(AccessTools.Method(typeof(WeatherDecider), "CurrentWeatherCommonality"),
            prefix: new HarmonyMethod(typeof(WeatherDecider_CurrentWeatherCommonality),
                nameof(WeatherDecider_CurrentWeatherCommonality.Prefix)));

        harmony.Patch(
            AccessTools.Method(typeof(JobGiver_SeekSafeTemperature), "TryGiveJob"),
            postfix: new HarmonyMethod(typeof(JobGiver_SeekSafeTemperature_TryGiveJob),
                nameof(JobGiver_SeekSafeTemperature_TryGiveJob.Postfix)));

        if (EffectSettings.allowPawnsSwim && !ModsConfig.OdysseyActive) {
            harmony.Patch(
                AccessTools.Method(typeof(PawnRenderNodeWorker_Body), nameof(PawnRenderNodeWorker_Body.CanDrawNow)),
                postfix: new HarmonyMethod(typeof(PawnRenderNodeWorker_Body_CanDrawNow),
                    nameof(PawnRenderNodeWorker_Body_CanDrawNow.Postfix)));
        }

        if (EffectSettings.allowPlantEffects) {
            harmony.Patch(AccessTools.PropertyGetter(typeof(Plant), nameof(Plant.Graphic)),
                postfix: new HarmonyMethod(typeof(Plant_Graphic),
                    nameof(Plant_Graphic.Postfix)));
        }

        if (EffectSettings.terrainAffectTemperature) {
            harmony.Patch(AccessTools.PropertyGetter(typeof(Thing), nameof(Thing.AmbientTemperature)),
                postfix: new HarmonyMethod(typeof(Thing_AmbientTemperature),
                    nameof(Thing_AmbientTemperature.Postfix)));
        }



        if (RimBrellasActive) {
            HasUmbrella = AccessTools.MethodDelegate<HasUmbrellaDelegate>(
                AccessTools.Method("Umbrellas.UmbrellaDefMethods:HasUmbrella"));

            if (HasUmbrella == null) {
                Log.Warning(
                    "[Natures Pretty Sweet]: Rimbrella loaded but could not find the correct method to check for umbrellas");
                RimBrellasActive = false;
            }
        }
    }
}