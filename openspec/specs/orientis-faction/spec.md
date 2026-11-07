# Orientis Faction Specification

## Purpose
Orientis is the path of light, offered once per colony when a psychotic pawn reaches the third break (see `tenebral-rework`). Choosing it purges the colony's tenebral parts and Umbral pawns, and turns the stripped pawns into the Orientis xenotype. Orientis has no bionics. Its pawns meditate to generate a resource, meditation power, which powers its automated buildings. Its body enhancements come from meditation at the blessing bench. Its buildings are placed by an ability, not built from the Architect menu.

Status: implemented in `Source/Orientis/`, with placeholder art and untuned numbers. See Implementation decisions.

## Style
- Smooth, gold, white, and silver. Lofty, beautiful, welcoming but imposing.
- Buildings are Kwazaari-adjacent, with real-world references to mosques, churches, and temples.

## Requirements

### Requirement: The choice
When a counted break ends for a player pawn that has reached the `tenebral-rework` break threshold (three breaks), the game SHALL send a choice letter. It SHALL be sent after the mental state ends, not when it starts. Only psychosis and the break count SHALL matter: the pawn SHALL NOT need to hold tenebral parts. The pending letter and the colony's choice SHALL survive a save and load.

The letter SHALL read:

> A great shadow looms over the Settlement. {colonist} feels a sick sensation in their chest, their limbs, their groin. Something that is both unfathomably dark --but so bright it feels like needles in their eyes -- blinds them. They lift their palm to shield their face, when a voice rumbles through the shadows. It sounds like a chorus, and surrounds {colonist}.
>
> “For years, another Will has spoken through this Colony. Your Home. It has shaped your buildings. It has shaped your people. It may yet shape your future. But here you stand, on the edge of The Precipice. Only madness remains.
>
> I give you a choice -- go down the dark path and lose yourself to the Dark Archotech's machinations, or turn away and embrace the light. Whatever you choose, there is no going back.”

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
Choosing either path SHALL show the response "So be it." Choosing the Path of Dark SHALL set the colony's path permanently. Nothing else SHALL change. Break counting and crystallization SHALL continue as defined in `tenebral-rework`.

### Requirement: Path of Light
Choosing the Path of Light SHALL set the colony's path permanently and SHALL run these steps in order, each finishing before the next starts. It applies to every living pawn belonging to the colony (player faction, slaves included), wherever it is: on any map, in a caravan, in a transport pod, off-map, or held in a container such as a Tenebral pod or cryptosleep casket. Prisoners, guests, and pawns of other factions SHALL NOT be affected.

1. Stripping (see Requirement: Stripping).
2. Umbral judgment (see Requirement: Umbral judgment).
3. Every pawn that was stripped and survived SHALL become the Orientis xenotype. So SHALL the pawn that triggered the letter if it is still alive, even if it had nothing to strip.
4. Every Orientis pawn SHALL gain the manifest ability.

### Requirement: The Dark Archotech's answer
One in-game hour after the Path of Light is chosen, a letter SHALL say "Dark Archotechs are not pleased." It SHALL NOT expire or be dismissible with a right click. A few real seconds after it is closed, the game SHALL unpause if paused, the camera SHALL jump to the strike, and dark lightning SHALL strike a random standable, unfogged home area cell that can reach a colonist, or a cell near a colonist if there is none. A `BOR_Umbral` SHALL spawn there. It SHALL go for the closest non-Umbral colonist it can reach, bashing doors if it must, pin them, tear off their limbs and kill them, then skip away and leave the map. If the target is carried off or leaves the map, it SHALL take the next closest. If the target dies any other way, it SHALL skip away. It can be fought, and it SHALL not use its dark abilities or its swarm behaviour. If no home map has a colonist it can reach, nothing happens and a warning is logged.

### Requirement: Stripping
Every tenebral part SHALL be removed from every pawn holding one: every hediff with `DefModExtension_TenebralImplant` and every `BOR_Crystallized` hediff.

- The body part underneath SHALL be restored and SHALL NOT be left missing. It SHALL then carry a `BOR_GoldenScar` hediff, which SHALL work like a vanilla scar (a permanent injury) and SHALL never be enough to destroy the part.
- Stripping SHALL NOT kill a pawn, including when the part is the torso or heart.
- A spawned pawn being cleansed SHALL glow with a golden aura for 10 seconds from the choice.
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
| Blessing bench | `blessing_bench` | `research_unlocker` |

