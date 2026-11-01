using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;

namespace BeyondOurReach;

// Tunables for the ability rolls; placed on BOR_DarkAbilityHits.
public class DefModExtension_DarkAbilities : DefModExtension
{
    public List<AbilityDef> abilities = new();

    // Hits one ability needs before the pawn gains it.
    public int hitsToLearn = 3;

    // Empty outcomes added to the roll on each crystallization.
    public int crystallizationEmptyRolls = 2;

    // Empty outcomes added to the roll on each Umbral transformation.
    public int umbralEmptyRolls = 8;
}

[DefOf]
public static class BOR_DarkAbilityDefOf
{
    public static HediffDef BOR_DarkAbilityHits = null!;
    public static HediffDef BOR_ThrallBrainDamage = null!;

    static BOR_DarkAbilityDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(BOR_DarkAbilityDefOf));
    }
}

public static class DarkAbilities
{
    public static DefModExtension_DarkAbilities Settings =>
        BOR_DarkAbilityDefOf.BOR_DarkAbilityHits.GetModExtension<DefModExtension_DarkAbilities>() ?? new DefModExtension_DarkAbilities();

    public static void RollCrystallization(Pawn pawn) => Roll(pawn, Settings.crystallizationEmptyRolls);

    public static void RollUmbralTransformation(Pawn pawn) => Roll(pawn, Settings.umbralEmptyRolls);

    // Abilities the pawn already has stay in the pool, so rolling one is a wasted hit.
    private static void Roll(Pawn pawn, int emptyRolls)
    {
        List<AbilityDef> abilities = Settings.abilities;
        int roll = Rand.Range(0, abilities.Count + emptyRolls);
        if (roll >= abilities.Count)
        {
            return;
        }
        AbilityDef ability = abilities[roll];
        pawn.abilities ??= new Pawn_AbilityTracker(pawn);
        if (pawn.abilities.GetAbility(ability) != null)
        {
            return;
        }
        if (pawn.health.hediffSet.GetFirstHediffOfDef(BOR_DarkAbilityDefOf.BOR_DarkAbilityHits) is not Hediff_DarkAbilityHits hits)
        {
            hits = (Hediff_DarkAbilityHits)HediffMaker.MakeHediff(BOR_DarkAbilityDefOf.BOR_DarkAbilityHits, pawn);
            pawn.health.AddHediff(hits);
        }
        if (hits.AddHit(ability) < Settings.hitsToLearn)
        {
            return;
        }
        pawn.abilities.GainAbility(ability);
        if (pawn.Faction == Faction.OfPlayer)
        {
            Find.LetterStack.ReceiveLetter("BOR_DarkAbilityLearnedLabel".Translate(pawn.LabelShortCap, ability.label),
                "BOR_DarkAbilityLearnedText".Translate(pawn.LabelShortCap, ability.label, ability.description), LetterDefOf.PositiveEvent, pawn);
        }
    }

    public static bool Reject(string key, LocalTargetInfo target, bool throwMessages)
    {
        if (throwMessages)
        {
            Messages.Message(key.Translate(target.Pawn.LabelShort), target.Pawn, MessageTypeDefOf.RejectInput, false);
        }
        return false;
    }
}

public static class DarkAbilitiesDebug
{
    private static void Grant(Pawn pawn, IEnumerable<AbilityDef> abilities)
    {
        if (!pawn.RaceProps.Humanlike)
        {
            return;
        }
        pawn.abilities ??= new Pawn_AbilityTracker(pawn);
        foreach (AbilityDef ability in abilities)
        {
            if (pawn.abilities.GetAbility(ability) == null)
            {
                pawn.abilities.GainAbility(ability);
            }
        }
        DebugActionsUtility.DustPuffFrom(pawn);
    }

    [DebugAction("Pawns", "Grant dark ability...", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static List<DebugActionNode> GrantDarkAbility()
    {
        List<AbilityDef> abilities = DarkAbilities.Settings.abilities;
        List<DebugActionNode> nodes = new()
        {
            new DebugActionNode("*All", DebugActionType.ToolMapForPawns, pawnAction: pawn => Grant(pawn, abilities))
        };
        foreach (AbilityDef ability in abilities)
        {
            nodes.Add(new DebugActionNode(ability.defName, DebugActionType.ToolMapForPawns, pawnAction: pawn => Grant(pawn, new[] { ability })));
        }
        return nodes;
    }
}

// Hidden tally of rolls per ability. Separate from the break counter so it survives the move from tenebral to Umbral.
public class Hediff_DarkAbilityHits : HediffWithComps
{
    private Dictionary<AbilityDef, int> hits = new();

    public override bool Visible => false;

    public int AddHit(AbilityDef ability)
    {
        hits.TryGetValue(ability, out int count);
        return hits[ability] = count + 1;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref hits, "abilityHits", LookMode.Def, LookMode.Value);
        hits ??= new Dictionary<AbilityDef, int>();
    }
}

public class CompProperties_AbilityBloodlustCost : CompProperties_AbilityEffect
{
    public float cost = 0.5f;

