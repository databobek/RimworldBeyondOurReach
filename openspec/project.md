# Project Context

## Purpose
Beyond Our Reach (BOR) is a RimWorld mod adding late-game tech tiers (Solar, Kwazaari, Tenebral), neutronium-driven production, and integrations that move third-party mods (MedPod, Replimat) onto those tiers.

## Scope
- Active work targets **RimWorld 1.7 only**: everything lives under `1.7/`. Do not touch `1.5/` or `1.6/`.
- `Languages/` (English, Polish) at the repo root holds keyed strings and DefInjected translations.

## Tech Stack
- C# (file-scoped namespace `BeyondOurReach`, nullable enabled), project `1.7/Source/BeyondOurReach.csproj`, output `1.7/Assemblies/BeyondOurReach.dll`.
- Second project `1.7/SourceMedicine/MedicineCare.csproj` builds `1.7/MedicineCare/Assemblies/BORMedicineCare.dll`, loaded only without Mod Medicine Patch.
- Harmony (assembly-wide `PatchAll` in `SettingsWindowPatch`), PipeSystem (neutronium net), ModSettingsFramework.

## Conventions
- Brace-always style, `Props` accessor pattern for comps, `[DefOf]` classes named `BOR_*DefOf`.
- Translation keys prefixed `BOR_`; user-visible strings go in `Languages/*/Keyed`.
- Soft dependencies (EvenMoreLinkables, Ideology) are resolved by reflection / `GetNamedSilentFail` so the mod loads without them.

## Specs
Specs cover the C# code in `1.7/Source/` (and `1.7/SourceMedicine/`) only; XML defs, patches and translations are out of scope.

Capabilities in `openspec/specs/`: `forge`, `component-harvest`, `tenebral-bloodlust`, `tier-integration`, `medicine-care`.
These describe the behaviour implemented on branch `redux1_0` as of 2026-10-24 and are the baseline for further changes.
