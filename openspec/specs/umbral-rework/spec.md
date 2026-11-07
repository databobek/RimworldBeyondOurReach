# Umbral Rework Specification

## Purpose
Umbral is a xenotype reached at the end of the Tenebral path (see `tenebral-rework`). Umbrals cannot take bionics from surgery. Instead they slowly transform over three phases, each triggered by killing, until their body is replaced by Umbral parts. Umbral buildings are usable only by Umbral pawns.

## Requirements

### Requirement: Umbral xenotype
Umbral SHALL be a xenotype. Pawns with the Umbral xenotype SHALL carry genes that prevent them from receiving any body part through an operation, including artificial parts and replacement of a lost part. A lost body part SHALL NOT be recoverable by surgery.

### Requirement: Genes cannot be extracted
A gene extractor SHALL refuse Umbral and Orientis pawns. Umbral genes SHALL NOT appear in generated genepacks.

### Requirement: Psychosis resistance gene
The Umbral xenotype SHALL include a gene that makes the pawn resistant to tenebral psychosis.

### Requirement: Tenebrite-boosted transformation
Exposure to tenebrite SHALL increase the chance of the transformation hediff being triggered in an Umbral pawn. The size of the increase is to be decided during implementation.

### Requirement: Hidden Umbral faction
A hidden `BOR_Umbrals` faction SHALL exist in every new world, permanently hostile, with no settlements and no entry in the faction list. Its only pawn kind SHALL be `BOR_Umbral`. It SHALL NOT be chosen for random raids; only a raid that names the faction directly can use it.

### Requirement: Umbral ideoligion
With Ideology, the `BOR_Umbrals` faction SHALL hold a fixed ideoligion, "The Other Will", with the Archist, Darkness and Supremacist memes and one deity, Horax. It SHALL have no roles and no rituals shown in the tab; vanilla's hidden rituals stay because the game requires them. Slavery SHALL be honorable and execution required, and it SHALL not care about corpses or clothing. Beyond those, it SHALL hold only the precepts its memes require and vanilla's special precepts. It SHALL stay out of the Ideoligions tab until a pawn holding it spawns. A player pawn that turns Umbral SHALL switch to it, and the rest of the colony SHALL keep theirs. Worlds created without the faction get no support.

### Requirement: Umbrals feel nothing
Turning Umbral SHALL remove all of a pawn's traits and memories. An Umbral SHALL have one thought, `BOR_UmbralEmpty`, with no mood effect, and SHALL gain no other, social thoughts included. Its mood SHALL stay at 50% whatever the difficulty.

### Requirement: Righteous Slaughter
The Righteous Slaughter SHALL follow the `component-harvest` rules for research, bench and operator, but use a live prisoner instead of a corpse, use no picocluster and give no components. It SHALL be hidden until the colony has an Umbral, and only an Umbral (by xenotype or `BOR_Umbral` hediff) SHALL be able to perform it. The operator SHALL pin the prisoner, carry them to the bench, tear off their limbs from the outside in and then kill them, and it SHALL count as a vanilla execution. If the job ends early, the prisoner SHALL be released, keeping any wounds already dealt. The body SHALL be destroyed, leaving a skull named after the victim. It SHALL spread blood, bile and feces further than a harvest and have a 10-day cooldown per bench. When it finishes, every Umbral of the player's faction SHALL recover abilities 50% faster (a cooldown lasts 1/1.5 as long) and transform 25% faster for 10 days. Another slaughter SHALL restart the blessing, not stack it. Cooldowns already running are not shortened.

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
The Umbral exoskeleton SHALL be a special armor type. An exoskeleton-equipped pawn SHALL have a gizmo that switches between four modes. A new exoskeleton starts in no mode:

- **NO MODE:** no armor and no bonuses.
- **CAMO:** the pawn is invisible but cannot interact with anything (phaseshift). It lasts 2 hours.
- **UTILITY:** no armor, and all work-related stats are improved. It lasts 24 hours.
- **COMBAT:** very high armor, including the exoskull, and all work-related stats are reduced significantly. It lasts 12 hours.

When a mode's time runs out, the exoskeleton SHALL revert to no mode.

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

## Implementation decisions
Implemented in `Umbral.cs`. Tunables live on `BOR_UmbralTransformation` and the exoskeleton hediff stages.

- Phases A, B and C share one transformation hediff. Each unit of tenebrite emission adds 50% to the trigger chance.
- Without Biotech, the `BOR_Umbral` hediff stands in for the xenotype and its genes.
- Utility mode adds 250% global work speed, 960% mining, construction and plant work, 150% research and tending, 2250% cooking and 180% bionic crafting. These are the Tier 3 implant and Tier 4 Adreamoline numbers times 1.5. Selecting a mode restarts its timer, with no cooldown. No mode has placeholder art, using the utility textures.
- The exoskeleton, exoskull and utility pod are hediffs. Their visuals are locked, destroy-on-drop apparel and weapons, swapped for each mode. The exoskull follows the exoskeleton's mode.
- Camo: invisible, Manipulation capped at 0. Utility: no armor, work speed x1.5 (mining, construction and plants x1.3). Combat: +2.5 sharp, +1.2 blunt and +1.8 heat armor, work speed x0.4.
- Utility pod forms: blade, tool (work-speed bonuses), gun (Tier 3 rifle bullet), launcher (explosive).
