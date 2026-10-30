# Umbral Rework Specification

## Purpose
Umbral is a xenotype reached at the end of the Tenebral path (see `tenebral-rework`). Umbrals cannot take bionics from surgery. Instead they slowly transform over three phases, each triggered by killing, until their body is replaced by Umbral parts. Umbral buildings are usable only by Umbral pawns.

## Requirements

### Requirement: Umbral xenotype
Umbral SHALL be a xenotype. Pawns with the Umbral xenotype SHALL carry genes that prevent them from receiving any body part through an operation, including artificial parts and replacement of a lost part. A lost body part SHALL NOT be recoverable by surgery.

### Requirement: Psychosis resistance gene
The Umbral xenotype SHALL include a gene that makes the pawn resistant to tenebral psychosis.

### Requirement: Tenebrite-boosted transformation
Exposure to tenebrite SHALL increase the chance of the transformation hediff being triggered in an Umbral pawn. The size of the increase is to be decided during implementation.

### Requirement: Umbral-only buildings
Every Umbral building SHALL be buildable and usable only by pawns holding the Umbral xenotype.

### Requirement: Transformation gene
Umbral pawns SHALL carry a gene that makes them slowly transform. The transformation SHALL progress through three phases.

### Requirement: Transformation hediff
The Umbral transformation hediff SHALL behave like a flu-style disease: it progresses over a few days and slowly debilitates the pawn.

### Requirement: Phase A — body part replacement
- WHEN an Umbral kills a humanoid, THEN with 25% chance the pawn SHALL gain an Umbral transformation hediff.
- WHEN the transformation hediff reaches 100% severity, THEN the pawn SHALL NOT die. Instead the pawn SHALL receive a randomly selected Umbral body part and the hediff SHALL be cleared.
- Replaceable parts SHALL be: eyes, ears, jaw, brain, arms, legs, heart, liver, kidneys, and lungs.

#### Scenario: Kill triggers transformation
- WHEN an Umbral kills a humanoid and the 25% roll succeeds
- THEN the pawn gains the transformation hediff

### Requirement: Phase B — exoskeleton and exoskull
- WHEN every replaceable body part has been replaced, THEN every humanoid kill SHALL carry a 5% chance to trigger the transformation hediff.
- The first completion SHALL grant an Umbral exoskeleton (armor).
- A second completion SHALL grant an Umbral exoskull (helmet).
- The exoskeleton and exoskull SHALL be rendered as apparel.

#### Scenario: Exoskeleton
- WHEN the phase B transformation completes the first time
- THEN the pawn receives the Umbral exoskeleton, worn as apparel

### Requirement: Phase C — utility pod
- WHEN the exoskull phase is complete, THEN every humanoid kill SHALL carry a 20% chance to trigger the transformation hediff.
- Completion SHALL produce the Umbral utility pod bionic.
- The utility pod SHALL be rendered as a weapon.

### Requirement: Tenebrite exposure
Tenebrite-made items SHALL affect Umbral pawns the same way as in `tenebral-rework` (Tenebrite exposure requirement), growing tenebral psychosis over a few days.

### Requirement: Exoskeleton modes
The Umbral exoskeleton SHALL be a special armor type. An exoskeleton-equipped pawn SHALL have a gizmo that switches between three modes:

- **CAMO:** the pawn is invisible but cannot interact with anything (phaseshift).
- **UTILITY:** no armor, and all work-related stats are improved.
- **COMBAT:** very high armor, and all work-related stats are reduced significantly.

Switching mode SHALL change both the armor visual and the helmet visual. The pawn SHALL look as if it wears armor and a helmet in every mode, even though it has no real apparel.

The exoskeleton SHALL prevent the pawn from equipping any apparel and SHALL nullify the nudity hediff.

### Requirement: Utility pod weapon modes
The Umbral utility pod SHALL provide a gizmo that switches the pawn's displayed weapon between four types:

- a melee weapon
- a melee tool
- a gun
- a launcher

The utility weapons SHALL use no ammo and SHALL render similarly to a normal weapon. A utility pod SHALL prevent the pawn from equipping any other weapon.

## Open questions
- Rendering rules for the exoskeleton, exoskull, and utility pod (to be specified later).
- Whether phases B and C use the same transformation hediff as phase A, or a separate one. To be decided during implementation.
- Size of the armor, work-stat, and phaseshift effects for each exoskeleton mode.
- Stats (damage, range, cooldown) for each utility weapon type.
- Whether the exoskull's helmet visual follows the same mode switch as the exoskeleton, or is tied to it separately.
