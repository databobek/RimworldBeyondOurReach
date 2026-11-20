# AGENTS

## Scope Guardrails

- The mod targets RimWorld 1.7 only; there are no per-version folders.
- Content lives in `Common/` (core defs/patches), `Mods/<Name>/` (compat loaded via `LoadFolders.xml`) and `Source/` (C#, builds to `Assemblies/`).
- Do not add support for older game versions unless the user explicitly requests it.
- `site/` is the player website (GitHub Pages), not part of the mod. Its stats are generated from the defs; see `site/README.md`.
- Files in different load folders must not share a relative path (e.g. two `Patches/PawnKindGroups.xml`): when both folders load, RimWorld keeps only the one from the later folder.

## Content Guardrails

- Beam weapons exist only in `Mods/CE`: vanilla has no beam weapons, and BOR does not recreate them in the base game. Do not add or restore base-game beam weapons.
- They fire BOR's own laser shots: `Shot_BOR_LaserLow` (pistol), `Shot_BOR_LaserMed` (rifle, shotgun, bullpup, DMR) and `Shot_BOR_LaserHigh` (sniper, cannon). Keep them when editing or regenerating the CE patches; do not swap in vanilla charge sounds.

## Source Layout

- `Source/` is organized into feature folders (`Core`, `Buildings`, `Forge`, `Harvest`, `Kwazaari`, `Orientis`, `Signals`, `Tenebral`, `Umbral`).
- Everything stays in the single `BeyondOurReach` namespace; XML references classes as `BeyondOurReach.<Class>`, so do not introduce sub-namespaces.
