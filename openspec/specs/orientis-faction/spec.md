# Orientis Faction Specification

## Purpose
Orientis is the path of light, offered once per colony when a psychotic pawn reaches the third break (see `tenebral-rework`). Choosing it purges the colony's tenebral parts and Umbral pawns, and turns the stripped pawns into the Orientis xenotype. Orientis has no bionics. Its pawns meditate to generate a resource, meditation power, which powers its automated buildings. Its body enhancements come from meditation at the blessing bench. Its buildings are placed by an ability, not built from the Architect menu.

Status: not implemented. The code still has the earlier surgical exit under the Holytech name (`BOR_HolytechCandidate`, `holytechThreshold`, `CanTakeHolytechExit`, `BOR_HolytechExit*`). This spec replaces it.

## Style
- Smooth, gold, white, and silver. Lofty, beautiful, welcoming but imposing.
- Buildings are Kwazaari-adjacent, with real-world references to mosques, churches, and temples.

## Requirements

### Requirement: The choice
When a counted break ends for a player pawn that has reached the `tenebral-rework` break threshold (three breaks), the game SHALL send a choice letter. It SHALL be sent after the mental state ends, not when it starts. Only psychosis and the break count SHALL matter: the pawn SHALL NOT need to hold tenebral parts. The pending letter and the colony's choice SHALL survive a save and load.

The letter SHALL read:

> You are at the precipice of madness. I give you a choice - go down the dark path and lose yourself to the Dark Archotech's machinations, or turn away and embrace the light.

It SHALL offer two options: **Path of Light** and **Path of Dark**. It SHALL NOT expire or be dismissible without choosing.

The choice SHALL be made once per colony and SHALL be final. While a letter is unanswered, no other SHALL be sent. Once either path is chosen, no more letters SHALL be sent and the other path SHALL no longer be available.

#### Scenario: Letter waits for the break to end
- WHEN a psychotic player pawn goes berserk on its third break
- THEN no letter appears until the pawn is out of the mental state

#### Scenario: Pawn does not come back
- WHEN the pawn dies or defects during the break
- THEN no letter is sent for that pawn, and another eligible pawn can still trigger it later

#### Scenario: Exposure only
- WHEN a player pawn with no tenebral parts reaches three breaks from tenebrite exposure
- THEN the letter is sent when that break ends

### Requirement: Path of Dark
Choosing the Path of Dark SHALL show the response "So be it." and SHALL set the colony's path permanently. Nothing else SHALL change. Break counting and crystallization SHALL continue as defined in `tenebral-rework`.

### Requirement: Path of Light
Choosing the Path of Light SHALL set the colony's path permanently and SHALL run these steps in order, each finishing before the next starts. It applies to every living pawn belonging to the colony (player faction, slaves included), wherever it is: on any map, in a caravan, in a transport pod, off-map, or held in a container such as a Tenebral pod or cryptosleep casket. Prisoners, guests, and pawns of other factions SHALL NOT be affected.

1. Stripping (see Requirement: Stripping).
2. Umbral judgment (see Requirement: Umbral judgment).
3. Every pawn that was stripped and survived SHALL become the Orientis xenotype. So SHALL the pawn that triggered the letter if it is still alive, even if it had nothing to strip.
4. Every Orientis pawn SHALL gain the manifest ability.

### Requirement: Stripping
Every tenebral part SHALL be removed from every pawn holding one: every hediff with `DefModExtension_TenebralImplant` and every `BOR_Crystallized` hediff.

- The body part underneath SHALL be restored and SHALL NOT be left missing. It SHALL then carry a `BOR_GoldenScar` hediff, which SHALL work like a vanilla scar (a permanent injury) and SHALL never be enough to destroy the part.
- Stripping SHALL NOT kill a pawn, including when the part is the torso or heart.
- A spawned pawn SHALL glow faintly with a golden aura while it is being stripped.
- Stripped pawns SHALL also lose tenebral psychosis, the break counter, dark ability hits, and dark abilities.

