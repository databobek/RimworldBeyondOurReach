# Solar Flare Shield Specification

## Purpose
A Solar-tier building that lets players keep one room running through a solar flare, at a steep price in stored power and heat. Everything outside a shielded room still goes dark, as in vanilla.

## Requirements

### Requirement: Room protection
While a map has a flare (`ElectricityDisabled`), a working shield SHALL keep every power consumer and generator whose `Position` is in its room running as normal. Everything else SHALL shut down at vanilla's pace (5% of consumers every 20 ticks).

A shield is working when its power is on and its room does not use outdoor temperature. Protection follows the room, so a building on a separate net in that room is protected too.

#### Scenario: Comms console in a shielded room
- WHEN a comms console stands in a shielded room during a flare
- THEN it SHALL stay unusable, because the signal still has to cross the flare outside

#### Scenario: Building in the wall
- WHEN a building's cell has no room, such as a wall-mounted cooler
- THEN it SHALL NOT be protected, so coolers cannot cancel the heat

### Requirement: Power draw
A shield SHALL draw 60,000 W during a flare and nothing otherwise. Ten full Solar batteries (6,000 Wd each) run a lone shield for about one day; their 5 W self-discharge makes that slightly under. Two Solarite reactors (41,667 W each) in the room cover the draw on their own.

### Requirement: Power sources
During a flare, a shielded net SHALL draw from any battery on the net, wherever it stands, since the flare leaves battery charge intact. Generators SHALL count only if they stand in a shielded room or in the room of a shield waiting to power back on. Surplus from those generators MAY charge any battery on the net.

#### Scenario: Running out of power
- WHEN stored and generated power can no longer meet demand
- THEN protected consumers, the shield included, SHALL drop off at vanilla's pace
- AND once a shield is off, its room SHALL lose protection until it can power back on

#### Scenario: Shield off when the flare starts
- WHEN a shield is off but switched on and not broken down
- THEN it SHALL be able to power itself on during the flare from stored power or from generators in its own room
- AND the room's consumers SHALL stay off until it does

### Requirement: Heat
A working shield SHALL heat its room during a flare, up to `targetTemperature` (65°C) and at most `heatPerSecond` (200) per second. A push SHALL never overshoot the target, so small rooms do not spike past it. About 200 heat holds a 30x30 room at 65°C in temperate weather; larger rooms or cold climates end up cooler.

### Requirement: Placement and rain
The shield SHALL only be placeable in a room that does not count as outdoors (`UsesOutdoorTemperature`, which covers map-edge rooms and rooms at least 25% unroofed), using `PlaceWorker_SolarFlareShield`. It SHALL use `shortCircuitInRain`, which applies if its cell loses its roof later.

#### Scenario: Room opened after building
- WHEN a shield's room comes to count as outdoors
- THEN the shield SHALL stop protecting and its inspect pane SHALL say why

## Approaches
- **Reuse the vanilla tick (chosen):** a `PowerNetTick` patch narrows `powerComps` to protected buildings for the call, skips the flare check, and lets vanilla run. About 30 lines, keeps vanilla's logic, and other mods' patches keep working. The cost is swapping a live list during the call.
- **Own flare tick (rejected):** a full replacement during flares. Easier to read, but about 60 lines copied from vanilla, and other mods' patches stop running during flares.

## Implementation
`Source/Buildings/SolarFlareShield.cs`:
- `CompProperties_SolarFlareShield` / `CompSolarFlareShield`: sets the draw each tick, because vanilla resets it when power comes back. Pushes heat every 60 ticks. Shields register per map on spawn and despawn.
- Prefix and finalizer on `PowerNet.PowerNetTick`: when the map has a flare and this net has protected parts, swap `powerComps`, set a flag, shut down unprotected consumers, and restore the list in the finalizer. `batteryComps` stays whole. Nets with no protected parts run vanilla untouched.
- Prefix on `GameConditionManager.ElectricityDisabled`: returns false only while that flag is set.

## Builder's calls (not specified by the requester)
- Nothing is reused from existing mods or old compat folders; values are scaled from BOR's Solar-tier buildings. The only exception is the requested `solarshield2` texture.
- `BOR_SolarFlareShield`, label "solar flare shield", in `tier1/Buildings_Power.xml` under the `BOR` category. Scaled from the Solarite reactor: 3x3, 4x4 draw size, 375 HP, 5,000 work, construction skill 9, 165 sunsteel (reactor x1.5), 5 advanced components, flammability 0.833, beauty -8, flickable, breakdownable.
- New research `BOR_Research_SolarFlareShield`, label "solar flare shield": 20,000 cost, requires `BOR_Research_Battery_I`, tree slot (4.00, 1.30). New English and Polish text. Old Polish keys for the removed compat research stay untouched.
- `solarshield2.png` is copied from `TexturesAbandoned` to `Textures/BOR/Things/Building/tier1/`. The `_top` part is left out.
- Several shielded rooms on one net share its batteries, even across unprotected wire.
- Not patched: `CompAutoPowered` (lightball, loudspeaker) stays off in a shielded room. `CompFacilityInactiveWhenElectricityDisabled` has no users in the game data or BOR.

## Non-goals
Colony-wide protection, an umbral tier, and spinning-rotor visuals.

## Verification
`dotnet build` compiles, then an in-game dev-mode test: force a flare and check protected and unprotected buildings, battery drain, generators, heat, placement, and a lost roof in rain.
