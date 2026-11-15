using System.Collections.Generic;
using RimWorld;
using Verse;

namespace BeyondOurReach;

// Gives the pawn holding the weapon its abilities, and takes them back when the weapon leaves their hands. The
// abilities themselves are plain AbilityDefs, so the shared cooldown comes from their AbilityGroupDef.
public class CompProperties_OrientisWeaponAbilities : CompProperties
{
    public List<AbilityDef> abilities = new();

    public CompProperties_OrientisWeaponAbilities()
    {
        compClass = typeof(CompOrientisWeaponAbilities);
    }
}

public class CompOrientisWeaponAbilities : ThingComp
{
    private CompProperties_OrientisWeaponAbilities Props => (CompProperties_OrientisWeaponAbilities)props;

    public override void Notify_Equipped(Pawn pawn)
    {
        base.Notify_Equipped(pawn);
        if (pawn.abilities == null)
        {
            return;
        }
        foreach (AbilityDef def in Props.abilities)
        {
            pawn.abilities.GainAbility(def);
        }
    }

    public override void Notify_Unequipped(Pawn pawn)
    {
        base.Notify_Unequipped(pawn);
        if (pawn.abilities == null)
        {
            return;
        }
        foreach (AbilityDef def in Props.abilities)
        {
            pawn.abilities.RemoveAbility(def);
        }
    }
}
