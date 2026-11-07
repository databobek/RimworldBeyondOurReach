# Faction pawnkinds: Solar, Kwazaari, Tenebral

Status: awaiting approval. Nothing here is built. Combat power values are untested starting points.

## Intent

Solar and Kwazaari only have a handful of expensive fighters, and Tenebral has one. Give each faction a readable ladder of pawns, from civilians to elites, that plays differently at each step and uses the Melee and Guns mods where possible.

## Goals

- Every pawn wears a full matching set from its own tier, with that tier's bodysuit, weapons and bionics. No mixing.
- Combat power rises in even steps, and each step has its own combat role.
- Optional mods add options automatically, with no duplicate kinds.

## Non-goals

- Mech support units for the Machine Lord (phase 2).
- Guns mod disposable launchers as CE sidearms (phase 2, needs an in-game test).
- Changing Officers, traders or existing Kwazaari civilians.
- The 1.6 folders.

## Hard rules

1. **Budgets never choose gear.** Gear comes from `apparelRequired`, `techHediffsRequired` and narrow weapon tags. `weaponMoney` is set to `100000~100000`, above every price, and shield money gets the same treatment. `apparelMoney` is `0~0` and `apparelTags` are cleared, so nothing extra is bought.
2. `apparelIgnoreSeasons` is true on every new or rewritten kind, so no cold-weather clothes appear.
3. Straight quotes only in all text.
4. Polish labels are added for every new or relabeled kind and favor.
5. Maces are the anti-mechanoid weapon, because of their EMP damage. Only the Guard Mechbreaker carries one.

## Approaches

| Approach | Trade-off | |
|---|---|---|
| A. Add narrow tags with patches in BOR | Everything is in BOR's git history. Patches run on the unified XML, so CE copies of guns are covered too. Needs one new gated load folder. | **Chosen (Claude's call)** |
| B. Edit tags in the Guns and Melee defs | Simple to read, but those mods have no git history, and Guns keeps separate CE copies that override by path, so every tag goes in twice. | Rejected |
| C. Use existing broad tags and limit by price | No new tags, but `BOR_AssaultRifle_I` includes gauss guns, and this relies on budgets. | Ruled out by the user |

## Solar and Kwazaari roster

Kwazaari costs are Solar x 1.5. "Raids from" is where the Solar cost curve first allows the kind.

| Kind | Solar / Kwazaari cost | Raids from | Weapons (groups listed under Weapon tags) |
|---|---|---|---|
| Townsman | 60 / 60 | civilian | Conv_Pistol, Knife, Gladius |
| Farmer | 60 / 60 | civilian | Conv_Pistol, Knife |
| Miner | 70 / 70 | civilian | Conv_Pistol, Hammer |
| Neutronium Grafter | 70 / 70 | civilian | Conv_Pistol, Knife |
| Gunsmith | 70 / 70 | civilian | Conv_Pistol |
| Scientist | 70 / 70 | civilian | Conv_Pistol |
| Militiaman | 90 / 135 | ~600 | Solar: Conv_Bullpup. Kwazaari: Conv_AR (no conventional bullpup exists) |
| Militia Skirmisher | 90 / 135 | ~600 | Spear, Gladius |
| Police | 120 / 180 | ~1,000 | Conv_Shotgun. CE: Beam_Shotgun only |
| Riot Police | 130 / 195 | ~1,200 | Baton, plus shield under CE |
| Guard | 200 / 300 | ~2,650 | Conv_AR, Charge_Rifle |
| Guard Breacher | 220 / 330 | ~3,100 | Conv_Shotgun, Charge_Shotgun |
| Guard Mechbreaker | 230 / 345 | ~3,350 | Mace only |
| Guard Blademaster | 240 / 360 | ~3,600 | Longsword, Gladius |
| Field Officer | 260 / 390 | ~4,050 | Conv_Pistol, Gauss_Pistol, Charge_Pistol |
| Elite Trooper | 300 / 450 | ~5,000 | Gauss_Rifle |
| Elite Marksman | 320 / 480 | ~5,400 | Gauss_Marksman |
| Elite Heavy | 340 / 510 | ~5,900 | LMG, Gauss_Heavy, Charge_LMG |
| Daedalus Trooper (Solar, Royalty) | 380 | ~11,600 | Gauss_Rifle |
| Daedalus Assaulter (Solar, Royalty) | 400 | ~15,300 | Gauss_Shotgun |
| Machine Lord (Kwazaari, Biotech) | 600 | never raids | Gauss_Rifle |

