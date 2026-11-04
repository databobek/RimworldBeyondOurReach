# Orientis Main Weapons Rebalance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Retune the six main Orientis weapons (4 melee, 2 crossbows) so damage sits between Solar and Quasar, and add the laser armor-break and boombow Umbral/anomaly-only stun debuffs.

**Architecture:** Two small C# damage workers in `Source/Orientis/` (one stuns only Umbral, Tenebral and anomaly targets, one applies an armor-break hediff to Umbrals in combat mode), a hook on the existing Umbral exoskeleton comp to force the combat armor off, and XML retuning in `Common/` plus matching CE_NEW patches.

**Tech Stack:** RimWorld 1.7 (.NET 10, Waylith), C# in the single `BeyondOurReach` namespace, XML defs, `dotnet build` on `Source/BeyondOurReach.csproj` (outputs to `Assemblies/`).

**Spec:** `docs/superpowers/specs/2026-11-03-orientis-weapon-rebalance-design.md`

## Global Constraints

- The mod targets RimWorld 1.7 only; no per-version folders. Everything stays in the `BeyondOurReach` namespace (`AGENTS.md`).
- Damage sits between Solar and Quasar; penetration stays in the Orientis band (above Quasar, below Tenebral).
- Stun applies only to Umbral, Tenebral and anomaly pawns. Colonists, raiders and animals in the blast get damage only.
- The main weapons stay `tradeability None`, `generateCommonality 0`, offerings-bench only.
- Straight quotes only in text and translations. Do not copy values from other mods; derive them from the Phase5 ladder tables.
- No automated test framework exists. "Test" steps are `dotnet build` and dev-mode checks in game. Commit only if the user asks.
- Verify vanilla API names by decompiling `Assembly-CSharp.dll` before relying on them (memory: RimWorld 1.7 runs on .NET 10 / Waylith).

## Review Focus

- Boombow explosion beside a colonist and an Umbral: only the Umbral is stunned; the colonist still takes damage.
- Laser hit on a non-Umbral, or an Umbral not in combat mode: no hediff lingers and nothing is disabled.
- Save and load while the exoskeleton is locked off: the lock survives and still ends on time.
- Biotech off: an Umbral identified only by the `BOR_Umbral` hediff is still a valid stun target.
- Anomaly DLC off: the target check does not throw.

---

### Task 1: Stun-target filter and shockwave worker

**Files:**
- Create: `Source/Orientis/OrientisShockwave.cs`
- Modify: `Common/Defs/ThingDefs_Items/Orientis/Orientis_Weapons.xml` (boombow bolt)

**Interfaces:**
- Produces: `public static class OrientisTargets { public static bool IsStunTarget(Pawn pawn); }`
- Produces: `public class DefModExtension_TargetStun : DefModExtension { public int stunTicks = 120; }`
- Produces: `public class DamageWorker_OrientisBlast : DamageWorker_Bomb` (overrides `Apply(DamageInfo dinfo, Thing victim)`: calls base, then `pawn.stances.stunner.StunFor(ext.stunTicks, dinfo.Instigator, addBattleLog: false)` only when `IsStunTarget`).
- Produces: `DamageDef BOR_OrientisBlast` (`workerClass BeyondOurReach.DamageWorker_OrientisBlast`, based on vanilla `Bomb`, with `DefModExtension_TargetStun`).

- [ ] **Step 1: Decompile and confirm** the exact vanilla names: `Pawn.RaceProps.IsAnomalyEntity` (or its 1.7 equivalent), `DamageWorker_Bomb.Apply`, `StunHandler.StunFor`. Note any difference in a code comment only if it changes behavior.
- [ ] **Step 2: Implement `OrientisTargets.IsStunTarget`** in `OrientisShockwave.cs`: true if the pawn has hediff `BOR_UmbralDefOf.BOR_Umbral` or `BOR_UmbralExoskeleton`, or hediff `BOR_TenebralPsychosis`, or is an anomaly entity. It must not touch Anomaly-only types when the DLC is off (use the race flag only).
- [ ] **Step 3: Implement the extension and `DamageWorker_OrientisBlast`** per the Interfaces block.
- [ ] **Step 4: Add `BOR_OrientisBlast` DamageDef** and point `BOR_Bullet_OrientisPlasmaBolt` at it; keep `Projectile_Explosive`. Reduce the explosion damage to the Solar to Quasar value from Task 4's rule and keep radius small (vanilla 2.9 down to 1.9).
- [ ] **Step 5: Verify build.** Run `dotnet build Source/BeyondOurReach.csproj -c Release`. Expected: 0 errors.
- [ ] **Step 6: Dev-mode check** (Review Focus 1 and 4): spawn a colonist and an Umbral side by side, fire the boombow at the Umbral. Expected: Umbral shows the stunned stance; colonist takes damage and is not stunned. Repeat with Biotech disabled.

### Task 2: Armor-break hediff and exoskeleton lock

**Files:**
- Create: `Source/Orientis/OrientisArmorBreak.cs`
- Modify: `Source/Umbral/UmbralExoskeleton.cs` (`HediffComp_UmbralModeApparel`, `CompExposeData`)
- Modify: `Common/Defs/HediffDefs/Hediffs_Orientis.xml`

