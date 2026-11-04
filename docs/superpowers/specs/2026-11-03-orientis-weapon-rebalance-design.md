# Orientis weapon rebalance: design

Date: 2026-11-03. Branch: rework/orientis. Status: awaiting approval.

## Intent

Orientis weapons win through debuffs and single decisive hits, not through raw damage. Damage sits between Solar and Quasar. Penetration and the debuffs are where they excel.

## Scope and order

Two projects, in this order. Project 2 depends on project 1.

1. **Main weapons**, in BOR: the six that have textures. That is the crusader power axe, longsword, dagger and pole hammer, the laser crossbow and the plasma crossbow ("boombow"). Existing defs are in `Common/Defs/ThingDefs_Items/Orientis/Orientis_Weapons.xml`.
2. **Secondary sets** for AllBambaGuns and AllBambaMelee. They get their own design pass after project 1 ships. Only the shared rules are recorded here.

## Project 1: main weapons

### Melee (axe, longsword, dagger, pole hammer)
- Heavy and slow, with long cooldowns. They are not fencing weapons: wait for the moment, then strike once.
- Damage per hit sits between Solar and Quasar. Penetration is tremendous, in the Orientis band (above Quasar, below Tenebral). One hit does real damage even to Umbral armor, so a pawn can trade blows with an Umbral.
- Existing penetration values in `CE_NEW/Patches/additions/orientis.xml` (sharp 60 to 74, hammer blunt 260) are the starting point. Damage and cooldown are the main changes.
- The dagger stays the lightest and fastest of the four, but is still slow by normal standards.

### Laser crossbow
- A designated marksman rifle for single targets. Damage is Solar to Advanced Spacer level. It is not the weapon's strength.
- Each hit applies a new hediff. The hediff has a chance to disable the Umbral's combat armor (`BOR_UmbralExoskeleton_Combat`, and the exoskull if it applies).
- Applied through the projectile's damage def (`additionalHediffs`). A small hediff component in C# rolls the disable chance.

### Plasma crossbow (boombow)
- A launcher/shotgun role. It fires an energy bolt that causes a small vaporizing explosion on the target.
- The explosion also carries a shockwave that stuns **only** pawns carrying the Umbral hediff, Tenebral pawns and anomaly creatures. Colonists, raiders and animals in the blast take the explosion damage and no stun.
- A new damage worker in BOR does the filtering. Plain XML cannot, because vanilla stun hits every pawn in the blast.
- The explosion damage stays modest. The stun is the point.

### Tiering
- Damage sits between Solar and Quasar. Penetration keeps the Orientis band from the 2026-11-02 ladder.
- The Orientis band rule applies to penetration. It is now explicitly separate from damage.
- Cost, work and market value are not changed unless balance needs it.

### Acquisition
- Unchanged for the main weapons: made at the offerings bench for meditation power, `tradeability None`, `generateCommonality 0`, no loot, no quest rewards.

### Checked
- Nothing in BOR's Umbral hediffs, apparel or source mentions stun, so Umbrals are not stun-immune today. Re-check when testing.

## Shared rules for project 2

- Adv Spacer damage, Quasar-like penetration, a slight stun on Umbrals, Tenebrals and anomaly creatures. The same damage worker is reused.
- Guns: charged, no ammo, single plasma-like shots, slow to fire. Pistol (flintlock), shotgun (blunderbuss), rifle (arquebus), sniper (musket with scope), launcher (handcannon). All have a slight explosive quality.
- Melee: dagger (jambiya shape), gladius (short European sword), longsword (saif shape), spear, axe, and a mace for EMP. Glowing charged weapons, heavy and slow but less than the main weapons.
- They load only when BOR is active, through `IfModActive` entries in each mod's `LoadFolders.xml`, as with other BOR content.
- Not buyable for now. They also cannot be crafted, looted or given as rewards.

## Approaches considered

- **A. XML first, small C# (chosen).** The numbers, the laser hediff and the boombow explosion are XML. C# covers the Umbral-only stun worker and the hediff's armor-disable roll.
- **B. Custom C# for every debuff.** More control, more code to maintain.
- **C. Numbers now, debuffs later.** Fastest, but the crossbows are mostly their debuffs.

## Goals, non-goals, anti-goals

- Goals: melee that trades with Umbrals on one hit, a laser crossbow that threatens Umbral armor, and a boombow that stuns only the right targets.
- Non-goals: a trader or shop, new textures, retuning vanilla or non-Orientis tiers, the secondary sets themselves.
- Anti-goals: the stun hitting colonists or ordinary raiders, damage that overlaps Quasar, and a weapon that is strong without its debuff.

## Left to the builder

- Exact damage, cooldown and chance numbers, within the bands above.
- The armor-disable chance and duration, and whether it is a timed disable or a damage threshold.
- The stun duration and whether it scales with distance from the blast centre.
- How anomaly creatures are identified for the stun filter.
- Whether the Arquebus ladder (Industrial to Archotech) changes. It is out of scope unless asked.
- Vanilla values and CE_NEW values both need setting. CE_NEW is currently not loaded by default.
