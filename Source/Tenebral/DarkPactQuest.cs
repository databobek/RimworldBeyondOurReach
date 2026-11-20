using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace BeyondOurReach;

public enum DarkPactObjective
{
    DestroySettlement,
    KillPeople,
    ExecuteColonist,
    SlaughterAnimals,
    BurnAlive,
    BetrayGuests,
    Kinslayer,
    HarvestOrgans,
    Ossuary
}

// Rolls the price of a dark pact. The counts are tuned in the BOR_DarkPact quest script.
public class QuestNode_Root_DarkPact : QuestNode
{
    // Rolled each time the pact is made.
    public IntRange people = new(40, 60);
    public IntRange animals = new(100, 150);
    public int burned = 10;
    public int guests = 5;
    public int organs = 10;
    public int corpses = 15;
    public float ossuaryRadius = 10f;
    public float durationDays = 7f;
    // About three days of caravan travel, so a settlement can be reached and razed in time.
    public float maxSettlementDistance = 25f;

    public static QuestNode_Root_DarkPact Settings => (QuestNode_Root_DarkPact)BOR_DarkPactDefOf.BOR_DarkPact.root;

    public static int DurationTicks => (int)(Settings.durationDays * GenDate.TicksPerDay);

    public static string DurationLabel => DurationTicks.ToStringTicksToPeriod();

    protected override bool TestRunInt(Slate slate) => true;

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        Quest quest = QuestGen.quest;
        ResearchProjectDef research = slate.Get<ResearchProjectDef>("research");
        Pawn pawn = slate.Get<Pawn>("pawn");
        QuestPart_DarkPact part = new()
        {
            research = research,
            pactMaker = pawn,
            mirror = slate.Get<Thing>("mirror"),
            durationTicks = DurationTicks,
            radius = ossuaryRadius,
            inSignalEnable = slate.Get<string>("inSignal")
        };

        List<DarkPactObjective> pool = new()
        {
            DarkPactObjective.KillPeople, DarkPactObjective.SlaughterAnimals, DarkPactObjective.BurnAlive,
            DarkPactObjective.HarvestOrgans, DarkPactObjective.Ossuary
        };
        Settlement? settlement = PickSettlement();
        if (settlement != null)
        {
            pool.Add(DarkPactObjective.DestroySettlement);
        }
        Pawn? executee = PickExecutee(pawn);
        if (executee != null)
        {
            pool.Add(DarkPactObjective.ExecuteColonist);
        }
        if (Find.FactionManager.AllFactionsVisible.Any(f => !f.IsPlayer && !f.HostileTo(Faction.OfPlayer)))
        {
            pool.Add(DarkPactObjective.BetrayGuests);
        }
        (Pawn killer, Pawn relative)? kin = PickKin();
        if (kin != null)
        {
            pool.Add(DarkPactObjective.Kinslayer);
        }

