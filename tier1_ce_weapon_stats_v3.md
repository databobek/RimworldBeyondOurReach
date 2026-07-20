# Tier 1 CE Weapon Stats — v3 (Role Differentiation + Per-Weapon Calibers)

Builds on [tier1_ce_weapon_stats_v2.md](tier1_ce_weapon_stats_v2.md). Files in scope:
- `1.6/Mods/CombatExtended/Patches/base/gen1/gen_1_ammo.xml` (ballistic AmmoSets/AmmoDefs)
- `1.6/Mods/CombatExtended/Patches/base/CE_Gun_patches/gun_gen_1.xml` (ballistic gun Properties/AmmoUser/accuracy)
- `1.6/Mods/CombatExtended/Patches/additions/xcom_mag.xml` (gauss AmmoSets + gun Properties)
- `1.6/Mods/CombatExtended/Patches/additions/xcom_laser.xml` (laser AmmoSets + gun ThingDefs)

**Scope**: Ballistic keeps its current 5 weapons (AR, Sniper, LMG, ChainShotgun, Autopistol) — no ballistic DMR/Bullpup exist as ThingDefs/art, so none are added. Gauss and Laser keep all 7 (Rifle, SniperRifle, Cannon, Pistol, Bullpup, DMR, Shotgun). Ammo secondary-damage role scheme (FSJ/QaP/Ni/Nc/TC/EMP_N, gauss ×0.67 dmg/×0.67 sharp/×1.33 blunt derivation, laser real-scale Solar/IR/UV/EM) is unchanged from v2.

