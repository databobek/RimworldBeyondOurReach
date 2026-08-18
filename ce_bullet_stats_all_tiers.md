# BOR Combat Extended — Bullet/Projectile Stats by Tier

Extracted directly from the projectile (`CombatExtended.ProjectilePropertiesCE` / `CombatExtended.Lasers.LaserBeamDefCE`) blocks in each patch file. Values are exactly what each `ThingDef` overrides on its own — a blank **Speed** cell means the bullet doesn't override `<speed>` and instead inherits it from its `ParentName` base (e.g. `LaserBulletGreen`, `Base6x18mmChargedBullet`), which is defined outside this repo (core CE / not re-stated here).

Columns: **AP Sharp/Blunt** = `armorPenetrationSharp` / `armorPenetrationBlunt`. **Secondary Damage** = `secondaryDamage` li `def:amount` pairs. **Notes** carries `pelletCount`/`spreadMult` (shotguns) and `empShieldBreakChance`.

---

## Tier I — Ballistic (Gen I "NX", `1.6/Mods/CombatExtended/Patches/base/gen1/`)

### Autopistol — 9.3×35mm NX (`gen_1_pistol_bullet.xml`)

| Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Speed | Secondary Damage | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| FSJ | 10 | Bullet | 24 | 55 | 147 | — | — |
| QaP | 7 | Bullet | 36 | 55 | 147 | — | — |
| Ni | 6 | Bullet | 27 | 47 | 147 | Bomb_Secondary: 6 | — |
| TC | 7 | Bullet | 54 | 77 | 191 | — | — |
| Nc | 7 | Bullet | 36 | 55 | 147 | Flame_Secondary: 5 | — |
| EMP-N | 6 | Bullet | 24 | 55 | 147 | EMP: 7 | EMP shield break 0.4 |

### Assault Rifle — 12.1×74mm NX (`gen_1_rifle_bullet.xml`)

| Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Speed | Secondary Damage | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| FSJ | 20 | Bullet | 48.5 | 110 | 181.2 | — | — |
| QaP | 14 | Bullet | 72.45 | 110 | 181.2 | — | — |
| Ni | 12 | Bullet | 55 | 95 | 181.2 | Bomb_Secondary: 8 | — |
| TC | 15 | Bullet | 108 | 154 | 235 | — | — |
| Nc | 14 | Bullet | 72.45 | 110 | 181.2 | Flame_Secondary: 8 | — |
| EMP-N | 12 | Bullet | 48.5 | 110 | 181.2 | EMP: 7 | EMP shield break 0.4 |

### Sniper Rifle — 16.5×120mm NX (`gen_1_rifle_bullet.xml`)

| Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Speed | Secondary Damage | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| FSJ | 100 | Bullet | 240 | 550 | 181.2 | — | — |
| QaP | 70 | Bullet | 360 | 550 | 181.2 | — | — |
| Ni | 60 | Bullet | 275 | 475 | 181.2 | Bomb_Secondary: 8 | — |
| TC | 75 | Bullet | 540 | 770 | 235 | — | — |
| Nc | 70 | Bullet | 360 | 550 | 181.2 | Flame_Secondary: 8 | — |
| EMP-N | 60 | Bullet | 240 | 550 | 181.2 | EMP: 7 | EMP shield break 0.4 |

### LMG — 16.5×120mm NX (`gen_1_rifle_bullet.xml`)

| Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Speed | Secondary Damage | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| FSJ | 17 | Bullet | 40 | 93 | 181.2 | — | — |
| QaP | 11 | Bullet | 61 | 93 | 181.2 | — | — |
| Ni | 10 | Bullet | 46 | 80 | 181.2 | Bomb_Secondary: 8 | — |
| TC | 12 | Bullet | 91 | 130 | 235 | — | — |
| Nc | 11 | Bullet | 61 | 93 | 181.2 | Flame_Secondary: 8 | — |
| EMP-N | 10 | Bullet | 40 | 93 | 181.2 | EMP: 7 | EMP shield break 0.4 |

### Chain Shotgun — 16mm NX (`gen_1_shotgun_bullet.xml`)

| Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Speed | Secondary Damage | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| N (pellet) | 30 | Bullet | 48.5 | 103.5 | 181.2 | — | 9 pellets, spread 7.12 |
| Slug (QaP) | 20 | Bullet | 72.45 | 155.25 | 181.2 | — | single slug |
| Ion (pellet) | 30 | Bullet | 48.5 | 103.5 | 181.2 | EMP: 7 | 9 pellets, spread 7.12; EMP shield break 0.4 |
| Slug (FSJ) | 60 | Bullet | 215 | 465 | 181.2 | — | single slug |
| QaP (pellet) | 30 | Bullet | 48.5 | 103.5 | 181.2 | — | 9 pellets, spread 7.12 |
| Ni (pellet) | 30 | Bullet | 41 | 89 | 181.2 | Bomb_Secondary: 10 | 9 pellets, spread 7.12 |
| Nc (pellet) | 30 | Bullet | 48.5 | 103.5 | 181.2 | Flame_Secondary: 8 | 9 pellets, spread 7.12 |
| EMP-N (pellet) | 30 | Bullet | 48.5 | 103.5 | 181.2 | EMP: 7 | 9 pellets, spread 7.12; EMP shield break 0.4 |
| Slug (TC) | 100 | Bullet | 360 | 775 | 181.2 | — | single slug |

---

## Tier I — Gauss (Solar Railgun, `additions/gauss_solar.xml`)

### Pistol — 4mm Railgun

| Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Speed | Secondary Damage | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| TC | 8 | Bullet | 48 | 31 | 200 | — | — |
| Nc | 7 | Bullet | 32 | 22 | 160 | Flame_Secondary: 6 | — |
| N-i | 8 | Bullet | 22 | 19 | 160 | Bomb_Secondary: 6 | — |
| EMP-N | 8 | Bullet | 14 | 22 | 160 | EMP: 6 | — |
| SS | 10 | Bullet | 16 | 22 | 140 | — | — |

### Battle Rifle — 6mm Railgun

| Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Speed | Secondary Damage | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| TC | 10 | Bullet | 72 | 204 | 400 | — | — |
| Nc | 9 | Bullet | 48 | 146 | 300 | Flame_Secondary: 10 | — |
| Ni | 8 | Bullet | 36 | 128 | 300 | Bomb_Secondary: 16 | — |
| EMP-N | 8 | Bullet | 32 | 146 | 300 | EMP: 10 | — |
| SS | 13 | Bullet | 32 | 146 | 260 | — | — |

### Bullpup — 6mm Railgun

| Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Speed | Secondary Damage | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| TC | 8 | Bullet | 61 | 173 | 400 | — | — |
| Nc | 7 | Bullet | 40 | 124 | 300 | Flame_Secondary: 10 | — |
| Ni | 6 | Bullet | 30 | 108 | 300 | Bomb_Secondary: 16 | — |
| EMP-N | 6 | Bullet | 27 | 124 | 300 | EMP: 10 | — |
| SS | 11 | Bullet | 27 | 124 | 260 | — | — |

### DMR — 6mm Railgun

| Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Speed | Secondary Damage | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| TC | 14 | Bullet | 100 | 285 | 400 | — | — |
| Nc | 12 | Bullet | 67 | 204 | 300 | Flame_Secondary: 10 | — |
| Ni | 11 | Bullet | 50 | 179 | 300 | Bomb_Secondary: 16 | — |
| EMP-N | 11 | Bullet | 44 | 204 | 300 | EMP: 10 | — |
| SS | 18 | Bullet | 44 | 204 | 260 | — | — |

### Sniper Rifle — 8mm Railgun

| Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Speed | Secondary Damage | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| TC | 50 | Bullet | 360 | 1020 | 400 | — | — |
| Nc | 45 | Bullet | 240 | 730 | 300 | Flame_Secondary: 10 | — |
| Ni | 40 | Bullet | 180 | 640 | 300 | Bomb_Secondary: 16 | — |
| EMP-N | 40 | Bullet | 160 | 730 | 300 | EMP: 10 | — |
| SS | 65 | Bullet | 160 | 730 | 260 | — | — |

### Cannon — 8mm Railgun

| Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Speed | Secondary Damage | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| TC | 8 | Bullet | 61 | 173 | 400 | — | — |
| Nc | 7 | Bullet | 40 | 124 | 300 | Flame_Secondary: 10 | — |
| Ni | 6 | Bullet | 30 | 108 | 300 | Bomb_Secondary: 16 | — |
| EMP-N | 6 | Bullet | 27 | 124 | 300 | EMP: 10 | — |
| SS | 11 | Bullet | 27 | 124 | 260 | — | — |

### Cannon (heavy round) — 12mm Railgun *(single leftover TC variant, no other material tiers defined)*

| Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Speed | Secondary Damage | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| TC | 248 | Bullet | 461 | 22500 | 1687 | — | AP Blunt value (22500) looks like a data-entry outlier vs. every other bullet in the file |

### Shotgun — 12 Gauge Scatter Railgun

| Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Speed | Secondary Damage | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| TC (slug) | 13 | Bullet | 48 | 206 | 300 | — | — |
| SS (slug) | 7 | Bullet | 28 | 123 | 220 | — | — |
| Nc (slug) | 20 | Bullet | 32 | 136 | 260 | Flame_Secondary: 10 | — |
| N-i (slug) | 20 | Bullet | 28 | 120 | 260 | Bomb_Secondary: 12 | — |
| EMP-N (slug) | 20 | Bullet | 32 | 136 | 260 | EMP: 10 | — |

---

## Tier I — Laser (`additions/xcom_laser.xml`, standalone weapon set)

Base laser color = damage/AP role; `apB` is always explicitly zeroed (lasers ignore blunt armor). Speed is inherited from the base laser bullet ParentName (not overridden here).

### Rifle

| Variant | Damage | Dmg Type | AP Sharp | AP Blunt | Secondary Damage |
| --- | --- | --- | --- | --- | --- |
| Solar (green) | 44 | Bullet | 55 | 0 | — |
| Incendiary (red) | 26 | Bullet | 36 | 0 | Flame: 20 |
| HP (purple) | 55 | Bullet | 29 | 0 | Bomb_Secondary: 13 |
| EMP (blue) | 13 | Bullet | 21 | 0 | EMP: 10 |

### Sniper Rifle

| Variant | Damage | Dmg Type | AP Sharp | AP Blunt | Secondary Damage |
| --- | --- | --- | --- | --- | --- |
| Solar | 220 | Bullet | 275 | 0 | — |
| Incendiary | 130 | Bullet | 180 | 0 | Flame: 26 |
| HP | 275 | Bullet | 145 | 0 | Bomb_Secondary: 20 |
| EMP | 65 | Bullet | 105 | 0 | EMP: 13 |

### Shotgun

| Variant | Damage | Dmg Type | AP Sharp | AP Blunt | Secondary Damage | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| Solar | 25 | Bullet | 18 | 0 | — | 10 pellets |
| Incendiary | 15 | Bullet | 12 | 0 | Flame: 10 | 10 pellets |
| HP | 31 | Bullet | 11 | 0 | Bomb_Secondary: 8 | 7 pellets |
| EMP | 10 | Bullet | 12 | 0 | EMP: 7 | 10 pellets |

### Cannon

| Variant | Damage | Dmg Type | AP Sharp | AP Blunt | Secondary Damage |
| --- | --- | --- | --- | --- | --- |
| Solar | 37 | Bullet | 46 | 0 | — |
| Incendiary | 22 | Bullet | 30 | 0 | Flame: 16 |
| HP | 46 | Bullet | 24 | 0 | Bomb_Secondary: 19 |
| EMP | 11 | Bullet | 17 | 0 | EMP: 8 |

### Pistol

| Variant | Damage | Dmg Type | AP Sharp | AP Blunt | Secondary Damage |
| --- | --- | --- | --- | --- | --- |
| Solar | 22 | Bullet | 13 | 0 | — |
| Incendiary | 13 | Bullet | 8 | 0 | Flame: 10 |
| HP | 27 | Bullet | 8 | 0 | Bomb_Secondary: 10 |
| EMP | 6 | Bullet | 9 | 0 | EMP: 7 |

### Bullpup

| Variant | Damage | Dmg Type | AP Sharp | AP Blunt | Secondary Damage |
| --- | --- | --- | --- | --- | --- |
| Solar | 37 | Bullet | 46 | 0 | — |
| Incendiary | 22 | Bullet | 30 | 0 | Flame: 16 |
| HP | 46 | Bullet | 24 | 0 | Bomb_Secondary: 19 |
| EMP | 11 | Bullet | 17 | 0 | EMP: 8 |

