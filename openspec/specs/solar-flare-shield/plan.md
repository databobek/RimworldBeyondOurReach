# Solar Flare Shield Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Solar-tier building that keeps its room powered through a solar flare, at 60 kW and with heavy room heating.

**Architecture:** A comp sets the draw and pushes heat. A per-map registry tracks shields. A `PowerNetTick` patch narrows the net to protected buildings for the call, so vanilla's own logic runs them during a flare.

**Tech Stack:** C#, Harmony (assembly-wide `PatchAll` already runs in `SettingsWindowPatch`), RimWorld 1.7 defs.

**Spec:** `openspec/specs/solar-flare-shield/spec.md`

## Global Constraints

- Everything stays in the `BeyondOurReach` namespace; use brace-always style and the `Props` accessor pattern.
- Do not reuse values, defs or names from existing mods or old compat folders. The `solarshield2` texture is the only exception.
- Do not patch `Building_CommsConsole`; it stays unusable during a flare.
- Do not commit. The branch `rework/orientis` has unrelated uncommitted work, and the user has not asked for commits.
- The repo has no test harness. Automated checks are `dotnet build` and well-formed XML; behaviour is checked through the manual `test-steps.md`.

## Review Focus

- A shield is destroyed or minified mid-flare: its room loses protection on the next tick with no errors (test step 7).
- Save and reload mid-flare: draw, protection and registry come back with no power blip (test step 8).
- The room is opened mid-flare (wall or roof lost): the shield stops protecting and heating (test step 6).
- Two shields in one room: both draw 60 kW and the room is protected once (test step 9).
- Very small room: temperature settles at 65°C without overshooting (test step 4).

---

### Task 1: Shield comp and flare patch

**Files:**
- Create: `Source/Buildings/SolarFlareShield.cs`

**Interfaces:**
- Produces: `BeyondOurReach.CompProperties_SolarFlareShield` with `float targetTemperature = 65f` and `float heatPerSecond = 200f`, whose `compClass` is `CompSolarFlareShield`.

- [ ] **Step 1: Add `CompSolarFlareShield : ThingComp`**

Exposes `bool ShieldingRoom` (trader `PowerOn`, and `parent.GetRoom()` is non-null and not `UsesOutdoorTemperature`).
- `PostSpawnSetup`: cache `CompPowerTrader`, register with the map component, apply the draw. Applying it here prevents a one-tick blip of full draw.
- `PostDeSpawn(Map map, DestroyMode mode)`: unregister from `map`.
- `CompTick`: every tick set `PowerOutput` to `-trader.Props.PowerConsumption` during a flare, otherwise `0`. The full draw comes from `basePowerConsumption`, so the info card shows 60,000 W.
- `CompTick`, every 60 ticks via `IsHashIntervalTick`, during a flare while `ShieldingRoom`: `room.PushHeat(Mathf.Min(Props.heatPerSecond, (Props.targetTemperature - room.Temperature) * room.CellCount))`, only if that amount is positive.

- [ ] **Step 2: Add `MapComponent_SolarFlareShields : MapComponent`**

Holds `List<CompSolarFlareShield> shields`. Use a map component, not a static list, so entries cannot outlive a loaded game. Comps register again on load through `PostSpawnSetup`, so it saves nothing.

- [ ] **Step 3: Add the `PowerNet.PowerNetTick` patch**

Prefix signature: `public static void Prefix(PowerNet __instance, out List<CompPowerTrader>? __state)`. Finalizer: `public static void Finalizer(PowerNet __instance, List<CompPowerTrader>? __state)`.

```
Prefix:
  __state = null
  if not ElectricityDisabled(map): return
  rooms = room of each ShieldingRoom shield on this map
  shields = net's powerComps whose parent has CompSolarFlareShield
  if rooms is empty and shields is empty: return      // vanilla shutdown runs
  protected = powerComps where room is in rooms, or parent has a shield comp
  every 20 ticks: PowerOn = false on max(1, round(5%)) random lit consumers outside protected
  __state = net.powerComps; net.powerComps = protected; ignoreFlare = true

Finalizer:
  if __state != null: net.powerComps = __state; ignoreFlare = false
```

Shields not yet on stay in `protected` so vanilla's restart logic can power them on; their rooms join `rooms` only once they are on. Leave `batteryComps` whole.

- [ ] **Step 4: Add the `GameConditionManager.ElectricityDisabled` prefix**

`public static bool Prefix(ref bool __result)`: when `ignoreFlare` is set, set `__result = false` and return `false`; otherwise return `true`. Read the flare state in Step 3 before setting the flag.

