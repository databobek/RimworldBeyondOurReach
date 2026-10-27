# Component Harvest Specification

## Purpose
Lets the player turn a fresh human corpse plus a picocluster into crystalline joints (`BOR_ComponentIII`) at a butcher table, at a heavy mood cost modulated by Ideology precepts. Implemented by `CompComponentHarvest.cs`, `HarvestComponents.cs`, `BOR_HarvestDefOf.cs`.

## Requirements

### Requirement: Availability
`CompProperties_ComponentHarvest` SHALL configure a parent building with `cooldownTicks` (default 1 hour), `componentCount` (default 10) and `minCorpseHealthPct` (default 0.75).

#### Scenario: Research not finished
- WHEN `BOR_Research_ComponentHarvest` is not finished, or the parent is unspawned or not player-owned
- THEN no gizmo and no inspect-string cooldown are shown

### Requirement: Gizmo gating
The "Harvest components" gizmo SHALL be disabled with a reason when no `BOR_Picoclusters` exist on the map, or while the cooldown is running (remaining time shown).

### Requirement: Selection flow
Clicking the gizmo SHALL open a pawn menu of free colonists on the map (downed or Manipulation-incapable pawns are listed disabled), then a corpse menu of valid corpses not forbidden to that pawn.

A corpse is valid when spawned, of a humanlike pawn, in the Fresh rot stage, with hit points >= `minCorpseHealthPct` of max.

#### Scenario: No candidates
- WHEN no colonists or no valid corpses exist
- THEN a reject-input message is shown and no menu opens

### Requirement: Order validation
On ordering, the system SHALL re-validate (menus can be stale): table/pawn spawned, pawn alive and not downed, corpse valid and not destroyed, cooldown elapsed, a reachable and reservable picocluster, corpse and table reservable. On failure it SHALL show an "unreachable" message and issue no job.

### Requirement: Harvest job
`BOR_HarvestComponents` SHALL, in order: fetch one picocluster and drop it at the table; haul the corpse to the table and drop it; work 600 ticks (Crafting skill XP 0.05/tick, progress bar, blood/bile filth splatter every 4 ticks within radius 3); then finish.

- Any carried item is dropped if the job ends.
- The job fails if the corpse is no longer fresh/healthy enough, or the picocluster or corpse leaves the table.

### Requirement: Finish effects
On completion, if all participants are still present, the system SHALL destroy one picocluster and the corpse, spawn `componentCount` (10) `BOR_ComponentIII` near the table's interaction cell, add a burst of filth (40 splatters), start the cooldown, and apply thoughts. If anything vanished, the job ends incompletable and nothing is consumed.

### Requirement: Mood and opinion thoughts
After a harvest the operator SHALL gain `BOR_MutilatedBody`; if they already hold it, the new memory starts one stage higher, capped at the last stage. Every other free colonist on the map SHALL gain `BOR_MutilatedBodySocial` with the operator as the other pawn.

#### Scenario: Ideology precepts
- WHEN Ideology is active and the pawn has an ideo, precepts are looked up by name (silent-fail if missing):
  - `BOR_BiomassHarvest_Exalted` -> operator gets `BOR_MutilatedBodyExalted`; observers get `BOR_MutilatedBodySocialExalted` (each falling back to the base thought if the def is missing)
  - `BOR_BiomassHarvest_Neutral` -> no thought
  - anything else -> base negative thoughts
- WHEN Ideology is inactive or the pawn has no ideo
- THEN base negative thoughts apply
- The operator's ideo decides the operator's thought; each observer's own ideo decides their opinion thought.

### Requirement: Cooldown persistence
The table SHALL save `nextAllowedTick` and show the remaining cooldown in its inspect string while research is finished.
