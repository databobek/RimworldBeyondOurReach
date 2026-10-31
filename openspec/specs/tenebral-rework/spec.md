# Tenebral Rework Specification

## Purpose
Tenebral bionics are a stepping stone on the road to Umbral. Today they cause psychosis (see `tenebral-bloodlust`). This rework tracks how often a psychosis-afflicted pawn breaks, offers an exit to the Holytech path (see `holytech-faction`), and, if breaks go too far, crystallizes the pawn into Umbral (see `umbral-rework`). Tenebral production moves from the regular buildings to the Umbral line, with a new Dark Archotech Altar as an additional way to make tenebral items. Umbral buildings produce the same items normally.

## Requirements

### Requirement: Hidden break counter
A pawn with tenebral psychosis SHALL have a hidden hediff that counts the number of mental breaks that pawn has suffered while psychotic. The counter SHALL NOT be shown to the player.

### Requirement: Holytech exit
Once the break counter reaches the holytech threshold, the player SHALL be able to choose to remove one or more tenebral bionics from the pawn. Removing them SHALL enable the pawn to pursue the Holytech path.

#### Scenario: Exit available
- WHEN the pawn's break count reaches the holytech threshold
- THEN the player can remove that pawn's tenebral bionic(s)

### Requirement: Crystallization
Once the break counter reaches the umbral threshold, the pawn's body SHALL begin to crystallize. Each crystallized body part SHALL be marked as crystallized.

- Crystallized body parts SHALL NOT be removable.
- Crystallized body parts SHALL be non-functional.
- Only the parts listed under Implementation decisions crystallize; the heart and brain never do.

#### Scenario: Half crystallized
- WHEN 50% of the pawn's body is crystallized
- THEN the pawn can be placed in a Tenebral pod in transformation mode

### Requirement: Transformation mode hold
A pawn placed in a Tenebral pod set to transformation mode SHALL be held for `(100 - crystallized%)`% of a year. The hold SHALL NOT end early.

#### Scenario: Partial crystallization
- WHEN the pawn is 50% crystallized
- THEN the hold lasts 50% of a year

### Requirement: Emergence as Umbral
When the hold ends, the pawn SHALL emerge as Umbral, as defined in `umbral-rework`.

### Requirement: Building split
Every Tenebral building SHALL be replaced by an Umbral building, except the turret, which SHALL remain Tenebral. The Umbral tier has no items of its own. Its buildings (tier IV) SHALL produce the tenebral-tier items (weapons, guns, apparel, vaccines, organs, appendages) as well as the items moved to the Umbral line (drugs and medicine). Umbral pawns resist tenebral psychosis, so they cannot use the Dark Archotech Altar and SHALL NOT need it.

### Requirement: New Tenebral buildings
Tenebral SHALL gain the Dark Archotech Altar. The Umbral transformation chamber is a mode of the Tenebral pod, not a separate building.

### Requirement: Dark Archotech Altar
The Dark Archotech Altar SHALL be a special Forge whose grid is prefilled with tenebral items: melee weapons, guns, apparel, tenebrite, tenebral organ, tenebral appendage, femtoclusters, and the tenebral and umbral vaccines. Every item on the altar SHALL also have a normal recipe at the Umbral tier IV buildings; the altar is an additional source, not the only one.

- Every grid cell SHALL start prefilled but locked.
- A locked cell SHALL be unlocked only by performing work on it.
- The altar SHALL be usable only by a pawn with the Tenebral Psychosis hediff.
- Analysis and production on the altar SHALL drain the pawn's Bloodlust need.
- Progress SHALL persist on the altar, so work can be paused and resumed. (Unlike a regular forge.)

#### Scenario: Requesting an item
- WHEN the player brings the scaled amount of crystalline joints (componentIII) to the altar
- THEN the altar "requests" the item

### Requirement: Tenebrite exposure
Everything created with `BOR_Tenebrite` (buildings, floors, walls, weapons, armors) SHALL cause tenebral psychosis to grow temporarily in nearby pawns, even without tenebral bionics. Growth SHALL be delayed by a few days, similar to how pollution affects pawns.

- Buildings, floors, and walls SHALL apply psychosis by emission, like pollution.
- Weapons and armors SHALL apply psychosis directly by applying the hediff to the wearer or holder. They SHALL work like a bionic, with the ratio to be decided during implementation.

#### Scenario: Pawn without bionics
- WHEN a pawn without tenebral bionics stands near a tenebrite-made building or wears tenebrite armor
- THEN tenebral psychosis grows in that pawn after a few days

### Requirement: Tenebral pod
The Tenebral pod SHALL be a Tenebral building that combines a bionic installator, a healing pod, and a stasis pod. It SHALL provide a mode gizmo with four modes: stasis, healing, operating, and transformation. The mode cannot be changed while a transformation is under way.

#### Scenario: Healing mode
- WHEN a pawn with a tenebral hediff enters the pod in healing mode
- THEN the hediff is healed at 10 times the normal healing rate, scaled by the pawn's injuries

#### Scenario: Stasis mode
- WHEN a pawn without a tenebral hediff enters the pod in stasis mode
- THEN the pawn is frozen, like a cryptosleep pod, and the player must take them out manually

### Requirement: Operating mode
Before the pod can operate, a tenebral appendage or organ and femtoclusters SHALL be placed inside. The player SHALL then select where and what to install. Installation SHALL be a single step, and its duration SHALL come from the existing recipe work amount.

## Open questions
- Value of the holytech threshold ("a few" breaks).
- Value of the umbral threshold ("too many" breaks).
- Which body parts count toward the 50%.
- Whether a pawn can be placed in transformation mode before reaching 50%.
- Whether crystallization progresses while the pawn is not psychotic.
- The scaling formula for crystalline joints (componentIII) per requested item.
- How many work actions unlock each locked cell on the altar.

## Implementation decisions
Implemented in `TenebralPsychosis.cs`, `TenebralSurgery.cs`, `TenebralPods.cs`, `DarkAltar.cs`. Tunables live in XML mod extensions on `BOR_TenebralPsychosis` and `BOR_TenebralBreakCounter`.

- Psychosis is the `BOR_TenebralPsychosis` hediff (severity 0..1). It is hidden below 0.1 (latent), which is the "delayed by a few days" part. A pawn is psychotic once it is visible.
- Severity floor = (implants + tenebrite gear x 0.5) x 0.1. Nearby tenebrite emission (buildings 1, floor cells 0.1, radius 6, capped at 3) adds 0.03 severity per day per unit. Without emission, severity decays 0.05/day down to the floor.
- Holytech threshold: 3 breaks. Umbral threshold: 6 breaks. Breaks caused by damage or psycasts don't count.
- Tenebral bionics can be removed by surgery only from the holytech threshold onward; removing one adds `BOR_HolytechCandidate`.
- Crystallization: nothing happens on the 6th break itself. Every break after it has a 50% chance to crystallize one random eligible part. There is no timer, so a pawn who stops breaking stops crystallizing. Eligible parts are the arms, legs, eyes, ears, the left kidney, the right lung and the torso (11 parts on a human, so 50% is 6 parts). Nothing else crystallizes or counts. The percentage counts eligible parts.
- Transformation mode refuses a pawn below 50%.
- Altar: unlocking a cell takes the forge analysis time for the item. Each run costs ceil(value x yield x 0.5 / joint value) crystalline joints (at least 1). Work drains 2 days' worth of bloodlust per day of work.
