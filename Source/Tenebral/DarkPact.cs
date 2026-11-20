using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

// Marks a research that only a dark pact at the dark mirror can finish. Set by BOR_Research_Base_III_Pact.
public class DefModExtension_DarkPact : DefModExtension
{
}

[DefOf]
public static class BOR_DarkPactDefOf
{
    public static QuestScriptDef BOR_DarkPact = null!;
    public static JobDef BOR_DarkMirrorPact = null!;
    public static HediffDef BOR_DarkPunishment = null!;

    static BOR_DarkPactDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BOR_DarkPactDefOf));
    }
}

public static class DarkPacts
{
    private static List<ResearchProjectDef>? all;

    private static readonly Dictionary<ResearchProjectDef, ThingDef?> iconDefs = new();

    public static readonly Color DarkRed = new(1f, 0.12f, 0.12f);

    // In DefDatabase order, which is stable for a given mod list; the pact job passes its research as an index into this.
    public static List<ResearchProjectDef> All => all ??= DefDatabase<ResearchProjectDef>.AllDefsListForReading.Where(r => r.HasModExtension<DefModExtension_DarkPact>()).ToList();

    // The icon a Pacts tab row shows: a building the research unlocks if it has one, else the first unlocked thing with an icon.
    // Some researches only unlock modes of other buildings; they get the mirror's icon. The tab redraws every frame, so this is kept.
    public static ThingDef? IconDefFor(ResearchProjectDef research)
    {
        if (!iconDefs.TryGetValue(research, out ThingDef? def))
        {
            List<ThingDef> things = research.UnlockedDefs.OfType<ThingDef>().Where(d => d.uiIcon != null && d.uiIcon != BaseContent.BadTex).ToList();
            def = things.FirstOrDefault(d => d.building != null) ?? things.FirstOrDefault();
            iconDefs[research] = def;
        }
        return def;
    }

    // The pact under way, if any. Only one runs at a time.
    public static QuestPart_DarkPact? Active
    {
        get
        {
            List<Quest> quests = Find.QuestManager.ActiveQuestsListForReading;
            for (int i = 0; i < quests.Count; i++)
            {
                if (quests[i].State != QuestState.Ongoing)
                {
                    continue;
                }
                List<QuestPart> parts = quests[i].PartsListForReading;
                for (int j = 0; j < parts.Count; j++)
                {
                    if (parts[j] is QuestPart_DarkPact { State: QuestPartState.Enabled } pact)
                    {
                        return pact;
                    }
                }
            }
            return null;
        }
    }

    public static bool IsMirror(Thing thing) => thing.TryGetComp<CompDarkPact>() != null;

    public static IEnumerable<Thing> MirrorsOn(Map map) => map.listerBuildings.allBuildingsColonist.Where(IsMirror);

    public static bool Powered(Thing mirror) => mirror.TryGetComp<CompPowerTrader>()?.PowerOn ?? true;

    // The colony hit someone who still trusted it. Remembered for the betrayal pact, since that first hit can turn the
    // victim's faction hostile before they die.
    public static void Notify_ColonyHurt(Pawn victim)
    {
        if (victim.Dead || !victim.RaceProps.Humanlike || victim.Faction == null || victim.Faction.IsPlayer
            || victim.IsPrisoner || victim.IsSlave || victim.HostileTo(Faction.OfPlayer))
        {
            return;
        }
        Active?.Notify_Betrayal(victim);
    }
}

public class CompProperties_DarkPact : CompProperties
{
    public CompProperties_DarkPact()
    {
        compClass = typeof(CompDarkPact);
    }
}

// The dark mirror's pacts, driven from its Pacts tab (ITab_DarkPacts).
public class CompDarkPact : ThingComp
{
    private bool Communing => parent.Map.mapPawns.FreeColonistsSpawned.Any(p => p.CurJobDef == BOR_DarkPactDefOf.BOR_DarkMirrorPact);

    // Why a pact for this research cannot be made right now, or null if it can.
    public string? BlockReason(ResearchProjectDef r)
    {
        if (!r.PrerequisitesCompleted)
        {
            IEnumerable<ResearchProjectDef> missing = (r.prerequisites ?? new List<ResearchProjectDef>())
                .Concat(r.hiddenPrerequisites ?? new List<ResearchProjectDef>()).Where(p => !p.IsFinished);
            return "BOR_DarkPact_NeedsResearch".Translate(missing.Select(p => p.LabelCap.Resolve()).ToCommaList());
        }
        if (DarkPacts.Active is QuestPart_DarkPact active)
        {
            return "BOR_DarkPact_Active".Translate(active.research?.LabelCap ?? "");
        }
        if (Communing)
        {
            return "BOR_DarkPact_Communing".Translate();
        }
        if (!DarkPacts.Powered(parent))
        {
            return "BOR_DarkPact_NeedsPower".Translate();
        }
        if (r.requiresMechanitor && !MechanitorUtility.AnyMechanitorInPlayerFaction())
        {
            return "BOR_DarkPact_NeedsMechanitor".Translate();
        }
        return null;
    }

