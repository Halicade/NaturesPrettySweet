using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TKKN_NPS;

public class BiomesController : Mod
{
    
    public BiomesController(ModContentPack content)
        : base(content) {
        GetSettings<BiomeSettings>();
        BiomeSettings.MedievalOverhaulActive= ModLister.GetActiveModWithIdentifier("dankpyon.medieval.overhaul") != null;
        
        LongEventHandler.QueueLongEvent(action: HarmonyPatches,
            textKey: null,
            doAsynchronously: true,
            exceptionHandler: null
        );
    }
    
    public override void DoSettingsWindowContents(Rect inRect) {
        BiomeSettings.DoWindowContents(inRect);
    }

    public override string SettingsCategory() {
        return "NPS_Biomes".Translate();
    }

    private static void HarmonyPatches() {
        var harmony = new Harmony("Hali.NPS_BiomeEffects");
        
        if (BiomeSettings.modifyAridShrubland) {
            harmony.Patch(
                AccessTools.Method(typeof(BiomeWorker_AridShrubland), nameof(BiomeWorker_AridShrubland.GetScore)),
                postfix: new HarmonyMethod(typeof(BiomeWorker_AridShrubland_GetScore),
                    nameof(BiomeWorker_AridShrubland_GetScore.Postfix)));
        }

        if (BiomeSettings.modifyTemperateForest) {
            harmony.Patch(
                AccessTools.Method(typeof(BiomeWorker_TemperateForest), nameof(BiomeWorker_TemperateForest.GetScore)),
                postfix: new HarmonyMethod(typeof(BiomeWorker_TemperateForest_GetScore),
                    nameof(BiomeWorker_TemperateForest_GetScore.Postfix)));
        }
    }

    
}