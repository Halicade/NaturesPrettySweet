using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace TKKN_NPS;

public class WeatherEffectsController : Mod
{

    public WeatherEffectsController(ModContentPack content)
        : base(content) {
        GetSettings<EffectSettings>();
    }

    public override void DoSettingsWindowContents(Rect inRect) {
        EffectSettings.DoWindowContents(inRect);
    }

    public override string SettingsCategory() {
        return "NPS_WeatherEffects".Translate();
    }
    
    public override void WriteSettings() {
        if (!EffectSettings.doColdEffects) {
            EffectSettings.doIce = false;
            EffectSettings.doColdBreath = false;
            EffectSettings.showFrostGrid = false;
        }

        if (!EffectSettings.allowPawnEffects) {
            EffectSettings.pawnEffectsOnlyColonists = false;
            EffectSettings.allowPawnsToGetWet = false;
            EffectSettings.allowPawnsDrowning = false;
            EffectSettings.allowPawnsSwim = false;
            EffectSettings.doDirtPath = false;
            EffectSettings.doWalkThroughSnow = false;
        }
        base.WriteSettings();
        
        if (Current.ProgramState == ProgramState.Playing) {
            foreach (var map in Find.Maps) {
                var watcher= map.GetComponent<Watcher>();
                watcher.validPawns.Clear();
            }
        }
    }
}