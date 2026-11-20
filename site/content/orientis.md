---
summary: The Path of Light. Meditation instead of electricity, blessings instead of bionics, and crusader gear. Spoilers.
card: tex:BOR/Things/Factions/OrientisIcon
---
{{ emblem("BOR/Things/Factions/OrientisIcon", "Path of Light", "Spoilers for the Tenebral road") }}

This page gives away the end of [the Tenebral road](tenebral-road.html). If you'd rather find out in game, turn back here.

Orientis is still new. Its art is placeholder and its numbers aren't final, so expect changes.

## The choice

When a colonist in tenebral psychosis breaks for the third time, your colony gets a letter. It can't be dismissed or left to expire:

{{ keyed_quote("BOR_OrientisChoiceText") }}

It offers two answers, the **Path of Light** and the **Path of Dark**, and either one is final. The Path of Dark changes nothing for now: your colonists go on breaking and crystallizing toward what waits at the end of [the Tenebral road](tenebral-road.html). The Path of Light turns your colony into Orientis.

{{ screenshot("orientis-choice", "The choice letter") }}

## What the light takes

The light reaches every colonist at once, wherever they are: on any map, in a caravan, in a transport pod, even in a cryptosleep casket. Prisoners and guests are left alone.

- Every tenebral part is torn out, crystallized parts included. The body part grows back with a golden wound, and nobody dies of it, not even with a tenebral heart.
- Every Umbral in the colony is struck down by a golden beam from the sky.
- A colonist in the middle of a transformation in a tenebral pod dies with the pod.
- Every dark archotech altar is struck too, and sends a ring of light across its map. Tenebral and Umbral buildings die as the ring passes and are left as grey husks. Dark gear burns, worn or not.
- The stripped colonists, and the one who received the letter, become Orientis.

An hour later, the Dark Archotechs answer. Something comes for one of your colonists. It can be fought.

## Living in the light

With Biotech, Orientis is a xenotype. Without it, a hediff stands in.

{{ def_quote("XenotypeDef", "BOR_OrientisXenotype") }}

- Orientis colonists can't have bionics or implants installed. Natural organ transplants still work.
- Light and dark stay apart. Orientis colonists won't wear, wield or even haul tenebral or Umbral things, tenebrite, or anything made with them, down to femtoclusters, Umbral medicine and Adreamoline. Tenebral and Umbral pawns can't use Orientis buildings or gear either.
- Orientis buildings don't come from the Architect menu and need no research. Every Orientis colonist can **manifest** them.

## Manifest

{{ abilities("BOR_OrientisManifest") }}

Manifest opens a window listing every Orientis building with its cost and how much of each material your map holds. Pick one you can afford and place it like any other building. The materials vanish from wherever they lie on the map, nearest first, and the finished building appears at once.

{{ screenshot("orientis-manifest", "The manifest window") }}

## Meditation power

Orientis buildings run on meditation power instead of electricity or neutronium. Orientis colonists make it by meditating. That's a work type of its own: only Orientis colonists do it, and they take the best free spot first.

- A **meditation pod** makes {{ value("ThingDef", "BOR_Orientis_MeditationPod", "comps/li[@Class='BeyondOurReach.CompProperties_MeditationSpot']/powerPerDay") }} a day at 100% meditation strength.
- A **meditation bench** makes {{ value("ThingDef", "BOR_Orientis_MeditationBench", "comps/li[@Class='BeyondOurReach.CompProperties_MeditationSpot']/powerPerDay") }} a day and a **meditation mat** {{ value("ThingDef", "BOR_Orientis_MeditationMat", "comps/li[@Class='BeyondOurReach.CompProperties_MeditationSpot']/powerPerDay") }}.
- A **meditation cache** stores {{ value("ThingDef", "BOR_Orientis_MeditationCache", "comps/li[@Class='BeyondOurReach.CompProperties_MeditationCache']/capacity") }}. It needs no wires: all the caches on a map share one pool, and every Orientis building on that map draws from it.

