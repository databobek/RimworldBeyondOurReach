# Kwazaari Rework Specification

## Purpose
Kwazaari (`BOR_KwazaarBazaar`, the tier II "Quasar" faction) stop being a regular world faction. They become a hidden faction with no settlements, like the core mechanoids, and the only way to deal with them is the comm console. Their Royalty honor system is replaced by a custom currency, **scrip**: the player sells ultra-and-beyond tech to the Kwazaari for scrip, then spends scrip on favors that have cooldowns.

## Requirements

### Requirement: Hidden faction
`BOR_KwazaarBazaar` SHALL be a hidden faction with no settlements (`hidden` true, `settlementGenerationWeight` 0). It SHALL NOT appear on the world map or in the faction list. Kwazaari SHALL NOT send raids, caravans, visitors, or traders. The faction SHALL still exist so that Kwazaari pawns can be generated.

### Requirement: Permanent neutrality
The player's relation with Kwazaari SHALL always be neutral. Kwazaari SHALL never become hostile and SHALL never raid. Goodwill SHALL NOT be used by this system.

### Requirement: Old save migration
WHEN a save made before the rework is loaded, THEN every Kwazaari settlement and other Kwazaari world object SHALL be removed, and the faction SHALL be hidden.

### Requirement: No honor system
Kwazaari SHALL NOT use royal titles or permits. The following SHALL be removed:

- the `royalTitleTags` patch for Kwazaari (`Mods/Royalty/Patches/permit_requirement.xml`)
- the corporate titles `BOR_Candidate` through `BOR_BranchManager` (`Mods/Royalty/Defs/Permits/CorpoTitles.xml`), and their thoughts
- the corporate permits (`Mods/Royalty/Defs/Permits/CorpoPermits.xml`)
- the matching DefInjected translations

Anything that only those defs use, such as `BOR_Kwazaari_Heavy_Marine`, SHALL be reviewed and either removed or kept.

### Requirement: Scrip ledger
A `GameComponent` SHALL keep:

- the scrip balance, a decimal number that MAY be negative (the `RoyalFavorValue` stat is fractional, e.g. 0.015 per gold)
- the tick at which each favor next becomes available
- the active borrowed-pawn contracts

All of it SHALL be saved with the game.

### Requirement: Comm console gizmo
The `CommsConsole` SHALL have a **"Connect with Kwazaari"** gizmo with the Kwazaari icon (`BOR/Things/Factions/KwazaariIcon`). The gizmo SHALL be added through an XML patch that adds a comp. It SHALL be disabled while the console is unpowered. Clicking it SHALL open a menu for choosing a colonist.

#### Scenario: Connecting
- WHEN the player picks a colonist from the gizmo menu
- THEN the colonist walks to the console's interaction cell and works for 180 ticks (about 3 seconds) with a progress bar
- AND then the Kwazaari window opens

### Requirement: Kwazaari window
The Kwazaari window SHALL show the scrip balance and two choices:

- **Sell technology**, with the tooltip "Kwazaari appreciate delivering ultra and beyond tech to them".
- **Spend scrip**, with the tooltip "You can spend scrip you earned for selling technology on favors".

### Requirement: Sellable items
An item SHALL be sellable when all of these are true:

- it is spawned on the console's map, inside the home area
- its `techLevel` is Ultra or higher
- it is an item, not a pawn or a building
- it is not on the trade blacklist

Forbidden items and items in storage SHALL be included. Items worn or carried by pawns SHALL NOT be included. No trade beacon SHALL be required.

### Requirement: Scrip value
Each unit of an item SHALL be worth:

- its `RoyalFavorValue` stat, when that is greater than 0
- otherwise, `1 + floor(marketValue / 1000)`

A sale SHALL be worth the sum over all units sold. There SHALL be no cap on scrip per sale or per day. The blacklist is the tool for stopping exploits.

#### Scenario: Default value
- WHEN the player sells one item with no `RoyalFavorValue` and a market value of 2500
- THEN the player gains 3 scrip

### Requirement: Sell window
The sell window SHALL list the sellable items, grouped by def, with quality and stack size. Each row SHALL have a quantity selector. The window SHALL show the running scrip total and a confirm button.

