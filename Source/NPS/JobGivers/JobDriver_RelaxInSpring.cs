using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace TKKN_NPS;

public class JobDriver_RelaxInSpring : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed) {
        return true;
    }

    // Using Skygazing as reference
    protected override IEnumerable<Toil> MakeNewToils() {
        yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
        Toil toil = ToilMaker.MakeToil("MakeNewToils");
        toil.initAction = delegate { pawn.jobs.posture = PawnPosture.LayingOnGroundFaceUp; };
        toil.tickIntervalAction = delegate(int delta) {
            JoyUtility.JoyTickCheckEnd(pawn, delta, JoyTickFullJoyAction.EndJob, 1f);
            pawn.mindState.lastSwamTick = GenTicks.TicksGame;
        };
        toil.defaultCompleteMode = ToilCompleteMode.Delay;
        toil.defaultDuration = job.def.joyDuration;

        toil.FailOn(() => pawn.Map.weatherManager.RainRate > 0.1f && !pawn.Position.Roofed(pawn.Map));
        /*
        toil.FailOn(() => pawn.Map.weatherManager.curWeather.preventSkygaze);
        toil.FailOn(() => !JoyUtility.EnjoyableOutsideNow(pawn));*/
        yield return toil;
    }
}