Falaq robes add {{ value("ThingDef", "BOR_Orientis_MonkBodysuit", "equippedStatOffsets/BOR_MeditationStrength", "pct") }} meditation strength.

{{ screenshot("orientis-meditation", "Orientis colonists meditating") }}

## Buildings

{{ things("BOR_Orientis_MeditationPod", "BOR_Orientis_MeditationBench", "BOR_Orientis_MeditationMat", "BOR_Orientis_MeditationCache", "BOR_Orientis_HolyForge", "BOR_Orientis_OfferingsBench", "BOR_Orientis_BlessingBench") }}

### The meditation shield

A meditation pod can raise a shield {{ value("ThingDef", "BOR_Orientis_MeditationPod", "comps/li[@Class='CompProperties_ProjectileInterceptor']/radius", "across") }} tiles across. An Orientis colonist has to walk to the pod to raise it. While it's up:

- hostile shots, mortar shells and orbital strikes can't get through;
- powered buildings under it keep running through a solar flare;
- tenebral and Umbral pawns inside burn: they lose a third of their consciousness at once, and more every hour they stay.

It draws {{ value("ThingDef", "BOR_Orientis_MeditationPod", "comps/li[@Class='BeyondOurReach.CompProperties_MeditationShield']/powerPerDay") }} meditation power a day, and falls when the caches run dry.

{{ screenshot("orientis-shield", "A meditation shield holding off a raid") }}

### Holy forge and offerings bench

The **holy forge** is a [forge](materials.html#forges) that runs itself on meditation power, with a {{ value("ThingDef", "BOR_Orientis_HolyForge", "comps/li[@Class='BeyondOurReach.CompProperties_Forge']/gridWidth") }}×{{ value("ThingDef", "BOR_Orientis_HolyForge", "comps/li[@Class='BeyondOurReach.CompProperties_Forge']/gridHeight") }} grid. Teaching it a new item is still done by hand.

The **offerings bench** makes Orientis weapons and armor from its bills, with no one working it and no materials, only meditation power. Nothing else can make them:

{{ made_at("BOR_Orientis_OfferingsBench") }}

## Blessings

Orientis colonists have no bionics. Instead, a colonist meditates at the **blessing bench** and picks body parts to bless. When the meditation ends, each chosen part gets +{{ value("HediffDef", "BOR_Blessing", "stages/li/partEfficiencyOffset", "pct") }} efficiency for {{ value("HediffDef", "BOR_Blessing", "comps/li[@Class='HediffCompProperties_Disappears']/disappearsAfterTicks", "days") }} days. It works like a bionic, but the part isn't replaced.

- Several parts can be blessed in one meditation.
- Blessing a part again restarts its timer. Blessings don't stack.
- A part that already has an artificial part can't be blessed.

{{ screenshot("orientis-blessing", "Choosing parts at the blessing bench") }}

## Weapons

Orientis fights with crusader power weapons and charge guns. The power weapons' blades glow while held, and their hits add burn damage. The charge guns fire one heavy slug at a time after a long aim, with no ammo to carry. There's also a plasma crossbow that lobs exploding bolts, and a laser crossbow for marksmen.

The charge guns carry three special shells, which share one cooldown:

{{ abilities("BOR_OrientisShot_Vaporize", "BOR_OrientisShot_Emp", "BOR_OrientisShot_UmbralStun") }}

{{ weapon_tables("orientis") }}

{{ melee_tables("orientis") }}

## Armor

Three suits and a robe:

- **Confractor**: light mail and flowing cloth in the style of the First Crusade. It barely slows the wearer.
- **Eversor**: medium, high-tech armor styled after medieval plate, with a cape.
- **Domitor**: heavy, sealed plate, black and heavy with gold.
- **Falaq robes**: simple robes worn with the hood down, which deepen the wearer's meditation.

With Combat Extended, Orientis also gets a crusader shield and a pavise.

{{ apparel_tables("armor", "orientis") }}

{{ screenshot("orientis-crusaders", "Orientis crusaders") }}
