# Project Context

## Purpose
Beyond Our Reach (BOR) is a RimWorld mod adding late-game tech tiers (Solar, Kwazaari, Tenebral), neutronium-driven production, and compatibility folders for other mods.

## Scope
- Active work targets **RimWorld 1.7 only**; there are no per-version folders. Content lives in `Common/` (core defs and patches), `Mods/<Name>/` (compat, loaded via `LoadFolders.xml`) and `Source/` (C#).
- `Languages/` (English, Polish) at the repo root holds keyed strings and DefInjected translations.

## Tech Stack
- C# (file-scoped namespace `BeyondOurReach`, nullable enabled), project `Source/BeyondOurReach.csproj`, output `Assemblies/BeyondOurReach.dll`.
- Compat projects in `Mods/<Name>/Source/` build into `Mods/<Name>/Assemblies/` and load with their folder. `Mods/MedicineCare` builds `BORMedicineCare.dll`, loaded only without Mod Medicine Patch.
- Harmony (assembly-wide `PatchAll` in `SettingsWindowPatch`), PipeSystem (neutronium net).

## Conventions
- Brace-always style, `Props` accessor pattern for comps, `[DefOf]` classes named `BOR_*DefOf`.
- Translation keys prefixed `BOR_`; user-visible strings go in `Languages/*/Keyed`.
- Soft dependencies (EvenMoreLinkables, Ideology) are resolved by reflection / `GetNamedSilentFail` so the mod loads without them.

## Specs
Specs cover the C# code in `Source/` (and the compat sources in `Mods/<Name>/Source/`) only; XML defs, patches and translations are out of scope.

Capabilities in `openspec/specs/`: `artifact-rework`, `component-harvest`, `forge`, `kwazaari-rework`, `medicine-care`, `orientis-faction`, `solar-flare-shield`, `tenebral-abilities`, `tenebral-bloodlust`, `tenebral-rework`, `ui-theme`, `umbral-rework`, `umbral-shield`.
These describe the behaviour implemented on branch `redux1_0` as of 2026-10-24 and are the baseline for further changes.
