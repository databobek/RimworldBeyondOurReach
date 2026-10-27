# Tier Integration Specification

## Purpose
Settings-driven integration that moves third-party mods onto a BOR tech tier. Implemented by `PatchOperationTierIntegration.cs` (a ModSettingsFramework patch operation configured from XML) and `SettingsWindowPatch.cs`.

## Requirements

### Requirement: Tier and cost sliders
For each integration the settings menu SHALL show a snapping "Technology tier" slider (one step per configured tier) and a "Research cost" slider (min/max/step per patch, default step 1000). Tier 0 leaves the original mod untouched; changes need a game restart.

#### Scenario: Cost follows tier
- WHEN the tier changes
- THEN the stored cost resets to that tier's `researchCost` until the player fine-tunes it again (tracked via `<id>_costTier`), always clamped to [minCost, maxCost]

### Requirement: Patch application
At patch time, for the selected tier, the operation SHALL:
- set the research def's `baseCost`, and optionally `techLevel`, `prerequisites` (replaced), `requiredResearchFacilities` (appended), view position, and BOR research tab (when a prerequisite is set);
- substitute cost-list ingredients of listed things in a single pass (no cascading, e.g. Steel->Plasteel and Plasteel->Sunsteel do not chain);
- for tiers above 0, apply the configured `powerOverrides` xpath values.

#### Scenario: Missing nodes
- WHEN a configured def, cost list, or xpath does not exist
- THEN the operation is skipped for it and the patch still succeeds

### Requirement: Settings window offset
The BOR settings window (`bambaryla.bor.redux`) SHALL be pushed down 30 px so the framework's restart notice does not overlap the first section.