        part.objective = pool.RandomElement();
        switch (part.objective)
        {
            case DarkPactObjective.DestroySettlement:
                part.settlement = settlement;
                part.settlementTag = QuestGen.GenerateNewTargetQuestTag("settlement");
                QuestUtility.AddQuestTag(settlement, part.settlementTag);
                part.objectiveText = "BOR_DarkPact_Obj_DestroySettlement".Translate(settlement!.LabelCap, settlement.Faction.Name);
                break;
            case DarkPactObjective.KillPeople:
                part.goal = people.RandomInRange;
                part.objectiveText = "BOR_DarkPact_Obj_KillPeople".Translate(part.goal);
                break;
            case DarkPactObjective.ExecuteColonist:
                part.target = executee;
                part.objectiveText = "BOR_DarkPact_Obj_ExecuteColonist".Translate(executee!.LabelShortCap);
                break;
            case DarkPactObjective.SlaughterAnimals:
                part.goal = animals.RandomInRange;
                part.objectiveText = "BOR_DarkPact_Obj_KillAnimals".Translate(part.goal);
                break;
            case DarkPactObjective.BurnAlive:
                part.goal = burned;
                part.objectiveText = "BOR_DarkPact_Obj_BurnAlive".Translate(burned);
                break;
            case DarkPactObjective.BetrayGuests:
                part.goal = guests;
                part.objectiveText = "BOR_DarkPact_Obj_BetrayGuests".Translate(guests);
                break;
            case DarkPactObjective.Kinslayer:
                (Pawn killer, Pawn relative) = kin!.Value;
                part.killer = killer;
                part.target = relative;
                string relation = killer.GetMostImportantRelation(relative)?.GetGenderSpecificLabel(relative) ?? "";
                part.objectiveText = "BOR_DarkPact_Obj_Kinslayer".Translate(killer.LabelShortCap, relative.LabelShortCap, relation);
                break;
            case DarkPactObjective.HarvestOrgans:
                part.goal = organs;
                part.objectiveText = "BOR_DarkPact_Obj_HarvestOrgans".Translate(organs);
                break;
            case DarkPactObjective.Ossuary:
                part.goal = corpses;
                part.objectiveText = "BOR_DarkPact_Obj_Ossuary".Translate(corpses, ossuaryRadius.ToString("0"));
                break;
        }
        quest.AddPart(part);
        slate.Set("resolvedQuestName", "BOR_DarkPact_QuestName".Translate(research.LabelCap).Resolve());
        slate.Set("resolvedQuestDescription",
            "BOR_DarkPact_QuestDescription".Translate(pawn.LabelShortCap, research.LabelCap, part.objectiveText, DurationLabel).Resolve());
    }

    // A settlement of a faction that still calls the colony a friend: an ally if there is one in range, else anyone not hostile.
    private Settlement? PickSettlement()
    {
        List<Map> homes = Find.Maps.Where(m => m.IsPlayerHome).ToList();
        List<Settlement> inRange = Find.WorldObjects.Settlements.Where(s =>
            s.Faction != null && !s.Faction.IsPlayer && !s.Faction.Hidden && !s.Faction.def.permanentEnemy && !s.Faction.HostileTo(Faction.OfPlayer)
            && homes.Any(m => m.Tile.Layer == s.Tile.Layer && Find.WorldGrid.ApproxDistanceInTiles(m.Tile, s.Tile) <= maxSettlementDistance)).ToList();
        List<Settlement> allies = inRange.Where(s => s.Faction.PlayerRelationKind == FactionRelationKind.Ally).ToList();
        return (allies.Count > 0 ? allies : inRange).RandomElementWithFallback();
    }

    private static Pawn? PickExecutee(Pawn pactMaker)
    {
        List<Pawn> colonists = PawnsFinder.AllMaps_FreeColonistsSpawned;
        if (colonists.Count < 2)
        {
            return null;
        }
        return colonists.Where(p => p != pactMaker && p.DevelopmentalStage.Adult()).RandomElementWithFallback();
    }

    // A colonist with an adult relative or partner who is on a home map and not an enemy: a colonist, prisoner, slave or guest.
    private static (Pawn, Pawn)? PickKin()
    {
        List<(Pawn, Pawn)> pairs = new();
        foreach (Pawn colonist in PawnsFinder.AllMaps_FreeColonistsSpawned.Where(p => p.relations != null && p.DevelopmentalStage.Adult()))
        {
            IEnumerable<Pawn> kin = colonist.relations.FamilyByBlood
                .Concat(LovePartnerRelationUtility.ExistingLovePartners(colonist, false).Select(r => r.otherPawn));
            foreach (Pawn relative in kin.Distinct())
            {
                if (relative != colonist && !relative.Dead && relative.Spawned && relative.Map.IsPlayerHome && relative.RaceProps.Humanlike
                    && relative.DevelopmentalStage.Adult() && (relative.IsPrisonerOfColony || !relative.HostileTo(Faction.OfPlayer)))
                {
                    pairs.Add((colonist, relative));
                }
            }
        }
        return pairs.Count > 0 ? pairs.RandomElement() : null;
    }
}