    public void Offer(ResearchProjectDef research)
    {
        if (BlockReason(research) == null)
        {
            ChoosePawn(research);
        }
    }

    public static void ConfirmAbandon() =>
        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("BOR_DarkPact_AbandonConfirm".Translate(),
            () => DarkPacts.Active?.Fail("BOR_DarkPact_Reason_Abandoned".Translate()), true));

    private void ChoosePawn(ResearchProjectDef research)
    {
        List<FloatMenuOption> options = new();
        foreach (Pawn pawn in parent.Map.mapPawns.FreeColonistsSpawned)
        {
            Pawn p = pawn;
            if (p.Downed)
            {
                options.Add(new FloatMenuOption(p.LabelShortCap + " (" + "BOR_DarkPact_PawnDowned".Translate() + ")", null));
            }
            else if (!p.CanReach(parent, PathEndMode.InteractionCell, Danger.Deadly))
            {
                options.Add(new FloatMenuOption(p.LabelShortCap + " (" + "NoPath".Translate() + ")", null));
            }
            else
            {
                options.Add(new FloatMenuOption(p.LabelShortCap, () => Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "BOR_DarkPact_Confirm".Translate(p.LabelShortCap, research.LabelCap, QuestNode_Root_DarkPact.DurationLabel), () => Order(p, research), true))));
            }
        }
        if (options.Count == 0)
        {
            options.Add(new FloatMenuOption("BOR_DarkPact_NoColonists".Translate(), null));
        }
        Find.WindowStack.Add(new FloatMenu(options));
    }

    private void Order(Pawn pawn, ResearchProjectDef research)
    {
        if (pawn.Dead || !pawn.Spawned || DarkPacts.Active != null)
        {
            return;
        }
        Job job = JobMaker.MakeJob(BOR_DarkPactDefOf.BOR_DarkMirrorPact, parent);
        job.count = DarkPacts.All.IndexOf(research);
        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
    }

    // The communion is over: the Dark Archotechs name their price.
    public void StartPact(Pawn pawn, ResearchProjectDef research)
    {
        if (DarkPacts.Active != null || research.IsFinished || !research.PrerequisitesCompleted)
        {
            return;
        }
        Slate slate = new();
        slate.Set("research", research);
        slate.Set("pawn", pawn);
        slate.Set("mirror", (Thing)parent);
        Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(BOR_DarkPactDefOf.BOR_DarkPact, slate);
        if (quest == null)
        {
            return;
        }
        QuestUtility.SendLetterQuestAvailable(quest);
        DarkPulse.Flash(parent.Map, parent.TrueCenter(), 4f);
    }

    public override string CompInspectStringExtra()
    {
        QuestPart_DarkPact? active = DarkPacts.Active;
        if (active?.research == null)
        {
            return null!;
        }
        return "BOR_DarkPact_Inspect".Translate(active.research.LabelCap, active.StatusLine, active.TicksLeft.ToStringTicksToPeriod());
    }
}

public class JobDriver_DarkMirrorPact : JobDriver
{
    private const TargetIndex MirrorInd = TargetIndex.A;
    private const int WorkTicks = 600;
    private const int FlashInterval = 60;

    private int ticksDone;

    private Thing Mirror => job.GetTarget(MirrorInd).Thing;

    private ResearchProjectDef? Research => job.count >= 0 && job.count < DarkPacts.All.Count ? DarkPacts.All[job.count] : null;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(Mirror, job, 1, -1, null, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(MirrorInd);
        this.FailOn(() => !DarkPacts.Powered(Mirror) || DarkPacts.Active != null || Research == null || Research.IsFinished);

        yield return Toils_Goto.GotoThing(MirrorInd, PathEndMode.InteractionCell);

        Toil commune = ToilMaker.MakeToil("DarkMirrorPact");
        commune.initAction = () => ticksDone = 0;
        commune.tickAction = () =>
        {
            ticksDone++;
            pawn.rotationTracker.FaceTarget(Mirror);
            if (ticksDone % FlashInterval == 0)
            {
                DarkPulse.Flash(Mirror.Map, Mirror.TrueCenter(), 1.5f + 2f * ticksDone / WorkTicks);
            }
            if (ticksDone >= WorkTicks)
            {
                ReadyForNextToil();
            }
        };
        commune.defaultCompleteMode = ToilCompleteMode.Never;
        commune.WithProgressBar(MirrorInd, () => (float)ticksDone / WorkTicks);
        yield return commune;

        yield return Toils_General.Do(() =>
        {
            if (Research is ResearchProjectDef research)
            {
                Mirror.TryGetComp<CompDarkPact>()?.StartPact(pawn, research);
            }
        });
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref ticksDone, "ticksDone");
    }
}
