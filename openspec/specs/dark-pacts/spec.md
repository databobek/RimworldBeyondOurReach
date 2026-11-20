# Dark Pacts Specification

## Purpose
Tenebral research is not studied; it is bargained for. Every tenebral research costs 1E+15 at the bench, like the umbral tier V research, and can only be finished by a dark pact made at the dark mirror (`BOR_Research_Unlocker_III`). A pact is a quest with a terrible price and seven days to pay it. Paying finishes the research. Failing sends a red pulse from the mirror that punishes everyone it reaches. Implemented in `Source/Tenebral/DarkPact.cs`, `DarkPactQuest.cs` and `DarkPulse.cs`.

## Requirements

### Requirement: Pact research
A research SHALL be a pact research when it inherits `BOR_Research_Base_III_Pact`, which sets a base cost of 1E+15 and the `DefModExtension_DarkPact` marker. Every tenebral research is one, compat researches included, except `BOR_Research_Gate_III`, which stays at the bench. `BOR_Research_Analyzer_III`, which unlocks the dark mirror, is tier II and stays at the bench too.

### Requirement: Pacts tab
The dark mirror SHALL have a **Pacts** inspector tab (`ITab_DarkPacts`) and no pact gizmos. The tab SHALL show:
- At the top, the pact under way: its research, the price, a progress bar for counted prices, the time left, an **Abandon pact** button and, in dev mode, **Complete** and **Expire** buttons. With no pact under way, a line saying so.
- Below, a scrolling list of every pact research, ready ones first, then ones held back for the moment, then ones locked by research, then granted ones. Each row SHALL show the building the research unlocks (else the first unlocked thing with an icon, else the mirror itself) and the research name.
- On the right of each row: **Make pact** for a ready research; the same button greyed out, with the reason in its tooltip, while a pact is under way, a colonist is communing, the mirror is unpowered, or a needed mechanitor is missing; "Under way" for the research of the current pact; "Needs: ..." listing the missing research (hidden prerequisites included); or "Granted".
- A row tooltip with the research's description, everything it unlocks and the time limit.

### Requirement: Communing
Clicking **Make pact** SHALL open a menu of the map's free colonists (downed and unreachable ones listed disabled), then a confirmation. The chosen colonist SHALL walk to the mirror and commune for 600 ticks, with a progress bar and red flashes. When it ends, the pact quest SHALL start, auto-accepted, with a letter showing its description.

### Requirement: One pact at a time
Only one pact SHALL run at a time. While it runs, the mirror's inspect string SHALL also show its progress and time left.

### Requirement: The price
The price SHALL be rolled at random from the objectives that can run at that moment. Counts are fields of the `BOR_DarkPact` quest script root.

| Objective | Available when | Done when | Fails when |
| --- | --- | --- | --- |
| Destroy a friendly settlement | A settlement of a non-hostile, visible, non-permanent-enemy faction lies within 25 tiles of a home map (allies preferred) | That settlement is destroyed | |
| Kill 40–60 people (rolled per pact) | Always | Humanlikes die with a player-faction instigator | |
| Execute a colonist | 2 or more colonists; an adult other than the pact maker is named | The named colonist dies by `ExecutionCut` damage from the colony (vanilla execution, Righteous Slaughter) | They die any other way, or are lost |
| Kill 100–150 animals (rolled per pact) | Always | Animals die with a player-faction instigator | |
| Burn 10 people alive | Always | Humanlikes die burning, or by `Flame` or `Burn` damage, whoever lit them | |
| Betray 5 guests | A visible non-hostile faction exists | A humanlike of another faction dies by the colony, having been non-hostile (and not a prisoner or slave) when the colony first hurt them | |
| Kinslayer | An adult colonist has an adult blood relative or love partner on a home map who is not an enemy | The named relative dies with the named colonist as instigator | The relative dies any other way, or the colonist dies or is lost |
| Harvest 10 living organs | Always | A natural, clean organ that drops an item is cut out of a living humanlike by a player-faction surgeon | |
| Ossuary | Always | 15 humanlike corpses lie within 10 cells of a player dark mirror at once (checked every long tick) | |

Named targets SHALL be adults.

### Requirement: Time limit
A pact SHALL last 7 days from the moment it starts. The quest window SHALL show the time left; counted objectives SHALL also show their progress there.

### Requirement: Success
When the price is paid, the research SHALL be finished (credited to the pact maker if alive), a letter SHALL say so, and the quest SHALL end as a success.

### Requirement: Dark Punishment
Every failure (time running out, a target dying wrongly, abandoning) SHALL be punished:
- The mirror the pact was made at (else any player dark mirror on a home map) SHALL send out a red ring, drawn like the Path of Light's shockwave, that crosses the whole map.
- Every living pawn the ring reaches, of any faction, SHALL get `BOR_DarkPunishment` (+20% pain for 1 day) once per pulse. A pawn who already has it SHALL have its timer reset.
- A negative letter SHALL name the research and the reason. With no mirror standing, only the letter comes.
- The ring SHALL be saved with the map and resume after loading.

After a failure, the research SHALL be offered again in the tab.

#### Scenario: Pact expires
- WHEN seven days pass and the price is unpaid
- THEN the quest fails, a red ring sweeps out from the mirror, and every pawn it reaches has Dark Punishment for a day

## Implementation decisions
- The quest is one `QuestScriptDef` (`BOR_DarkPact`, never fired at random) with a C# root, `QuestNode_Root_DarkPact`, and one `QuestPartActivable`, `QuestPart_DarkPact`, which counts, keeps the clock and settles the pact. Name and description are set through the `resolvedQuestName` and `resolvedQuestDescription` slate values, so they come straight from Keyed strings.
- Executions are recognized by their `ExecutionCut` damage, so no patch is needed. Betrayal uses a prefix on `Thing.TakeDamage`. An organ harvest counts when `MedicalRecipesUtility.SpawnNaturalPartIfClean` returns the organ during `Recipe_RemoveBodyPart.ApplyOnPawn` (it only does on a successful surgery). A prefix on `Pawn.Kill` notes whether the pawn was burning, since vanilla despawns it before notifying quests.
- The settlement gets a quest tag, so its `Destroyed` signal completes the pact; a long-tick check catches any other removal.
- The mirror passes the research to the communing job as an index into the pact research list.
- The golden and red rings share `ShockwaveRing` for drawing.

## Non-goals
Pacts for umbral research (the umbral spire keeps its communion illness), and quests offered at random.