**Interfaces:**
- Consumes: `ExoskeletonMode`, `HediffComp_UmbralModeApparel` (existing; `Mode`, private `ApplyMode`, `EndMode`).
- Produces: `public bool HediffComp_UmbralModeApparel.TryDisableCombat(int lockTicks)` — returns true and forces the owner exoskeleton to `ExoskeletonMode.None` only when its current mode is `Combat`; refuses any mode selection (gizmo and AI) until `Find.TickManager.TicksGame >= modeLockEndTick`. Saves `modeLockEndTick` in `CompExposeData`.
- Produces: `public class HediffCompProperties_OrientisArmorBreak : HediffCompProperties { public float disableChance = 0.3f; public float lockSeconds = 30f; }` and `HediffComp_OrientisArmorBreak` which, on `CompPostPostAdd`, rolls `disableChance` and calls `TryDisableCombat` on the pawn's `BOR_UmbralExoskeleton` comp.
- Produces: `HediffDef BOR_OrientisArmorBreak`, short-lived (disappears after a few seconds, `isBad true`).
- Produces: `public class DamageWorker_OrientisLaser : DamageWorker_Cut`-equivalent for bullets (derive from the same vanilla worker `Bullet` uses, confirmed by decompile) that adds `BOR_OrientisArmorBreak` after the damage lands, only when the victim has the exoskeleton hediff in combat mode. `DamageDef BOR_OrientisLaser` uses it. Confirm first whether vanilla `DamageDef.additionalHediffs` applies on projectile hits; if it does and can be conditioned, prefer it.

- [ ] **Step 1: Add `TryDisableCombat` and the lock field** to `HediffComp_UmbralModeApparel`; make every gizmo option disabled while locked and show the remaining time.
- [ ] **Step 2: Implement the comp properties, comp and laser damage worker** per the Interfaces block.
- [ ] **Step 3: Add the hediff and `DamageDef BOR_OrientisLaser`** to XML; point `BOR_Bullet_OrientisLaserBolt` at it.
- [ ] **Step 4: Verify build.** `dotnet build Source/BeyondOurReach.csproj -c Release`. Expected: 0 errors.
- [ ] **Step 5: Dev-mode checks** (Review Focus 2 and 3): shoot an Umbral in combat mode repeatedly with `disableChance` temporarily 1.0; expected exoskeleton drops to no mode and every mode option is blocked, then returns after the 30-second lock. Save and reload mid-lock; expected the lock still ends on time. Shoot a colonist and a non-combat Umbral; expected no hediff stays and nothing changes.

### Task 3: Melee retune

**Files:**
- Modify: `Common/Defs/ThingDefs_Items/Orientis/Orientis_Weapons.xml` (the four power weapons' `<tools>`)
- Modify: `Mods/CE_NEW/Patches/additions/orientis.xml` (the four `ToolCE` blocks)

- [ ] **Step 1: Read the Solar and Quasar melee rows** in `AllBambaGuns/CE_NEW/Docs/Phase5_TierLadder_Proposal.md` and the vanilla Solar and Quasar tool powers. Per weapon class (axe, longsword, dagger, hammer), set `power` to the geometric mean of the Solar and Quasar values for that class. Record each chosen value in a comment above the tool.
- [ ] **Step 2: Raise `cooldownTime`** by 30 to 40% over the current value for the axe, longsword and hammer; the dagger rises by 25% and stays the fastest. Keep the current `armorPenetration` and CE `armorPenetrationSharp`/`Blunt`.
- [ ] **Step 3: Update the descriptions** to say the weapon is slow and waits for a perfect strike, with no typographic quotes.
- [ ] **Step 4: Verify.** The vanilla file and the CE_NEW patch parse (`dotnet build` plus loading the game with no red errors). In dev mode, the Marine-armored Umbral dummy takes one hit from the hammer and a measurable fraction of its health.

### Task 4: Crossbow damage retune

**Files:**
- Modify: `Common/Defs/ThingDefs_Items/Orientis/Orientis_Weapons.xml`
- Modify: `Mods/CE_NEW/Defs/additions/orientis.xml`

- [ ] **Step 1: Pick values by one rule:** laser bolt damage is the Solar-to-Advanced-Spacer figure from the Phase5 ranged table; plasma explosion damage is the geometric mean of the Solar and Quasar explosive figures. Keep laser `armorPenetrationSharp` and boombow range as they are.
- [ ] **Step 2: Apply to vanilla and CE_NEW** and point the CE bolts at `BOR_OrientisLaser` and `BOR_OrientisBlast`. Confirm in game with CE_NEW loaded that the custom damage workers still run (CE's projectile impact must reach `Apply`).
- [ ] **Step 3: Verify.** Build, load, and repeat the Task 1 and Task 2 dev-mode checks once with CE_NEW active.

### Task 5: Text and wrap-up

**Files:**
- Modify: `Languages/English/Keyed/BOR_Orientis.xml` (any new keyed strings; hediff label and description inline in the def)

- [ ] **Step 1: Add** labels and descriptions for `BOR_OrientisArmorBreak` and the lock message, in straight-quote English text.
- [ ] **Step 2: Update the crossbow descriptions** to name their debuffs.
- [ ] **Step 3: Final check.** Full `dotnet build`; start the game with BOR alone, then with BOR and CE_NEW. Expected: no errors in the log from the new defs or classes. Report anything not run.
