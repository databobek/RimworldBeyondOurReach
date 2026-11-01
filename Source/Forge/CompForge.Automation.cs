using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BeyondOurReach;

// Robot Assistant automation: works the forge without a pawn when a linked work helper is in WORK mode.
public partial class CompForge
{
    // EvenMoreLinkables' Robot Assistant is identified by its work-helper comp, resolved by reflection (soft dependency).
    private const string WorkHelperType = "Hobbes.MoreLinkablesProduction.CompWorkHelper";
    private const float AutomationForgeSpeed = 1.5f;

    private static readonly System.Type? WorkHelperComp = GenTypes.GetTypeInAnyAssembly(WorkHelperType);
    private static readonly System.Reflection.FieldInfo? WorkHelperState = WorkHelperComp?.GetField("state");

    private int robotSlot = -1;
    private int robotTicks;

    // A powered, switched-on work helper in WORK mode (the automatic mode) works linked forges without a pawn.
    private static bool IsAutomating(Thing facility)
    {
        if (WorkHelperComp == null)
        {
            return false;
        }
        ThingComp? helper = (facility as ThingWithComps)?.AllComps.FirstOrDefault(c => c.GetType() == WorkHelperComp);
        if (helper == null || WorkHelperState?.GetValue(helper)?.ToString() != "WORK")
        {
            return false;
        }
        return (facility.TryGetComp<CompPowerTrader>()?.PowerOn ?? true)
            && (facility.TryGetComp<CompFlickable>()?.SwitchIsOn ?? true);
    }

    private float AutomationSpeed
    {
        get
        {
            if (WorkHelperComp == null || !PowerOn || parent.Map == null)
            {
                return 0f;
            }
            List<Thing>? linked = parent.GetComp<CompAffectedByFacilities>()?.LinkedFacilitiesListForReading;
            return linked != null && linked.Any(IsAutomating) ? AutomationForgeSpeed : 0f;
        }
    }

    public override void CompTick()
    {
        base.CompTick();
        if (!parent.Spawned)
        {
            return;
        }
        UpdateOverdrivePower();
        float speed = AutomationSpeed;
        if (speed <= 0f)
        {
            return;
        }
        // A pawn working the forge takes precedence.
        if (parent.Map.reservationManager.IsReservedByAnyoneOf(parent, Faction.OfPlayer))
        {
            return;
        }
        if (IsFormatting)
        {
            FormatTick();
            return;
        }
        if (PendingAnalysis.Count > 0)
        {
            return;
        }
        int slot = robotSlot >= 0 && robotSlot < Capacity && ShouldProduce(robotSlot) ? robotSlot : NextProductionSlot();
        if (slot < 0)
        {
            robotSlot = -1;
            robotTicks = 0;
            return;
        }
        if (slot != robotSlot)
        {
            robotSlot = slot;
            robotTicks = 0;
        }
        if (robotTicks % Props.workTicksPerDraw == 0 && !TryDrawResource())
        {
            return;
        }
        ThingDef def = slots[slot]!;
        int total = ForgeMath.RobotProductionTicks(def, speed);
        robotTicks++;
        NotifyProducing();
        SetProgress(slot, (float)robotTicks / total);
        if (robotTicks >= total)
        {
            robotTicks = 0;
            SetProgress(slot, 0f);
            FinishProduction(slot);
        }
    }
}