### DMR

| Variant | Damage | Dmg Type | AP Sharp | AP Blunt | Secondary Damage |
| --- | --- | --- | --- | --- | --- |
| Solar | 61 | Bullet | 77 | 0 | — |
| Incendiary | 36 | Bullet | 50 | 0 | Flame: 26 |
| HP | 77 | Bullet | 40 | 0 | Bomb_Secondary: 20 |
| EMP | 18 | Bullet | 29 | 0 | EMP: 13 |

---

## Tier II — Ballistic (Gen II "QA", `base/gen2/`)

### Autopistol — 11.6×45mm QA (`gen_2_pistol_bullet.xml`)

| Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Speed | Secondary Damage |
| --- | --- | --- | --- | --- | --- | --- |
| QaP | 11 | Bullet | 28.8 | 66 | 200 | — |
| Nc | 8 | Bullet | 43.2 | 66 | 200 | Flame_Secondary: 3 |
| Ni | 7 | Bullet | 32.4 | 56.4 | 185 | Bomb_Secondary: 8 |
| TC | 8 | Bullet | 64.8 | 92.4 | 230 | — |

### Rifle — 20.5×95mm QA (`gen_2_rifle_bullet.xml`)

| Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Speed | Secondary Damage |
| --- | --- | --- | --- | --- | --- | --- |
| QaP | 22 | Bullet | 58.2 | 132 | 250 | — |
| Nc | 15 | Bullet | 86.94 | 132 | 250 | Flame_Secondary: 5 |
| Ni | 13 | Bullet | 66 | 114 | 220 | Bomb_Secondary: 12 |
| TC | 17 | Bullet | 129.6 | 184.8 | 300 | — |

### Shotgun — 21mm QA (`gen_2_shotgun_bullet.xml`)

| Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Speed | Secondary Damage | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| N (pellet) | 33 | Bullet | 58.2 | 124.2 | 250 | — | 9 pellets, spread 6 |
| Slug (QaP) | 22 | Bullet | 86.94 | 186.3 | 250 | — | single slug |
| Ion (pellet) | 33 | Bullet | 58.2 | 124.2 | 250 | EMP: 10 | 9 pellets, spread 8; EMP shield break 0.4 |

*Note: unlike Gen I, Gen II has no dedicated Sniper Rifle or LMG bullet files — only Pistol/Rifle/Shotgun exist.*

---

## Tier II — Gauss (Quasar "Kwazeer", `additions/gauss_quasar.xml`)

All Tier II gauss ammo shares the same 3-variant scheme: **Std** (kinetic), **Inc** (incendiary), **EMP**.

### Rifle — 6mm kwazaari

| Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Speed | Secondary Damage |
| --- | --- | --- | --- | --- | --- | --- |
| Std | 5 | Bullet | 16 | 73 | 260 | Bomb_Secondary: 8 |
| Inc | 5 | Bullet | 16 | 73 | 260 | Bomb_Secondary: 5; Flame_Secondary: 4 |
| EMP | 5 | Bullet | 16 | 73 | 260 | Bomb_Secondary: 5; EMP: 4 (shield break 0.4) |

### Bullpup — 6mm kwazaari

| Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Speed | Secondary Damage |
| --- | --- | --- | --- | --- | --- | --- |
| Std | 4 | Bullet | 13.5 | 62 | 260 | Bomb_Secondary: 6 |
| Inc | 4 | Bullet | 13.5 | 62 | 260 | Bomb_Secondary: 4; Flame_Secondary: 3 |
| EMP | 4 | Bullet | 13.5 | 62 | 260 | Bomb_Secondary: 4; EMP: 3 (shield break 0.4) |

### DMR — 6mm kwazaari

| Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Speed | Secondary Damage |
| --- | --- | --- | --- | --- | --- | --- |
| Std | 7 | Bullet | 22 | 102 | 260 | Bomb_Secondary: 11 |
| Inc | 7 | Bullet | 22 | 102 | 260 | Bomb_Secondary: 7; Flame_Secondary: 6 |
| EMP | 7 | Bullet | 22 | 102 | 260 | Bomb_Secondary: 7; EMP: 6 (shield break 0.4) |

