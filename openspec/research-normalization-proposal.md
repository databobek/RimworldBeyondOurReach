# Research normalization (proposal, nothing applied yet)

Rules: tier N buildings and items are unlocked by tier N research. The one exception is the research bench of the next tier, which is unlocked by the final research of the tier before it. Researches may be moved, repriced, repositioned, renamed and added, never removed. Descriptions stay untouched and postfixes must match the tier.

## What is wrong now

Research postfix is one tier behind the content it unlocks, from tier III up:

| Research postfix | What it actually unlocks |
|---|---|
| `_III` (Base_III, done at Unlocker_III) | tier III (altar, pod, tenebral weapons/armor/implants) **and** tier IV (Bench/Forge/ToolCabinet/MedicineBench/Vitals/Joy/Battery/Reactor/Hydroponics _IV, drugs, dark medicine, umbral vaccine) |
| `_IV` (Base_IV, done at Unlocker_V) | tier V only (Reactor_V, Forge_V, tier 5 bionics, neutronium net) |

Other mismatches:
- `Gate_IV` is the final tier III research (Base_III, hidden prerequisites are all the _III researches) and is what unlocks the Unlocker_V bench. That already follows the exception rule, but both names carry the wrong postfix.
- `Vaccines_Combined` (Base_III) gates 15 tier II combined vaccines.
- Circuit board (tier I) needs `Analyzer_II`.

## Proposed researches

"Now" is the current defName. Costs for new or moved tier IV research are about 1.4x the tier III equivalent. Coordinates use x columns after the existing tier III block (14 to 19), so nothing overlaps.

### Renames, old IV becomes V (do these first, so the names are free)

| Now | New | Base | Cost | x, y |
|---|---|---|---|---|
| `Power_IV` | `Power_V` | Base_V (new) | 1,200,000 | 29, 2.0 |
| `Bionics_IV` | `Bionics_V` | Base_V | 1,200,000 | 29, 6.0 |
| `NeutProc_IV` | `NeutProc_V` | Base_V | 1,200,000 | 29, 4.0 |
| `NSynth_IV` | `NSynth_V` | Base_V | 1,800,000 | 30, 0.1 |
| `Solar_Flare_Shield_IV` | see open question 3 | | 150,000 | |
| `BOR_ArchotechBionics` | keeps its name, moves to Base_V (no postfix to change) | Base_V | 1,000,000 | 29, 1.0 |

Prerequisites of all of these become the new `Gate_IV`.

### Final tier III research

| Now | New | Base | Cost | x, y | Note |
|---|---|---|---|---|---|
| `Gate_IV` | `Gate_III` | Base_III | 1,000,000 | 20, 3.0 | Hidden prerequisites: every tier III research that still exists (`Production_III`, `Armor_III`, `Weapons_III`, `Medicine_III`, `Bionics_III`, `Mechtech_III`, `Joy_III`, `Hydroponics_III`, `Vaccines_Combined`, `Core_Drill_III`, `ComponentHarvest`, `PacifierTurret_III`), replacing the ones that move to IV below. Unlocks the Unlocker_IV bench. |

### Tier IV research (Base_IV, done at Unlocker_IV, formerly Unlocker_V)

| New | Source | Cost | Prerequisite | x, y | Unlocks |
|---|---|---|---|---|---|
| `Production_IV` | new | 600,000 | `Gate_III` | 22, 2.9 | Bench_IV, Forge_IV, ToolCabinet_IV |
| `Medicine_IV` | new | 700,000 | `Production_IV` | 23, 2.2 | MedicineBench_IV, Vitals_IV, dark medicine |
| `Drugs_IV` | `Drugs_III` renamed and moved, 125k to 175k | 175,000 | `Medicine_IV` | 24, 3.2 | adreamoline, crewmates, gallop, pax |
| `Vaccines_IV` | new | 250,000 | `Medicine_IV` | 24, 4.2 | umbral vaccine |
| `Power_IV` | `Power_III` renamed and moved, 500k to 700k | 700,000 | `Production_IV` | 22, 0.9 | Reactor_IV |
| `Battery_IV` | `Battery_III` renamed and moved, 125k to 175k | 175,000 | `Power_IV` | 23, 0.9 | Battery_IV |
| `Joy_IV` | new | 700,000 | `Production_IV` | 22, 3.9 | Megascreen/Telescope/PC _IV |
| `Hydroponics_IV` | new | 700,000 | `Production_IV` | 22, 5.8 | HydroponicsBasin_IV |
| `Gate_IV` | new | 2,000,000 | all of the above (hidden) | 26, 3.0 | Final tier IV research, unlocks the Unlocker_V bench |

Not new, stay as they are: `Joy_III` (tenebral desk, grand projectors), `Hydroponics_III` (pollution pump III), `Storage_III` (BauxBoxIII), `Production_III` (altar, bodysuit, tenebrite), `Medicine_III` (pod, femtoclusters, clusters recipes).