**Baseline correction**: v2's Range/Magazine/Burst/Accuracy numbers were never actually applied to the real verb-level XML (only ammo Damage/ArmorPen were). Every real ballistic weapon currently deals identical FSJ damage (20), all 7 real gauss weapons deal identical SS damage (13), and laser Rifle/Bullpup/DMR literally share one ammoset/damage. v3 uses the **real current verb-level values** as the baseline, not v2's stated numbers. Also fixes a real bug: `BOR_Gun_LMG_TI` range was 155 (longer than Sniper's 90) — now set to AR's range per the LMG=AR-range rule below.

**Method**: each weapon's ammo table is v2's per-weapon-family FSJ:QaP:Ni:Nc:TC:EMP_N (and gauss/laser equivalents) ratios, scaled by a single per-weapon damage/pen multiplier from the role formula sheet below. This preserves existing ammo-type proportions while hitting the new per-role damage targets. All damage/pen floored to int; Range/Magazine/Burst rounded to sensible values.

## Weapon-role formula sheet

D/R/M/B = that category's own Assault Rifle baseline Damage/Range/Magazine/Burst.

| Role | Damage | Range | Magazine | Burst | Accuracy (SightsEff / ShotSpread / Sway) |
| --- | --- | --- | --- | --- | --- |
| Assault Rifle (baseline) | D | R | M | B | baseline |
| Sniper Rifle | 5×D | 1.6×R | 0.6×M | 1 (no burst) | 2.5×S / 0.5×P / 1.5×W |
| DMR (gauss/laser only) | 1.4×D | 1.3×R | 0.6×M | 2 | 1.7×S / 0.65×P / 1.4×W |
| Bullpup (gauss/laser only) | 0.85×D | 0.65×R | 1.5×M | 1.5×B | 0.9×S / 1.5×P / 1.3×W |
| LMG (ballistic) / Cannon (gauss/laser, same role) | 0.85×D | 1×R (= AR) | 2×M | 2.5×B | 0.7×S / 1.6×P / 3.0×W |
| Shotgun | slug ammo ×5 (sniper-tier alpha); pellet ammo unscaled (already sniper-tier via pellet count) | 0.5×R | 0.5×M | unchanged | unchanged (already worst-tier) |
| Pistol/Autopistol | 0.5×D (lowest) | 0.4×R (lowest) | 0.5×M (lowest) | unchanged | unchanged (already worst-tier) |

Rank orders preserved in every category: Range Pistol < Shotgun < Bullpup < AR = LMG/Cannon < DMR < Sniper. Magazine Pistol ≤ Shotgun < DMR ≈ Sniper < AR < Bullpup < LMG/Cannon. Damage Pistol < Bullpup ≈ LMG/Cannon < AR < DMR < Sniper ≈ Shotgun(alpha).

## Ballistic

| Weapon | Ammo type | Damage | Armor penetration | Range | Magazine | Burst | Ticks |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `BOR_Gun_AssaultRifle_TI` (`BOR_AmmoSet_Gen1_Rifle`) | FSJ | 20 | 48 sharp / 110 blunt | 66.0 | 32 | 8 | 3 |
| `BOR_Gun_AssaultRifle_TI` | QaP | 14 | 72 / 110 | 66.0 | 32 | 8 | 3 |
| `BOR_Gun_AssaultRifle_TI` | Ni | 12 | 55 / 95 | 66.0 | 32 | 8 | 3 |
| `BOR_Gun_AssaultRifle_TI` | Nc | 14 | 72 / 110 | 66.0 | 32 | 8 | 3 |
| `BOR_Gun_AssaultRifle_TI` | TC | 15 | 108 / 154 | 66.0 | 32 | 8 | 3 |
| `BOR_Gun_AssaultRifle_TI` | EMP_N | 12 | 48 / 110 | 66.0 | 32 | 8 | 3 |
| `BOR_Gun_SniperRifle_TI` (`BOR_AmmoSet_Gen1_SniperRifle`, NEW) | FSJ | 100 | 240 / 550 | 105.6 | 19 | 1 | 0 |
| `BOR_Gun_SniperRifle_TI` | QaP | 70 | 360 / 550 | 105.6 | 19 | 1 | 0 |
| `BOR_Gun_SniperRifle_TI` | Ni | 60 | 275 / 475 | 105.6 | 19 | 1 | 0 |
| `BOR_Gun_SniperRifle_TI` | Nc | 70 | 360 / 550 | 105.6 | 19 | 1 | 0 |
| `BOR_Gun_SniperRifle_TI` | TC | 75 | 540 / 770 | 105.6 | 19 | 1 | 0 |
| `BOR_Gun_SniperRifle_TI` | EMP_N | 60 | 240 / 550 | 105.6 | 19 | 1 | 0 |
| `BOR_Gun_LMG_TI` (`BOR_AmmoSet_Gen1_LMG`, NEW) | FSJ | 17 | 40 / 93 | 66.0 | 64 | 20 | 6 |
| `BOR_Gun_LMG_TI` | QaP | 11 | 61 / 93 | 66.0 | 64 | 20 | 6 |
| `BOR_Gun_LMG_TI` | Ni | 10 | 46 / 80 | 66.0 | 64 | 20 | 6 |
| `BOR_Gun_LMG_TI` | Nc | 11 | 61 / 93 | 66.0 | 64 | 20 | 6 |
| `BOR_Gun_LMG_TI` | TC | 12 | 91 / 130 | 66.0 | 64 | 20 | 6 |
| `BOR_Gun_LMG_TI` | EMP_N | 10 | 40 / 93 | 66.0 | 64 | 20 | 6 |
| `BOR_Gun_ChainShotgun_TI` (`BOR_AmmoSet_Gen1_Shotgun`) | FSJ (slug) | 60 | 215 / 465 | 33.0 | 16 | 4 | 0 |
| `BOR_Gun_ChainShotgun_TI` | QaP | 30 (9 pellets) | 48 / 103 | 33.0 | 16 | 4 | 0 |
| `BOR_Gun_ChainShotgun_TI` | Ni | 30 (9 pellets) | 41 / 89 | 33.0 | 16 | 4 | 0 |
| `BOR_Gun_ChainShotgun_TI` | Nc | 30 (9 pellets) | 48 / 103 | 33.0 | 16 | 4 | 0 |
| `BOR_Gun_ChainShotgun_TI` | TC (slug) | 100 | 360 / 775 | 33.0 | 16 | 4 | 0 |
| `BOR_Gun_ChainShotgun_TI` | EMP_N | 30 (9 pellets) | 48 / 103 | 33.0 | 16 | 4 | 0 |
| `BOR_Gun_Autopistol_TI` (`BOR_AmmoSet_Gen1_Pistol`) | FSJ | 10 | 24 / 55 | 26.4 | 16 | 4 | 5 |
| `BOR_Gun_Autopistol_TI` | QaP | 7 | 36 / 55 | 26.4 | 16 | 4 | 5 |
| `BOR_Gun_Autopistol_TI` | Ni | 6 | 27 / 47 | 26.4 | 16 | 4 | 5 |
| `BOR_Gun_Autopistol_TI` | Nc | 7 | 36 / 55 | 26.4 | 16 | 4 | 5 |
| `BOR_Gun_Autopistol_TI` | TC | 7 | 54 / 77 | 26.4 | 16 | 4 | 5 |
| `BOR_Gun_Autopistol_TI` | EMP_N | 6 | 24 / 55 | 26.4 | 16 | 4 | 5 |

**Ballistic accuracy**

| Weapon | SightsEfficiency | ShotSpread | SwayFactor |
| --- | --- | --- | --- |
| `BOR_Gun_AssaultRifle_TI` | 1.32 | 0.064 | 0.96 |
| `BOR_Gun_SniperRifle_TI` | 3.30 | 0.032 | 1.44 |
| `BOR_Gun_LMG_TI` | 0.92 | 0.102 | 2.88 |
| `BOR_Gun_ChainShotgun_TI` | 1.20 | 0.112 | 1.00 |
| `BOR_Gun_Autopistol_TI` | 0.79 | 0.220 | 1.54 |

## Gauss

| Weapon | Ammo type | Damage | Armor penetration | Range | Magazine | Burst | Ticks |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `BOR_Gun_GaussRifle_TI` (`AmmoSet_6mmRailgunXCOM`, now Rifle-only) | SS | 13 | 32 / 146 | 55.0 | 30 | 6 | 6 |
| `BOR_Gun_GaussRifle_TI` | Ni | 8 | 36 / 128 | 55.0 | 30 | 6 | 6 |
| `BOR_Gun_GaussRifle_TI` | TC | 10 | 72 / 204 | 55.0 | 30 | 6 | 6 |
| `BOR_Gun_GaussRifle_TI` | Nc | 9 | 48 / 146 | 55.0 | 30 | 6 | 6 |
| `BOR_Gun_GaussRifle_TI` | EMP_N | 8 | 32 / 146 | 55.0 | 30 | 6 | 6 |
| `BOR_Gun_GaussSniperRifle_TI` (`AmmoSet_8mmRailgunXCOM`, now SniperRifle-only) | SS | 65 | 160 / 730 | 88.0 | 18 | 1 | 0 |
| `BOR_Gun_GaussSniperRifle_TI` | Ni | 40 | 180 / 640 | 88.0 | 18 | 1 | 0 |
| `BOR_Gun_GaussSniperRifle_TI` | TC | 50 | 360 / 1020 | 88.0 | 18 | 1 | 0 |
| `BOR_Gun_GaussSniperRifle_TI` | Nc | 45 | 240 / 730 | 88.0 | 18 | 1 | 0 |
| `BOR_Gun_GaussSniperRifle_TI` | EMP_N | 40 | 160 / 730 | 88.0 | 18 | 1 | 0 |
| `BOR_Gun_GaussCannon_TI` (`AmmoSet_8mmRailgunXCOM_Cannon`, NEW) | SS | 11 | 27 / 124 | 55.0 | 60 | 15 | 6 |
| `BOR_Gun_GaussCannon_TI` | Ni | 6 | 30 / 108 | 55.0 | 60 | 15 | 6 |
| `BOR_Gun_GaussCannon_TI` | TC | 8 | 61 / 173 | 55.0 | 60 | 15 | 6 |
| `BOR_Gun_GaussCannon_TI` | Nc | 7 | 40 / 124 | 55.0 | 60 | 15 | 6 |
| `BOR_Gun_GaussCannon_TI` | EMP_N | 6 | 27 / 124 | 55.0 | 60 | 15 | 6 |
| `BOR_Gun_GaussBullpup_TI` (`AmmoSet_6mmRailgunXCOM_Bullpup`, NEW) | SS | 11 | 27 / 124 | 35.8 | 45 | 9 | 6 |
| `BOR_Gun_GaussBullpup_TI` | Ni | 6 | 30 / 108 | 35.8 | 45 | 9 | 6 |
| `BOR_Gun_GaussBullpup_TI` | TC | 8 | 61 / 173 | 35.8 | 45 | 9 | 6 |
| `BOR_Gun_GaussBullpup_TI` | Nc | 7 | 40 / 124 | 35.8 | 45 | 9 | 6 |
| `BOR_Gun_GaussBullpup_TI` | EMP_N | 6 | 27 / 124 | 35.8 | 45 | 9 | 6 |
| `BOR_Gun_GaussDMR_TI` (`AmmoSet_6mmRailgunXCOM_DMR`, NEW) | SS | 18 | 44 / 204 | 71.5 | 18 | 2 | 4 |
| `BOR_Gun_GaussDMR_TI` | Ni | 11 | 50 / 179 | 71.5 | 18 | 2 | 4 |
| `BOR_Gun_GaussDMR_TI` | TC | 14 | 100 / 285 | 71.5 | 18 | 2 | 4 |
| `BOR_Gun_GaussDMR_TI` | Nc | 12 | 67 / 204 | 71.5 | 18 | 2 | 4 |
| `BOR_Gun_GaussDMR_TI` | EMP_N | 11 | 44 / 204 | 71.5 | 18 | 2 | 4 |
| `BOR_Gun_GaussPistol_TI` (`AmmoSet_4mmRailgunXCOM`) | SS | 6 | 10 / 14 | 22.0 | 15 | 1 | 0 |
| `BOR_Gun_GaussPistol_TI` | Ni | 5 | 14 / 12 | 22.0 | 15 | 1 | 0 |
| `BOR_Gun_GaussPistol_TI` | TC | 5 | 31 / 20 | 22.0 | 15 | 1 | 0 |
| `BOR_Gun_GaussPistol_TI` | Nc | 4 | 20 / 14 | 22.0 | 15 | 1 | 0 |
| `BOR_Gun_GaussPistol_TI` | EMP_N | 5 | 9 / 14 | 22.0 | 15 | 1 | 0 |
| `BOR_Gun_GaussShotgun_TI` (`AmmoSet_ScatterRailgunXCOM`) | SS (slug) | 35 | 140 / 615 | 27.5 | 15 | 1 | 0 |
| `BOR_Gun_GaussShotgun_TI` | Ni (slug) | 100 | 140 / 600 | 27.5 | 15 | 1 | 0 |
| `BOR_Gun_GaussShotgun_TI` | TC (slug) | 65 | 240 / 1030 | 27.5 | 15 | 1 | 0 |
| `BOR_Gun_GaussShotgun_TI` | Nc (slug) | 100 | 160 / 680 | 27.5 | 15 | 1 | 0 |
| `BOR_Gun_GaussShotgun_TI` | EMP_N (slug) | 100 | 160 / 680 | 27.5 | 15 | 1 | 0 |

**Gauss accuracy**

| Weapon | SightsEfficiency | ShotSpread | SwayFactor |
| --- | --- | --- | --- |
| `BOR_Gun_GaussRifle_TI` | 1.10 | 0.060 | 1.26 |
| `BOR_Gun_GaussSniperRifle_TI` | 2.75 | 0.030 | 1.89 |
| `BOR_Gun_GaussCannon_TI` | 0.77 | 0.096 | 3.78 |
| `BOR_Gun_GaussBullpup_TI` | 0.99 | 0.090 | 1.64 |
| `BOR_Gun_GaussDMR_TI` | 1.87 | 0.039 | 1.76 |
| `BOR_Gun_GaussPistol_TI` | 0.70 | 0.190 | 0.72 |
| `BOR_Gun_GaussShotgun_TI` | 0.70 | 0.150 | 1.32 |

## Laser

| Weapon | Ammo type | Damage | Armor penetration | Range | Magazine | Burst | Ticks |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `BOR_Gun_LaserRifle_TI` (`BOR_SolarLaser_Rifle`, now Rifle-only) | Solar | 44 | 55 / 0 | 38.0 | 20 | 5 | 2 |
| `BOR_Gun_LaserRifle_TI` | IR | 26 | 36 / 0 | 38.0 | 20 | 5 | 2 |
| `BOR_Gun_LaserRifle_TI` | UV | 55 | 29 / 0 | 38.0 | 20 | 5 | 2 |
| `BOR_Gun_LaserRifle_TI` | EM | 13 | 21 / 0 | 38.0 | 20 | 5 | 2 |
| `BOR_Gun_LaserSniperRifle_TI` (`BOR_SolarLaser_Sniper`) | Solar | 220 | 275 / 0 | 60.8 | 12 | 1 | 0 |
| `BOR_Gun_LaserSniperRifle_TI` | IR | 130 | 180 / 0 | 60.8 | 12 | 1 | 0 |
| `BOR_Gun_LaserSniperRifle_TI` | UV | 275 | 145 / 0 | 60.8 | 12 | 1 | 0 |
| `BOR_Gun_LaserSniperRifle_TI` | EM | 65 | 105 / 0 | 60.8 | 12 | 1 | 0 |
| `BOR_Gun_LaserCannon_TI` (`BOR_SolarLaser_Cannon`) | Solar | 37 | 46 / 0 | 38.0 | 40 | 13 | 7 |
| `BOR_Gun_LaserCannon_TI` | IR | 22 | 30 / 0 | 38.0 | 40 | 13 | 7 |
| `BOR_Gun_LaserCannon_TI` | UV | 46 | 24 / 0 | 38.0 | 40 | 13 | 7 |
| `BOR_Gun_LaserCannon_TI` | EM | 11 | 17 / 0 | 38.0 | 40 | 13 | 7 |
| `BOR_Gun_LaserBullpup_TI` (`BOR_SolarLaser_Bullpup`, NEW) | Solar | 37 | 46 / 0 | 24.7 | 30 | 8 | 7 |
| `BOR_Gun_LaserBullpup_TI` | IR | 22 | 30 / 0 | 24.7 | 30 | 8 | 7 |
| `BOR_Gun_LaserBullpup_TI` | UV | 46 | 24 / 0 | 24.7 | 30 | 8 | 7 |
| `BOR_Gun_LaserBullpup_TI` | EM | 11 | 17 / 0 | 24.7 | 30 | 8 | 7 |
| `BOR_Gun_LaserDMR_TI` (`BOR_SolarLaser_DMR`, NEW) | Solar | 61 | 77 / 0 | 49.4 | 12 | 2 | 4 |
| `BOR_Gun_LaserDMR_TI` | IR | 36 | 50 / 0 | 49.4 | 12 | 2 | 4 |
| `BOR_Gun_LaserDMR_TI` | UV | 77 | 40 / 0 | 49.4 | 12 | 2 | 4 |
| `BOR_Gun_LaserDMR_TI` | EM | 18 | 29 / 0 | 49.4 | 12 | 2 | 4 |
| `BOR_Gun_LaserPistol_TI` (`BOR_SolarLaser_Pistol`) | Solar | 22 | 13 / 0 | 15.2 | 10 | 1 | 0 |
| `BOR_Gun_LaserPistol_TI` | IR | 13 | 8 / 0 | 15.2 | 10 | 1 | 0 |
| `BOR_Gun_LaserPistol_TI` | UV | 27 | 8 / 0 | 15.2 | 10 | 1 | 0 |
| `BOR_Gun_LaserPistol_TI` | EM | 6 | 9 / 0 | 15.2 | 10 | 1 | 0 |
| `BOR_Gun_LaserShotgun_TI` (`BOR_SolarLaser_Shotgun`) | Solar | 25 (10 pellets) | 18 / 0 | 19.0 | 10 | 1 | 0 |
| `BOR_Gun_LaserShotgun_TI` | IR | 15 (10 pellets) | 12 / 0 | 19.0 | 10 | 1 | 0 |
| `BOR_Gun_LaserShotgun_TI` | UV | 31 (7 pellets) | 11 / 0 | 19.0 | 10 | 1 | 0 |
| `BOR_Gun_LaserShotgun_TI` | EM | 10 (10 pellets) | 12 / 0 | 19.0 | 10 | 1 | 0 |

**Laser accuracy**

| Weapon | SightsEfficiency | ShotSpread | SwayFactor |
| --- | --- | --- | --- |
| `BOR_Gun_LaserRifle_TI` | 0.92 | 0.090 | 1.49 |
| `BOR_Gun_LaserSniperRifle_TI` | 2.30 | 0.045 | 2.235 |
| `BOR_Gun_LaserCannon_TI` | 0.644 | 0.144 | 4.47 |
| `BOR_Gun_LaserBullpup_TI` | 0.828 | 0.135 | 1.937 |
| `BOR_Gun_LaserDMR_TI` | 1.564 | 0.0585 | 2.086 |
| `BOR_Gun_LaserPistol_TI` | 0.67 | 0.220 | 1.29 |
| `BOR_Gun_LaserShotgun_TI` | 0.83 | 0.550 | 1.39 |

## Caliber split (old shared AmmoSet → new per-weapon AmmoSets)

| Category | Old shared AmmoSet | Weapons that shared it | New assignment |
| --- | --- | --- | --- |
| Ballistic | `BOR_AmmoSet_Gen1_Heavy` | SniperRifle, LMG | `BOR_AmmoSet_Gen1_SniperRifle` (NEW), `BOR_AmmoSet_Gen1_LMG` (NEW) |
| Gauss | `AmmoSet_6mmRailgunXCOM` | Rifle, Bullpup, DMR | Rifle keeps it; `AmmoSet_6mmRailgunXCOM_Bullpup` (NEW), `AmmoSet_6mmRailgunXCOM_DMR` (NEW) |
| Gauss | `AmmoSet_8mmRailgunXCOM` | SniperRifle, Cannon | SniperRifle keeps it; `AmmoSet_8mmRailgunXCOM_Cannon` (NEW) |
| Laser | `BOR_SolarLaser_Rifle` | Rifle, Bullpup, DMR | Rifle keeps it; `BOR_SolarLaser_Bullpup` (NEW), `BOR_SolarLaser_DMR` (NEW) |

7 new AmmoSetDefs total (2 ballistic + 3 gauss + 2 laser). Each needs full ammo-type parity: 6 types for ballistic (FSJ/QaP/Ni/Nc/TC/EMP_N), 5 for gauss (SS/Ni/TC/Nc/EMP_N), 4 for laser (Solar/IR/UV/EM) — new AmmoDef + bullet ThingDef pairs, ~35 pairs combined. `AmmoSet_4mmRailgunXCOM` (gauss Pistol), `AmmoSet_ScatterRailgunXCOM` (gauss Shotgun), `BOR_AmmoSet_Gen1_Rifle`/`_Shotgun`/`_Pistol` (ballistic), and `BOR_SolarLaser_Sniper`/`_Cannon`/`_Pistol`/`_Shotgun` (laser) were already exclusive to one weapon and need no change.

1. Armor penetration should not be rescaled
2. Laser Assault Rifle previously needs to have burst now
3. Ballistic/Gauss shotgun rule ("shotgun = sniper-tier damage") was only applied to slug ammo types (FSJ/TC for ballistic, all of gauss since gauss shotgun is fully slug-based); pellet-based ammo types (QaP/Ni/Nc/EMP_N) were left unscaled since pellet multiplicity already puts their total alpha strike at or above sniper tier.
4. New AmmoSetDef names should be prefixed with BOR_
5. Gauss/Laser Pistol and Shotgun accuracy values were left at their real numbers.