### Sniper Rifle — 8mm kwazaari

| Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Speed | Secondary Damage |
| --- | --- | --- | --- | --- | --- | --- |
| Std | 26 | Bullet | 80 | 365 | 260 | Bomb_Secondary: 39 |
| Inc | 26 | Bullet | 80 | 365 | 260 | Bomb_Secondary: 26; Flame_Secondary: 21 |
| EMP | 26 | Bullet | 80 | 365 | 260 | Bomb_Secondary: 26; EMP: 21 (shield break 0.4) |

### Cannon — 8mm kwazaari

| Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Speed | Secondary Damage |
| --- | --- | --- | --- | --- | --- | --- |
| Std | 4 | Bullet | 13.5 | 62 | 260 | Bomb_Secondary: 6 |
| Inc | 4 | Bullet | 13.5 | 62 | 260 | Bomb_Secondary: 4; Flame_Secondary: 3 |
| EMP | 4 | Bullet | 13.5 | 62 | 260 | Bomb_Secondary: 4; EMP: 3 (shield break 0.4) |

### Pistol — 4mm kwazaari

| Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Speed | Secondary Damage |
| --- | --- | --- | --- | --- | --- | --- |
| Std | 4 | Bullet | 8 | 11 | 140 | Bomb_Secondary: 6 |
| Inc | 4 | Bullet | 8 | 11 | 140 | Bomb_Secondary: 4; Flame_Secondary: 3 |
| EMP | 4 | Bullet | 8 | 11 | 140 | Bomb_Secondary: 4; EMP: 3 (shield break 0.4) |

### Shotgun — 12 Gauge kwazaari slug

| Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Speed | Secondary Damage |
| --- | --- | --- | --- | --- | --- | --- |
| Std | 3 | Bullet | 14 | 61.5 | 220 | Bomb_Secondary: 5 |
| Inc | 3 | Bullet | 14 | 61.5 | 220 | Bomb_Secondary: 3; Flame_Secondary: 2 |
| EMP | 3 | Bullet | 14 | 61.5 | 220 | Bomb_Secondary: 3; EMP: 2 (shield break 0.4) |

---

## Tier II — Laser (Kwazaari Less-Lethal Beam, in `xcom_laser.xml`)

Non-lethal/utility beams. `dmgDef` is `Stun`, `Neuralizer`, or `Bullet` (for the EMP beam); no armor penetration is defined for the Stun/Neuralizer beams.

| Weapon | Stun beam | Neuralizer beam | EMP beam (dmg / AP Sharp+Blunt / secondary) | Notes |
| --- | --- | --- | --- | --- |
| Rifle | 8 dmg | 2 dmg | 8 dmg / 0 / EMP: 15 (shield break 0.4) | — |
| Sniper Rifle | 20 dmg | 4 dmg | 15 dmg / 0 / EMP: 30 (shield break 0.4) | — |
| Shotgun | 5 dmg | 1 dmg | 5 dmg / 0 / EMP: 10 (shield break 0.4) | 10 pellets on all 3 |
| Cannon | 7 dmg | 2 dmg | 7 dmg / 0 / EMP: 13 (shield break 0.4) | — |
| Pistol | 5 dmg | 1 dmg | 5 dmg / 0 / EMP: 9 (shield break 0.4) | — |

---

## Tier III — Ballistic (Gen III Charged/Neutronium, `base/gen3/gen_3_bullet.xml`)

Two parallel families share the same ammo-role naming (N / AP-N / Ion-N) but different damage scaling:
- **`BOR_Bullet_Charged_III_*`** — ~15% reduced damage, intended for vanilla CE charged guns (per in-file comment).
- **`BOR_Bullet_Gen3*`** — full/original stats, used by BOR's own Gen III guns.

All use `damageDef=TenebralBullet` (except the HE slug variants, which use `Bomb`) and `speed=2000`.

### Pistol — 6x18mm Charged

