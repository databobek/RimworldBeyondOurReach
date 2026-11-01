using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

public class CompProperties_KwazaariUplink : CompProperties
{
    public int connectTicks = 180;

    public CompProperties_KwazaariUplink()
    {
        compClass = typeof(CompKwazaariUplink);
    }
}

// "Connect with Kwazaari" on the comm console: a colonist works the console briefly, then the Kwazaari window opens.
public class CompKwazaariUplink : ThingComp
{
    public CompProperties_KwazaariUplink Props => (CompProperties_KwazaariUplink)props;

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo g in base.CompGetGizmosExtra())
        {
            yield return g;
        }
        if (!parent.Spawned || parent.Faction != Faction.OfPlayer)
        {
            yield break;
        }
        Command_Action cmd = new Command_Action
        {
            defaultLabel = "BOR_Kwazaari_Connect_Label".Translate(),
            defaultDesc = "BOR_Kwazaari_Connect_Desc".Translate(),
            icon = KwazaariTex.Icon,
            action = () => SignalUtility.OpenPawnMenu(parent, BOR_KwazaariDefOf.BOR_ConnectKwazaari, 0)
        };
        if (!SignalUtility.Powered(parent))
        {
            cmd.Disable("NoPower".Translate());
        }
        yield return cmd;

        if (DebugSettings.ShowDevGizmos)
        {
            yield return new Command_Action
            {
                defaultLabel = "DEV: Open Kwazaari window",
                action = () => Find.WindowStack.Add(new Dialog_Kwazaari(parent))
            };
            yield return new Command_Action
            {
                defaultLabel = "DEV: +10 scrip",
                action = () => GameComponent_Kwazaari.Instance.AddScrip(10f)
            };
        }
    }
}

public class JobDriver_ConnectKwazaari : JobDriver
{
    private int ticksDone;

    private Thing Console => job.GetTarget(TargetIndex.A).Thing;

    private int TotalTicks => Console.TryGetComp<CompKwazaariUplink>()?.Props.connectTicks ?? 180;

    public override bool TryMakePreToilReservations(bool errorOnFailed) =>
        pawn.Reserve(Console, job, 1, -1, null, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOn(() => !SignalUtility.Powered(Console));

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);

        Toil work = ToilMaker.MakeToil("ConnectKwazaari");
        work.initAction = () => ticksDone = 0;
        work.tickAction = () =>
        {
            ticksDone++;
            pawn.rotationTracker.FaceTarget(Console);
            if (ticksDone >= TotalTicks)
            {
                ReadyForNextToil();
            }
        };
        work.defaultCompleteMode = ToilCompleteMode.Never;
        work.WithProgressBar(TargetIndex.A, () => (float)ticksDone / Mathf.Max(1, TotalTicks));
        yield return work;

        yield return Toils_General.Do(() => Find.WindowStack.Add(new Dialog_Kwazaari(Console)));
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticksDone, "ticksDone");
    }
}

// Patched via the assembly-wide PatchAll in SettingsWindowPatch.
public static class KwazaariPatches
{
    private static bool IsKwazaari(Faction? f) => f != null && f.def == BOR_KwazaariDefOf.BOR_KwazaarBazaar;

    private static bool Borrowed(Pawn? pawn) => pawn != null && Current.Game != null && GameComponent_Kwazaari.Instance.IsBorrowed(pawn);

    // Kwazaari stay neutral: goodwill with the player never changes.
    [HarmonyPatch(typeof(Faction), nameof(Faction.TryAffectGoodwillWith))]
    public static class Faction_TryAffectGoodwillWith_Patch
    {
        public static bool Prefix(Faction __instance, Faction other, ref bool __result)
        {
            if ((IsKwazaari(__instance) && other != null && other.IsPlayer) || (__instance.IsPlayer && IsKwazaari(other)))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    // Lent soldiers join the player's faction but keep the Kwazaari as their home faction, like quest-lent pawns.
    [HarmonyPatch(typeof(QuestUtility), nameof(QuestUtility.GetExtraFactionsFromQuestParts))]
    public static class QuestUtility_GetExtraFactionsFromQuestParts_Patch
    {
        public static void Postfix(Pawn pawn, List<ExtraFaction> outExtraFactions, Quest? forQuest)
        {
            Faction? kwazaari;
            if (forQuest != null || !Borrowed(pawn) || (kwazaari = GameComponent_Kwazaari.Faction) == null
                || outExtraFactions.Any(e => e.faction == kwazaari))
            {
                return;
            }
            outExtraFactions.Add(new ExtraFaction(kwazaari, ExtraFactionType.HomeFaction));
        }
    }

    // Vanilla leaves hidden factions out of the info card's extra faction lines, so add the Kwazaari one here.
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.FactionDesc))]
    public static class Pawn_FactionDesc_Patch
    {
        public static void Postfix(Pawn __instance, bool extraFactionsInfo, ref TaggedString __result)
        {
            Faction? kwazaari;
            if (!extraFactionsInfo || !Borrowed(__instance) || (kwazaari = GameComponent_Kwazaari.Faction) == null)
            {
                return;
            }
            __result += "\n" + ExtraFactionType.HomeFaction.GetLabel().CapitalizeFirst() + ": " + kwazaari.NameColored.Resolve();
        }
    }

    // Lent soldiers keep their gear: no stripping, no dropping or swapping their weapon.
    [HarmonyPatch(typeof(StrippableUtility), nameof(StrippableUtility.CanBeStrippedByColony))]
    public static class StrippableUtility_CanBeStrippedByColony_Patch
    {
        public static void Postfix(Thing th, ref bool __result)
        {
            if (__result && th is Pawn pawn && Borrowed(pawn))
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(ITab_Pawn_Gear), "InterfaceDrop")]
    public static class ITab_Pawn_Gear_InterfaceDrop_Patch
    {
        public static bool Prefix(Thing t)
        {
            Pawn? owner = (t.ParentHolder as Pawn_EquipmentTracker)?.pawn ?? (t.ParentHolder as Pawn_ApparelTracker)?.pawn;
            if (!Borrowed(owner))
            {
                return true;
            }
            Messages.Message("BOR_Kwazaari_GearLocked".Translate(), owner, MessageTypeDefOf.RejectInput, false);
            return false;
        }
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.TryTakeOrderedJob))]
    public static class Pawn_JobTracker_TryTakeOrderedJob_Patch
    {
        public static bool Prefix(Pawn ___pawn, Job job, ref bool __result)
        {
            if ((job.def != JobDefOf.Equip && job.def != JobDefOf.DropEquipment) || !Borrowed(___pawn))
            {
                return true;
            }
            Messages.Message("BOR_Kwazaari_GearLocked".Translate(), ___pawn, MessageTypeDefOf.RejectInput, false);
            __result = false;
            return false;
        }
    }
}