- [ ] **Step 5: Build**

Run: `dotnet build Source/BeyondOurReach.csproj`
Expected: `Build succeeded`, with no new errors or warnings from `SolarFlareShield.cs`.

### Task 2: Building, research, texture, translations and test steps

**Files:**
- Modify: `Common/Defs/ThingDefs_Buildings/tier1/Buildings_Power.xml`, `Common/Defs/ResearchDefs/ResearchProjectDefs.xml`
- Modify: `Languages/English/DefInjected/ThingDef/ThingDefs_Buildings/tier1/Buildings_Power.xml`, `Languages/English/DefInjected/ResearchProjectDef/ResearchDefs/ResearchProjectDefs.xml`
- Modify: `Languages/Polish/DefInjected/ThingDef/BOR_1.6_Expanded.xml`, `Languages/Polish/DefInjected/ResearchProjectDef/BOR_CoreResearch.xml`
- Create: `Textures/BOR/Things/Building/tier1/solarshield2.png` (copied from `TexturesAbandoned/SolarFlareShield/`; the source stays)
- Create: `openspec/specs/solar-flare-shield/test-steps.md`

**Interfaces:**
- Consumes: `BeyondOurReach.CompProperties_SolarFlareShield` from Task 1.

- [ ] **Step 1: Add the `BOR_SolarFlareShield` ThingDef after `BOR_Battery_I`**

`ParentName="BuildingBase"`, label "solar flare shield", `tickerType` Normal, `drawerType` MapMeshAndRealTime, `rotatable` false, `passability` PassThroughOnly, `canOverlapZones` false, minified into `BuildingsPower`, `designationCategory` BOR, `terrainAffordanceNeeded` Heavy, `constructEffect` ConstructMetal, `constructionSkillPrerequisite` 9. Values come from the spec: 3x3, `Graphic_Single` at `BOR/Things/Building/tier1/solarshield2` with draw size (4,4), 375 HP, 5,000 work, flammability 0.833, beauty -8, 165 `BOR_Sunsteel`, 5 `ComponentSpacer`, research `BOR_Research_SolarFlareShield`.

Comps, with power first so it is set up before the shield comp:
- `CompProperties_Power`: `compClass` `CompPowerTrader`, `basePowerConsumption` 60000, `shortCircuitInRain` true.
- `CompProperties_Flickable`, `CompProperties_Breakdownable`.
- `BeyondOurReach.CompProperties_SolarFlareShield`.

`placeWorkers`: `PlaceWorker_MustBeIndoors`. The description must state the room scope, the 60 kW draw during flares and the heat.

- [ ] **Step 2: Add `BOR_Research_SolarFlareShield`**

`ParentName="BOR_Research_Base_I"`, label "solar flare shield", 20,000 cost, `techLevel` Ultra, prerequisite `BOR_Research_Battery_I`, tree slot (4.00, 1.30). The description must say it shields a single room.

- [ ] **Step 3: Add English and Polish label and description entries for both defs**

Match the neighbouring `BOR_Battery_I` and `BOR_Research_Battery_I` entries. Leave the old Polish shield research keys alone.

- [ ] **Step 4: Copy the texture and write `test-steps.md`**

Follow the format of `openspec/specs/orientis-faction/test-steps.md`, with setup first (dev colony, god mode, a walled and roofed room). Steps, each with its expected result:

1. Build a shield with 10 Solar batteries outside its room and a lamp inside, on one net. Without a flare it reads 0 W.
2. Trigger a flare. The lamp stays on, consumers outside the room go off, and the shield reads 60,000 W.
3. With batteries full and nothing else drawing, the shield lasts about one in-game day.
4. In rooms of about 25, 100 and 900 cells, temperature climbs to 65°C or the size's plateau and does not overshoot.
5. A chemfuel generator in the room slows the drain; one outside the room adds nothing. A comms console in the room stays unusable.
6. Remove the roof mid-flare. The room loses protection and heating. Rain can short-circuit the unroofed shield.
7. Destroy the shield mid-flare. The room goes dark with no log errors.
8. Save and reload mid-flare. Everything resumes with no log errors.
9. Two shields in one room both draw 60 kW. Switching one off keeps the room protected.
10. Placement outdoors is refused. When the flare ends, the draw returns to 0 W, heating stops and outside buildings come back.

- [ ] **Step 5: Verify**

Run: `dotnet build Source/BeyondOurReach.csproj`, then parse each edited XML file with an XML parser.
Expected: the build succeeds and every file parses. The in-game steps in `test-steps.md` are for the user to run, since nothing here can launch the game.
