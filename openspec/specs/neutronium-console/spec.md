# Neutronium Console Specification

## Purpose
One place to read a neutronium net: what flows in, what flows out, how much is stored, and how close each tank is to going up. Neutronium is the riskiest resource in BOR, since an unpowered tank damages itself until it explodes. Today the only readouts are VEF's storage bars and pipe overlay, so a player only finds out about a dead tank when it blows.

Status: proposed, not implemented.

## Background
- The net is a VEF `PipeSystem.PipeNetDef`, `BOR_NeutroniumNet` (`NeutroniumNet.xml`).
- Producers are condensers (`CompProperties_DeepExtractor`, a portion every 250 ticks from the deep resource below).
- Consumers are buildings with `CompResource` or `CompResourceTrader` on the net (tier 1, 2 and 4 production, the tier 2 power plant), forges that draw `pipeAmount` per work step (`CompForge`), taps that convert to items, and drains.
- A tank stores up to 1,000,000. While unpowered it takes 100 damage every 4,000 ticks (`VCEPlusUltra.CompProperties_DamageWhenUnpowered`). Its wick starts at 33.3% hit points (`CompProperties_ExplosiveContent`), and the blast size depends on what it holds. Destroying it causes a flashstorm. With 1,500 hit points, a full-health tank lights about 16 in-game hours after losing power.
- Solar flares cut power to tanks unless they stand in a shielded room (`solar-flare-shield`).

## Requirements

### Requirement: Neutronium tab
Every building with a resource comp on `BOR_NeutroniumNet` (pipes, condensers, tanks, valves, taps, drains and every consumer) SHALL have a "Neutronium" inspector tab (`BorTab`) for its net. If the map has more than one neutronium net, a dropdown SHALL switch between them.

```
+--------------------------------------------------------+
| NEUTRONIUM  net 1 of 2 [v]                             |
| Stored 412,300 / 3,000,000   [######-----------]       |
| Net flow  +18,200/day          full in about 141 days  |
| [graph: stored amount over the last day]               |
+--------------------------------------------------------+
| IN                         | OUT                       |
|  Solar condenser x2 +24,000|  Forge II        -4,100   |
|   deposit: 31 days left    |  Neutronium plant -1,700  |
|                            |  Taps (items)     -0      |
+--------------------------------------------------------+
| TANKS                                                  |
|  Tank A  [#####---] 210k  powered     1500/1500        |
|  Tank B  [##------]  80k  UNPOWERED    900/1500 wick 6h|
|  Tank C  [########] 122k  flare: shielded              |
+--------------------------------------------------------+
```

### Requirement: Totals
The tab SHALL show stored and total capacity with a bar, the net flow per day (inflow minus outflow), and the time until the net is full or empty at that rate. It SHALL show a graph of the stored amount over the last in-game day.

### Requirement: Flow by source
The IN column SHALL list each producer def with a count, its rate per day, and the days left in the deep deposits under it. The OUT column SHALL list each consumer def with a count and its rate per day.

Rates SHALL be measured, not taken from defs, averaged over the last 2,500 ticks, because forges and taps draw only while working. The sampling SHALL be cheap: no per-frame work and no map scans.

### Requirement: Tanks
Each tank SHALL have a row with its fill, its power state, its hit points, and, when unpowered, the time until its wick lights. The time SHALL be computed from its hit points, the damage interval, the damage amount and the wick threshold, read from its comps and not hard-coded. During a flare, a tank in a working shielded room SHALL show as shielded. Clicking a row SHALL select the tank and jump the camera to it.

#### Scenario: Power cut
- WHEN a full-health tank loses power
- THEN its row turns red and counts down from about 16 hours

### Requirement: Blast preview
Hovering a tank row, or selecting a tank, SHALL draw the radius its explosion would have now, from the same values `CompExplosiveContent` uses.

### Requirement: Alerts
- **Neutronium tank unpowered** (`Alert_Critical` within 4 hours of the wick, otherwise `Alert`): lists the tanks with time left; clicking cycles through them.
- **Neutronium running out** (`Alert`): when a net with consumers drawing will be empty within one day.

### Requirement: No save data
The flow history is display data. It MAY be lost on load and SHALL refill as the game runs.

## Approaches
- **Inspector tab plus alerts (chosen):** shows up where the player already looks, on any part of the net, and needs no new main-bar button.
- **Main tab window (rejected):** yet another button on a bar already crowded by many mods, and one more step away from the net itself.
- **Overlay only (rejected):** it can show where things are but not rates or countdowns.

## Open questions
- Measuring per-building flow may need a Harmony patch on VEF's `PipeNet` draw and add methods, since VEF may not expose per-comp flow. The builder should check VEF first.
- Should the alerts also cover deep deposits running out under condensers?

## Proposed defaults (Claude's calls, to confirm)
- No new building or research; the tab is on every net building from the start.
- The 2,500-tick average and the 4-hour critical threshold are starting points.

## Non-goals
Controlling the net from the tab (opening valves or setting priorities), and other VEF pipe nets.
