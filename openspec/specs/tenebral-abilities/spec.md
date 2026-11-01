# Tenebral Abilities Specification

## Purpose
Crystallizing tenebral pawns and transforming Umbral pawns can gain up to four dark abilities through repeated rolls. Implemented by `DarkAbilities.cs`.

## Requirements

### Requirement: Abilities
| Ability | Effect | Cooldown |
| --- | --- | --- |
| Wound | Wounds the target all over until it is downed | 2 hours |
| Kill | Destroys the target's heart; the kill is credited to the caster | 4 hours |
| Thrall | Target becomes a slave of the caster's faction and gains permanent brain damage | 12 hours |
| Dark bolt | Red lightning strike and a vaporize explosion at the target | 6 hours |

### Requirement: Crystallization roll
Each crystallization event that turns at least one part to crystal SHALL roll between the four abilities and two empty outcomes. An empty outcome adds no hit.

### Requirement: Umbral roll
Each Umbral transformation step SHALL roll between the four abilities and eight empty outcomes.

### Requirement: Gaining an ability
A pawn SHALL gain an ability on its third hit. Hits are stored in the hidden `BOR_DarkAbilityHits` hediff, which SHALL survive the tenebral to Umbral transformation. A hit on an ability the pawn already has is wasted.

### Requirement: Bloodlust cost
A non-Umbral caster SHALL lose 50% of the bloodlust bar on each cast, and the ability SHALL be disabled while the pawn has less than that (or no bloodlust need). Umbral casters pay nothing.

#### Scenario: Kill sates the caster
- WHEN a tenebral pawn kills a humanlike with Kill
- THEN the cost is paid first and the kill then refills bloodlust to maximum

### Requirement: Thrall without Ideology
Slavery needs Ideology. Without it, Thrall SHALL recruit the target into the caster's faction instead, still with brain damage.