### Requirement: Light and dark stay apart
Orientis pawns SHALL NOT use tenebral or Umbral weapons, armor or clothing, tenebrite, or anything made with tenebrite. Tenebral and Umbral pawns SHALL NOT interact with Orientis buildings or items.

### Requirement: Melee weapon glow
The blades of Orientis melee weapons SHALL glow faintly while held, and their handles SHALL NOT.

### Requirement: Dark buildings fall
Choosing the Path of Light SHALL end the colony's tenebral and Umbral buildings on every map.
- Each dark archotech altar SHALL be struck by a golden beam from the sky, at the same time as the Umbral pawns, and SHALL become an inert husk at that moment. It SHALL drop no materials and none of the components it held.
- Each struck altar SHALL send a ring of light across the whole map. A tenebral or Umbral building on that map SHALL die as the ring reaches it. Buildings on maps with no altar SHALL die when the sequence ends.
- Every other tenebral and Umbral building SHALL become an inert husk that has its footprint and looks like it, greyed out. Pawns in a Tenebral pod SHALL be ejected first.
- All dark gear on the colony's pawns SHALL burn at the moment tenebral parts are stripped, whether or not the pawn is converted or on a map: worn, wielded, carried or in inventory. An Umbral pawn's grown exoskeleton and utility pod are not gear. Those pawns SHALL glow as the others do.
- Dark items lying on the map SHALL burn away as the ring reaches them. Any still left when the sequence ends, on any map, SHALL burn then, including gear dropped during conversion. Items worn, carried or inside a container SHALL NOT burn.
- Their blueprints and frames SHALL be cancelled with a refund.
- From the moment the path is chosen, these buildings SHALL NOT appear in the build menu, and pawns SHALL NOT build or use any that remain. They MAY still be deconstructed.

### Requirement: Meditation power
Meditation power SHALL be an abstract resource. It is generated by pawns meditating in a meditation pod. Buildings SHALL use it as power or fuel. It SHALL be storable in a meditation cache. It SHALL NOT require power-grid connectivity.

### Requirement: Meditation pod
The meditation pod SHALL be the place where Orientis pawns meditate to generate meditation power.

### Requirement: Meditation shield
A meditation pod SHALL have an "Enable shield" gizmo for a shield covering a circle 50 tiles across. An Orientis pawn SHALL have to walk to the pod to raise it.
- The shield SHALL stop hostile projectiles, mortar fire and orbital bombardments.
- Tenebral and Umbral pawns inside SHALL lose 35% consciousness at once and 5% more each in-game hour.
- The shield SHALL draw meditation power from the map's caches, and SHALL fall when there is none left.

### Requirement: Meditation cache
A pawn meditating near a meditation cache SHALL load meditation power into it. Automated buildings SHALL draw from the cache to run.

### Requirement: Holy forge
The holy forge SHALL be automatic and SHALL be powered by meditation power, not neutronium. It SHALL have a 12x12 grid. Analysis SHALL remain manual.

### Requirement: Offerings bench
The offerings bench SHALL be a bench with bills that cannot be interacted with directly. It SHALL produce only Orientis items, automatically. It SHALL be fueled by meditation power.

### Requirement: Meditation bench and meditation mat
The meditation bench and meditation mat SHALL generate a smaller amount of meditation power and load it into a cache, as a pod does. Using them SHALL be normal Meditate work for Orientis pawns. They SHALL NOT count as recreation or use the meditation schedule.

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
- What happens to tenebrite gear after the Path of Light. It is left as it is, and exposure can still give an Orientis pawn psychosis.
- Whether a colony can get more Orientis pawns after the Path of Light, for example by recruiting or birth.
- Which genes the Orientis xenotype carries. It has none yet.
- Final name of the manifest ability (working name), and whether it should have a cooldown or casting time. It has neither.
- Final art for the buildings, gear, and symbols.
- Balance of every number below, and a Combat Extended patch for the new gear.

## Implementation decisions
Implemented in `Orientis.cs`, `OrientisPath.cs`, `OrientisManifest.cs`, `MeditationPower.cs`, `OfferingsBench.cs` and `Blessing.cs`. Path tunables live on the `BOR_OrientisPath` def; building tunables live in each building's comp props.

