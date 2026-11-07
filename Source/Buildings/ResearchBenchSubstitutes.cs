using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BeyondOurReach;

// Vanilla only lets a research project run at a bench whose def is exactly requiredResearchBuilding.
// A bench with this extension also counts as every bench in countsAs, so a higher tier bench can do the lower tier work.
public class DefModExtension_CountsAsResearchBench : DefModExtension
{
    public List<ThingDef> countsAs = new();
}

// Patched via the assembly-wide PatchAll in SettingsWindowPatch.
public static class ResearchBenchSubstitutePatches
{
    private static readonly FieldInfo ThingDef_Field = AccessTools.Field(typeof(Thing), nameof(Thing.def));
    private static readonly MethodInfo EffectiveDef_Method = AccessTools.Method(typeof(ResearchBenchSubstitutePatches), nameof(EffectiveDef));

    /// <summary>The def the bench is compared by: the required def when the bench counts as it, otherwise its own def.</summary>
    public static ThingDef EffectiveDef(Thing bench, ThingDef? required)
    {
        if (required != null && bench.def != required
            && bench.def.GetModExtension<DefModExtension_CountsAsResearchBench>()?.countsAs.Contains(required) == true)
        {
            return required;
        }
        return bench.def;
    }

    // Replaces the single bench.def read with EffectiveDef(bench, required), leaving the rest of vanilla's method untouched.
    private static IEnumerable<CodeInstruction> RewriteBenchDef(IEnumerable<CodeInstruction> instructions, params CodeInstruction[] loadRequired)
    {
        bool done = false;
        foreach (CodeInstruction instruction in instructions)
        {
            if (!done && instruction.LoadsField(ThingDef_Field))
            {
                done = true;
                for (int i = 0; i < loadRequired.Length; i++)
                {
                    CodeInstruction load = loadRequired[i].Clone();
                    if (i == 0)
                    {
                        load.labels.AddRange(instruction.labels);
                    }
                    yield return load;
                }
                yield return new CodeInstruction(OpCodes.Call, EffectiveDef_Method);
                continue;
            }
            yield return instruction;
        }
        if (!done)
        {
            Log.Error("[BOR] Research bench substitutes: bench.def read not found, patch skipped.");
        }
    }

    [HarmonyPatch(typeof(ResearchProjectDef), nameof(ResearchProjectDef.CanBeResearchedAt))]
    public static class ResearchProjectDef_CanBeResearchedAt_Patch
    {
        private static readonly FieldInfo Required = AccessTools.Field(typeof(ResearchProjectDef), nameof(ResearchProjectDef.requiredResearchBuilding));

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
            RewriteBenchDef(instructions, new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldfld, Required));
    }

    // Research tab: the facility list is read off the best matching bench, which must include substitutes.
    [HarmonyPatch(typeof(MainTabWindow_Research), "FindBenchFulfillingMostRequirements")]
    public static class MainTabWindow_Research_FindBench_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
            RewriteBenchDef(instructions, new CodeInstruction(OpCodes.Ldarg_1));
    }

    // "Need a research bench" alert: a substitute bench satisfies it too.
    [HarmonyPatch(typeof(Alert_NeedResearchBench), "HasRequiredResearchBench", MethodType.Getter)]
    public static class Alert_NeedResearchBench_Patch
    {
        public static void Postfix(ref bool __result)
        {
            if (__result)
            {
                return;
            }
            ThingDef? required = Find.ResearchManager.GetProject()?.requiredResearchBuilding;
            if (required == null)
            {
                return;
            }
            foreach (Map map in Find.Maps)
            {
                foreach (Building building in map.listerBuildings.allBuildingsColonist)
                {
                    if (EffectiveDef(building, required) == required)
                    {
                        __result = true;
                        return;
                    }
                }
            }
        }
    }
}