// Tracks a dark pact: counts the deeds, watches the targets and the clock, and settles the bargain either way.
public class QuestPart_DarkPact : QuestPartActivable
{
    private const int LongTickInterval = GenTicks.TickLongInterval;

    public ResearchProjectDef? research;
    public DarkPactObjective objective;
    public string objectiveText = "";
    public int goal;
    public int progress;
    public int durationTicks;
    public float radius;
    public Pawn? pactMaker;
    // The colonist to execute, or the relative the kinslayer must kill.
    public Pawn? target;
    public Pawn? killer;
    public Thing? mirror;
    public Settlement? settlement;
    public string? settlementTag;
    private List<int> betrayed = new();

    private int ossuaryCount;
    private int ossuaryCountTick = -1;

    public int TicksLeft => State == QuestPartState.Enabled ? enableTick + durationTicks - Find.TickManager.TicksGame : 0;

    public bool Counted => goal > 0;

    public int Current => objective == DarkPactObjective.Ossuary ? OssuaryCount(false) : progress;

    // Shown on the dark mirror.
    public string StatusLine => Counted ? "BOR_DarkPact_Progress".Translate(Current, goal).Resolve() : objectiveText;

    public override string ExpiryInfoPart => quest.Historical ? null! : "BOR_DarkPact_TimeLeft".Translate(TicksLeft.ToStringTicksToPeriod());

    public override string DescriptionPart => State == QuestPartState.Enabled && Counted ? "BOR_DarkPact_Progress".Translate(Current, goal) : null!;

    public override IEnumerable<GlobalTargetInfo> QuestLookTargets
    {
        get
        {
            if (mirror is { Spawned: true })
            {
                yield return mirror;
            }
            if (settlement is { Destroyed: false })
            {
                yield return settlement;
            }
            if (target != null)
            {
                yield return target;
            }
            if (killer != null)
            {
                yield return killer;
            }
        }
    }

    public override bool QuestPartReserves(Pawn p) => State == QuestPartState.Enabled && (p == target || p == killer);

    public override void QuestPartTick()
    {
        if (TicksLeft <= 0)
        {
            Fail("BOR_DarkPact_Reason_Expired".Translate());
            return;
        }
        if (Find.TickManager.TicksGame % LongTickInterval != 0)
        {
            return;
        }
        switch (objective)
        {
            case DarkPactObjective.Ossuary when OssuaryCount(true) >= goal:
            // The signal normally gets there first; this catches a settlement removed some other way.
            case DarkPactObjective.DestroySettlement when settlement == null || settlement.Destroyed:
                Succeed();
                break;
            case DarkPactObjective.ExecuteColonist or DarkPactObjective.Kinslayer when target == null || (target.Destroyed && !target.Dead):
                Fail("BOR_DarkPact_Reason_TargetLost".Translate(target?.LabelShortCap ?? "?"));
                break;
        }
    }

    protected override void ProcessQuestSignal(Signal signal)
    {
        if (settlementTag != null && signal.tag == settlementTag + ".Destroyed")
        {
            Succeed();
        }
    }

    public override void Notify_PawnKilled(Pawn pawn, DamageInfo? dinfo)
    {
        if (State != QuestPartState.Enabled)
        {
            return;
        }
        Thing? instigator = dinfo?.Instigator;
        bool byColony = instigator?.Faction == Faction.OfPlayer;
        switch (objective)
        {
            case DarkPactObjective.KillPeople when byColony && pawn.RaceProps.Humanlike:
            case DarkPactObjective.SlaughterAnimals when byColony && pawn.RaceProps.Animal:
            case DarkPactObjective.BurnAlive when pawn.RaceProps.Humanlike && (Pawn_Kill_DarkPact_Patch.dyingBurning == pawn || dinfo?.Def == DamageDefOf.Flame || dinfo?.Def == DamageDefOf.Burn):
            case DarkPactObjective.BetrayGuests when byColony && betrayed.Contains(pawn.thingIDNumber):
                Count();
                break;
            case DarkPactObjective.ExecuteColonist when pawn == target:
                if (byColony && dinfo?.Def == DamageDefOf.ExecutionCut)
                {
                    Succeed();
                }
                else
                {
                    Fail("BOR_DarkPact_Reason_TargetDiedWrong".Translate(pawn.LabelShortCap));
                }
                break;
            case DarkPactObjective.Kinslayer when pawn == target:
                if (instigator == killer)
                {
                    Succeed();
                }
                else
                {
                    Fail("BOR_DarkPact_Reason_TargetDiedWrong".Translate(pawn.LabelShortCap));
                }
                break;
            case DarkPactObjective.Kinslayer when pawn == killer:
                Fail("BOR_DarkPact_Reason_KillerDied".Translate(pawn.LabelShortCap));
                break;
        }
    }

