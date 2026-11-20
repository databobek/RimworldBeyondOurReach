# Schism Specification

## Purpose
After the colony chooses a path (see `orientis-faction`), the side it turned away from becomes its nemesis. A Path of Dark colony is hunted by an Orientis crusade. A Path of Light colony is hunted by Umbral remnants, continuing what "The Dark Archotech's answer" starts. Pressure builds on a meter the player can see, read and push back against, and it ends in a champion fight. This gives the late game a lasting threat that follows from the colony's own choice.

Status: proposed, not implemented. To be reworked on top of the standalone Expeditions mod (`Mods/Expeditions/openspec/expeditions-mod-design.md`) once that mod exists: nemesis camps and the champion stronghold become expedition PoIs.

## Requirements

### Requirement: Activation
The Schism SHALL start when either path is chosen and SHALL never run while the path is `ColonyPath.None`. A mod setting SHALL turn it off. With the setting off, nothing in this spec happens and the meter is hidden.

### Requirement: Nemesis
| Colony path | Nemesis | Faction | Pawns |
| --- | --- | --- | --- |
| Dark | Orientis crusade | New hidden, permanently hostile `BOR_OrientisCrusade` | Orientis xenotype crusaders with Orientis weapons |
| Light | Umbral remnants | Existing hidden `BOR_Umbrals` | `BOR_Umbral`, using the existing swarm AI and abilities |

`BOR_OrientisCrusade` SHALL be hidden like `BOR_Umbrals`: no settlements, never picked for random raids, used only by Schism events.

### Requirement: Wrath meter
The colony SHALL have one wrath meter from 0 to 100, kept in a game component and saved. It is labelled "Crusade" on the Dark path and "Hunger" on the Light path. It SHALL rise by 2 per day on its own and by the colony's actions:

| Path | Action | Wrath |
| --- | --- | --- |
| Dark | A pawn completes an Umbral transformation phase | +5 |
| Dark | A request at the dark archotech altar | +3 |
| Dark | A kill that refills bloodlust | +1 |
| Dark | An Umbral shield up for a full day | +3 |
| Light | An Orientis building manifested | +3 |
| Light | A blessing given at the blessing bench | +2 |
| Light | Every 1,000 meditation power generated | +1 |

### Requirement: Stages
Crossing a threshold upward SHALL send a letter and start that stage:

| Wrath | Stage | Effect |
| --- | --- | --- |
| 0-24 | Quiet | Nothing |
| 25 | Omens | A map condition for 1-2 days (golden aurora or black rain) with a small mood effect |
| 50 | Heralds | One lone hunter goes after a single colonist, as "The Dark Archotech's answer" does (`UmbralHerald.cs` for Light; an Orientis equivalent for Dark) |
| 75 | Raids | A full raid at storyteller points; another every 4-7 days while wrath stays at 75 or more |
| 100 | Champion | A siege led by a unique champion pawn |

Raid sizes SHALL follow storyteller raid points and the threat scale. On a storyteller with no raids, stages from Raids up SHALL only send their letters.

### Requirement: Pushing back
Each defeated Schism raid SHALL lower wrath by 15, and each killed herald by 10. Each path SHALL also have one active countermeasure, used from the Schism window:

| Path | Countermeasure | Cost | Effect |
| --- | --- | --- | --- |
| Dark | Veil, done at the dark altar | Tenebrite | -20 wrath; 10 day cooldown |
| Light | Ward, done at the offerings bench | Meditation power | -20 wrath; 10 day cooldown |

### Requirement: Champion
At 100 wrath, a champion siege SHALL arrive. If the champion dies, wrath SHALL drop to 0, the cycle count SHALL go up by one, and the champion SHALL drop a reward that fits the colony's path. If the champion leaves the map alive, wrath SHALL stay at 100 and a new champion siege SHALL come 5 days later. Each cycle SHALL raise the passive rise by 1 per day and Schism raid points by 15%.

#### Scenario: Champion defeated
- WHEN a Path of Light colony kills the Umbral champion
- THEN wrath resets to 0, the cycle count goes to 1, and the champion leaves a purified reward

### Requirement: Light and dark stay apart
Champion rewards SHALL obey `orientis-faction`'s rule: a Light colony SHALL never receive dark items, and dark gear dropped by Umbral remnants SHALL burn on the map as it does after the Path of Light. A Dark colony SHALL never receive usable Orientis gear. Orientis items dropped by crusaders SHALL turn to silver and gold when a Dark colony picks them up.

### Requirement: Schism window
A `BorWindow` SHALL open from a gizmo on the dark archotech altar (Dark path), the blessing bench (Light path), and from every Schism letter.

```
+-----------------------------------------------------------+
| THE CRUSADE                                  cycle 1     X|
|  [emblem]  Quiet   Omens   Heralds   Raids    Champion    |
|            |-------|=======|====>----|--------|           |
|                          wrath 58                         |
|  Next: Raids at 75  (about 6 days at the current pace)    |
+-----------------------------------------------------------+
| Recent                          | Countermeasure          |
|  +5  Ann finished phase B       |  VEIL      40 tenebrite |
|  +3  Altar request (Bob)        |  -20 wrath              |
|  -15 Crusade raid repelled      |  [ Perform ]  ready     |
|  +2  daily                      |                         |
+-----------------------------------------------------------+
| Champions defeated: Seraph Aldric (day 214)               |
+-----------------------------------------------------------+
```

It SHALL show the meter with stage markers, the next threshold and an estimate of when it comes, the last 15 changes with their reasons, the countermeasure with its cost and cooldown, and the defeated champions.

### Requirement: Persistence
Wrath, the cycle count, stage, recent changes, cooldowns, a pending champion siege, and the defeated champions SHALL be saved.

## Approaches
- **Own meter and events (chosen):** a game component tracks wrath and fires BOR incidents at thresholds. The player can see it coming and has something to do about it.
- **Storyteller incidents (rejected):** registering the raids as normal incidents gives no build-up and nothing to push back with.
- **Quest chain (rejected):** heavier to build, and quests expire or fail in ways that do not fit a permanent nemesis.

## Open questions
- Orientis crusader gear: is there Orientis armor to use, or do crusaders need new apparel?
- Champion rewards: what should each path get? Proposal: Dark colonies get a "corrupted reliquary" that yields tenebrite and a dark ability roll. Light colonies get a "purified crystal" that yields gold and a burst of meditation power.
- Should wrath also rise from things the player can't control (a raid by another faction ending, the Umbral shield being up), or only from their choices?

## Proposed defaults (Claude's calls, to confirm)
- One meter per colony, not per map.
- All numbers (rise per action, stage thresholds, -15/-10/-20, cooldowns, +15% per cycle) are starting points for tuning.
- Omen conditions are cosmetic plus a small mood effect, with no gameplay penalty.

## Non-goals
A peace or diplomacy option with the nemesis, the nemesis on the world map, and switching paths.