### Gear

Every kind wears the bodysuit (`BOR_Solar_Carbonsuit`, `BOR_Quasar_Carbonsuit`).

| Tier | Solar | Kwazaari |
|---|---|---|
| Civilian | `BOR_Sunforge_Welders_Coat` | `BOR_Quasar_Masterforge_Mantle` |
| Neutronium Grafter | `BOR_Bionic_Assembly_Harness` | `BOR_Quasar_Assembly_Carapace` |
| Gunsmith | coat, `BOR_Sunforge_Calibration_Visor` | mantle |
| Scientist | coat, `BOR_QuantumRebreatherMask` | mantle, `BOR_Quasar_Research_Integration_Helm` |
| Militia | coat, `BOR_Solar_Apparel_FlakPants`, `BOR_Solar_Apparel_ArmorHelmetRecon` | mantle, `BOR_Quasar_Apparel_FlakPants`, `BOR_Quasar_Apparel_ArmorHelmetRecon` |
| Police | `BOR_Solar_Apparel_ArmorRecon`, no helmet | `BOR_Quasar_Apparel_ArmorRecon`, no helmet |
| Guard, Field Officer, Elite | `BOR_Solar_Apparel_PowerArmor`, `..._PowerArmorHelmet` | `BOR_Quasar_Apparel_PowerArmor`, `..._PowerArmorHelmet` |
| Elite Heavy | marine set, `BOR_Solar_Apparel_Backpack` | marine set, `BOR_Quasar_Apparel_Backpack` |
| Daedalus Trooper, Assaulter | `BOR_Solar_Apparel_ArchangelArmor`, `..._ArchangelHelmet` | |
| Machine Lord | | `BOR_Apparel_QuasarMechlordSuit`, `BOR_Apparel_QuasarArmorHelmetMechlordHelmet` |

The specialists' gear is existing BOR work gear. The harness and carapace replace the flak jacket on the Grafter because they are described as implant assembly gear and fit over a bodysuit. BOR's work gloves are left out, because they clash with the bodysuit on the skin layer.