    public override void Notify_PawnDiscarded(Pawn pawn)
    {
        if (State == QuestPartState.Enabled && (pawn == target || pawn == killer))
        {
            Fail("BOR_DarkPact_Reason_TargetLost".Translate(pawn.LabelShortCap));
        }
    }

    public void Notify_Betrayal(Pawn victim)
    {
        if (State == QuestPartState.Enabled && objective == DarkPactObjective.BetrayGuests && !betrayed.Contains(victim.thingIDNumber))
        {
            betrayed.Add(victim.thingIDNumber);
        }
    }

    public void Notify_OrganHarvested()
    {
        if (State == QuestPartState.Enabled && objective == DarkPactObjective.HarvestOrgans)
        {
            Count();
        }
    }

    private void Count()
    {
        progress++;
        if (progress >= goal)
        {
            Succeed();
        }
    }

    // Humanlike corpses, skeletons too, lying within reach of any of the colony's dark mirrors. Counted at most once a
    // second for the inspect string and quest window, which ask every frame.
    private int OssuaryCount(bool fresh)
    {
        int now = Find.TickManager.TicksGame;
        if (!fresh && ossuaryCountTick >= 0 && now - ossuaryCountTick < GenTicks.TicksPerRealSecond)
        {
            return ossuaryCount;
        }
        int best = 0;
        foreach (Map map in Find.Maps)
        {
            List<Thing> mirrors = DarkPacts.MirrorsOn(map).ToList();
            if (mirrors.Count == 0)
            {
                continue;
            }
            List<Thing> corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
            foreach (Thing m in mirrors)
            {
                int count = corpses.Count(c => c is Corpse { InnerPawn: not null } corpse && corpse.InnerPawn.RaceProps.Humanlike
                    && corpse.Position.InHorDistOf(m.Position, radius));
                best = System.Math.Max(best, count);
            }
        }
        ossuaryCount = best;
        ossuaryCountTick = now;
        return best;
    }

    public void Succeed()
    {
        if (State != QuestPartState.Enabled)
        {
            return;
        }
        Complete();
        if (research != null && !research.IsFinished)
        {
            Find.ResearchManager.FinishProject(research, true, pactMaker is { Dead: false } ? pactMaker : null);
        }
        Find.LetterStack.ReceiveLetter("BOR_DarkPact_SuccessLabel".Translate(), "BOR_DarkPact_SuccessText".Translate(research?.LabelCap ?? ""),
            LetterDefOf.PositiveEvent, mirror is { Spawned: true } ? mirror : LookTargets.Invalid, null, quest);
        quest.End(QuestEndOutcome.Success, sendLetter: false);
    }

    // Every broken pact is punished, abandoned ones too: the pulse starts at the mirror the pact was made at, or any other.
    public void Fail(string reason)
    {
        if (State != QuestPartState.Enabled)
        {
            return;
        }
        Complete();
        Thing? source = mirror is { Spawned: true } && mirror.Map.IsPlayerHome ? mirror
            : Find.Maps.Where(m => m.IsPlayerHome).SelectMany(DarkPacts.MirrorsOn).FirstOrDefault();
        string label = research?.LabelCap ?? "";
        if (source != null)
        {
            source.Map.GetComponent<MapComponent_DarkPulse>()?.Emit(source.Position);
        }
        string text = (source != null ? "BOR_DarkPact_FailedText" : "BOR_DarkPact_FailedTextNoMirror").Translate(label, reason);
        Find.LetterStack.ReceiveLetter("BOR_DarkPact_FailedLabel".Translate(), text, LetterDefOf.NegativeEvent,
            source != null ? source : LookTargets.Invalid, null, quest);
        quest.End(QuestEndOutcome.Fail, sendLetter: false);
    }