#### Scenario: Crystallized torso
- WHEN a pawn with a crystallized torso is stripped
- THEN the torso is restored with a golden scar, and the pawn survives and becomes Orientis

#### Scenario: Tenebral arm
- WHEN a pawn with a tenebral arm is stripped
- THEN the pawn has a natural arm with a golden scar, and becomes Orientis

#### Scenario: Crystallized parts from exposure
- WHEN a pawn with no implants has crystallized parts from tenebrite exposure
- THEN the parts are restored with golden scars, and the pawn becomes Orientis

#### Scenario: Psychosis without parts
- WHEN a pawn has psychosis from tenebrite exposure but no tenebral part, and did not trigger the letter
- THEN the pawn is not stripped and does not become Orientis

#### Scenario: Triggering pawn without parts
- WHEN the pawn that triggered the letter has no tenebral part
- THEN the pawn is not stripped but still becomes Orientis

### Requirement: Transforming pawns
A Tenebral pod holding a colony pawn in transformation mode SHALL be destroyed during the stripping step, and that pawn SHALL be killed with it. The pawn SHALL NOT become Orientis. Tenebral pods in other modes SHALL be left alone, and pawns held in them SHALL be stripped as normal.

#### Scenario: Pod mid-transformation
- WHEN the Path of Light is chosen while a colonist is transforming in a Tenebral pod
- THEN the pod is destroyed and the colonist dies

### Requirement: Umbral judgment
Every Umbral pawn belonging to the colony (`Umbral.IsUmbral`, so the hediff fallback counts) SHALL be killed. A spawned pawn SHALL be killed by a golden beam from the sky. An unspawned pawn SHALL die without a beam. Armor, exoskeleton modes, and invisibility SHALL NOT prevent the death, and the beam SHALL NOT harm anything else. An Umbral pawn SHALL NOT become Orientis, even if it was stripped first.

### Requirement: Orientis xenotype
Orientis SHALL be a xenotype. Without Biotech, a `BOR_Orientis` hediff SHALL stand in for the xenotype and its genes, as `BOR_Umbral` does for Umbral.

### Requirement: Manifest ability
The manifest ability SHALL be how Orientis buildings are placed. Using it SHALL open a window that lists every Orientis building and shows its image, description, and cost, with the amount of each material available on the caster's map.

- A building the map cannot pay for SHALL be shown but SHALL NOT be selectable.
- Selecting a building SHALL close the window and start normal placement, with the same ghost, rotation, and placement rules as an Architect building.
- Confirming a location SHALL check the cost again. If the map can no longer pay, nothing SHALL be taken or placed, and the player SHALL be told why.
- Otherwise the cost SHALL be taken from the map with a skip effect over each stack it comes from. The finished building, owned by the player, SHALL then appear at the location with a big skip effect.
- Materials SHALL come from spawned, unforbidden, unfogged items on the caster's map, nearest to the location first. Partial stacks SHALL be split.
- The location SHALL be limited only by placement rules, not by the caster's range.

#### Scenario: Not enough materials
- WHEN the map holds less than a building's cost
- THEN the building is listed with its cost but cannot be selected

#### Scenario: Placing a building
- WHEN the player selects an affordable building and confirms a valid location
- THEN the materials vanish with skip effects and the building appears at once with a big skip effect

### Requirement: Orientis buildings
Every Orientis building SHALL have no research requirement and SHALL NOT appear in the Architect menu. Each SHALL have a fixed cost and SHALL NOT use a stuff choice. Every building under the requirements below is an Orientis building.

### Requirement: Placeholder textures
Until final art exists, Orientis buildings SHALL use the copies of tier 2 textures in `Textures/BOR/Things/Building/orientis/`.