### Tier V research (Base_V, new bench Unlocker_V)

The four renamed researches in the first table, all gated by `Gate_IV`.

## Content that has to switch research

| Content | Tier | From | To |
|---|---|---|---|
| BOR_Bench_IV, BOR_Forge_IV, BOR_ToolCabinet_IV | IV | `Production_III` | `Production_IV` |
| BOR_MedicineBench_IV, BOR_Vitals_IV | IV | `Medicine_III` | `Medicine_IV` |
| BOR_MedicineIV (dark medicine) | IV | `Medicine_III` | `Medicine_IV` |
| BOR_Adreamoline, BOR_Crewmates, BOR_Gallop, BOR_Pax | IV | `Drugs_III` | `Drugs_IV` |
| BOR_UmbralVaccine | IV | `Vaccines_Combined` | `Vaccines_IV` |
| BOR_Reactor_IV | IV | `Power_III` | `Power_IV` |
| BOR_Battery_IV | IV | `Battery_III` | `Battery_IV` |
| BOR_MegascreenTelevision_IV, BOR_Telescope_IV, BOR_PC_IV | IV | `Joy_III` | `Joy_IV` |
| BOR_HydroponicsBasin_IV | IV | `Hydroponics_III` | `Hydroponics_IV` |
| BOR_Reactor_V | V | `Power_IV` | `Power_V` |
| BOR_Forge_V | V | `NeutProc_IV` | `NeutProc_V` |
| Tier 5 bionics (`Common/Defs/BodyPartDefs/Tier5`) | V | `Bionics_IV` | `Bionics_V` |
| BOR_NeutroniumNet | V | `NSynth_IV` | `NSynth_V` |
| Research bench BOR_Research_Unlocker_V | IV | requires `Gate_IV` | renamed `BOR_Research_Unlocker_IV`, requires `Gate_III` |
| New bench BOR_Research_Unlocker_V | V | none | requires the new `Gate_IV` |

The 15 tier II combined vaccines (BaseMech, MSMDMech, nano and so on) currently use `Vaccines_Combined`. Proposal: new `Vaccines_Combined_II` (Base_II, prerequisite `Vaccines_II`, about 100,000, x 9, y 4.2) for them. The tenebral combined vaccines keep `Vaccines_Combined`.

Everything that references the renamed defNames also changes: `Mods/*` patches (SolarFlareShield, Holograms, SimpleLearning, ArchotechExpanded, Biotech, Vaccines, QCR, MedPod, Replimat), the `DefInjected/ResearchProjectDef` translation files (keys follow the defName, text stays), `Buildings_Research.xml`, and the `requiredResearchBuilding` lines in `ResearchProjectDefs.xml`.

## Things to know

- Tenebral implants and vaccines (tier III) are only made at MedicineBench_IV, so they need `Medicine_IV` as well as their own research. Only altar items (weapons, apparel) are craftable with tier III research alone.
- The tier IV benches stay reachable only through `Gate_III`, which is the most expensive tier III research. This is intended by the exception rule.

## Decisions and what was applied

1. No separate tier V bench. Tier V research (`Base_V`) is done at `Unlocker_IV`, gated by the new `Gate_IV` research.
2. The tier IV bench is renamed `BOR_Research_Unlocker_IV` and requires `Gate_III`.
3. The magnetic shield research is tier III: `Solar_Flare_Shield_III` (Base_III), prerequisites `Weapons_III` and `Power_III`.
4. The tier I circuit board stays on `Analyzer_II`. It is a component of the Unlocker_II bench, and `Analyzer_II` is the research unlocked by the data drive, so it is a tier II item crafted at a tier I bench.
5. Renaming defNames breaks saves that have researched them: accepted.

Differences from the draft above:
- `Power_III` was kept (tenebral neutronium condenser research `NSynth_III` and the QCR heater depend on it) and `Power_IV` was added, like Joy, Hydroponics, Production and Medicine. Only `Battery_III` and `Drugs_III` were renamed.
- `Gate_III` hidden prerequisites are the core ones only: `Bionics_III`, `Armor_III`, `Weapons_III`, `Medicine_III`, `Joy_III`, `Hydroponics_III`, `Power_III`, `Core_Drill_III`. Research that comes from optional mods (Mechtech, Vaccines, flare shield) is not listed, so missing mods cannot break it.
- `Gate_IV` hidden prerequisites are `Medicine_IV`, `Drugs_IV`, `Power_IV`, `Battery_IV`, `Joy_IV`, `Hydroponics_IV`.
- `Vaccines_Combined_II` gates the basic and nano combined vaccines; `Vaccines_Combined` keeps the archo and tenebral ones. `Vaccines_IV` gates the umbral vaccine and requires `Vaccines_Combined` as a hidden prerequisite.
- Descriptions of existing researches were left alone, so `Medicine_III` still mentions dark medicine and the fabricator, which now need `Medicine_IV`.