| Family | Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Secondary Damage |
| --- | --- | --- | --- | --- | --- | --- |
| Charged_III (−15%) | N | 1 | TenebralBullet | 57.6 | 17.28 | Bomb_Secondary: 26 |
| Charged_III (−15%) | AP-N | 1 | TenebralBullet | 115 | 34 | Bomb_Secondary: 13 |
| Charged_III (−15%) | Ion-N | 1 | TenebralBullet | 57.6 | 17.28 | EMP: 43 (shield break 2) |
| Gen3 (full) | N | 1 | TenebralBullet | 57.6 | 17.28 | Bomb_Secondary: 30 |
| Gen3 (full) | AP-N | 1 | TenebralBullet | 115 | 34 | Bomb_Secondary: 15 |
| Gen3 (full) | Ion-N | 1 | TenebralBullet | 57.6 | 17.28 | EMP: 50 (shield break 2) |

### Rifle — 6x24mm Charged

| Family | Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Secondary Damage |
| --- | --- | --- | --- | --- | --- | --- |
| Charged_III (−15%) | N | 26 | TenebralBullet | 110 | 240 | EMP: 43 |
| Charged_III (−15%) | AP-N (Concentrated Neutronium) | 13 | TenebralBullet | 220 | 420 | — |
| Charged_III (−15%) | Ion-N | 26 | TenebralBullet | 110 | 240 | EMP: 128 (shield break 5) |
| Gen3 (full) | N | 30 | TenebralBullet | 110 | 240 | EMP: 50 |
| Gen3 (full) | AP-N | 15 | TenebralBullet | 220 | 420 | — |
| Gen3 (full) | Ion-N | 30 | TenebralBullet | 110 | 240 | EMP: 150 (shield break 5) |

### Heavy — 12x72mm Charged

| Family | Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Secondary Damage |
| --- | --- | --- | --- | --- | --- | --- |
| Charged_III (−15%) | N | 26 | TenebralBullet | 110 | 240 | EMP: 43 |
| Charged_III (−15%) | AP-N (Concentrated Neutronium) | 128 | TenebralBullet | 110 | 240 | Electrical: 255 |
| Charged_III (−15%) | Ion-N | 43 | TenebralBullet | 110 | 240 | EMP: 255 (shield break 10) |
| Gen3 (full) | N | 30 | TenebralBullet | 110 | 240 | EMP: 50 |
| Gen3 (full) | AP-N | 150 | TenebralBullet | 110 | 240 | Electrical: 300 |
| Gen3 (full) | Ion-N | 50 | TenebralBullet | 110 | 240 | EMP: 300 (shield break 10) |

### Shotgun — 12 Gauge Charge Shot

| Family | Ammo | Damage | Dmg Type | AP Sharp | AP Blunt | Secondary Damage | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Charged_III (−15%) | N (pellet) | 26 | TenebralBullet | 110 | 240 | EMP: 43 | 3 pellets, spread 4 |
| Charged_III (−15%) | Slug (HE Shell) | 85 | Bomb | 110 | 240 | Flame_Secondary: 43 | single slug |
| Charged_III (−15%) | Ion-N (pellet) | 26 | TenebralBullet | 110 | 240 | EMP: 128 | 3 pellets, spread 4; shield break 5 |
| Gen3 (full) | N (pellet) | 30 | TenebralBullet | 110 | 240 | EMP: 50 | 3 pellets, spread 4 |
| Gen3 (full) | Slug (HE) | 100 | Bomb | 110 | 240 | Flame_Secondary: 50 | single slug |
| Gen3 (full) | Ion-N (pellet) | 30 | TenebralBullet | 110 | 240 | EMP: 150 | 3 pellets, spread 4; shield break 5 |

---

## Tier III — Plasma Beam (`additions/xcom_plasma.xml`)

4 material variants per weapon: **Quasar Alloy**, **Sunsteel**, **Tenebrite**, **Neutronium**. All use `damageDef=XCOM_Plasma` and inherit `speed` from their laser-color `ParentName` (not overridden). Every variant carries a `Flame_Secondary` burn.

### Rifle

| Material | Damage | AP Sharp | AP Blunt | Secondary Damage |
| --- | --- | --- | --- | --- |
| Quasar Alloy | 18 | 41.88 | 83.15 | Flame_Secondary: 35 |
| Sunsteel | 18 | 13.75 | 5.43 | Flame_Secondary: 33 |
| Tenebrite | 19 | 100 | 148.5 | Flame_Secondary: 35 |
| Neutronium | 13 | 27.92 | 55.43 | Flame_Secondary: 20 |

