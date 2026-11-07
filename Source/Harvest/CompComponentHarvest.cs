using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BeyondOurReach;

public class CompProperties_ComponentHarvest : CompProperties
{
    public int cooldownTicks = GenDate.TicksPerHour;
    public int componentCount = 10;
    public float minCorpseHealthPct = 0.75f;
    public int slaughterCooldownTicks = 10 * GenDate.TicksPerDay;

    public CompProperties_ComponentHarvest()
    {
        compClass = typeof(CompComponentHarvest);
    }
}

public class CompComponentHarvest : ThingComp
{
    private int nextAllowedTick;
    private int slaughterNextAllowedTick;

    public CompProperties_ComponentHarvest Props => (CompProperties_ComponentHarvest)props;

    public void StartCooldown()
    {
        nextAllowedTick = Find.TickManager.TicksGame + Props.cooldownTicks;
    }

    public void StartSlaughterCooldown()
    {
        slaughterNextAllowedTick = Find.TickManager.TicksGame + Props.slaughterCooldownTicks;
    }

    public bool CorpseValid(Corpse corpse)
    {
        return corpse.Spawned
            && corpse.InnerPawn != null
            && corpse.InnerPawn.RaceProps.Humanlike
            && corpse.GetRotStage() == RotStage.Fresh
            && corpse.HitPoints >= Props.minCorpseHealthPct * corpse.MaxHitPoints;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo g in base.CompGetGizmosExtra())
        {
            yield return g;
        }
        if (!parent.Spawned || parent.Faction != Faction.OfPlayer || !BOR_HarvestDefOf.BOR_Research_ComponentHarvest.IsFinished)
        {
            yield break;
        }

        Command_Action cmd = new Command_Action
        {
            defaultLabel = "BOR_Harvest_Label".Translate(),
            defaultDesc = "BOR_Harvest_Desc".Translate(Props.componentCount),
            icon = BOR_HarvestDefOf.BOR_ComponentIII.uiIcon,
            action = () => OpenPawnMenu(false)
        };
        if (parent.Map.listerThings.ThingsOfDef(BOR_HarvestDefOf.BOR_Picoclusters).Count == 0)
        {
            cmd.Disable("BOR_Harvest_NoPicoclusters".Translate());
        }
        else if (Find.TickManager.TicksGame < nextAllowedTick)
        {
            cmd.Disable("BOR_Harvest_Cooldown".Translate((nextAllowedTick - Find.TickManager.TicksGame).ToStringTicksToPeriod()));
        }
        yield return cmd;

