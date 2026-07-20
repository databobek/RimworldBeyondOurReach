# Tier 1 CE Weapon Stats — v2 (Rebalanced)
This table covers the plan for Tier 1 ranged weaponry defined in:
- `1.6/Mods/CombatExtended/Patches/base/gen1`
- `1.6/Mods/CombatExtended/Patches/base/CE_Gun_patches/gun_gen_1.xml`
- `1.6/Mods/CombatExtended/Patches/additions/xcom_laser.xml`
- `1.6/Mods/CombatExtended/Patches/additions/xcom_mag.xml`

This is a recomputed version of [tier1_ce_weapon_stats.md](tier1_ce_weapon_stats.md). The
ballistic table is the base (numbers floored to integers, range forced to `X.9` format).
Laser and Gauss tables are derived from it. See `Scripts/backup/tier1_ce_weapons_20260702/`
for backups of the real source files this will eventually be applied to (a fresh
`Scripts/backup/tier1_ce_weapons_20260703/` backup will be taken right before this v2 plan
is applied to the real XML).

## Ammo role scheme (naming/secondary-damage consistency pass)
This revision aligns ballistic, laser and gauss ammo naming/roles around the pattern already
used by the (correctly implemented) railgun ammo and already described by the existing
`AmmoCategoryDef` fluff text in `ammoclasses.xml`:
- **FSJ** (ballistic) / **SS "Solar Sabot"** (gauss) / **Solar** (laser) — baseline round, no secondary damage.
- **QaP** — quasar alloy-penetrating AP round, no secondary damage. (Ballistic/laser only; gauss does not get a QaP-equivalent per review.)
- **Nc** (ballistic/gauss) / **IR** (laser) — incendiary analog of real CE's AP-I: `secondaryDamage: Flame_Secondary`. Replaces the old placeholder "Incendiary" ballistic ammo (which was an exact QaP copy).
- **Ni** — HP/AP-HE hybrid analog of real CE's AP-HE: `secondaryDamage: Bomb_Secondary`, **and** armor penetration (sharp+blunt) reduced 20% below the average of that weapon's other ammo types (mimicking HP's lower penetration). Previously Ni incorrectly carried an EMP secondary — that effect moves to EMP_N.
- **TC** — Tenebrite-Coated Sabot analog of real CE's Sabot: plain `Bullet` damageDef (no longer `Bomb`/explosive), boosted sharp+blunt penetration and speed, no secondary damage. Gauss TC already matched this pattern; ballistic TC did not and is corrected here.
- **EMP_N** (ballistic/gauss) / **EM** (laser) — Ionized Neutronium: `secondaryDamage: EMP` (+ `empShieldBreakChance`). Replaces the old placeholder "EMP" ballistic ammo, inheriting the EMP secondary/amount that used to incorrectly live on Ni.
- Shotgun (ballistic and gauss) additionally gets an **FSJ/SS slug**: a single-projectile round (not pellets) at ~60% of that family's TC on **both** damage and armor penetration, reflecting cheaper solar-tier crafting material vs TC's tenebrite/quasar-alloy tier. Laser Shotgun stays pellet-based for all four ammo types (including Solar) at its existing real damage/pen values — laser doesn't have a slug/pellet distinction the way ballistic/gauss shotguns do.

## Rules applied
- Ballistic: armor pen floored to int; range floored then formatted as `X.9`.
- Laser: real implementation values are kept as-is for Solar/IR/EM (they are independently tuned, not derived from the ballistic table by a formula — see the correction note under the Laser table); only the secondary-damage roles are fixed (Solar loses an erroneous Flame_Secondary, UV/HP switches to Bomb_Secondary) and UV/HP's sharp penetration is reduced per the -20%-vs-average rule below. Blunt penetration is always 0 for laser in this mod.
- Gauss: damage -33% (×0.67), sharp penetration -33% (×0.67), blunt penetration +33% (×1.33), range +50% (all floored, `.9` kept). Ammo roles derive from the same-named ballistic role (SS←FSJ, Nc←Nc, TC←TC, EMP_N←EMP_N); **gauss Ni's -20% penetration reduction is computed against the average of the other *gauss* ammo types for that weapon**, not re-derived from ballistic Ni.
- Full ammo parity: every ballistic gun (including Chain Shotgun) now has 6 types (FSJ, QaP, Ni, Nc, TC, EMP_N); every gauss gun (including Gauss Shotgun) now has 5 types (SS, Ni, TC, Nc, EMP_N — no QaP); every laser gun keeps its original 4 types (Solar, IR, UV, EM).
- DMR = Assault Rifle damage base + Sniper Rifle range base (both families). Bullpup = Assault Rifle damage base + Chain Shotgun range base (both families). Cannon = LMG base directly.
- Nc's damage/pen values reuse QaP's numbers for that weapon (same convention the old placeholder "Incendiary" used); EMP_N's damage/pen values reuse Ni's *pre-rework* numbers for that weapon (same convention the old placeholder "EMP" used) — only the `secondaryDamage`/`ammoClass` differ between these pairs and their donors.
- TC (ballistic) proposed Sabot formula, pending your approval: `damage = floor(FSJ.damage × 0.75)`, `sharp = floor(QaP.sharp × 1.5)`, `blunt = floor(FSJ.blunt × 1.4)`. Gauss/laser TC-equivalents are then derived from this via the standard gauss/laser conversion rules above.
- Shotgun FSJ/SS slug formula: `damage = floor(TC.damage × 0.6)`, `sharp = floor(TC.sharp × 0.6)`, `blunt = floor(TC.blunt × 0.6)`, computed within the same family (ballistic slug from ballistic TC, gauss slug from gauss TC).

Ignore melee weapons.

## Ballistic (base)

| Weapon | Ammo type | Damage | Armor penetration | Range | Magazine | Burst | Ticks between burst |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `BOR_Gun_AssaultRifle_TI` | FSJ | 20 | 48 sharp / 110 blunt | 39.9 | 32 | 8 | 3 |
| `BOR_Gun_AssaultRifle_TI` | QaP | 14 | 72 sharp / 110 blunt | 39.9 | 32 | 8 | 3 |
| `BOR_Gun_AssaultRifle_TI` | Ni | 12 | 55 sharp / 95 blunt | 39.9 | 32 | 8 | 3 |
| `BOR_Gun_AssaultRifle_TI` | Nc | 14 | 72 sharp / 110 blunt | 39.9 | 32 | 8 | 3 |
| `BOR_Gun_AssaultRifle_TI` | TC | 15 | 108 sharp / 154 blunt | 39.9 | 32 | 8 | 3 |
| `BOR_Gun_AssaultRifle_TI` | EMP_N | 12 | 48 sharp / 110 blunt | 39.9 | 32 | 8 | 3 |
| `BOR_Gun_SniperRifle_TI` | FSJ | 20 | 48 sharp / 110 blunt | 55.9 | 18 | 1 | 0 |
| `BOR_Gun_SniperRifle_TI` | QaP | 14 | 72 sharp / 110 blunt | 55.9 | 18 | 1 | 0 |
| `BOR_Gun_SniperRifle_TI` | Ni | 12 | 55 sharp / 95 blunt | 55.9 | 18 | 1 | 0 |
| `BOR_Gun_SniperRifle_TI` | Nc | 14 | 72 sharp / 110 blunt | 55.9 | 18 | 1 | 0 |
| `BOR_Gun_SniperRifle_TI` | TC | 15 | 108 sharp / 154 blunt | 55.9 | 18 | 1 | 0 |
| `BOR_Gun_SniperRifle_TI` | EMP_N | 12 | 48 sharp / 110 blunt | 55.9 | 18 | 1 | 0 |
| `BOR_Gun_LMG_TI` | FSJ | 20 | 48 sharp / 110 blunt | 31.9 | 60 | 20 | 6 |
| `BOR_Gun_LMG_TI` | QaP | 14 | 72 sharp / 110 blunt | 31.9 | 60 | 20 | 6 |
| `BOR_Gun_LMG_TI` | Ni | 12 | 55 sharp / 95 blunt | 31.9 | 60 | 20 | 6 |
| `BOR_Gun_LMG_TI` | Nc | 14 | 72 sharp / 110 blunt | 31.9 | 60 | 20 | 6 |
| `BOR_Gun_LMG_TI` | TC | 15 | 108 sharp / 154 blunt | 31.9 | 60 | 20 | 6 |
| `BOR_Gun_LMG_TI` | EMP_N | 12 | 48 sharp / 110 blunt | 31.9 | 60 | 20 | 6 |
| `BOR_Gun_ChainShotgun_TI` | FSJ (slug) | 12 | 43 sharp / 93 blunt | 16.9 | 16 | 4 | 0 |
| `BOR_Gun_ChainShotgun_TI` | QaP | 30 (9 pellets) | 48 sharp / 103 blunt | 16.9 | 16 | 4 | 0 |
| `BOR_Gun_ChainShotgun_TI` | Ni | 30 (9 pellets) | 41 sharp / 89 blunt | 16.9 | 16 | 4 | 0 |
| `BOR_Gun_ChainShotgun_TI` | Nc | 30 (9 pellets) | 48 sharp / 103 blunt | 16.9 | 16 | 4 | 0 |
| `BOR_Gun_ChainShotgun_TI` | TC (slug) | 20 | 72 sharp / 155 blunt | 16.9 | 16 | 4 | 0 |
| `BOR_Gun_ChainShotgun_TI` | EMP_N | 30 (9 pellets) | 48 sharp / 103 blunt | 16.9 | 16 | 4 | 0 |
| `BOR_Gun_Autopistol_TI` | FSJ | 16 | 24 sharp / 17 blunt | 32.9 | 16 | 4 | 5 |
| `BOR_Gun_Autopistol_TI` | QaP | 11 | 48 sharp / 17 blunt | 32.9 | 16 | 4 | 5 |
| `BOR_Gun_Autopistol_TI` | Ni | 13 | 34 sharp / 14 blunt | 32.9 | 16 | 4 | 5 |
| `BOR_Gun_Autopistol_TI` | Nc | 11 | 48 sharp / 17 blunt | 32.9 | 16 | 4 | 5 |
| `BOR_Gun_Autopistol_TI` | TC | 12 | 72 sharp / 24 blunt | 32.9 | 16 | 4 | 5 |
| `BOR_Gun_Autopistol_TI` | EMP_N | 13 | 21 sharp / 17 blunt | 32.9 | 16 | 4 | 5 |

## Laser (matches the real, already-implemented laser damage/penetration scale — see correction note below; only the secondary-damage *roles* and UV's -20% pen change)

| Weapon | Ammo type | Damage | Armor penetration | Range | Magazine | Burst | Ticks between burst |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `BOR_Gun_LaserRifle_TI` | Solar | 44 | 55 sharp / 0 blunt | 19.9 | 20 | 5 | 2 |
| `BOR_Gun_LaserRifle_TI` | IR (Incendiary) | 26 | 36 sharp / 0 blunt | 19.9 | 20 | 5 | 2 |
| `BOR_Gun_LaserRifle_TI` | UV (HP) | 55 | 29 sharp / 0 blunt | 19.9 | 20 | 5 | 2 |
| `BOR_Gun_LaserRifle_TI` | EM (EMP) | 13 | 21 sharp / 0 blunt | 19.9 | 20 | 5 | 2 |
| `BOR_Gun_LaserSniperRifle_TI` | Solar | 53 | 92 sharp / 0 blunt | 27.9 | 5 | 2 | 2 |
| `BOR_Gun_LaserSniperRifle_TI` | IR (Incendiary) | 32 | 60 sharp / 0 blunt | 27.9 | 5 | 2 | 2 |
| `BOR_Gun_LaserSniperRifle_TI` | UV (HP) | 66 | 46 sharp / 0 blunt | 27.9 | 5 | 2 | 2 |
| `BOR_Gun_LaserSniperRifle_TI` | EM (EMP) | 23 | 23 sharp / 0 blunt | 27.9 | 5 | 2 | 2 |
| `BOR_Gun_LaserCannon_TI` | Solar | 53 | 92 sharp / 0 blunt | 15.9 | 75 | 20 | 4 |
| `BOR_Gun_LaserCannon_TI` | IR (Incendiary) | 32 | 60 sharp / 0 blunt | 15.9 | 75 | 20 | 4 |
| `BOR_Gun_LaserCannon_TI` | UV (HP) | 66 | 45 sharp / 0 blunt | 15.9 | 75 | 20 | 4 |
| `BOR_Gun_LaserCannon_TI` | EM (EMP) | 13 | 20 sharp / 0 blunt | 15.9 | 75 | 20 | 4 |
| `BOR_Gun_LaserPistol_TI` | Solar | 35 | 22 sharp / 0 blunt | 16.9 | 15 | 3 | 1 |
| `BOR_Gun_LaserPistol_TI` | IR (Incendiary) | 21 | 14 sharp / 0 blunt | 16.9 | 15 | 3 | 1 |
| `BOR_Gun_LaserPistol_TI` | UV (HP) | 44 | 13 sharp / 0 blunt | 16.9 | 15 | 3 | 1 |
| `BOR_Gun_LaserPistol_TI` | EM (EMP) | 10 | 15 sharp / 0 blunt | 16.9 | 15 | 3 | 1 |
| `BOR_Gun_LaserBullpup_TI` | Solar | 44 | 55 sharp / 0 blunt | 8.9 | 30 | 6 | 1 |
| `BOR_Gun_LaserBullpup_TI` | IR (Incendiary) | 26 | 36 sharp / 0 blunt | 8.9 | 30 | 6 | 1 |
| `BOR_Gun_LaserBullpup_TI` | UV (HP) | 55 | 29 sharp / 0 blunt | 8.9 | 30 | 6 | 1 |
| `BOR_Gun_LaserBullpup_TI` | EM (EMP) | 13 | 21 sharp / 0 blunt | 8.9 | 30 | 6 | 1 |
| `BOR_Gun_LaserDMR_TI` | Solar | 44 | 55 sharp / 0 blunt | 27.9 | 8 | 3 | 1 |
| `BOR_Gun_LaserDMR_TI` | IR (Incendiary) | 26 | 36 sharp / 0 blunt | 27.9 | 8 | 3 | 1 |
| `BOR_Gun_LaserDMR_TI` | UV (HP) | 55 | 29 sharp / 0 blunt | 27.9 | 8 | 3 | 1 |
| `BOR_Gun_LaserDMR_TI` | EM (EMP) | 13 | 21 sharp / 0 blunt | 27.9 | 8 | 3 | 1 |
| `BOR_Gun_LaserShotgun_TI` | Solar | 25 (10 pellets) | 18 sharp / 0 blunt | 8.9 | 5 | 1 | 0 |
| `BOR_Gun_LaserShotgun_TI` | IR (Incendiary) | 15 (10 pellets) | 12 sharp / 0 blunt | 8.9 | 5 | 1 | 0 |
| `BOR_Gun_LaserShotgun_TI` | UV (HP) | 31 (7 pellets) | 11 sharp / 0 blunt | 8.9 | 5 | 1 | 0 |
| `BOR_Gun_LaserShotgun_TI` | EM (EMP) | 10 (10 pellets) | 12 sharp / 0 blunt | 8.9 | 5 | 1 | 0 |

**Correction note**: the real `xcom_laser.xml` laser stats were never actually derived from the ballistic table by the stated ×1.33/×0.67 formula — they were independently tuned to a much larger damage/penetration scale, and **blunt penetration is always 0 for laser weapons in this mod** (a deliberate "energy beams have no blunt component" design, not an oversight). Earlier drafts of this table incorrectly invented smaller ballistic-derived numbers and non-zero blunt values that didn't match the real implementation. This revision instead keeps Solar/IR/EM at their existing real values unchanged, and only: (1) removes the erroneous `Flame_Secondary` that Solar incorrectly carried, (2) converts UV/HP from `Flame_Secondary` to `Bomb_Secondary`, and (3) reduces UV/HP's sharp penetration to 80% of the average of that weapon's Solar/IR/EM sharp values (damage unchanged). Also note `BOR_Gun_LaserBullpup_TI` and `BOR_Gun_LaserDMR_TI` both reuse the Rifle ammoset in the real XML (not their own), so they share Rifle's numbers exactly — Cannon and SniperRifle each have their own distinct (but similar) ammosets.

## Gauss (derived from ballistic: dmg -33%, sharp -33%, blunt +33%, range +50%; no QaP-equivalent; Ni's -20% pen is vs the average of the *other gauss* ammo types, not re-derived from ballistic Ni)

| Weapon | Ammo type | Damage | Armor penetration | Range | Magazine | Burst | Ticks between burst |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `BOR_Gun_GaussRifle_TI` | SS | 13 | 32 sharp / 146 blunt | 58.9 | 30 | 2 | 3 |
| `BOR_Gun_GaussRifle_TI` | Ni | 8 | 36 sharp / 128 blunt | 58.9 | 30 | 2 | 3 |
| `BOR_Gun_GaussRifle_TI` | TC | 10 | 72 sharp / 204 blunt | 58.9 | 30 | 2 | 3 |
| `BOR_Gun_GaussRifle_TI` | Nc | 9 | 48 sharp / 146 blunt | 58.9 | 30 | 2 | 3 |
| `BOR_Gun_GaussRifle_TI` | EMP_N | 8 | 32 sharp / 146 blunt | 58.9 | 30 | 2 | 3 |
| `BOR_Gun_GaussSniperRifle_TI` | SS | 13 | 32 sharp / 146 blunt | 82.9 | 5 | 1 | 0 |
| `BOR_Gun_GaussSniperRifle_TI` | Ni | 8 | 36 sharp / 128 blunt | 82.9 | 5 | 1 | 0 |
| `BOR_Gun_GaussSniperRifle_TI` | TC | 10 | 72 sharp / 204 blunt | 82.9 | 5 | 1 | 0 |
| `BOR_Gun_GaussSniperRifle_TI` | Nc | 9 | 48 sharp / 146 blunt | 82.9 | 5 | 1 | 0 |
| `BOR_Gun_GaussSniperRifle_TI` | EMP_N | 8 | 32 sharp / 146 blunt | 82.9 | 5 | 1 | 0 |
| `BOR_Gun_GaussCannon_TI` | SS | 13 | 32 sharp / 146 blunt | 46.9 | 80 | 5 | 4 |
| `BOR_Gun_GaussCannon_TI` | Ni | 8 | 36 sharp / 128 blunt | 46.9 | 80 | 5 | 4 |
| `BOR_Gun_GaussCannon_TI` | TC | 10 | 72 sharp / 204 blunt | 46.9 | 80 | 5 | 4 |
| `BOR_Gun_GaussCannon_TI` | Nc | 9 | 48 sharp / 146 blunt | 46.9 | 80 | 5 | 4 |
| `BOR_Gun_GaussCannon_TI` | EMP_N | 8 | 32 sharp / 146 blunt | 46.9 | 80 | 5 | 4 |
| `BOR_Gun_GaussPistol_TI` | SS | 10 | 16 sharp / 22 blunt | 48.9 | 5 | 1 | 0 |
| `BOR_Gun_GaussPistol_TI` | Ni | 8 | 22 sharp / 19 blunt | 48.9 | 5 | 1 | 0 |
| `BOR_Gun_GaussPistol_TI` | TC | 8 | 48 sharp / 31 blunt | 48.9 | 5 | 1 | 0 |
| `BOR_Gun_GaussPistol_TI` | Nc | 7 | 32 sharp / 22 blunt | 48.9 | 5 | 1 | 0 |
| `BOR_Gun_GaussPistol_TI` | EMP_N | 8 | 14 sharp / 22 blunt | 48.9 | 5 | 1 | 0 |
| `BOR_Gun_GaussBullpup_TI` | SS | 13 | 32 sharp / 146 blunt | 24.9 | 30 | 3 | 4 |
| `BOR_Gun_GaussBullpup_TI` | Ni | 8 | 36 sharp / 128 blunt | 24.9 | 30 | 3 | 4 |
| `BOR_Gun_GaussBullpup_TI` | TC | 10 | 72 sharp / 204 blunt | 24.9 | 30 | 3 | 4 |
| `BOR_Gun_GaussBullpup_TI` | Nc | 9 | 48 sharp / 146 blunt | 24.9 | 30 | 3 | 4 |
| `BOR_Gun_GaussBullpup_TI` | EMP_N | 8 | 32 sharp / 146 blunt | 24.9 | 30 | 3 | 4 |
| `BOR_Gun_GaussDMR_TI` | SS | 13 | 32 sharp / 146 blunt | 82.9 | 5 | 3 | 5 |
| `BOR_Gun_GaussDMR_TI` | Ni | 8 | 36 sharp / 128 blunt | 82.9 | 5 | 3 | 5 |
| `BOR_Gun_GaussDMR_TI` | TC | 10 | 72 sharp / 204 blunt | 82.9 | 5 | 3 | 5 |
| `BOR_Gun_GaussDMR_TI` | Nc | 9 | 48 sharp / 146 blunt | 82.9 | 5 | 3 | 5 |
| `BOR_Gun_GaussDMR_TI` | EMP_N | 8 | 32 sharp / 146 blunt | 82.9 | 5 | 3 | 5 |
| `BOR_Gun_GaussShotgun_TI` | SS (slug) | 7 | 28 sharp / 123 blunt | 24.9 | 6 | 1 | 0 |
| `BOR_Gun_GaussShotgun_TI` | Ni (slug) | 20 | 28 sharp / 120 blunt | 24.9 | 6 | 1 | 0 |
| `BOR_Gun_GaussShotgun_TI` | TC (slug) | 13 | 48 sharp / 206 blunt | 24.9 | 6 | 1 | 0 |
| `BOR_Gun_GaussShotgun_TI` | Nc (slug) | 20 | 32 sharp / 136 blunt | 24.9 | 6 | 1 | 0 |
| `BOR_Gun_GaussShotgun_TI` | EMP_N (slug) | 20 | 32 sharp / 136 blunt | 24.9 | 6 | 1 | 0 |

**Correction/implementation note**: while implementing this in the real `xcom_mag.xml`, discovered the real Gauss Shotgun ("Scatter"/12-gauge caliber) previously only had TC implemented (as a 16-pellet shot, not a slug) — Ni/Nc/EMP_N never existed for this caliber. Per your decision, **all** Gauss Shotgun ammo (including TC) is now a single-projectile slug, and Ni/Nc/EMP_N were built from scratch to match. Other real bugs fixed along the way: 8mm's Nc bullet incorrectly had `Bomb_Secondary` instead of `Flame_Secondary`; 6mm and 8mm were missing their TC `AmmoDef`/`RecipeDef` entirely (only the bullet existed); every gauss gun's `defaultProjectile` pointed at a dangling, never-defined bullet name (fixed by pointing at the new SS bullets instead); `BOR_Gun_GaussDMR_TI`/`TII` were incorrectly wired to the 8mm ammoSet (matching this table, which already assumed rifle/6mm for DMR) — corrected to 6mm.

## Summary

| Type | Damage | Sharp penetration | Blunt penetration | Range |
| --- | --- | --- | --- | --- |
| ballistic | mid | mid | mid | mid |
| laser | high(burn) | low | mid | low |
| gauss | low | low | high | high |

## Open flags (review before applying to real XML)
1. **TC Sabot formula (approved)**: `damage = FSJ×0.75`, `sharp = QaP×1.5`, `blunt = FSJ×1.4` (floored). This makes TC hit harder-penetrating than either FSJ or QaP at a damage cost, "better" than real CE's Sabot (which trades ~48% damage for ~3.5x sharp pen) without going to that extreme, since our baseline pen numbers are already much higher than real CE's.
2. **Nc/EMP_N reusing QaP/Ni donor numbers (approved)**: Nc's damage/pen numbers reuse QaP's numbers for that weapon (same convention the old placeholder "Incendiary" used); EMP_N's damage/pen numbers reuse Ni's *pre-rework* numbers (same convention the old placeholder "EMP" used). Only `secondaryDamage`/`ammoClass` differ from their donors.
3. **Resolved — Laser Shotgun stays pellet-based**: Solar remains a 10-pellet shot (matching IR/EM) rather than becoming a slug, keeping its existing real damage/pen (25 dmg / 18 sharp, unchanged). UV/HP keeps its existing 7-pellet count.
4. **Resolved — dangling `defaultProjectile` bug**: every gauss gun's `defaultProjectile` (e.g. `Bullet_XCOM6mmRailgun_Sabot`, `Bullet_XCOM4mmRailgun_Sabot`, `Bullet_XCOM8mmRailgun_Sabot`, `Bullet_XCOMScatterRailgun_Sabot`) references a bullet defName that is never actually defined anywhere in the mod — a pre-existing dangling reference/bug. Implementation will fix this by pointing these `defaultProjectile` fields at the new SS bullet defNames per caliber, since SS is the natural "default/baseline" gauss round.
5. **Gauss caliber-per-weapon mapping (confirmed)**: Pistol = 4mm (`AmmoSet_4mmRailgunXCOM`), Rifle/Bullpup/**DMR** = 6mm (`AmmoSet_6mmRailgunXCOM`), SniperRifle/Cannon = 8mm (`AmmoSet_8mmRailgunXCOM`), Shotgun = Scatter (`AmmoSet_ScatterRailgunXCOM`). Note: the current real XML incorrectly wires `BOR_Gun_GaussDMR_TI`/`TII` to the 8mm ammoSet/defaultProjectile (same as SniperRifle/Cannon) — implementation will correct this to the 6mm ammoSet, matching the table above (Gauss DMR's numbers were already computed from the Rifle-family baseline, so no table values change, only the XML wiring).