# Medicine Care Specification

## Purpose
Replaces the Mod Medicine Patch (`koth.modmedicinepatch`) so every modded medicine gets its own medical care level. Implemented by the separate project `1.7/SourceMedicine/` (`BORMedicineCare.dll`), which is only loaded when Mod Medicine Patch is not active.

## Requirements

### Requirement: Conditional loading
`LoadFolders.xml` SHALL load `1.7/MedicineCare` only when `koth.modmedicinepatch` is not active. Mod Medicine Patch is a `loadAfter` entry, not a dependency.

### Requirement: Care levels
`CareTiers` SHALL give each `MedicalCareCategory` value one tier. Values 0 to 4 keep their vanilla meaning (no care, no meds, herbal, industrial, ultratech, so `Best` is the ultratech tier). Every other `IsMedicine` def is appended from value 5, ordered by `MedicalPotency` then `defName` (ordinal). Appended defs SHALL be added to the `InventoryStockGroupDefOf.Medicine` stock group, and the vanilla `careTextures` array SHALL be replaced with one icon per tier.

#### Scenario: Saved value stability
- WHEN the set of medicines is unchanged
- THEN a saved care value maps to the same medicine
- WHEN a medicine mod is added or removed, or one is changed in potency
- THEN values above it can map to a different medicine

### Requirement: Allowed medicine
For tiers `Best` and above, `AllowsMedicine` SHALL allow a medicine when its `MedicalPotency` is at most the tier's potency. Lower tiers keep vanilla behaviour.

### Requirement: Label
For tiers `Best` and above whose tier has a medicine, `GetLabel` SHALL return `BOR_MedicineCare_OrWorse` formatted with the medicine's label.

### Requirement: Care UI
`MedicalCareSetter` SHALL draw one icon per tier, sorted by potency, wrapped over up to 4 rows, with click and drag-paint selection and a border on the selected tier. `MedicalCareSelectButton` SHALL be a dropdown over the same tiers plus "ChangeDefaults".

### Requirement: Choose Your Medicine compatibility
Choose Your Medicine detects Mod Medicine Patch by active mod name and reads it by reflection. `MedicineCareMod` SHALL answer true for `ModLister.HasActiveModWithName("Mod Medicine Patch 1.6")` only when called from the `ChooseYourMedicine` assembly. The class `ModMedicinePatch.ModMedicinePatch` SHALL expose `medList`, `GetOrderedCareList`, `GetMedicineByCare` and `GetCareTexture` with the same shapes as the original, backed by the potency-sorted tier list.

#### Scenario: Default Choose Your Medicine entries
- WHEN Choose Your Medicine uses `Best` for its strongest slot
- THEN doctors use ultratech at most, and only entries set to a modded tier use the modded medicines (intended: modded medicine is scarce)

### Requirement: Not supported
Smart Medicine and Pharmacist compatibility is not implemented.
