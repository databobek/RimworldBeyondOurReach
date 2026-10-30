# Tenebral Rework Specification

## Purpose
Tenebral bionics are a stepping stone on the road to Umbral. Today they cause psychosis (see `tenebral-bloodlust`). This rework tracks how often a psychosis-afflicted pawn breaks, offers an exit to the Holytech path (see `holytech-faction`), and, if breaks go too far, crystallizes the pawn into Umbral (see `umbral-rework`). Tenebral production moves from the regular buildings to the Umbral line, with a new Dark Archotech Altar for tenebral items.

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
- The heart and brain SHALL NOT crystallize.

#### Scenario: Half crystallized
- WHEN 50% of the pawn's body is crystallized
- THEN the pawn can be placed in the Umbral Transformation Pod

### Requirement: Umbral Transformation Pod hold
A pawn placed in the Umbral Transformation Pod SHALL be held for `(100 - crystallized%)`% of a year. The hold SHALL NOT end early.

#### Scenario: Partial crystallization
- WHEN the pawn is 50% crystallized
- THEN the hold lasts 50% of a year

### Requirement: Emergence as Umbral
When the hold ends, the pawn SHALL emerge as Umbral, as defined in `umbral-rework`.

### Requirement: Building split
Every Tenebral building SHALL be replaced by an Umbral building, except the turret, which SHALL remain Tenebral. Tenebral drugs and medicine SHALL be produced for the Umbral line instead of the Tenebral line.

### Requirement: New Tenebral buildings
Tenebral SHALL gain two new buildings: the Dark Archotech Altar and the Umbral Transformation Pod.

### Requirement: Dark Archotech Altar
The Dark Archotech Altar SHALL be a special Forge whose grid is prefilled with tenebral items: melee weapons, guns, tenebrite, tenebral organ, tenebral appendage, and femtoclusters.

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
The Tenebral pod SHALL be a Tenebral building that combines a bionic installator, a healing pod, and a stasis pod. It SHALL provide a mode gizmo with three modes: stasis, healing, and operating.

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
- Whether a pawn can be placed in the Umbral Transformation Pod before reaching 50%.
- Whether crystallization progresses while the pawn is not psychotic.
- The scaling formula for crystalline joints (componentIII) per requested item.
- How many work actions unlock each locked cell on the altar.