        // Hidden until the colony has an Umbral, so it doesn't give the path away.
        if (!RighteousSlaughter.AnyColonyUmbrals)
        {
            yield break;
        }
        DefModExtension_RighteousSlaughter settings = RighteousSlaughter.Settings;
        Command_Action slaughter = new Command_Action
        {
            defaultLabel = "BOR_Slaughter_Label".Translate(),
            defaultDesc = "BOR_Slaughter_Desc".Translate((settings.cooldownSpeed - 1f).ToStringPercent(), (settings.transformationSpeed - 1f).ToStringPercent(),
                RighteousSlaughter.BlessingTicks.ToStringTicksToPeriod(), Props.slaughterCooldownTicks.ToStringTicksToPeriod()),
            icon = ContentFinder<Texture2D>.Get("BOR/Things/Factions/TenebralIcon"),
            action = () => OpenPawnMenu(true)
        };
        if (Find.TickManager.TicksGame < slaughterNextAllowedTick)
        {
            slaughter.Disable("BOR_Slaughter_Cooldown".Translate((slaughterNextAllowedTick - Find.TickManager.TicksGame).ToStringTicksToPeriod()));
        }
        yield return slaughter;
    }

    private void OpenPawnMenu(bool slaughter)
    {
        if (!parent.Spawned)
        {
            return;
        }
        List<FloatMenuOption> options = new();
        foreach (Pawn p in parent.Map.mapPawns.FreeColonistsSpawned)
        {
            if (slaughter && !Umbral.IsUmbral(p))
            {
                continue;
            }
            Pawn pawn = p;
            string tip = "BOR_Harvest_SelectingPawn".Translate(pawn.LabelShortCap);
            if (pawn.Downed || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                options.Add(new FloatMenuOption(pawn.LabelShortCap + " (" + "BOR_Harvest_Incapable".Translate() + ")", null));
                continue;
            }
            options.Add(new FloatMenuOption(pawn.LabelShortCap, () =>
            {
                if (slaughter)
                {
                    OpenVictimMenu(pawn);
                }
                else
                {
                    OpenCorpseMenu(pawn);
                }
            })
            {
                tooltip = new TipSignal(tip)
            });
        }
        if (options.Count == 0)
        {
            Messages.Message((slaughter ? "BOR_Slaughter_NoUmbrals" : "BOR_Harvest_NoPawns").Translate(), parent, MessageTypeDefOf.RejectInput, false);
            return;
        }
        Find.WindowStack.Add(new FloatMenu(options));
    }

    private void OpenVictimMenu(Pawn pawn)
    {
        if (!parent.Spawned || !pawn.Spawned || pawn.Dead || pawn.Downed)
        {
            return;
        }
        List<FloatMenuOption> options = new();
        foreach (Pawn p in parent.Map.mapPawns.PrisonersOfColonySpawned)
        {
            if (RighteousSlaughter.CanBeVictim(p))
            {
                Pawn victim = p;
                options.Add(new FloatMenuOption(victim.LabelShortCap, () => OrderSlaughter(pawn, victim)));
            }
        }
        if (options.Count == 0)
        {
            Messages.Message("BOR_Slaughter_NoPrisoners".Translate(), parent, MessageTypeDefOf.RejectInput, false);
            return;
        }
        Find.WindowStack.Add(new FloatMenu(options));
    }

    private void OrderSlaughter(Pawn pawn, Pawn victim)
    {
        // The menus can stay open for a while; everything may have changed since they were built.
        if (!parent.Spawned || !pawn.Spawned || pawn.Dead || pawn.Downed || !Umbral.IsUmbral(pawn) || !victim.Spawned || !RighteousSlaughter.CanBeVictim(victim)
            || Find.TickManager.TicksGame < slaughterNextAllowedTick
            || !pawn.CanReserveAndReach(victim, PathEndMode.ClosestTouch, Danger.Deadly)
            || !pawn.CanReserveAndReach(parent, PathEndMode.InteractionCell, Danger.Deadly))
        {
            Messages.Message("BOR_Slaughter_Unreachable".Translate(pawn.LabelShortCap), parent, MessageTypeDefOf.RejectInput, false);
            return;
        }
        Job job = JobMaker.MakeJob(BOR_HarvestDefOf.BOR_RighteousSlaughter, parent, LocalTargetInfo.Invalid, victim);
        job.count = 1;
        pawn.jobs?.TryTakeOrderedJob(job, JobTag.Misc);
    }

    private void OpenCorpseMenu(Pawn pawn)
    {
        if (!parent.Spawned || !pawn.Spawned || pawn.Dead || pawn.Downed)
        {
            return;
        }
        List<FloatMenuOption> options = new();
        foreach (Thing t in parent.Map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse))
        {
            if (t is not Corpse c || !CorpseValid(c) || c.IsForbidden(pawn))
            {
                continue;
            }
            Corpse corpse = c;
            string tip = "BOR_Harvest_SelectingBody".Translate(corpse.InnerPawn.LabelShortCap, pawn.LabelShortCap,
                ((float)corpse.HitPoints / corpse.MaxHitPoints).ToStringPercent());
            options.Add(new FloatMenuOption(corpse.InnerPawn.LabelShortCap, () => Order(pawn, corpse))
            {
                tooltip = new TipSignal(tip)
            });
        }
        if (options.Count == 0)
        {
            Messages.Message("BOR_Harvest_NoBodies".Translate(), parent, MessageTypeDefOf.RejectInput, false);
            return;
        }
        Find.WindowStack.Add(new FloatMenu(options));
    }

    private void Order(Pawn pawn, Corpse corpse)
    {
        // The menus can stay open for a while; everything may have changed since they were built.
        if (!parent.Spawned || !pawn.Spawned || pawn.Dead || pawn.Downed || corpse.Destroyed || !CorpseValid(corpse)
            || Find.TickManager.TicksGame < nextAllowedTick)
        {
            Messages.Message("BOR_Harvest_Unreachable".Translate(pawn.LabelShortCap), parent, MessageTypeDefOf.RejectInput, false);
            return;
        }
        Thing? pico = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map,
            ThingRequest.ForDef(BOR_HarvestDefOf.BOR_Picoclusters), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f,
            x => !x.IsForbidden(pawn) && pawn.CanReserve(x, 1, 1));
        if (pico == null || !pawn.CanReserveAndReach(corpse, PathEndMode.ClosestTouch, Danger.Deadly, 1, 1)
            || !pawn.CanReserveAndReach(parent, PathEndMode.InteractionCell, Danger.Deadly))
        {
            Messages.Message("BOR_Harvest_Unreachable".Translate(pawn.LabelShortCap), parent, MessageTypeDefOf.RejectInput, false);
            return;
        }
        Job job = JobMaker.MakeJob(BOR_HarvestDefOf.BOR_HarvestComponents, parent, pico, corpse);
        job.count = 1;
        pawn.jobs?.TryTakeOrderedJob(job, JobTag.Misc);
    }

    public override string CompInspectStringExtra()
    {
        if (!BOR_HarvestDefOf.BOR_Research_ComponentHarvest.IsFinished)
        {
            return null!;
        }
        List<string> lines = new();
        int left = nextAllowedTick - Find.TickManager.TicksGame;
        if (left > 0)
        {
            lines.Add("BOR_Harvest_Cooldown".Translate(left.ToStringTicksToPeriod()).Resolve());
        }
        int slaughterLeft = slaughterNextAllowedTick - Find.TickManager.TicksGame;
        if (slaughterLeft > 0)
        {
            lines.Add("BOR_Slaughter_Cooldown".Translate(slaughterLeft.ToStringTicksToPeriod()).Resolve());
        }
        return lines.Count > 0 ? string.Join("\n", lines) : null!;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref nextAllowedTick, "harvestNextAllowedTick");
        Scribe_Values.Look(ref slaughterNextAllowedTick, "slaughterNextAllowedTick");
    }
}
