# Umbral Shield Specification

## Purpose
An Umbral building that draws 1,000,000 W to hold up a crimson dome, 35 tiles in radius. It protects everything under it from outside fire, drop pods and solar flares, and in exchange gives every pawn under it tenebral psychosis.

## Requirements

### Requirement: Power
The shield SHALL draw 1,000,000 W at all times and have no off switch. It SHALL have no heat, roof or rain requirements.

#### Scenario: Dome down
- WHEN the dome is down for any reason: no power, or anything else that drops vanilla's interceptor (`CompProjectileInterceptor.Active`)
- THEN every effect of the dome SHALL stop until it is back up: interception, the drop pod ban, flare protection, psychosis and the Orientis thought
- AND during a flare, the shield SHALL be able to restart from stored power or from generators within its radius, while consumers there stay off until it does

### Requirement: Dome display
A gizmo SHALL show or hide the dome. A hidden dome SHALL keep working exactly the same.

### Requirement: Outside fire
The dome SHALL stop hostile gunfire and mortar fire crossing its edge from outside. Friendly shots and shots fired from inside the dome SHALL pass.

### Requirement: Drop pods
No drop pod SHALL be able to pick a landing spot inside the dome, whoever sent it. Shuttles are not affected.

### Requirement: Solar flares
Consumers and generators inside the dome SHALL stay on through a flare, with no draw beyond the shield's usual 1,000,000 W, through the same `PowerNetTick` patch as the solar flare shield. Batteries anywhere on the net count; generators outside the dome do not. Comms consoles SHALL stay unusable. While a powered RT Solar Flare Shield lifts the flare, this part SHALL stand down and the inspect pane SHALL say so; the solar flare shield spec has the details.

### Requirement: Psychosis
Inside the dome, a humanlike pawn's emission SHALL be `maxEmission` from the shield, replacing any tenebrite buildings and floors nearby. Implant and gear floors and the resistance gene SHALL still apply, and psychosis SHALL fade as normal after the pawn leaves.

#### Scenario: Orientis pawn under the dome
- WHEN an Orientis pawn is inside the dome
- THEN their emission SHALL be 0, because psychosis would mark them as dark
- AND they SHALL have a -20 mood thought for as long as they stay

### Requirement: Umbral only
The shield SHALL carry `DefModExtension_UmbralOnly`, so only Umbral pawns can build it, and a Path of Light colony cannot build it and turns existing ones into husks.

## Implementation
`Source/Umbral/UmbralShield.cs`: `CompUmbralShield.Covers(map, cell)` is the one range check, used by the `CanPhysicallyDropInto` postfix, `TenebralPsychosis.Emission`, the thought worker and the patch in `SolarFlareShield.cs`. A vanilla `CompProjectileInterceptor` does the intercepting, a postfix on its `Active` getter drops the dome without power, and a prefix on its `PostDraw` hides the dome.

## Builder's calls
- `BOR_UmbralShield`, 3x3 with 4x4 draw size, 1012 HP, 12,000 work, construction skill 15, 150 tenebrite, 8 `BOR_ComponentIII`.
- New research `BOR_Research_UmbralShield`: 350,000 cost, requires `BOR_Research_Power_IV`, tree slot (24.50, 1.55).
- Texture `solarshield3.png` copied to `Textures/BOR/Things/Building/tier4/`. The `_top` part is left out.
- Not covered: the Royalty power beam, which is not made of projectiles and has its own patch for the Orientis shield.
