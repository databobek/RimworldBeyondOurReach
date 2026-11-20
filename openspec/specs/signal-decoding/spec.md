# Signal Decoding Specification

## Purpose
Adds a short tuning puzzle to the strange signal chains (see `artifact-rework`). After a colonist investigates a signal, the player tunes it in on an oscilloscope. A cleaner lock gives a better outcome: a cheaper deal, an early raid warning, a smaller raid. An auto-decode button rolls the result from the investigator's skill, so the puzzle is never required.

Status: proposed, not implemented.

## Requirements

### Requirement: Decode step
Every signal channel (`CompStrangeSignals`, `CompSignalRaid`, `CompSignalTenebral`) SHALL get a **Decode** state between investigation and contact. When the investigation job finishes, the channel SHALL enter Decode, and its gizmo SHALL read "Decode signal" and open the decoder window. Locking or auto-decoding SHALL store the quality on the channel and move it on to the state it enters today. The rest of each chain SHALL stay as `artifact-rework` defines it, except for the effects below.

### Requirement: The puzzle
The received signal SHALL be a target wave (a sum of sine components) plus noise. The player SHALL have three controls, **Frequency**, **Phase** and **Gain**, that shape their own trace drawn over the received signal. A lock meter SHALL show how closely the trace matches the target, and the noise SHALL thin out as the match gets closer.

- **Lock** SHALL commit the current match as the quality and use up one attempt. The best lock so far counts.
- When the attempts run out, the best lock SHALL be final.
- The puzzle SHALL be seeded and saved with the channel. Closing and reopening the window, or reloading, SHALL show the same signal and the same attempts left.

| Channel | Components | Twist |
| --- | --- | --- |
| Strange signals (data drive) | 1 | None |
| Strange signal (Badzaari) | 2 | None |
| Peculiar broadcasts (Tenebral) | 3 | The target slowly drifts |

### Requirement: Investigator skill
The pawn who finished the investigation is the decoder. Their Intellectual skill SHALL set the number of attempts (1 at skill 0, up to 4 at skill 15 or more) and the noise level. At skill 15 or more, a faint ghost of the target SHALL show.

### Requirement: Auto-decode
An **Auto-decode** button SHALL set the quality from the decoder's Intellectual skill with some randomness, skipping the puzzle. Its expected quality SHALL sit a little below what a careful player gets.

### Requirement: Quality grades
| Grade | Match |
| --- | --- |
| Garbled | below 50% |
| Clear | 50% to 85% |
| Pristine | above 85% |

### Requirement: Effects
| Channel | Garbled | Clear | Pristine |
| --- | --- | --- | --- |
| Strange signals | Data drive costs 1,250 sunsteel | As today: 1,000 sunsteel, pilot waits 1 day | 750 sunsteel, pilot waits 2 days |
| Strange signal (Badzaari) | As today | A warning letter 2 hours before the shuttles land, with landing spots marked | Warning, and 6 raiders instead of 8 |
| Peculiar broadcasts | As today | A warning letter 2 hours before landing | Warning, and the reward shuttle drops an extra dilute tenebrite shard |

The contact dialogue SHALL show the grade, so the player sees what their tuning bought.

#### Scenario: Pristine Badzaari decode
- WHEN the player locks a Badzaari signal at 90% and later gives the coordinates
- THEN a warning letter marks the landing spots 2 hours before the shuttles arrive, and 6 raiders come out

### Requirement: Decoder window
The decoder SHALL be a `BorWindow`. The game SHALL NOT need to be paused or unpaused for it.

```
+-----------------------------------------------------------+
| DECODING: Peculiar broadcasts          Ann (Int 12)      X|
| +-------------------------------------------------------+ |
| |  ~/\~~/\/\~~~/\~  noisy received signal               | |
| |  _/ \_/ \_/ \_    your trace                          | |
| +-------------------------------------------------------+ |
|  Frequency [----|------]  Phase [------|----]  Gain [-|---]|
|  Lock  [###########-------]  64%   Clear                  |
|  Attempts left: 2   Best: 58%                             |
|            [ Lock ]   [ Auto-decode ]   [ Close ]         |
+-----------------------------------------------------------+
```

The signal and trace SHALL be drawn with lines in the skin's colors (`BorSkin.Current`), on a dark scope panel with a grid. A tooltip on the grade SHALL list the effects for that channel.

### Requirement: Persistence
The Decode state, the seed, attempts used, and the best match SHALL be saved per channel. The final grade SHALL be saved until the chain it affects is resolved.

## Approaches
- **Waveform tuning (chosen):** it reads as signal work, looks good as UI, and plays in seconds.
- **Symbol cipher (rejected):** a Mastermind-style code is more of a logic puzzle and takes longer per attempt, which wears thin on a repeatable event.
- **Skill check only (kept as Auto-decode):** good as the fallback, but on its own it is the plain letter chain this feature is meant to liven up.

## Open questions
- Should a Pristine decode of peculiar broadcasts also cost the decoder a little tenebrite exposure, as a temptation?
- If `expedition-board` is built, should a Pristine decode sometimes reveal an extra expedition?

## Proposed defaults (Claude's calls, to confirm)
- The decoder is the investigator, not a pawn picked again.
- Grade thresholds, attempt counts, prices, warning time and raider counts are starting points.

## Non-goals
Decoding outside the existing signal chains, and sound-based puzzles.
