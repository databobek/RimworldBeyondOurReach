# Forge Specification

## Purpose
Forges (`BOR_Forge_I`..`BOR_Forge_IV`) reproduce items. A forge has a fixed grid of schematic cells; items are *analyzed* (destroyed) to teach the forge a schematic, which can then be produced repeatedly, optionally drawing neutronium from the pipe net. Implemented by `CompForge` / `CompProperties_Forge`, `ForgeAnalyze.cs`, `ForgeProduce.cs`, `ForgeFormat.cs`, `ForgeOverdrive.cs`, `ForgeMath.cs`, `ITab_Forge.cs`.

## Requirements

### Requirement: Configurable grid and resource
A forge SHALL be configured through `CompProperties_Forge`: `gridWidth` x `gridHeight` cells (capacity = product, defaults 2x4), `damagePerOverdriveStep`, `stepMultiplier` (default 1), optional `pipeNet` and `pipeAmount` drawn per work step, `workTicksPerDraw` (default 10), an `exclusionList` of defNames/trade tags, and `analysisSuccessOffset` (percent). Each cell holds at most one learned item def.

#### Scenario: Forge without a pipe net
- WHEN a forge has no `pipeNet` or `pipeAmount <= 0`
- THEN resource checks SHALL always pass and nothing is drawn

### Requirement: Analysis eligibility
A thing SHALL be analyzable only if the forge has a free cell (free cells minus pending analyses > 0), it is an item (not a corpse or minified thing), haulable, has market value > 0, is not an unfinished thing, is not `BOR_Neutronium`, is not already known or already pending analysis, and is not on the forge's exclusion list (by defName or trade tag).

#### Scenario: Too-complex item
- WHEN the player picks an item matching the forge's `exclusionList` 
- THEN a "too complex" rejection message is shown and the item is not queued

### Requirement: Analysis work
A pawn SHALL carry one pending item to the forge and work for `AnalysisTicks`, gaining Intellectual XP (0.08/tick). The item is always destroyed on completion.