    public CompProperties_AbilityBloodlustCost()
    {
        compClass = typeof(CompAbilityEffect_BloodlustCost);
    }
}

// Tenebral pawns pay in bloodlust and need enough of it to pay; Umbral pawns cast for free.
public class CompAbilityEffect_BloodlustCost : CompAbilityEffect
{
    private float Cost => ((CompProperties_AbilityBloodlustCost)props).cost;

    private Need_Murder? Need => parent.pawn.needs?.TryGetNeed<Need_Murder>();

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        if (!Umbral.IsUmbral(parent.pawn) && Need is Need_Murder need)
        {
            need.CurLevel -= Cost;
        }
    }

    public override bool GizmoDisabled(out string reason)
    {
        if (!Umbral.IsUmbral(parent.pawn) && (Need?.CurLevel ?? 0f) < Cost)
        {
            reason = "BOR_AbilityNeedsBloodlust".Translate(Cost.ToStringPercent());
            return true;
        }
        return base.GizmoDisabled(out reason);
    }

    public override string ExtraTooltipPart() => "BOR_AbilityCostTip".Translate(Cost.ToStringPercent());
}

// Wounds the target all over until it goes down.
public class CompAbilityEffect_DarkWound : CompAbilityEffect
{
    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        if (target.Pawn is Pawn pawn)
        {
            HealthUtility.DamageUntilDowned(pawn);
        }
    }

    public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
    {
        if (target.Pawn is Pawn pawn && pawn.Downed)
        {
            return DarkAbilities.Reject("BOR_AbilityTargetDowned", target, throwMessages);
        }
        return base.Valid(target, throwMessages);
    }
}

// Destroys the heart. The kill is credited to the caster, so it sates bloodlust and can trigger an Umbral transformation.
public class CompAbilityEffect_DarkKill : CompAbilityEffect
{
    private static BodyPartRecord? Heart(Pawn pawn) =>
        pawn.RaceProps.body.GetPartsWithDef(BodyPartDefOf.Heart).FirstOrDefault(part => !pawn.health.hediffSet.PartIsMissing(part));

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        if (target.Pawn is not Pawn pawn || Heart(pawn) is not BodyPartRecord heart)
        {
            return;
        }
        Hediff_MissingPart missing = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, pawn, heart);
        missing.lastInjury = HediffDefOf.Cut;
        pawn.health.AddHediff(missing, heart, new DamageInfo(DamageDefOf.Cut, 99999f, 999f, -1f, parent.pawn, heart));
    }

    public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
    {
        if (target.Pawn is Pawn pawn && Heart(pawn) == null)
        {
            return DarkAbilities.Reject("BOR_AbilityTargetNoHeart", target, throwMessages);
        }
        return base.Valid(target, throwMessages);
    }
}

// Enslaves the target at the cost of brain damage. Slavery needs Ideology; without it the target joins outright.
public class CompAbilityEffect_DarkThrall : CompAbilityEffect
{
    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        if (target.Pawn is not Pawn pawn)
        {
            return;
        }
        Pawn caster = parent.pawn;
        if (!ModsConfig.IdeologyActive)
        {
            RecruitUtility.Recruit(pawn, caster.Faction, caster);
        }
        else if (!GenGuest.TryEnslavePrisoner(caster, pawn))
        {
            return;
        }
        pawn.health.AddHediff(BOR_DarkAbilityDefOf.BOR_ThrallBrainDamage, pawn.health.hediffSet.GetBrain());
    }

    public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
    {
        if (target.Pawn is Pawn pawn)
        {
            if (pawn.Faction == parent.pawn.Faction || pawn.IsSlave)
            {
                return DarkAbilities.Reject("BOR_AbilityTargetOwn", target, throwMessages);
            }
            if (pawn.IsQuestLodger())
            {
                return DarkAbilities.Reject("BOR_AbilityTargetLodger", target, throwMessages);
            }
        }
        return base.Valid(target, throwMessages);
    }
}

// A red lightning strike and a vaporize explosion at the target.
public class CompAbilityEffect_DarkBolt : CompAbilityEffect
{
    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        base.Apply(target, dest);
        Map map = parent.pawn.Map;
        IntVec3 cell = target.Cell;
        map.weatherManager.eventHandler.AddEvent(new WeatherEvent_RedLightning(map, cell));
        GenExplosion.DoExplosion(cell, map, parent.def.EffectRadius, DamageDefOf.Vaporize, parent.pawn);
    }
}