### Sniper Rifle

| Material | Damage | AP Sharp | AP Blunt | Secondary Damage |
| --- | --- | --- | --- | --- |
| Quasar Alloy | 38 | 63 | 130 | Flame_Secondary: 50 |
| Sunsteel | 35 | 18 | 88 | Flame_Secondary: 46 |
| Tenebrite | 54 | 169 | 236 | Flame_Secondary: 40 |
| Neutronium | 25 | 42 | 88 | Flame_Secondary: 33 |

### Shotgun

| Material | Damage | AP Sharp | AP Blunt | Secondary Damage | Notes |
| --- | --- | --- | --- | --- | --- |
| Quasar Alloy | 8 | 27 | 27 | Flame_Secondary: 10 | 10 pellets, spread 14.80 |
| Sunsteel | 8 | 8.75 | 17.68 | Flame_Secondary: 10 | 18 pellets, spread 8 |
| Tenebrite | 19 | 81 | 47.25 | Flame_Secondary: 15 | 5 pellets, spread 5 |
| Neutronium | 8 | 18 | 18 | Flame_Secondary: 7 | 7 pellets, spread 12 |

### Pistol

| Material | Damage | AP Sharp | AP Blunt | Secondary Damage |
| --- | --- | --- | --- | --- |
| Quasar Alloy | 13 | 33 | 43.75 | Flame_Secondary: 15 |
| Sunsteel | 13 | 10 | 29 | Flame_Secondary: 13 |
| Tenebrite | 13 | 81 | 81 | Flame_Secondary: 13 |
| Neutronium | 8 | 21 | 29 | Flame_Secondary: 10 |

*Note: Plasma only has 4 weapon families (Rifle/Sniper Rifle/Shotgun/Pistol) — no Cannon/Bullpup/DMR plasma bullets exist.*

---

## Charged Ammunition Compatibility Layer (`additions/charged.xml`)

This file defines its own `BOR_Bullet_Charged*_N` projectiles (table below), but its own `BOR_AmmoSet_Charged_*` `AmmoSetDef`s actually point at the Tier III `BOR_Bullet_Charged_III_*` bullets documented above instead of these — the bullets below are only wired up via the (separately named, duplicate-defName) ammo sets in `newammo.xml`. Included here for completeness since this is where their damage data lives.

| Ammo | Damage | AP Sharp | AP Blunt | Secondary Damage | Notes |
| --- | --- | --- | --- | --- | --- |
| Pistol N (6x18mm) | 13 | 13 | 18 | Bomb_Secondary: 8 | — |
| Pistol AP-N (Concentrated Neutronium) | 10 | 30 | 18 | Bomb_Secondary: 2 | — |
| Pistol Ion-N | 10 | 20 | 18 | EMP: 8 | shield break 0.33 |
| Rifle N (6x24mm) | 17 | 18 | 31 | Bomb_Secondary: 8 | — |
| Rifle AP-N (Concentrated Neutronium) | 13 | 38 | 27 | Bomb_Secondary: 3 | — |
| Rifle Ion-N | 13 | 28 | 31 | EMP: 10 | shield break 0.33 |
| Heavy N (12x72mm) | 48 | 38 | 375 | Bomb_Secondary: 20 | — |
| Heavy AP-N (Concentrated Neutronium) | 38 | 75 | 392 | Bomb_Secondary: 8 | — |
| Heavy Ion-N | 38 | 56 | 392 | EMP: 30 | shield break 1 |
| Shotgun N (pellet) | 13 | 10 | 18 | Bomb_Secondary: 8 | 9 pellets, spread 6.60 |
| Shotgun Slug (Quasar Alloy Slug) | 30 | 13 | 113 | Bomb_Secondary: 20 | single slug |
| Shotgun Ion-N (pellet) | 10 | 10 | 18 | EMP: 8 | 9 pellets, spread 11; shield break 0.06 |

*`dmgDef` is not overridden for any bullet in this file — inherited from `Base6x18mmChargedBullet` / `Base6x24mmChargedBullet` / `Base12x72mmChargedBullet` / `Base12GaugeChargedBullet`.*
