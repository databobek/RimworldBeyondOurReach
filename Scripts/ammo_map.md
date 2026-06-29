# BOR Weapon → Ammo Map (CE)

## Legend
| Abbrev | Full Name |
|--------|-----------|
| FSJ | Fin-Stabilized Jacket |
| QaP | Quasar Penetrator |
| Nc | Neutronium-Coated |
| Ni | Neutronium-Infused |
| TC | Tenebrite-Coated |
| N | Normal (FSJ equivalent, Gen3) |
| AP_N | Armor-Piercing Neutronium |
| Ion_N | Ionized Neutronium |
| Solar | Solar Lens |
| IR | Infrared (Incendiary) |
| UV | Ultraviolet (HP) |
| EM | EM Frequency (EMP) |
| QA | QuasarAlloy |
| SS | Sunsteel |
| Ten | Tenebrite |
| Neu | Neutronium |

---

## Gen 1 — Tier I BOR Weapons

| Weapon | defName | AmmoSet | Ammo Variants |
|--------|---------|---------|---------------|
| Assault Rifle | `BOR_Gun_AssaultRifle_TI` | `BOR_AmmoSet_Gen1_Rifle` | FSJ, QaP, Ni, TC |
| Sniper Rifle | `BOR_Gun_SniperRifle_TI` | `BOR_AmmoSet_Gen1_Heavy` | FSJ, QaP, Ni, TC |
| Chain Shotgun | `BOR_Gun_ChainShotgun_TI` | `BOR_AmmoSet_Gen1_Shotgun` | QaP, Ni, TC |
| LMG | `BOR_Gun_LMG_TI` | `BOR_AmmoSet_Gen1_Heavy` | FSJ, QaP, Ni, TC |
| Autopistol | `BOR_Gun_Autopistol_TI` | `BOR_AmmoSet_Gen1_Pistol` | FSJ, QaP, Ni, TC |

**Texture paths:** `BOR/Things/Item/Equipment/weaponranged/gen1/ammo/{rifle\|heavy\|pistol\|shotgun}/{variant}`

---

## Gen 2 — Tier II BOR Weapons

| Weapon | defName | AmmoSet | Ammo Variants |
|--------|---------|---------|---------------|
| Assault Rifle | `BOR_Gun_AssaultRifle_TII` | `BOR_AmmoSet_Gen2_Rifle` | QaP, Nc, Ni, TC |
| Sniper Rifle | `BOR_Gun_SniperRifle_TII` | `BOR_AmmoSet_Gen2_Heavy` | QaP, Nc, Ni, TC |
| Chain Shotgun | `BOR_Gun_ChainShotgun_TII` | `BOR_AmmoSet_Gen2_Shotgun` | Nc, Ni, TC |
| LMG | `BOR_Gun_LMG_TII` | `BOR_AmmoSet_Gen2_Heavy` | QaP, Nc, Ni, TC |
| Autopistol | `BOR_Gun_Autopistol_TII` | `BOR_AmmoSet_Gen2_Pistol` | QaP, Nc, Ni, TC |

**Texture paths:** `BOR/Things/Item/Equipment/weaponranged/gen2/ammo/{rifle\|heavy\|pistol\|shotgun}/{variant}`

---

## Gen 3 — Tier III BOR Weapons

| Weapon | defName | AmmoSet | Ammo Variants |
|--------|---------|---------|---------------|
| Assault Rifle | `BOR_Gun_AssaultRifle_TIII` | `BOR_AmmoSet_Gen3_rifle` | N, AP_N, Ion_N |
| Sniper Rifle | `BOR_Gun_SniperRifle_TIII` | `BOR_AmmoSet_Gen3_rifle` | N, AP_N, Ion_N |
| Chain Shotgun | `BOR_Gun_ChainShotgun_TIII` | `BOR_AmmoSet_Gen3_shotgun` | N, Slug, Ion_N |
| LMG | `BOR_Gun_LMG_TIII` | `BOR_AmmoSet_Gen3_lmg` | (see lmg set) |
| Autopistol | `BOR_Gun_Autopistol_TIII` | `BOR_AmmoSet_Gen3_pistol` | N, AP_N, Ion_N |

**Texture paths:** `BOR/Things/Item/Equipment/weaponranged/gen3/ammo/{rifle\|heavy\|pistol\|shotgun}/{variant}`

---

## Gauss / Magnetic — XCOM Weapons

| Weapon | defName (TI / TII) | AmmoSet | Ammo Variants | Texture Folder |
|--------|--------------------|---------|---------------|----------------|
| Gauss Pistol | `BOR_Gun_GaussPistol_TI/TII` | `AmmoSet_4mmRailgunXCOM` | TC, Nc, Ni, EMP_N | `ammo/magnetic/pistol` |
| Gauss Rifle | `BOR_Gun_GaussRifle_TI/TII` | `AmmoSet_6mmRailgunXCOM` | Nc, Ni, EMP_N | `ammo/magnetic/rifle` |
| Gauss Bullpup | `BOR_Gun_GaussBullpup_TI/TII` | `AmmoSet_6mmRailgunXCOM` | Nc, Ni, EMP_N | `ammo/magnetic/rifle` |
| Gauss DMR | `BOR_Gun_GaussDMR_TI/TII` | `AmmoSet_8mmRailgunXCOM` | Nc, Ni, EMP_N | `ammo/magnetic/rifle` |
| Gauss Sniper | `BOR_Gun_GaussSniperRifle_TI/TII` | `AmmoSet_8mmRailgunXCOM` | Nc, Ni, EMP_N | `ammo/magnetic/rifle` |
| Gauss Cannon | `BOR_Gun_GaussCannon_TI/TII` | `AmmoSet_8mmRailgunXCOM` | Nc, Ni, EMP_N | `ammo/magnetic/rifle` |
| Gauss Shotgun | `BOR_Gun_GaussShotgun_TI/TII` | `AmmoSet_ScatterRailgunXCOM` | TC (12G + 12mm) | `ammo/magnetic/shotgun` |