- `GameComponent_OrientisPath` holds the colony's path and the running sequence, so a save partway through resumes it.
- Orientis pawns can't take bionics or implants by surgery, tenebral ones included. Natural transplants still work.
- Meditation power is one pool per map. A pod makes 200 per day at 100% strength, a bench 100 and a mat 50, and each loads the caches on the map with no range limit, sharing evenly between those with room. A cache holds 1000. Every cache shows the whole map's pool.
- Meditating at a pod, bench or mat is its own work type, `BOR_Meditate`, set to priority 3 when a pawn becomes Orientis. Only Orientis pawns get the job, and they take the strongest free spot first.
- The shield is a radius of 25 and draws 400 power per day, so one pod's output covers half of it. It uses vanilla's projectile interceptor, plus a patch that stops the power beam. Bombardments and beams only stop if an enemy fired them. Disabling takes effect at once, with no pawn. A shield's effect on a pawn ends when it leaves the shield, and stops at 95% so it can't kill by itself. A pawn counts as dark if it is Umbral, holds a tenebral or crystallized part, has tenebral psychosis, or belongs to the Tenebrites faction or one of its pawn kinds. Burning pawns glow gold.
- A tenebral part is any added part or implant named tenebral or III, or made from a tenebral organ or appendage. All of them carry `DefModExtension_TenebralImplant`, including the Royalty and EPOE compat parts, so the Path of Light strips them and the shield sees them.
- A dark thing is any thing named tenebral or Umbral, tenebrite itself, or anything that costs or is crafted from a dark thing, followed through every step. This covers things like femtoclusters, tier IV medicine and Adreamoline. Those things and Orientis things are forbidden to the other side's colonists, including hauling, and can't be equipped. A pawn made Orientis by the debug action drops dark gear, or keeps it unused in inventory if off the map. Anything marked for deconstruction can still be taken down.
- Meditation strength is the stat `BOR_MeditationStrength`. The monk bodysuit adds 25%.
- The holy forge draws 0.05 power per 10 ticks of work, about 300 per day. Its production waits while analysis is pending. Its tab uses smaller cells so the 12x12 grid fits on screen.
- The offerings bench takes no materials and draws 0.005 power per tick of work, about 300 per day. It only makes Orientis items, and those items are made nowhere else.
- A blessing takes 1250 ticks per part and adds 220% part efficiency for one quadrum, so a healthy part works at 320%, between kwazaari (234%) and tenebral (422%). Several parts can be blessed in one meditation. Blessing a part again restarts its quadrum and does not stack. Parts with an added part can't be blessed.
- "Charge" means the vanilla charge weapon idea, not a new mechanic: guns fire single shots after a long warmup with no ammo, and melee hits add burn damage.
- Additional weapons are one arquebus each for Industrial, Spacer, Ultra and Archotech. They live in this mod, not in AllBambaGuns or AllBambaMelee.
- Orientis melee weapons have their own stand-in copies of existing art in `Equipment/orientis/`, each with a `_glow` texture that shows the blade only. `CompWeaponGlow` draws it over the held weapon with an additive shader, including a soft halo past the blade. It does not show on the ground. During Melee Animation's animations, `Mods/MeleeAnimation` draws it through a custom renderer named in each weapon's file in `WeaponTweakData`; the positions in those files are first estimates. Combat Extended is untested. Other gear textures point at existing art instead of copies. Armor is Tier 2 trooper art for light, Tier 2 for medium and Tier 3 for heavy. The monk bodysuit uses the Solar bodysuit art.
- The symbols are drawn placeholders: `BOR/UI/Orientis/MeditationPower` and `Manifest`, and `BOR/Things/Factions/OrientisIcon`. There is no `FactionDef`.
- Choosing the path unpauses the game and shows a message. Stripping lasts 120 ticks and parts come out at 60. The glow lasts 600 ticks from the choice and outlasts the rest of the sequence. The ring moves 0.75 tiles per tick, and the sequence waits for it to cross the map. The sequence waits 60 ticks on the beams, and not at all if there are no Umbral pawns.
- The golden scar takes 25% of the part's health, capped so the part keeps at least 1 health. Its pain is a vanilla cut scar's.
- Every pawn that becomes Orientis loses psychosis, the break counter and dark abilities, including a triggering pawn with nothing to strip.
- Building costs use tier 2 materials, kwazaari alloy and kwazaari universal parts, plus gold or silver. The amounts are placeholders in line with similar tier 2 buildings.
