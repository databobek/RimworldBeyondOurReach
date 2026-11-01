using System.Linq;
using RimWorld;
using Verse;

namespace BeyondOurReach;

public class CompProperties_OfferingsBench : CompProperties
{
    // Meditation power per tick of recipe work.
    public float powerPerWork = 0.005f;

    public CompProperties_OfferingsBench()
    {
        compClass = typeof(CompOfferingsBench);
    }
}

// Runs the bench's bills on its own, paying only in meditation power. No work giver lists this bench, so no pawn ever
// uses it; the player only manages the bills.
public class CompOfferingsBench : ThingComp
{
    private RecipeDef? recipe;
    private float workDone;
    private bool starved;

    public CompProperties_OfferingsBench Props => (CompProperties_OfferingsBench)props;

    private static float WorkFor(RecipeDef def) =>
        def.workAmount >= 0f ? def.workAmount : def.products[0].thingDef.GetStatValueAbstract(StatDefOf.WorkToMake);

    public override void CompTick()
    {
        base.CompTick();
        if (!parent.Spawned || parent is not Building_WorkTable table)
        {
            return;
        }
        Bill? bill = table.BillStack.Bills.FirstOrDefault(b => b.ShouldDoNow() && b.recipe.AvailableNow);
        if (bill == null)
        {
            recipe = null;
            workDone = 0f;
            starved = false;
            return;
        }
        if (bill.recipe != recipe)
        {
            recipe = bill.recipe;
            workDone = 0f;
        }
        starved = !MapComponent_MeditationPower.Of(parent.Map).TryDraw(Props.powerPerWork);
        if (starved)
        {
            return;
        }
        workDone++;
        if (workDone >= WorkFor(recipe))
        {
            workDone = 0f;
            Finish(bill);
        }
    }

    private void Finish(Bill bill)
    {
        foreach (ThingDefCountClass product in bill.recipe.products)
        {
            Thing thing = ThingMaker.MakeThing(product.thingDef, GenStuff.DefaultStuffFor(product.thingDef));
            thing.stackCount = product.count;
            thing.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
            GenPlace.TryPlaceThing(thing, parent.InteractionCell, parent.Map, ThingPlaceMode.Near);
        }
        bill.Notify_IterationCompleted(null, new System.Collections.Generic.List<Thing>());
    }

    public override string? CompInspectStringExtra()
    {
        if (recipe == null)
        {
            return null;
        }
        return starved
            ? "BOR_OfferingsNoPower".Translate(recipe.products[0].thingDef.label)
            : "BOR_OfferingsWorking".Translate(recipe.products[0].thingDef.label, (workDone / WorkFor(recipe)).ToStringPercent());
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Defs.Look(ref recipe, "offeringsRecipe");
        Scribe_Values.Look(ref workDone, "offeringsWorkDone");
    }
}