> ⚠️ All variants in each weapon class currently share the same texture. Separate per-variant subfolders needed if visual differentiation is desired.

---

## Laser — XCOM + BOR Solar Weapons

### Tier I (Solar Laser ammo: Solar, IR, UV, EM)

| Weapon | defName | AmmoSet | Ammo Variants |
|--------|---------|---------|---------------|
| Laser Rifle | `BOR_Gun_LaserRifle_TI` | `BOR_SolarLaser_Rifle` | Solar, IR, UV, EM |
| Laser Bullpup | `BOR_Gun_LaserBullpup_TI` | `BOR_SolarLaser_Rifle` | Solar, IR, UV, EM |
| Laser DMR | `BOR_Gun_LaserDMR_TI` | `BOR_SolarLaser_Rifle` | Solar, IR, UV, EM |
| Laser Sniper | `BOR_Gun_LaserSniperRifle_TI` | `BOR_SolarLaser_Sniper` | Solar, IR, UV, EM |
| Laser Shotgun | `BOR_Gun_LaserShotgun_TI` | `BOR_SolarLaser_Shotgun` | Solar, IR, UV, EM |
| Laser Cannon (LMG) | `BOR_Gun_LaserCannon_TI` | `BOR_SolarLaser_Cannon` | Solar, IR, UV, EM |
| Laser Pistol | `BOR_Gun_LaserPistol_TI` | `BOR_SolarLaser_Pistol` | Solar, IR, UV, EM |
| Solar Beam Rifle | `BOR_Gun_SolarBeamRifle` | `BOR_SolarLaser_Rifle` | Solar, IR, UV, EM |
| Solar Beam Bullpup | `BOR_Gun_SolarBeamBullpup` | `BOR_SolarLaser_Rifle` | Solar, IR, UV, EM |
| Solar Beam DMR | `BOR_Gun_SolarBeamDMR` | `BOR_SolarLaser_Sniper` | Solar, IR, UV, EM |
| Solar Beam Sniper | `BOR_Gun_SolarBeamSniperRifle` | `BOR_SolarLaser_Sniper` | Solar, IR, UV, EM |
| Solar Beam Shotgun | `BOR_Gun_SolarBeamShotgun` | `BOR_SolarLaser_Shotgun` | Solar, IR, UV, EM |
| Solar Beam LMG | `BOR_Gun_SolarBeamLMG` | `BOR_SolarLaser_Cannon` | Solar, IR, UV, EM |
| Solar Beam Pistol | `BOR_Gun_SolarBeamPistol` | `BOR_SolarLaser_Pistol` | Solar, IR, UV, EM |

### Tier II (Quasar Laser ammo: Solar, IR, UV, EM)

| Weapon | defName | AmmoSet |
|--------|---------|---------|
| Solar Beam Rifle TII | `BOR_Gun_SolarBeamRifle_TII` | `BOR_QuasarLaser_Rifle` |
| Solar Beam Bullpup TII | `BOR_Gun_SolarBeamBullpup_TII` | `BOR_QuasarLaser_Rifle` |
| Solar Beam DMR TII | `BOR_Gun_SolarBeamDMR_TII` | `BOR_QuasarLaser_Sniper` |
| Solar Beam Sniper TII | `BOR_Gun_SolarBeamSniperRifle_TII` | `BOR_QuasarLaser_Sniper` |
| Solar Beam Shotgun TII | `BOR_Gun_SolarBeamShotgun_TII` | `BOR_QuasarLaser_Shotgun` |
| Solar Beam LMG TII | `BOR_Gun_SolarBeamLMG_TII` | `BOR_QuasarLaser_Cannon` |
| Solar Beam Pistol TII | `BOR_Gun_SolarBeamPistol_TII` | `BOR_QuasarLaser_Pistol` |

**Current texture paths:** `BOR/Things/Item/Equipment/weaponranged/ammo/laser/{standard\|infrared\|ultraviolet\|em}`
> ⚠️ These paths do NOT exist on disk — only `ammo/laser/{pistol\|rifle\|lmg\|shotgun}` folders exist. Needs resolution.

---

## Plasma — XCOM Beam Weapons

| Weapon class | AmmoSet (CE vanilla) | Ammo Variants | Texture Folder |
|-------------|----------------------|---------------|----------------|
| Beam Pistol | `AmmoSet_XCOM_Gun_BeamPistol` | QA, SS, Ten, Neu | `ammo/plasma/pistol` |
| Beam Rifle | `AmmoSet_XCOM_Gun_BeamRifle` | QA, SS, Ten, Neu | `ammo/plasma/rifle` |
| Beam Sniper | `AmmoSet_XCOM_Gun_BeamSniperRifle` | QA, SS, Ten, Neu | `ammo/plasma/rifle` |
| Beam Shotgun | `AmmoSet_XCOM_Gun_BeamShotgun` | QA, SS, Ten, Neu | `ammo/plasma/rifle` |
| Heavy Beam | — | QA, SS, Ten, Neu | `ammo/plasma/heavy` |

> ⚠️ Textures are placeholder copies from laser folder. No dedicated plasma textures yet.
