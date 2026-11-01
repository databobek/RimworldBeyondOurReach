# AGENTS

## Scope Guardrails

- The mod targets RimWorld 1.7 only; there are no per-version folders.
- Content lives in `Common/` (core defs/patches), `Mods/<Name>/` (compat loaded via `LoadFolders.xml`) and `Source/` (C#, builds to `Assemblies/`).
- Do not add support for older game versions unless the user explicitly requests it.

## Source Layout

- `Source/` is organized into feature folders (`Core`, `Buildings`, `Forge`, `Harvest`, `Kwazaari`, `Signals`, `Tenebral`, `Umbral`).
- Everything stays in the single `BeyondOurReach` namespace; XML references classes as `BeyondOurReach.<Class>`, so do not introduce sub-namespaces.