- Duration = max(60, BaseWorkTicks x 2 / BOR_ForgeAnalysis stat, floor 0.1).
- Success chance = clamp01(Intellectual level / 25 x (100 + offset)/100). On success the forge learns the def (and the item's stuff if the def is made from stuff) into the first free cell; on failure the item is lost.

#### Scenario: Failed analysis
- WHEN the success roll fails
- THEN a negative message is shown and no schematic is added

#### Scenario: Unpowered or unassigned
- WHEN the forge is unpowered, formatting, forbidden, or assigned to other pawns only
- THEN no analysis job is offered to the pawn

### Requirement: Production work
A pawn SHALL produce from a slot where `ShouldProduce` is true, gaining Intellectual XP (0.08/tick).

- Duration = max(30, BaseWorkTicks / BOR_QuantumForging stat, floor 0.1).
- Every `workTicksPerDraw` ticks the forge SHALL draw `pipeAmount` neutronium; if it cannot, the job ends incompletable. The job is not offered when the net lacks the amount.
- On completion the forge SHALL spawn the item at the interaction cell (near-placement) with quality Normal, stack = `YieldFor(def)`, using the stored stuff (or default stuff).

#### Scenario: Neutronium runs out mid-job
- WHEN a draw fails during production
- THEN the job ends and progress for the slot resets to 0

### Requirement: Production modes
Each cell SHALL have a mode: `DoX`, `DoUntil`, or `DoForever`, and a queued count.

- DoX: produce while queued > 0; each finished run decrements queued.
- DoUntil: produce while queued > 0 and the map stack count of the def < queued (queued is the target; not decremented).
- DoForever: always produce.
- The next production slot is the lowest-index cell that should produce.

#### Scenario: DoUntil target reached
- WHEN the colony holds at least the target count of the item on the map
- THEN that cell is skipped

### Requirement: Work cost and yield formulas
- TechMultiplier = 1 + 0.5 x max(0, techLevel - 1).
- ValueMultiplier = 1 + sqrt(max(1, marketValue)) / 3.
- BaseWorkTicks = 600 x TechMultiplier x ValueMultiplier.
- Yield: stack limit <= 1 gives 1; otherwise clamp(round(stack x 0.75 x 1/(1+value/30) x 1/(1+0.25 x max(0, tech - Industrial))), 1, stack).

### Requirement: Overdrive
Each forge SHALL have an overdrive level, a per-forge setting with 10 stages (index 0..9) shown as 100%, 200%, ... 1000%. The default is stage 0 (100%). Overdrive applies only to production, never to analysis or formatting.

- **Step multiplier**: `CompProperties_Forge.stepMultiplier` scales everything each step adds (output, power and wear); the stages stay 10 for every forge. Forge I (Solar) 1, II (Quasar) 1.5, III (Tenebral) 2, IV (Umbral) 5. Formulas below use `s = stepMultiplier x stage`.
- **Output**: the produced stack SHALL be `floor(YieldFor(def) x (1 + 0.5 x s))`. Examples at multiplier 1: 1 gun gives 1 / 1 (1.5 rounded down) / 2 at 100% / 200% / 300%; 256 steel gives 256 / 384 / ... At multiplier 5 (Umbral), 200% gives 3.5x. The multiplier is applied after `YieldFor`'s clamp, so the result may exceed the def's stack limit and SHALL be spawned as multiple stacks if needed.
- **Power**: while the forge is in use for production, power draw SHALL be `base x (1 + 0.25 x s)` (at multiplier 1: 100% = +0%, 200% = +25%, 300% = +50%, ... 1000% = +225%). When idle, or while analysing or formatting, power draw is the unmodified base.
- **Wear**: while production work is in progress at stage > 0, the forge SHALL take damage of `damagePerOverdriveStep x s` percent of max hitpoints per second (`CompProperties_Forge.damagePerOverdriveStep`, default 0.1; a value of 1 means +1% per second per step at multiplier 1), where one second is 60 ticks of game time regardless of game speed. At multiplier 1 and the default value that is 0 at 100% (no wear), 0.1% per second at 200%, 0.2% at 300%, up to 0.9% at 1000%. Damage SHALL accumulate fractionally so it is not lost to rounding. If hitpoints reach 0 the forge is destroyed as with any other damage. Wear applies equally to pawn-driven and Robot Assistant production, and only on ticks where work is actually progressing.
- **Changing the level is a job**: the slider only sets a requested level. The forge keeps running at its current level until a pawn performs the overdrive job (Crafting work type, Manipulation required, 240 ticks, at the interaction cell), which applies the requested level. The job needs power and is not offered while the forge is reserved by another pawn, so the level cannot change during a pawn's work. Applying a level discards any unfinished production run (robot progress and slot progress reset to 0) so a run never straddles two levels. While a request is pending the slider shows both the requested and the current position.
- Overdrive does not change production duration or the neutronium drawn per work step.

#### Scenario: Overdriven production
- WHEN a Solar forge (multiplier 1) at 300% completes a production run for an item whose `YieldFor` is 1
- THEN 2 are spawned, the forge was drawing 150% of its base power during the work, and it took 0.2% of max hitpoints per second of work

#### Scenario: Forge not producing
- WHEN the forge is idle, analysing or formatting at any overdrive level
- THEN it takes no overdrive damage and draws base power

### Requirement: Formatting columns
The player SHALL be able to wipe a whole grid column. Formatting is queued per column, consumes one hour (`FormatTicksPerCell` = 2500 ticks) per filled cell, removes the topmost filled cell first (clearing its stuff, queue, mode, progress), and keeps progress across interruptions. Pawns perform it through a dedicated work giver.

#### Scenario: Column emptied
- WHEN the active column has no filled cells left
- THEN it is removed from the format queue and counters reset

### Requirement: Automated operation via Robot Assistant
If EvenMoreLinkables (`Hobbes.MoreLinkablesProduction.CompWorkHelper`) is present and a linked facility is powered, switched on, and in `WORK` state, the forge SHALL operate without a pawn at speed 1.5.

- A pawn reservation by the player faction takes precedence.
- Automation formats first, idles while analyses are pending, then produces using the same draw and yield rules (robot ticks = max(30, BaseWorkTicks / speed)).

#### Scenario: Soft dependency missing
- WHEN the work-helper type cannot be resolved
- THEN automation speed is 0 and the forge only works via pawns

### Requirement: Forge tab UI
The forge SHALL expose an inspector tab ("Forge") showing the grid, with per-cell: icon, map count, queued count (hidden for DoForever), progress fill, a mode selector, and a tooltip with yield.

- Left click queues 1 (Shift 5, Ctrl 10, Shift+Ctrl 50); right click removes the same amounts.
- An Analyze button opens an item picker (disabled when the grid is full); a Clear-column button toggles column selection (left click chooses, right click cancels).
- A footer lists pending analyses (with cancel) and the formatting row.
- To the right of the grid there SHALL be a vertical overdrive slider ("lever") with 10 snapping stops, 100% at the bottom and 1000% at the top, with the requested percentage shown and the word OVERDRIVE written vertically beside it. Its tooltip states the output multiplier, extra power use and wear rate for the selected stage. The slider is usable at any time, whether or not the forge is producing.

### Requirement: Persistence
The forge SHALL save slots, stuff, queued counts, modes, overdrive stage and requested stage, format queue/ticks/done, robot slot/ticks, and pending analysis references; collections are normalised to capacity after load.