#### Scenario: Confirming a sale
- WHEN the player confirms a sale
- THEN each sold item plays a skip effect (vanilla `Skip_Entry`) where it lies and is removed from the map
- AND the sale's scrip value is added to the balance

### Requirement: Trade blacklist
Items that cannot be sold to Kwazaari SHALL be listed in XML, in a new def type such as `BeyondOurReach.KwazaariTradeBlacklistDef`, which holds `thingDefs` and `thingCategories`. The lists from every such def SHALL be merged, so other files and patches can add to the blacklist. By default it SHALL contain only `Plasteel`, `MedicineUltratech` and `BOR_Neutronium`.

### Requirement: Spend window
The spend window SHALL list every favor with its cost, its cooldown, and the cooldown time remaining. A favor that cannot be bought SHALL be shown disabled with the reason: not enough scrip, cooling down, or a required DLC missing.

Buying a favor SHALL subtract its cost from the balance and start its cooldown.

### Requirement: Negative balance
While the balance is below 0, no favor SHALL be bought. Selling SHALL still be possible and SHALL pay the debt off. A negative balance SHALL have no other effect.

### Requirement: Favor definitions
Each favor SHALL be defined in XML as a `BeyondOurReach.KwazaariFavorDef` with a worker class. Its cost, cooldown, and contents SHALL be adjustable without changing code. A favor that needs an inactive DLC SHALL be disabled (`requiredAnyOf`: any one listed package is enough; the psylink favor accepts Royalty or Biotech).

| Favor | Scrip | Cooldown | Delivers |
|---|---|---|---|
| Orbital shot | 10 | 5 days | A vanilla `Bombardment` at a cell the player chooses with a targeter |
| Food delivery | 2 | 3 days | 500 packaged survival meals (`MealSurvivalPack`) |
| Skill trainers | 5 | 1 day | 3 skill neurotrainers for one skill, chosen from a dropdown (Royalty) |
| Medicine | 2 | 5 days | 50 industrial medicine (`MedicineIndustrial`) |
| Crafting materials | 1 | 1 day | 500 steel, 100 plasteel, 50 components, 10 advanced components |
| Building materials | 1 | 2 days | 1000 stone blocks of one type, chosen from a dropdown, plus 1000 wood |
| Reinforcements | 24 | 4 days | 4 borrowed Kwazaari pawns for 12 hours |
| Psylink | 4 | 15 days | A psylink neuroformer (Royalty) |
| Mechlink | 4 | 15 days | A mechlink (Biotech) |

### Requirement: Kwazaari drop pods
Every item favor SHALL arrive in blue Kwazaari drop pods near the map's trade drop spot. The pods SHALL reuse the vanilla drop pod graphics, tinted blue in XML. No new textures SHALL be needed.

### Requirement: Reinforcements
When the reinforcements favor is bought:

- a blue Kwazaari shuttle SHALL land, reusing the `BOR_SignalShuttle` graphic tinted blue
- 4 `BOR_Kwazaari_Guardsman` pawns SHALL step out, and the shuttle SHALL leave
- for 12 hours, the 4 pawns SHALL join the player's faction, marked as borrowed with the `BOR_KwazaariLoan` hediff, and the player SHALL control them
- only one loan SHALL be active at a time

#### Scenario: Return
- WHEN 12 hours have passed
- THEN a Kwazaari shuttle lands, the borrowed pawns are undrafted and ordered to board, and the shuttle leaves after a short wait
- AND pawns who boarded are removed from the game, returned to the Kwazaari
- IF no landing spot is free when the loan ends, THEN the pickup SHALL be retried until one is

#### Scenario: Pawn lost
- WHEN the return shuttle leaves and a borrowed pawn did not board (because it is dead, downed, captured, off the map, or missing)
- THEN 6 scrip is subtracted for that pawn, even if this makes the balance negative
- AND a surviving pawn that did not board stays with the colony, its gear unlocked and its loan marker removed

### Requirement: Borrowed pawn gear
The apparel and weapon a borrowed pawn arrives with SHALL be locked. The player SHALL NOT be able to strip them, drop them, or swap them out. Colony items a borrowed pawn picks up SHALL leave with that pawn.

### Requirement: Translations
Every new string visible to the player SHALL be a `BOR_Kwazaari_*` key in `Languages/English/Keyed`, with a Polish entry as well.

## Open questions
- None at this time.
