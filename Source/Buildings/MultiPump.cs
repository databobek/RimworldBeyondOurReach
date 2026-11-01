using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BeyondOurReach;

public class PumpMode
{
    // Full name of the pump comp this mode switches on.
    public string comp = "";
    public string labelKey = "";
    public string descKey = "";

    // Mode is offered once this is researched; none means always.
    public ResearchProjectDef? research;

    // Power drawn in this mode; 0 keeps the building's own draw.
    public float power;
}

// Lets one pump run any of several pump comps, one at a time. The other comps stay on the building but are held still.
public class CompProperties_MultiPump : CompProperties
{
    public List<PumpMode> modes = new();

    public CompProperties_MultiPump()
    {
        compClass = typeof(CompMultiPump);
    }
}

public class CompMultiPump : ThingComp
{
    private int modeIndex;

    public CompProperties_MultiPump Props => (CompProperties_MultiPump)props;

    private PumpMode Current => Props.modes[Mathf.Clamp(modeIndex, 0, Props.modes.Count - 1)];

    // Comps that are not one of the modes (power, flickable...) are never held.
    public bool IsActive(ThingComp other)
    {
        string? name = other.GetType().FullName;
        return !Props.modes.Any(m => m.comp == name) || Current.comp == name;
    }

    private void SetMode(int index)
    {
        PumpMode previous = Current;
        modeIndex = index;
        EndSustainer(previous);
        ApplyPower();
    }

    // Vanilla resets the draw to the building's default whenever power comes back on, so this runs again on the tick.
    private void ApplyPower()
    {
        float watts = Current.power;
        CompPowerTrader? trader = parent.GetComp<CompPowerTrader>();
        if (watts > 0f && trader != null && trader.PowerOn && trader.PowerOutput < 0f && !Mathf.Approximately(trader.PowerOutput, -watts))
        {
            trader.PowerOutput = -watts;
        }
    }

    // The terra pumps start a looping sound while working; the held comp would never stop it.
    private void EndSustainer(PumpMode mode)
    {
        ThingComp? held = parent.AllComps.FirstOrDefault(c => c.GetType().FullName == mode.comp);
        FieldInfo? field = held == null ? null : AccessTools.Field(held.GetType(), "sustainer");
        if (field?.GetValue(held) is Verse.Sound.Sustainer sustainer)
        {
            sustainer.End();
            field.SetValue(held, null);
        }
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref modeIndex, "pumpMode");
    }

    // The pollution pump ticks every tick, but the terra pump comps only work from the rare tick, which a normal-ticker
    // building never gets. Feed them one here.
    public override void CompTick()
    {
        if (!parent.IsHashIntervalTick(GenTicks.TickRareInterval))
        {
            return;
        }
        ApplyPower();
        foreach (PumpMode mode in Props.modes)
        {
            ThingComp? pump = parent.AllComps.FirstOrDefault(c => c.GetType().FullName == mode.comp);
            if (pump != null && IsActive(pump))
            {
                pump.CompTickRare();
            }
        }
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (parent.Faction != Faction.OfPlayer)
        {
            yield break;
        }
        yield return new Command_Action
        {
            defaultLabel = "BOR_PumpMode".Translate(Current.labelKey.Translate()),
            defaultDesc = "BOR_PumpModeDesc".Translate() + "\n\n" + Current.descKey.Translate(),
            icon = parent.def.uiIcon,
            action = () =>
            {
                List<FloatMenuOption> options = new();
                for (int i = 0; i < Props.modes.Count; i++)
                {
                    int index = i;
                    PumpMode mode = Props.modes[i];
                    if (mode.research != null && !mode.research.IsFinished)
                    {
                        options.Add(new FloatMenuOption(mode.labelKey.Translate() + " (" + "BOR_PumpModeNeedsResearch".Translate(mode.research.LabelCap) + ")", null));
                        continue;
                    }
                    options.Add(new FloatMenuOption(mode.labelKey.Translate(), () => SetMode(index)));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        };
    }
}

// Holds the pump comps that are not the selected mode. Patched by hand so it only exists when a pump has the comp.
[StaticConstructorOnStartup]
public static class MultiPumpPatches
{
    private static readonly string[] Skipped = { "CompTick", "CompTickRare", "PostDrawExtraSelectionOverlays" };

    static MultiPumpPatches()
    {
        List<PumpMode> modes = DefDatabase<ThingDef>.AllDefs
            .SelectMany(d => d.comps ?? new List<CompProperties>())
            .OfType<CompProperties_MultiPump>()
            .SelectMany(c => c.modes)
            .ToList();
        if (modes.Count == 0)
        {
            return;
        }
        Harmony harmony = new("bambaryla.bor.multipump");
        List<System.Type> types = modes.Select(m => GenTypes.GetTypeInAnyAssembly(m.comp)).Where(t => t != null).Distinct().ToList();
        types.Add(typeof(CompTerrainPump));
        foreach (System.Type type in types)
        {
            foreach (string name in Skipped)
            {
                Patch(harmony, type, name, nameof(Hold));
            }
            Patch(harmony, type, nameof(ThingComp.CompInspectStringExtra), nameof(HideString));
            Patch(harmony, type, nameof(ThingComp.CompGetGizmosExtra), nameof(HideGizmos));
        }
    }

    private static void Patch(Harmony harmony, System.Type type, string method, string handler)
    {
        MethodInfo? original = AccessTools.DeclaredMethod(type, method);
        if (original == null)
        {
            return;
        }
        HarmonyMethod patch = new(typeof(MultiPumpPatches), handler);
        if (handler == nameof(Hold))
        {
            harmony.Patch(original, prefix: patch);
        }
        else
        {
            harmony.Patch(original, postfix: patch);
        }
    }

    private static bool Active(ThingComp comp) => comp.parent.GetComp<CompMultiPump>()?.IsActive(comp) ?? true;

    public static bool Hold(ThingComp __instance) => Active(__instance);

    public static void HideString(ThingComp __instance, ref string? __result)
    {
        if (!Active(__instance))
        {
            __result = null;
        }
    }

    public static void HideGizmos(ThingComp __instance, ref IEnumerable<Gizmo> __result)
    {
        if (!Active(__instance))
        {
            __result = Enumerable.Empty<Gizmo>();
        }
    }
}