    // The quest stays in the history; nothing there needs these, and dead or destroyed ones would not load cleanly.
    public override void Cleanup()
    {
        base.Cleanup();
        pactMaker = null;
        target = null;
        killer = null;
        mirror = null;
        settlement = null;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref research, "research");
        Scribe_Values.Look(ref objective, "objective");
        Scribe_Values.Look(ref objectiveText, "objectiveText", "");
        Scribe_Values.Look(ref goal, "goal");
        Scribe_Values.Look(ref progress, "progress");
        Scribe_Values.Look(ref durationTicks, "durationTicks");
        Scribe_Values.Look(ref radius, "radius");
        Scribe_References.Look(ref pactMaker, "pactMaker");
        Scribe_References.Look(ref target, "target");
        Scribe_References.Look(ref killer, "killer");
        Scribe_References.Look(ref mirror, "mirror");
        Scribe_References.Look(ref settlement, "settlement");
        Scribe_Values.Look(ref settlementTag, "settlementTag");
        Scribe_Collections.Look(ref betrayed, "betrayed", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            betrayed ??= new List<int>();
            objectiveText ??= "";
        }
    }
}

// The betrayal pact: remembers guests the colony hurts while they still trust it.
[HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
public static class Thing_TakeDamage_DarkPact_Patch
{
    public static void Prefix(Thing __instance, DamageInfo dinfo)
    {
        if (__instance is Pawn victim && dinfo.Instigator?.Faction == Faction.OfPlayer)
        {
            DarkPacts.Notify_ColonyHurt(victim);
        }
    }
}

// The organ pact: a natural organ cut out of a living humanlike by the colony's surgeon. Vanilla only spawns the
// removed organ when the surgery succeeds, so that spawn is what counts; a botched surgery that wrecks the organ does not.
[HarmonyPatch(typeof(Recipe_RemoveBodyPart), nameof(Recipe_RemoveBodyPart.ApplyOnPawn))]
public static class Recipe_RemoveBodyPart_DarkPact_Patch
{
    // The surgeon while a removal is running; installing a bionic also spawns the natural part, and must not count.
    public static Pawn? surgeon;

    public static void Prefix(Pawn billDoer, out Pawn? __state)
    {
        __state = surgeon;
        surgeon = billDoer;
    }

    public static void Finalizer(Pawn? __state) => surgeon = __state;
}

[HarmonyPatch(typeof(MedicalRecipesUtility), nameof(MedicalRecipesUtility.SpawnNaturalPartIfClean))]
public static class MedicalRecipesUtility_SpawnNaturalPartIfClean_DarkPact_Patch
{
    public static void Postfix(Pawn pawn, Thing __result)
    {
        if (__result != null && Recipe_RemoveBodyPart_DarkPact_Patch.surgeon?.Faction == Faction.OfPlayer && !pawn.Dead && pawn.RaceProps.Humanlike)
        {
            DarkPacts.Active?.Notify_OrganHarvested();
        }
    }
}

// The burning pact: vanilla despawns a dying pawn (putting out its fire) before it tells the quests, so whether it
// was burning is noted here first.
[HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
public static class Pawn_Kill_DarkPact_Patch
{
    public static Pawn? dyingBurning;

    public static void Prefix(Pawn __instance, out Pawn? __state)
    {
        __state = dyingBurning;
        dyingBurning = __instance.Spawned && __instance.IsBurning() ? __instance : null;
    }

    public static void Finalizer(Pawn? __state) => dyingBurning = __state;
}
