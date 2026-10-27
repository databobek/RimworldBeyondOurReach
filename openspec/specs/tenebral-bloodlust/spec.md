# Tenebral Bloodlust Specification

## Purpose
Tenebral implants and bionics carry a cost: a "bloodlust" need that drains faster with more implants and is refilled only by killing humanlikes. Implemented by `MurderNeed.cs`.

## Requirements

### Requirement: Implant marker
Hediff defs carrying `DefModExtension_TenebralImplant` (placed on the abstract tenebral implant/bionic bases) SHALL each count as one implant for their pawn; `TenebralPainStripper` SHALL set `painOffset` to 0 on every stage of such defs at startup.

### Requirement: Need presence
`BOR_Murder` ("bloodlust") SHALL exist only for adult, humanlike, colonist pawns with at least one tenebral implant. The need set is refreshed when such a hediff is added, removed, or changed.

### Requirement: Drain rate
Days from full to empty = 15 when 0 implants, else max(1, 15 / implants). The need updates every 150 ticks and does not drain while frozen. New pawns start at full.

#### Scenario: Many implants
- WHEN a pawn has 15 or more implants
- THEN the need empties in 1 day (floor)

### Requirement: Refill by killing
When a pawn kills a living humanlike other than themselves (damage instigator), the killer's need SHALL be set to maximum.

### Requirement: Unsated penalty
`ThoughtWorker_Murder` SHALL be active (thought state on) while the need is at level <= 0.001.

### Requirement: Mental break override
While unsated, a random mood-caused mental break SHALL be replaced by Berserk (force-wake) with reason text. The break is skipped (no break) if breaks are disallowed, the pawn is downed, asleep, or already aggro.

#### Scenario: Defection
- WHEN a berserk starts for a player-faction pawn
- THEN with chance implants x 1% the pawn switches to a random enemy faction (falling back to hostile ancients) and a letter is sent
