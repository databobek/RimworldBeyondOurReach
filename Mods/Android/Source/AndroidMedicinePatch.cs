using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace BeyondOurReach.Android;

// Loaded only with VRE Androids (see LoadFolders.xml); patched via PatchAll in AndroidMedicineMod.
public static class AndroidMedicinePatches
{
    // Vanilla only offers medicine to a surgery bill through WorkGiver_DoBill.AddEveryMedicineToRelevantThings. VRE Androids
    // skips that method for android patients, so a recipe that names a BOR medicine as an ingredient is never fulfilled and
    // the surgery is never started. When another mod has skipped the method, offer BOR medicine ourselves; recipes that
    // don't ask for it are unaffected, and nothing changes for any other pawn.
    [HarmonyPatch(typeof(WorkGiver_DoBill), "AddEveryMedicineToRelevantThings")]
    public static class WorkGiver_DoBill_AddEveryMedicineToRelevantThings_Patch
    {
        private static readonly List<Thing> tmp = new List<Thing>();

        public static void Postfix(bool __runOriginal, Pawn pawn, Thing billGiver, List<Thing> relevantThings,
            Predicate<Thing> baseValidator, Map map)
        {
            if (__runOriginal)
            {
                return;
            }
            var medicineCategory = BOR_MedicineDefOf.BOR_Medicine;
            tmp.Clear();
            foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.Medicine))
            {
                if (thing.def.IsWithinCategory(medicineCategory) && baseValidator(thing)
                    && pawn.CanReach(thing, PathEndMode.OnCell, Danger.Deadly))
                {
                    tmp.Add(thing);
                }
            }
            tmp.SortBy(x => -x.GetStatValue(StatDefOf.MedicalPotency), x => x.Position.DistanceToSquared(billGiver.Position));
            relevantThings.AddRange(tmp);
            tmp.Clear();
        }
    }

    [DefOf]
    public static class BOR_MedicineDefOf
    {
        public static ThingCategoryDef BOR_Medicine = null!;

        static BOR_MedicineDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(BOR_MedicineDefOf));
    }
}