| Building | Texture | Copied from `tier2/` |
| --- | --- | --- |
| Meditation pod | `meditation_pod/meditation_pod` (multi) | `medicine_bench/medicine_bench` |
| Meditation cache | `meditation_cache/meditation_cache` (multi) | `battery/battery2` |
| Holy forge | `holy_forge` | `smelter` |
| Offerings bench | `offerings_bench` | `production_bench` |
| Meditation bench | `meditation_bench/meditation_bench` (multi) | `bench_linkable/bench_linkable` |
| Meditation mat | `meditation_mat` | `QuasarHydroponics_Tileable_Icon` |
| Archotech icon | `archotech_icon` | `reactor` |
| Blessing bench | `blessing_bench` | `research_unlocker` |

### Requirement: Meditation power
Meditation power SHALL be an abstract resource. It is generated by pawns meditating in a meditation pod. Buildings SHALL use it as power or fuel. It SHALL be storable in a meditation cache. It SHALL NOT require power-grid connectivity.

### Requirement: Meditation pod
The meditation pod SHALL be the place where Orientis pawns meditate to generate meditation power.

### Requirement: Meditation cache
A pawn meditating near a meditation cache SHALL load meditation power into it. Automated buildings SHALL draw from the cache to run.

### Requirement: Holy forge
The holy forge SHALL be automatic and SHALL be powered by meditation power, not neutronium. It SHALL have a 12x12 grid. Analysis SHALL remain manual.

### Requirement: Offerings bench
The offerings bench SHALL be a bench with bills that cannot be interacted with directly. It SHALL produce only Orientis items, automatically. It SHALL be fueled by meditation power.

### Requirement: Meditation bench and meditation mat
The meditation bench and meditation mat SHALL raise a pawn's meditation slightly. They SHALL count as recreation.

### Requirement: Archotech icon
An archotech icon statue SHALL be provided as a decorative building.

### Requirement: Blessing bench
Orientis pawns have no bionics. A pawn MAY go to the blessing bench and meditate.

- The pawn SHALL choose which body parts to bless.
- When meditation finishes, each chosen body part SHALL receive a hediff lasting one quadrum. The hediff SHALL act like a bionic, but the body part SHALL NOT be replaced. It is enhanced, not swapped.

#### Scenario: Blessing a part
- WHEN a pawn finishes meditating at the blessing bench with the heart selected
- THEN the heart receives a one-quadrum enhancement hediff

### Requirement: Weapons
Main weapons:
- Crusader-style power axe, power longsword, power dagger, and power pole hammer.
- Plasma crossbow, a boom-launcher style weapon.
- Laser crossbow, DMR-like.

Additional weapons:
- Guns for all tech levels: high-tech arquebus-like charge weapons.
- Melee for all tech levels: medieval-styled power weapons with charge.

### Requirement: Armor
- Light: Saladin-styled, in the style of the First Crusade.
- Medium: Templar armor with a cape, high-tech, styled after medieval plate.
- Heavy: Black Templar (Warhammer 40k) adjacent.

### Requirement: Apparel
A monk-style bodysuit with the hood down. It SHALL boost meditation strength.

### Requirement: Symbols
A symbol for the meditation resource, a faction symbol, and related icons.

## Open questions
- How much health the golden scar takes from a restored part.
- Whether an Orientis pawn can take tenebral parts or be exposed to psychosis, and what happens to tenebrite buildings and gear, the altar, and Umbral buildings after the Path of Light.
- Whether a colony can get more Orientis pawns after the Path of Light, for example by recruiting or birth.
- Which genes the Orientis xenotype carries.
- Final name of the manifest ability (working name), and whether it has a cooldown or casting time.
- Whether only Orientis pawns can use Orientis buildings.
- Delay between Path of Light steps, and the cost of each building.
- Meditation generation rate in the meditation pod, and the cache capacity.
- Meditation boost from the meditation bench, meditation mat, and monk bodysuit.
- Enhancement strength of the blessing hediff compared to a bionic.
- Whether a pawn can bless more than one body part per meditation, and whether a body part can be blessed again before its hediff expires.
- Ammo and charge mechanics for the charge weapons.