**Flak pants change (Claude's call):** "flak armor" has no matching item, and flak pants clash with bodysuits on the skin layer. Patch both pants to the Middle layer only. This also lets players wear them over a bodysuit.

Riot Police and Vanquishers get their tier's shield belt in the base def. `Mods/CE` removes the belt and adds a `LoadoutPropertiesExtension` with `SolarShield`, `QuasarShield` or `TenebralShield` at `shieldChance` 1. The non-CE belt is Claude's addition.

### Bionics, quality and skills

`techHediffsRequired` lists append on inheritance, so a duplicate entry gives a second part.

Kwazaari are more skilled than Solar at every step.

| Tier | Bionics | Quality | Main skill, Solar | Main skill, Kwazaari |
|---|---|---|---|---|
| Civilian | base: eye, spine | Excellent | see below | see below |
| Militia | base: eye, spine | Excellent | 6~10 | 8~12 |
| Police | + second eye | Excellent | 8~12 | 10~14 |
| Guard | + second eye, both arms | Excellent | 10~14 | 12~16 |
| Field Officer | + second eye, both arms | Excellent | Shooting 12~16, Social 10~14 | Shooting 14~18, Social 12~16 |
| Elite, Daedalus, Machine Lord | elite base: full set | Masterwork | 14~18 | 16~20 |

| Civilian | Main skill | Second skill | Solar | Kwazaari |
|---|---|---|---|---|
| Townsman | Social | | 4~8 | 8~12 |
| Farmer | Plants | | 8~12 | 12~16 |
| Miner | Mining | | 8~12 | 12~16 |
| Neutronium Grafter | Medicine | Intellectual | main 10~14, second 6~10 | main 14~18, second 10~14 |
| Gunsmith | Crafting | Shooting | main 10~14, second 6~10 | main 14~18, second 10~14 |
| Scientist | Intellectual | Crafting | main 10~14, second 6~10 | main 14~18, second 10~14 |

## Tenebral roster

All four wear `BOR_Tenebral_Apparel_PowerArmor`, `BOR_Tenebral_Apparel_PowerArmorHelmet`, `BOR_Tenebral_Carbonsuit` and `BOR_Tenebral_Apparel_Backpack`. Executioner stays unchanged as the leader at 5,000, and remains the only kind with a belt unless the non-CE fallback applies.

| Kind | Cost | Weapons | Base | Main skill |
|---|---|---|---|---|
| Assassin | 3,000 | `BOR_Gun_SniperRifle_TIII` | `BOR_BasePawn_III` | Shooting 18~20 |
| Slasher | 3,500 | Longsword, Gladius | `BOR_BasePawn_III` | Melee 16~18 |
| Pummeler | 4,000 | Polehammer | `BOR_ElitePawn_III` | Melee 17~19 |
| Vanquisher | 4,500 | Spear, plus shield under CE | `BOR_ElitePawn_III` | Melee 18~20 |

- Add a `raidCommonalityFromPointsCurve` of `(4999, 0)` and `(5000, 1)` so Tenebrals still arrive from 5,000 points.
- Combat group weights: Assassin 2, Slasher 3, Pummeler 2, Vanquisher 2, Executioner 1.

## Weapon tags

Every kind has one tag of its own, such as `BOR_Kind_Solar_Guard` or `BOR_Kind_Kwazaari_Guard`, and that tag goes on exactly the weapons the kind may use. Rewritten kinds use `<weaponTags Inherit="False">`, so nothing from their base or old list carries over. Changing one kind's weapons never affects another kind. A tag on a missing mod's weapon simply matches nothing, so "if Guns is loaded" needs no other logic.

The patches hold one block per kind, listing its weapons. These groups are shorthand for this document only and don't exist in the game:

| Group | Weapons (`_TI` / `_TII`, or S / Q in Guns) |
|---|---|
| Conv_Pistol | `BOR_Gun_Autopistol` |
| Conv_Bullpup | `BOR_Gun_Bullpup` (Solar only) |
| Conv_AR | `BOR_Gun_AssaultRifle` |
| Conv_Shotgun | `BOR_Gun_ChainShotgun` |
| Gauss_Pistol | `BOR_Gun_GaussPistol` |
| Gauss_Rifle | `BOR_Gun_GaussBattleRifle`, `BOR_Gun_GaussBullpup` |
| Gauss_Marksman | `BOR_Gun_GaussDMR`, `BOR_Gun_GaussSniperRifle` |
| LMG | `BOR_Gun_LMG` |
| Gauss_Heavy | `BOR_Gun_GaussCannon` |
| Charge_LMG | `ABG_Gun_SLMG`, `ABG_Gun_QLMG` |
| Gauss_Shotgun | `BOR_Gun_GaussShotgun` |
| Beam_Shotgun | `BOR_Gun_LaserShotgun_TI`, `BOR_Gun_QuasarLaserShotgun_TII` |
| Charge_Pistol | `ABG_Gun_SPistol`, `ABG_Gun_QPistol` |
| Charge_Rifle | `ABG_Gun_SBattleRifle`, `ABG_Gun_SBullpup`, and Q versions |
| Charge_Shotgun | `ABG_Gun_SShotgun`, `ABG_Gun_QShotgun` |
| Knife, Gladius, Hammer, Baton, Longsword, Mace, Spear, Polehammer | matching `Bamba_Melee_<Solar, Quasar or Tenebral>_<family>` |

Kwazaari "gauss" guns are the user's bolters, so there is no separate bolter group.

Patch locations:

- `Common/Patches/PawnKindWeaponTags.xml`: kind tags on BOR guns.
- `Mods/AllBambaMelee/Patches/PawnKindWeaponTags.xml`: kind tags on melee weapons, plus `CE_OneHandedWeapon` on `Bamba_Melee_Tenebral_Spear`. CE's own alert says a pawn with a shield and two-handed weapon can't attack. The ultra spear already has this tag.
- `Mods/AllBambaGuns/Patches/PawnKindWeaponTags.xml`: kind tags on charge guns. Add this folder to `LoadFolders.xml`, gated on Guns (`bambaryla.AllBambaGuns`).
- `Mods/CE/Patches/`: Police weapon swap (move the kind's tag from the conventional shotgun to the beam shotgun), shield swap, and a `LoadoutPropertiesExtension` with spare magazines on every ranged kind.

## Mod gating

- Melee-only kinds (Skirmisher, Mechbreaker, Blademaster, Riot Police, Slasher, Pummeler, Vanquisher) would spawn unarmed without Melee. Add them to faction groups through a patch in `Mods/AllBambaMelee`. Their defs stay in Common, so they load but are never picked. Civilians and other mixed kinds still get a gun.
- Daedalus Trooper and Assaulter are defined in `Mods/Royalty/Defs` and added to the Solar groups by a patch there.
- Machine Lord is defined in `Mods/Biotech/Defs`, with vanilla `Mechlink` in `techHediffsRequired`, as vanilla's Mechanitor kind has.

## Existing defs

Reusing defNames keeps translations and the reinforcement favor working.

| Existing | Becomes |
|---|---|
| `BOR_Citizen`, `BOR_Farmer`, `BOR_Miner` | Solar Townsman, Farmer, Miner gear. Existing labels stay |
| `BOR_Logger` | Same kit as the Farmer, so the Loggers group keeps working |
| `BOR_Solarite_Guardsman`, `BOR_Kwazaari_Guardsman` | Guard |
| `BOR_Solarite_Marine`, `BOR_Kwazaari_Marine` | Elite Trooper |
| `BOR_Solarite_Marksman`, `BOR_Kwazaari_Marksman` | Elite Marksman |
| `BOR_Kwazaari_Assault_Marine` | Kwazaari Guard Blademaster |

## Solar groups

- **Combat:** Militiaman 4, Skirmisher 3, Police 3, Riot 2, Guard 5, Breacher 3, Mechbreaker 1, Blademaster 3, Field Officer 2, Trooper 3, Marksman 2, Heavy 1, Daedalus Trooper 1, Daedalus Assaulter 1, Officer 1.
- **Settlement:** same list, with Police and Riot doubled and Militia halved.
- **Peaceful:** Townsman 14, Neutronium Grafter 2, Gunsmith 2, Scientist 2, plus Militia and Police as guards, and Field Officer 1.
- **Trader, worksites:** replace current guards with Militia and Police, and Officer with Field Officer. Worksites keep their matching civilian. The Officer def itself is unchanged and stays the faction leader.

## Kwazaari: hire-only

The faction can't raid and has no settlements, so soldiers reach the map only through favors. `KwazaariFavorDef` gets an optional squad list, and `FavorWorker_Reinforcements` spawns that list when present. `pawnKind` plus `pawnCount` keeps working. Cost per soldier scales from today's 6 scrip for a 300-cost pawn, with the same value as `lostPawnCost`.

| Favor | Squad | Cost | Lost pawn |
|---|---|---|---|
| Militia | 2 Militiaman, 2 Skirmisher | 12 | 3 |
| Police | 2 Police, 2 Riot Police | 16 | 4 |
| Guard (existing) | 1 Field Officer, 1 Guard, 1 Breacher, 1 Blademaster | 24 | 6 |
| Anti-mech | 4 Mechbreaker (needs Melee) | 28 | 7 |
| Elite | 2 Trooper, 1 Marksman, 1 Heavy | 36 | 9 |
| Machine Lord (Biotech) | 1 Machine Lord | 12 | 12 |

Melee entries in a squad need a fallback when Melee is missing. The builder should skip any squad member whose kind can't be armed, or use a Melee-gated patch for those squads.

## Disabled Kwazaari civilians

Added to the end of `PawnKinds_Kwazaari.xml`. The game ignores it while commented out, so the missing tags and translations cause no errors. Do not put a double hyphen inside the comment.

```xml
<!--
	Kwazaari civilians, disabled until the faction has a way to show them on the map.
	To enable: add each kind's tag to its weapons in the weapon tag patches, add these kinds
	to the faction's pawn groups, and add Polish labels.

	<PawnKindDef Name="BOR_KwazaariCivilianBase" ParentName="BOR_BasePawn_II" Abstract="True">
		<isFighter>false</isFighter>
		<gearHealthRange>1~2</gearHealthRange>
		<apparelIgnoreSeasons>true</apparelIgnoreSeasons>
		<apparelTags Inherit="False" />
		<apparelMoney>0~0</apparelMoney>
		<apparelRequired>
			<li>BOR_Quasar_Carbonsuit</li>
		</apparelRequired>
		<weaponMoney>100000~100000</weaponMoney>
	</PawnKindDef>

	<PawnKindDef ParentName="BOR_KwazaariCivilianBase">
		<defName>BOR_Kwazaari_Townsman</defName>
		<label>kwazaari townsman</label>
		<combatPower>60</combatPower>
		<apparelRequired>
			<li>BOR_Quasar_Masterforge_Mantle</li>
		</apparelRequired>
		<weaponTags>
			<li>BOR_Kind_Kwazaari_Townsman</li>
		</weaponTags>
		<skills>
			<li>
				<skill>Social</skill>
				<range>8~12</range>
			</li>
		</skills>
	</PawnKindDef>

	<PawnKindDef ParentName="BOR_KwazaariCivilianBase">
		<defName>BOR_Kwazaari_Farmer</defName>
		<label>kwazaari farmer</label>
		<combatPower>60</combatPower>
		<backstoryFiltersOverride>
			<li>
				<categoriesAdulthood>
					<li>Farmer</li>
				</categoriesAdulthood>
			</li>
		</backstoryFiltersOverride>
		<apparelRequired>
			<li>BOR_Quasar_Masterforge_Mantle</li>
		</apparelRequired>
		<weaponTags>
			<li>BOR_Kind_Kwazaari_Farmer</li>
		</weaponTags>
		<skills>
			<li>
				<skill>Plants</skill>
				<range>12~16</range>
			</li>
		</skills>
	</PawnKindDef>

	<PawnKindDef ParentName="BOR_KwazaariCivilianBase">
		<defName>BOR_Kwazaari_Miner</defName>
		<label>kwazaari miner</label>
		<combatPower>70</combatPower>
		<backstoryFiltersOverride>
			<li>
				<categoriesAdulthood>
					<li>Miner</li>
				</categoriesAdulthood>
			</li>
		</backstoryFiltersOverride>
		<apparelRequired>
			<li>BOR_Quasar_Masterforge_Mantle</li>
		</apparelRequired>
		<weaponTags>
			<li>BOR_Kind_Kwazaari_Miner</li>
		</weaponTags>
		<skills>
			<li>
				<skill>Mining</skill>
				<range>12~16</range>
			</li>
		</skills>
	</PawnKindDef>

	<PawnKindDef ParentName="BOR_KwazaariCivilianBase">
		<defName>BOR_Kwazaari_Grafter</defName>
		<label>kwazaari neutronium grafter</label>
		<combatPower>70</combatPower>
		<apparelRequired>
			<li>BOR_Quasar_Assembly_Carapace</li>
		</apparelRequired>
		<weaponTags>
			<li>BOR_Kind_Kwazaari_Grafter</li>
		</weaponTags>
		<skills>
			<li>
				<skill>Medicine</skill>
				<range>14~18</range>
			</li>
			<li>
				<skill>Intellectual</skill>
				<range>10~14</range>
			</li>
		</skills>
	</PawnKindDef>

	<PawnKindDef ParentName="BOR_KwazaariCivilianBase">
		<defName>BOR_Kwazaari_Gunsmith</defName>
		<label>kwazaari gunsmith</label>
		<combatPower>70</combatPower>
		<apparelRequired>
			<li>BOR_Quasar_Masterforge_Mantle</li>
		</apparelRequired>
		<weaponTags>
			<li>BOR_Kind_Kwazaari_Gunsmith</li>
		</weaponTags>
		<skills>
			<li>
				<skill>Crafting</skill>
				<range>14~18</range>
			</li>
			<li>
				<skill>Shooting</skill>
				<range>10~14</range>
			</li>
		</skills>
	</PawnKindDef>

	<PawnKindDef ParentName="BOR_KwazaariCivilianBase">
		<defName>BOR_Kwazaari_Scientist</defName>
		<label>kwazaari scientist</label>
		<combatPower>70</combatPower>
		<apparelRequired>
			<li>BOR_Quasar_Masterforge_Mantle</li>
			<li>BOR_Quasar_Research_Integration_Helm</li>
		</apparelRequired>
		<weaponTags>
			<li>BOR_Kind_Kwazaari_Scientist</li>
		</weaponTags>
		<skills>
			<li>
				<skill>Intellectual</skill>
				<range>14~18</range>
			</li>
			<li>
				<skill>Crafting</skill>
				<range>10~14</range>
			</li>
		</skills>
	</PawnKindDef>
-->
```

## Test in game

1. The game loads with no new errors with these mod sets: BOR only; plus Guns; plus Melee; plus CE; everything.
2. In dev mode, spawn each kind and confirm its gear and that it has a weapon from its allowed list.
3. Under CE, confirm a Vanquisher with spear and shield attacks, and ranged kinds carry spare ammo.
4. Machine Lord: the mechlord helmet has -4 `ShootingAccuracyPawn`. Check whether that makes the kind useless with a gun.
5. Daedalus Trooper and Assaulter: check whether AI pawns actually use the jump.
6. Police under CE fire neuralizer beams, so a raid may knock colonists out and not kill them. Check that this plays well.
7. Check how often each weapon appears. The game may favor pricier weapons, and with no budgets, tag lists are the only control. If the mix is bad, split the kind.

## Left to the builder

Skill ranges for secondary skills, exact abstract base layout per tier, and Polish wording.
