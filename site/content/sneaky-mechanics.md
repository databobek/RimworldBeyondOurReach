---
summary: Small changes that don't look like part of the mod, but keep it working with the rest of your game.
card: banner:artifacts
---
Some of what BOR does has nothing to do with the things it adds. These are the changes that sit in the background: they patch other mods, fix rough edges, or make the tiers hold together. You may never notice most of them, and that's the point.

## Anti-air that can't touch everything

BOR's factions arrive in drop pods, and every faction has a tier: Solar is I, Kwazaari and Badzaari are II, Tenebral is III and Umbral is IV. Anti-air has a tier too, and it can only bring down pods up to one tier above its own.

- **Ordinary anti-air**, meaning the point-defense turrets from Combat Extended and from Vanilla Furniture Expanded - Security, counts as tier 0. It downs Solar pods and nothing harder.
- **Tier I anti-air** reaches the Kwazaari and the Badzaari. The Solar defender is one, and with Combat Extended so is the charge AA turret from [Turrets For All](bamba-integrations.html#turrets-for-all-post-medieval-tech-levels).
- **Tenebral and Umbral pods** are out of reach of every anti-air turret for now.

The turret still fires, and spends its fuel or ammo doing it. The shot hits the pod, throws a spark and floats a "Deflected" over it. Pods from the strange signal chains are never shot at, so the visitor you're waiting for isn't taken out of the sky by your own defenses.

## Medicine with a care level of its own

Vanilla gives you four settings for how good a patient's medicine can be. Mods add medicine that fits none of them. **Medicine Care** gives every modded medicine its own care level in the medical tab, sorted by potency and placed above vanilla's ultratech, so you can say exactly which grade a patient may get. The setting shows one icon per level, and you can click or drag across them.

It works like [Mod Medicine Patch](https://github.com/Antaios/Mod-Medicine-Patch), which gave us the idea, and BOR's version is a separate rewrite. If you run Mod Medicine Patch, BOR steps aside and lets it do the job. Choose Your Medicine works with it as well.

One thing to know: levels are numbered by potency, so adding or removing a medicine mod in the middle of a save can move a patient's setting to a neighbouring medicine. A setting that points past the last level counts as the best medicine, so doctors never end up without anything.

## A skin for BOR's screens

BOR has its own windows and tabs: the forge, the dark altar, the Kwazaari console, the blessing bench and the manifest. With [LFS] UI Rethemed loaded they're drawn in that mod's style, and without it they keep the vanilla look.

It goes further than BOR's own screens. The **bills tab** belongs to vanilla, and UI Rethemed leaves it alone. On BOR's three fabricators, the sunsteel shaper, the Kwazifab and the umbral fabricator, it's dressed in the theme too, with a title bar, a border and themed buttons. Every other bench keeps the plain bills tab. The drill tab from Resource Generator Framework gets the same treatment.

Each tier has its own colors and pattern, taken from its buildings: Solar in gold and teal over a sunsteel tile, Kwazaari in periwinkle and silver, Orientis in gold over deep green with faint rising flames. If a UI Rethemed update ever breaks something, BOR drops back to the vanilla look for that session and logs one warning, instead of throwing errors at you.

## Research benches that stand in for each other

Vanilla only lets a project run at the exact bench it names. In BOR a higher bench counts as every bench below it, so the dark mirror can do the work of the solar fluid simulation array and the sigma-class sagittary autoscope, and every one of BOR's research benches counts as vanilla's hi-tech research bench. You can pull the old bench out once you've built the new one.

Artifacts go the other way. A Kwazaari artifact has to be analyzed at the solar fluid simulation array and a Tenebral one at the sagittary autoscope, not at whichever bench is closest.

## No research for sale

[Research Commission](https://steamcommunity.com/workshop/filedetails/?id=3822832387) lets friendly factions buy and sell research. BOR's factions never trade research with you, and BOR research can only be earned in your colony, so with Research Commission loaded, both are taken out of its offers.

## Shields that look like BOR's

With More Shader Visuals loaded, BOR's shields are drawn through that mod in their own colors and styles instead of the default dome. If an update to the mod changes something BOR relies on, the shields fall back to its normal drawing.

## Mechsuit polish

With the Coventech Mechsuit Framework, BOR fixes two small things. A large mech like the Helios gets a build-menu icon in the right size, and a turret with art for each facing, like the Helios rocket pod, keeps its sprite upright instead of spinning with the gun.
