# Ammo cost changes

Costs are per batch. Reference: Combat Extended recipes (workshop 2890901044). "Value" is steel-equivalent (steel = 1).

## Material values (steel = 1)

| Material | Value | Basis |
|---|---|---|
| Steel | 1 | reference |
| Plasteel | 4 | |
| Gold / Uranium | 5.3 / 3.2 | market value ratio to steel |
| Sunsteel | 4.34 | 25 plasteel + 1 gold + 1 uranium -> 25 sunsteel |
| Quasar Alloy | 5.02 | 25 sunsteel + 2 gold + 2 uranium -> 25 |
| Tenebrite | 5.70 | 25 quasar + 2 gold + 2 uranium -> 25 |
| FSX / Prometheum | 4 / 3 | CE market value ratio |
| Neutronium | 2 | deep-drilled; value chosen by the mod author |

## Tier ladder

Each tier costs 1.5x the previous one, starting from the CE industrial recipe (steel only, plus FSX/Prometheum for the better rounds).

| Tier | Multiplier | Used for |
|---|---|---|
| Industrial | 1.0 | CE baseline |
| Spacer | 1.5 | |
| Advanced spacer | 2.25 | |
| Solar (I) | 3.375 | Gen1, Sunsteel-based heavy ammo, solar gauss, 81mm / 155mm shells |
| Quasar (II) | 5.06 | Gen2, Badzaar, Quasar-based heavy ammo, vehicle rounds |
| Tenebral (III) | 7.59 | Gen3 (the only ammo that uses Tenebrite) |

Recipe value = CE steel-equivalent value x tier multiplier. Base metal count = CE steel count x multiplier / metal value. Premium (FSX / Prometheum in CE) is paid in Neutronium: CE count x 4 (FSX) or 3 (Prometheum) x multiplier / 2. Component counts are kept as they were and are not scaled.

CE reference per 200-round batch: pistol 9 steel, rifle 10, magnum/heavy 20, shotgun 22; HE adds 2-4 FSX, incendiary 1-2 Prometheum. Variants: Ni = HE, Nc = incendiary, EMP_N = 2x HE, Gen3 AP_N/Slug = incendiary, Gen3 Ion_N = 1.5x HE.

Tenebrite-coated (TC) ammo outside Gen3 and the Tenebrite plasma cells were removed.

## Not yet converted to this model

PDC / Ranger turret ammo, 130mm Type 63, 28cm and Judgment shells, Cracker shell, Neutronium mortar shell (NDev). The Ion_N mortar and howitzer shells also keep their old costs. Quasar gauss (Inc +5, EMP +8 steel-eq premium), laser packs (CE charge pack metal value 36) and plasma cells (CE 22 / 26 / 56) are converted, with their component counts kept; the laser packs lost their old fixed 40 Neutronium except on EMP/Neuralizer variants.

## Changed recipes (106)

| Recipe | Batch | Before | Value | After | Value |
|---|---|---|---|---|---|
| `BOR_Ammo_105x617mmRCannonShell_Ni` | 2 | 94 QuasarAlloy, 60 Neutronium, 2 ComponentSpacer | 802 | 95 QuasarAlloy, 61 Neutronium, 2 ComponentSpacer | 809 |
| `BOR_Ammo_105x617mmRCannonShell_NiAT` | 2 | 90 QuasarAlloy, 40 Neutronium, 2 ComponentSpacer | 742 | 91 QuasarAlloy, 40 Neutronium, 2 ComponentSpacer | 747 |
| `BOR_Ammo_120mmCannonShell_Ni` | 2 | 90 QuasarAlloy, 2 ComponentSpacer, 130 Neutronium | 922 | 91 QuasarAlloy, 132 Neutronium, 2 ComponentSpacer | 931 |
| `BOR_Ammo_120mmCannonShell_NiAT` | 2 | 90 QuasarAlloy, 2 ComponentSpacer, 70 Neutronium | 802 | 91 QuasarAlloy, 71 Neutronium, 2 ComponentSpacer | 809 |
| `BOR_Ammo_12GaugeRailgun_KwazeerEMP` | 500 | 5 QuasarAlloy, 1 ComponentII, 25 Neutronium | 259 | 86 QuasarAlloy, 20 Neutronium, 1 ComponentII | 656 |
| `BOR_Ammo_12GaugeRailgun_KwazeerInc` | 500 | 5 QuasarAlloy, 1 ComponentII, 25 Neutronium | 259 | 86 QuasarAlloy, 13 Neutronium, 1 ComponentII | 642 |
| `BOR_Ammo_12GaugeRailgun_KwazeerStd` | 500 | 5 QuasarAlloy, 1 ComponentII, 25 Neutronium | 259 | 86 QuasarAlloy, 1 ComponentII | 616 |
| `BOR_Ammo_12GaugeRailgun_SS` | 200 | 4 Sunsteel | 17 | 26 Sunsteel | 113 |
| `BOR_Ammo_155mmHowitzerShell_Nc` | 1 | 94 Sunsteel, 2 ComponentSpacer, 50 Neutronium | 718 | 73 Sunsteel, 25 Neutronium, 2 ComponentSpacer | 577 |
| `BOR_Ammo_155mmHowitzerShell_Ni` | 1 | 94 Sunsteel, 2 ComponentSpacer, 170 Neutronium | 958 | 73 Sunsteel, 115 Neutronium, 2 ComponentSpacer | 757 |
| `BOR_Ammo_28cmSpgrShell_Ef` | 5 | 200 QuasarAlloy, 100 Tenebrite, 2 ComponentII, 5000 Neutronium | 11942 | 300 QuasarAlloy, 2 ComponentII, 5000 Neutronium | 11874 |
| `BOR_Ammo_4mmRailgun_KwazeerEMP` | 500 | 3 QuasarAlloy, 1 ComponentII, 10 Neutronium | 219 | 15 QuasarAlloy, 20 Neutronium, 1 ComponentII | 299 |
| `BOR_Ammo_4mmRailgun_KwazeerInc` | 500 | 3 QuasarAlloy, 1 ComponentII, 10 Neutronium | 219 | 15 QuasarAlloy, 13 Neutronium, 1 ComponentII | 285 |
| `BOR_Ammo_4mmRailgun_KwazeerStd` | 500 | 3 QuasarAlloy, 1 ComponentII, 10 Neutronium | 219 | 15 QuasarAlloy, 1 ComponentII | 259 |
| `BOR_Ammo_4mmRailgun_SS` | 500 | 2 Sunsteel | 9 | 12 Sunsteel | 52 |
| `BOR_Ammo_57x307mmR_Ni` | 10 | 86 QuasarAlloy, 2 ComponentSpacer, 20 Neutronium | 682 | 87 QuasarAlloy, 20 Neutronium, 2 ComponentSpacer | 687 |
| `BOR_Ammo_57x307mmR_QaP` | 10 | 86 QuasarAlloy | 432 | 87 QuasarAlloy | 437 |
| `BOR_Ammo_6mmRailgunBullpup_SS` | 500 | 3 Sunsteel | 13 | 12 Sunsteel | 52 |
| `BOR_Ammo_6mmRailgunDMR_SS` | 500 | 3 Sunsteel | 13 | 12 Sunsteel | 52 |
| `BOR_Ammo_6mmRailgun_KwazeerEMP` | 500 | 4 QuasarAlloy, 1 ComponentII, 20 Neutronium | 244 | 15 QuasarAlloy, 20 Neutronium, 1 ComponentII | 299 |
| `BOR_Ammo_6mmRailgun_KwazeerInc` | 500 | 4 QuasarAlloy, 1 ComponentII, 20 Neutronium | 244 | 15 QuasarAlloy, 13 Neutronium, 1 ComponentII | 285 |
| `BOR_Ammo_6mmRailgun_KwazeerStd` | 500 | 4 QuasarAlloy, 1 ComponentII, 20 Neutronium | 244 | 15 QuasarAlloy, 1 ComponentII | 259 |
| `BOR_Ammo_6mmRailgun_SS` | 500 | 3 Sunsteel | 13 | 12 Sunsteel | 52 |
| `BOR_Ammo_75x350mmR_Ni` | 10 | 88 QuasarAlloy, 2 ComponentSpacer, 60 Neutronium | 772 | 177 QuasarAlloy, 122 Neutronium, 2 ComponentSpacer | 1343 |
| `BOR_Ammo_75x350mmR_QaP` | 10 | 88 QuasarAlloy | 442 | 177 QuasarAlloy | 889 |
| `BOR_Ammo_8mmRailgunCannon_SS` | 500 | 4 Sunsteel | 17 | 12 Sunsteel | 52 |
| `BOR_Ammo_8mmRailgun_KwazeerEMP` | 500 | 6 QuasarAlloy, 2 ComponentII, 30 Neutronium | 458 | 15 QuasarAlloy, 20 Neutronium, 2 ComponentII | 483 |
| `BOR_Ammo_8mmRailgun_KwazeerInc` | 500 | 6 QuasarAlloy, 2 ComponentII, 30 Neutronium | 458 | 15 QuasarAlloy, 13 Neutronium, 2 ComponentII | 469 |
| `BOR_Ammo_8mmRailgun_KwazeerStd` | 500 | 6 QuasarAlloy, 2 ComponentII, 30 Neutronium | 458 | 15 QuasarAlloy, 2 ComponentII | 443 |
| `BOR_Ammo_8mmRailgun_SS` | 500 | 4 Sunsteel | 17 | 12 Sunsteel | 52 |
| `BOR_Ammo_AntiMateriel_FSJ` | 200 | 50 Sunsteel | 217 | 39 Sunsteel | 169 |
| `BOR_Ammo_AntiMateriel_Ni` | 200 | 50 Sunsteel, 100 Neutronium | 417 | 39 Sunsteel, 68 Neutronium | 305 |
| `BOR_Ammo_Autocannon_Nc` | 200 | 104 Sunsteel, 100 Neutronium | 651 | 81 Sunsteel, 51 Neutronium | 454 |
| `BOR_Ammo_Autocannon_Ni` | 200 | 104 QuasarAlloy, 190 Neutronium | 902 | 105 QuasarAlloy, 192 Neutronium | 911 |
| `BOR_Ammo_Autocannon_QaP` | 200 | 104 QuasarAlloy | 522 | 105 QuasarAlloy | 527 |
| `BOR_Ammo_Badzaar_LMG` | 200 | 8 QuasarAlloy, 8 Neutronium | 56 | 20 QuasarAlloy, 15 Neutronium | 130 |
| `BOR_Ammo_Badzaar_Pistol` | 200 | 3 QuasarAlloy | 15 | 9 QuasarAlloy | 45 |
| `BOR_Ammo_Badzaar_Rifle` | 200 | 6 Sunsteel, 6 Neutronium | 38 | 12 Sunsteel, 8 Neutronium | 68 |
| `BOR_Ammo_Badzaar_Shotgun` | 200 | 4 Sunsteel, 4 Neutronium | 25 | 26 Sunsteel, 15 Neutronium | 143 |
| `BOR_Ammo_Badzaar_Sniper` | 200 | 8 QuasarAlloy, 8 Neutronium | 56 | 20 QuasarAlloy, 15 Neutronium | 130 |
| `BOR_Ammo_Gen1LMG_EMP_N` | 200 | 4 Sunsteel, 10 Neutronium | 37 | 16 Sunsteel, 54 Neutronium | 177 |
| `BOR_Ammo_Gen1LMG_FSJ` | 200 | 4 Sunsteel | 17 | 16 Sunsteel | 69 |
| `BOR_Ammo_Gen1LMG_Nc` | 200 | 4 Sunsteel, 5 Neutronium | 27 | 16 Sunsteel, 10 Neutronium | 89 |
| `BOR_Ammo_Gen1LMG_Ni` | 200 | 4 Sunsteel, 8 Neutronium | 33 | 16 Sunsteel, 27 Neutronium | 123 |
| `BOR_Ammo_Gen1LMG_QaP` | 200 | 4 QuasarAlloy | 20 | 13 QuasarAlloy | 65 |
| `BOR_Ammo_Gen1Pistol_EMP_N` | 200 | 2 Sunsteel, 4 Neutronium | 17 | 7 Sunsteel, 27 Neutronium | 84 |
| `BOR_Ammo_Gen1Pistol_FSJ` | 200 | 2 Sunsteel | 9 | 7 Sunsteel | 30 |
| `BOR_Ammo_Gen1Pistol_Nc` | 200 | 2 Sunsteel, 2 Neutronium | 13 | 7 Sunsteel, 5 Neutronium | 40 |
| `BOR_Ammo_Gen1Pistol_Ni` | 200 | 2 Sunsteel, 3 Neutronium | 15 | 7 Sunsteel, 14 Neutronium | 58 |
| `BOR_Ammo_Gen1Pistol_QaP` | 200 | 2 QuasarAlloy | 10 | 6 QuasarAlloy | 30 |
| `BOR_Ammo_Gen1Rifle_EMP_N` | 200 | 3 Sunsteel, 8 Neutronium | 29 | 8 Sunsteel, 27 Neutronium | 89 |
| `BOR_Ammo_Gen1Rifle_FSJ` | 200 | 3 Sunsteel | 13 | 8 Sunsteel | 35 |
| `BOR_Ammo_Gen1Rifle_Nc` | 200 | 3 Sunsteel, 4 Neutronium | 21 | 8 Sunsteel, 5 Neutronium | 45 |
| `BOR_Ammo_Gen1Rifle_Ni` | 200 | 3 Sunsteel, 6 Neutronium | 25 | 8 Sunsteel, 14 Neutronium | 63 |
| `BOR_Ammo_Gen1Rifle_QaP` | 200 | 3 QuasarAlloy | 15 | 7 QuasarAlloy | 35 |
| `BOR_Ammo_Gen1Shotgun_EMP_N` | 200 | 2 Sunsteel, 5 Neutronium | 19 | 17 Sunsteel, 54 Neutronium | 182 |
| `BOR_Ammo_Gen1Shotgun_FSJ` | 200 | 2 Sunsteel | 9 | 17 Sunsteel | 74 |
| `BOR_Ammo_Gen1Shotgun_Nc` | 200 | 2 Sunsteel, 3 Neutronium | 15 | 17 Sunsteel, 10 Neutronium | 94 |
| `BOR_Ammo_Gen1Shotgun_Ni` | 200 | 2 Sunsteel, 4 Neutronium | 17 | 17 Sunsteel, 27 Neutronium | 128 |
| `BOR_Ammo_Gen1Shotgun_QaP` | 200 | 2 QuasarAlloy | 10 | 15 QuasarAlloy | 75 |
| `BOR_Ammo_Gen1SniperRifle_EMP_N` | 200 | 4 Sunsteel, 10 Neutronium | 37 | 16 Sunsteel, 54 Neutronium | 177 |
| `BOR_Ammo_Gen1SniperRifle_FSJ` | 200 | 4 Sunsteel | 17 | 16 Sunsteel | 69 |
| `BOR_Ammo_Gen1SniperRifle_Nc` | 200 | 4 Sunsteel, 5 Neutronium | 27 | 16 Sunsteel, 10 Neutronium | 89 |
| `BOR_Ammo_Gen1SniperRifle_Ni` | 200 | 4 Sunsteel, 8 Neutronium | 33 | 16 Sunsteel, 27 Neutronium | 123 |
| `BOR_Ammo_Gen1SniperRifle_QaP` | 200 | 4 QuasarAlloy | 20 | 13 QuasarAlloy | 65 |
| `BOR_Ammo_Gen2Heavy_Nc` | 200 | 8 Sunsteel, 8 Neutronium | 51 | 23 Sunsteel, 15 Neutronium | 130 |
| `BOR_Ammo_Gen2Heavy_Ni` | 200 | 8 QuasarAlloy, 15 Neutronium | 70 | 20 QuasarAlloy, 40 Neutronium | 180 |
| `BOR_Ammo_Gen2Heavy_QaP` | 200 | 8 QuasarAlloy | 40 | 20 QuasarAlloy | 100 |
| `BOR_Ammo_Gen2Pistol_Nc` | 200 | 3 Sunsteel, 3 Neutronium | 19 | 10 Sunsteel, 8 Neutronium | 59 |
| `BOR_Ammo_Gen2Pistol_Ni` | 200 | 3 QuasarAlloy, 6 Neutronium | 27 | 9 QuasarAlloy, 20 Neutronium | 85 |
| `BOR_Ammo_Gen2Pistol_QaP` | 200 | 3 QuasarAlloy | 15 | 9 QuasarAlloy | 45 |
| `BOR_Ammo_Gen2Rifle_Nc` | 200 | 6 Sunsteel, 6 Neutronium | 38 | 12 Sunsteel, 8 Neutronium | 68 |
| `BOR_Ammo_Gen2Rifle_Ni` | 200 | 6 QuasarAlloy, 11 Neutronium | 52 | 10 QuasarAlloy, 20 Neutronium | 90 |
| `BOR_Ammo_Gen2Rifle_QaP` | 200 | 6 QuasarAlloy | 30 | 10 QuasarAlloy | 50 |
| `BOR_Ammo_Gen2Shotgun_Nc` | 200 | 4 Sunsteel, 4 Neutronium | 25 | 26 Sunsteel, 15 Neutronium | 143 |
| `BOR_Ammo_Gen2Shotgun_Ni` | 200 | 4 QuasarAlloy, 8 Neutronium | 36 | 22 QuasarAlloy, 40 Neutronium | 190 |
| `BOR_Ammo_Gen3Heavy_AP_N` | 200 | 6 Tenebrite, 8 Neutronium | 50 | 27 Tenebrite, 23 Neutronium | 200 |
| `BOR_Ammo_Gen3Heavy_Ion_N` | 200 | 5 Tenebrite, 9 Neutronium | 46 | 27 Tenebrite, 91 Neutronium | 336 |
| `BOR_Ammo_Gen3Heavy_N` | 200 | 6 Tenebrite | 34 | 27 Tenebrite | 154 |
| `BOR_Ammo_Gen3Pistol_AP_N` | 200 | 2 Tenebrite, 3 Neutronium | 17 | 12 Tenebrite, 11 Neutronium | 90 |
| `BOR_Ammo_Gen3Pistol_Ion_N` | 200 | 2 Tenebrite, 4 Neutronium | 19 | 12 Tenebrite, 46 Neutronium | 160 |
| `BOR_Ammo_Gen3Pistol_N` | 200 | 2 Tenebrite | 11 | 12 Tenebrite | 68 |
| `BOR_Ammo_Gen3Rifle_AP_N` | 200 | 4 Tenebrite, 6 Neutronium | 35 | 13 Tenebrite, 11 Neutronium | 96 |
| `BOR_Ammo_Gen3Rifle_Ion_N` | 200 | 4 Tenebrite, 7 Neutronium | 37 | 13 Tenebrite, 46 Neutronium | 166 |
| `BOR_Ammo_Gen3Rifle_N` | 200 | 4 Tenebrite | 23 | 13 Tenebrite | 74 |
| `BOR_Ammo_Gen3Shotgun_Ion_N` | 200 | 4 Tenebrite, 7 Neutronium | 37 | 29 Tenebrite, 91 Neutronium | 347 |
| `BOR_Ammo_Gen3Shotgun_N` | 200 | 4 Tenebrite | 23 | 29 Tenebrite | 165 |
| `BOR_Ammo_Gen3Shotgun_Slug` | 200 | 4 Tenebrite, 4 Neutronium | 31 | 29 Tenebrite, 23 Neutronium | 211 |
| `BOR_Ammo_JudgmentShell_Ef` | 5 | 200 QuasarAlloy, 100 Tenebrite, 2 ComponentII, 5000 Neutronium | 11942 | 300 QuasarAlloy, 2 ComponentII, 5000 Neutronium | 11874 |
| `BOR_Ammo_NegotiatorCracker_TII` | 1 | 1000 Neutronium, 35 Tenebrite, 600 QuasarAlloy, 10 ComponentII | 7052 | 1000 Neutronium, 635 QuasarAlloy, 10 ComponentII | 7028 |
| `BOR_Ammo_QuasarLaserPack_EMP` | 500 | 8 QuasarAlloy, 40 Neutronium, 8 ComponentII | 1592 | 36 QuasarAlloy, 20 Neutronium, 8 ComponentII | 1693 |
| `BOR_Ammo_QuasarLaserPack_Neuralizer` | 500 | 8 QuasarAlloy, 40 Neutronium, 8 ComponentII | 1592 | 36 QuasarAlloy, 20 Neutronium, 8 ComponentII | 1693 |
| `BOR_Ammo_QuasarLaserPack_Stun` | 500 | 8 QuasarAlloy, 40 Neutronium, 8 ComponentII | 1592 | 36 QuasarAlloy, 8 ComponentII | 1653 |
| `BOR_Ammo_XCOMLaserPackEMP_Neutronium` | 500 | 8 Sunsteel, 40 Neutronium, 8 ComponentSpacer | 955 | 28 Sunsteel, 14 Neutronium, 8 ComponentSpacer | 990 |
| `BOR_Ammo_XCOMLaserPack_solar` | 500 | 8 Sunsteel, 40 Neutronium, 8 ComponentSpacer | 955 | 28 Sunsteel, 8 ComponentSpacer | 962 |
| `BOR_Ammo_XCOM_PlasmaCellHeavyNeutronium` | 75 | 20 QuasarAlloy, 90 Neutronium, 2 ComponentSpacer | 490 | 56 QuasarAlloy, 20 Neutronium, 2 ComponentSpacer | 531 |
| `BOR_Ammo_XCOM_PlasmaCellHeavyQuasarAlloy` | 75 | 20 Plasteel, 9 QuasarAlloy, 2 ComponentSpacer | 335 | 56 QuasarAlloy, 2 ComponentSpacer | 491 |
| `BOR_Ammo_XCOM_PlasmaCellHeavySunsteel` | 75 | 2 Sunsteel, 9 QuasarAlloy, 2 ComponentSpacer | 264 | 44 Sunsteel, 2 ComponentSpacer | 401 |
| `BOR_Ammo_XCOM_PlasmaCellPistolNeutronium` | 500 | 14 QuasarAlloy, 20 Neutronium, 2 ComponentSpacer | 320 | 22 QuasarAlloy, 20 Neutronium, 2 ComponentSpacer | 360 |
| `BOR_Ammo_XCOM_PlasmaCellPistolQuasarAlloy` | 500 | 14 Plasteel, 2 QuasarAlloy, 2 ComponentSpacer | 276 | 22 QuasarAlloy, 2 ComponentSpacer | 320 |
| `BOR_Ammo_XCOM_PlasmaCellPistolSunsteel` | 500 | 2 Sunsteel, 2 QuasarAlloy, 2 ComponentSpacer | 229 | 17 Sunsteel, 2 ComponentSpacer | 284 |
| `BOR_Ammo_XCOM_PlasmaCellRifleNeutronium` | 500 | 18 QuasarAlloy, 20 Neutronium, 2 ComponentSpacer | 340 | 26 QuasarAlloy, 20 Neutronium, 2 ComponentSpacer | 381 |
| `BOR_Ammo_XCOM_PlasmaCellRifleQuasarAlloy` | 500 | 18 Plasteel, 2 QuasarAlloy, 2 ComponentSpacer | 292 | 26 QuasarAlloy, 2 ComponentSpacer | 341 |
| `BOR_Ammo_XCOM_PlasmaCellRifleSunsteel` | 500 | 2 Sunsteel, 2 QuasarAlloy, 2 ComponentSpacer | 229 | 20 Sunsteel, 2 ComponentSpacer | 297 |
| `BOR_Shell_Mortar_Nc` | 5 | 58 Sunsteel, 5 Prometheum, 50 Neutronium, 2 ComponentSpacer | 577 | 45 Sunsteel, 25 Neutronium, 2 ComponentSpacer | 455 |
| `BOR_Shell_Mortar_Ni` | 5 | 54 Sunsteel, 10 Neutronium, 2 ComponentSpacer | 464 | 42 Sunsteel, 68 Neutronium, 2 ComponentSpacer | 528 |

## Removed recipes (17)

| Recipe | Batch | Former cost |
|---|---|---|
| `BOR_Ammo_6mmRailgun_TC` | 500 | 2 QuasarAlloy, 3 Tenebrite |
| `BOR_Ammo_762x51mmNATO_TC` | 500 | 16 QuasarAlloy, 3 Tenebrite, 30 Neutronium |
| `BOR_Ammo_8mmRailgun_TC` | 500 | 3 QuasarAlloy, 4 Tenebrite |
| `BOR_Ammo_AntiMateriel_TC` | 200 | 30 QuasarAlloy, 6 Tenebrite, 60 Neutronium |
| `BOR_Ammo_Autocannon_TC` | 200 | 64 QuasarAlloy, 12 Tenebrite, 120 Neutronium |
| `BOR_Ammo_Gen1LMG_TC` | 200 | 2 QuasarAlloy, 1 Tenebrite, 5 Neutronium |
| `BOR_Ammo_Gen1Pistol_TC` | 200 | 1 QuasarAlloy, 1 Tenebrite, 2 Neutronium |
| `BOR_Ammo_Gen1Rifle_TC` | 200 | 2 QuasarAlloy, 1 Tenebrite, 4 Neutronium |
| `BOR_Ammo_Gen1Shotgun_TC` | 200 | 1 QuasarAlloy, 1 Tenebrite, 2 Neutronium |
| `BOR_Ammo_Gen1SniperRifle_TC` | 200 | 2 QuasarAlloy, 1 Tenebrite, 5 Neutronium |
| `BOR_Ammo_Gen2Heavy_TC` | 200 | 5 QuasarAlloy, 1 Tenebrite, 9 Neutronium |
| `BOR_Ammo_Gen2Pistol_TC` | 200 | 2 QuasarAlloy, 1 Tenebrite, 4 Neutronium |
| `BOR_Ammo_Gen2Rifle_TC` | 200 | 4 QuasarAlloy, 1 Tenebrite, 7 Neutronium |
| `BOR_Ammo_Gen2Shotgun_TC` | 200 | 3 QuasarAlloy, 1 Tenebrite, 5 Neutronium |
| `BOR_Ammo_XCOM_PlasmaCellHeavyTenebrite` | 75 | 20 QuasarAlloy, 9 Tenebrite, 2 ComponentSpacer |
| `BOR_Ammo_XCOM_PlasmaCellPistolTenebrite` | 500 | 14 QuasarAlloy, 2 Tenebrite, 2 ComponentSpacer |
| `BOR_Ammo_XCOM_PlasmaCellRifleTenebrite` | 500 | 18 QuasarAlloy, 2 Tenebrite, 2 ComponentSpacer |


## AllBambaGuns ammo (`CE/Defs/CE_Ammo_Lines.xml`)

Same model, using the tier rungs from `AllBambaGuns/CE/Docs/Phase5_TierLadder_Proposal.md`. Value is steel-equivalent (steel 1, plasteel 4).

| Line | Tier | Multiplier | Materials |
|---|---|---|---|
| Advanced charged, caseless | Advanced spacer | 2.25 | Plasteel + Steel (half the value each), FSX / Prometheum for premium rounds |
| Solar charged | Solar | 3.375 | Sunsteel, Neutronium for Ion |
| Glitter (hypersonic) | halfway between Solar and Quasar | 4.13 | Plasteel + Steel, FSX / Prometheum |
| Kwazaari charged | Quasar | 5.06 | Quasar Alloy, Neutronium for Ion |

Base value per 200 rounds comes from CE: charged pistol 14.4, rifle 15.2, heavy 136, shotgun 90 (plasteel 4 + steel 1); ballistic pistol 8.8, intermediate rifle 5.6, rifle 10.4, heavy 20, shotgun 22. Conc / AP charged rounds are 1.25x, beanbag 0.9x, HE / Incendiary add 2-4 FSX / 1-2 Prometheum (x multiplier), Ion and shotgun EMP add twice the HE premium. No components were added. The disposable launchers were not changed.

| Recipe | Before | Value | After | Value |
|---|---|---|---|---|
| `MakeAmmo_5x20mmCharged` | 1 Plasteel, 3 Steel | 7 | 4 Plasteel, 16 Steel | 32 |
| `MakeAmmo_5x20mmCharged_Conc` | 1 Plasteel, 3 Steel | 7 | 5 Plasteel, 20 Steel | 40 |
| `MakeAmmo_5x20mmCharged_Ion` | 1 Plasteel, 3 Steel | 7 | 4 Plasteel, 16 Steel, 9 FSX | 68 |
| `MakeAmmo_6x26mmCharged` | 1 Plasteel, 3 Steel | 7 | 4 Plasteel, 16 Steel | 32 |
| `MakeAmmo_6x26mmCharged_Conc` | 1 Plasteel, 4 Steel | 8 | 5 Plasteel, 20 Steel | 40 |
| `MakeAmmo_6x26mmCharged_Ion` | 1 Plasteel, 4 Steel | 8 | 4 Plasteel, 16 Steel, 9 FSX | 68 |
| `MakeAmmo_7x30mmCharged` | 1 Plasteel, 4 Steel | 8 | 4 Plasteel, 17 Steel | 33 |
| `MakeAmmo_7x30mmCharged_Conc` | 1 Plasteel, 5 Steel | 9 | 5 Plasteel, 21 Steel | 41 |
| `MakeAmmo_7x30mmCharged_Ion` | 2 Plasteel, 5 Steel | 13 | 4 Plasteel, 17 Steel, 9 FSX | 69 |
| `MakeAmmo_9x45mmCharged` | 3 Plasteel, 9 Steel | 21 | 38 Plasteel, 153 Steel | 305 |
| `MakeAmmo_9x45mmCharged_Conc` | 4 Plasteel, 11 Steel | 27 | 48 Plasteel, 191 Steel | 383 |
| `MakeAmmo_9x45mmCharged_Ion` | 4 Plasteel, 12 Steel | 28 | 38 Plasteel, 153 Steel, 18 FSX | 377 |
| `MakeAmmo_16GaugeCharged` | 2 Plasteel, 6 Steel | 14 | 25 Plasteel, 101 Steel | 201 |
| `MakeAmmo_16GaugeCharged_Slug` | 2 Plasteel, 6 Steel | 14 | 25 Plasteel, 101 Steel | 201 |
| `MakeAmmo_16GaugeCharged_Ion` | 2 Plasteel, 8 Steel | 16 | 25 Plasteel, 101 Steel, 18 FSX | 273 |
| `MakeAmmo_55x22mmSolarCharged` | 1 Plasteel, 4 Steel | 8 | 11 Sunsteel | 48 |
| `MakeAmmo_55x22mmSolarCharged_Conc` | 2 Plasteel, 5 Steel | 13 | 14 Sunsteel | 61 |
| `MakeAmmo_55x22mmSolarCharged_Ion` | 2 Plasteel, 5 Steel | 13 | 11 Sunsteel, 27 Neutronium | 102 |
| `MakeAmmo_65x28mmSolarCharged` | 1 Plasteel, 5 Steel | 9 | 12 Sunsteel | 52 |
| `MakeAmmo_65x28mmSolarCharged_Conc` | 2 Plasteel, 6 Steel | 14 | 15 Sunsteel | 65 |
| `MakeAmmo_65x28mmSolarCharged_Ion` | 2 Plasteel, 6 Steel | 14 | 12 Sunsteel, 27 Neutronium | 106 |
| `MakeAmmo_75x34mmSolarCharged` | 2 Plasteel, 6 Steel | 14 | 12 Sunsteel | 52 |
| `MakeAmmo_75x34mmSolarCharged_Conc` | 2 Plasteel, 7 Steel | 15 | 15 Sunsteel | 65 |
| `MakeAmmo_75x34mmSolarCharged_Ion` | 2 Plasteel, 8 Steel | 16 | 12 Sunsteel, 27 Neutronium | 106 |
| `MakeAmmo_10x50mmSolarCharged` | 5 Plasteel, 15 Steel | 35 | 106 Sunsteel | 460 |
| `MakeAmmo_10x50mmSolarCharged_Conc` | 6 Plasteel, 18 Steel | 42 | 132 Sunsteel | 573 |
| `MakeAmmo_10x50mmSolarCharged_Ion` | 6 Plasteel, 19 Steel | 43 | 106 Sunsteel, 54 Neutronium | 568 |
| `MakeAmmo_14GaugeSolarCharged` | 3 Plasteel, 9 Steel | 21 | 70 Sunsteel | 304 |
| `MakeAmmo_14GaugeSolarCharged_Slug` | 3 Plasteel, 9 Steel | 21 | 70 Sunsteel | 304 |
| `MakeAmmo_14GaugeSolarCharged_Ion` | 4 Plasteel, 12 Steel | 28 | 70 Sunsteel, 54 Neutronium | 412 |
| `MakeAmmo_62x24mmKwazaariCharged` | 2 Plasteel, 7 Steel | 15 | 15 QuasarAlloy | 75 |
| `MakeAmmo_62x24mmKwazaariCharged_Conc` | 2 Plasteel, 8 Steel | 16 | 18 QuasarAlloy | 90 |
| `MakeAmmo_62x24mmKwazaariCharged_Ion` | 3 Plasteel, 9 Steel | 21 | 15 QuasarAlloy, 40 Neutronium | 155 |
| `MakeAmmo_72x30mmKwazaariCharged` | 2 Plasteel, 8 Steel | 16 | 15 QuasarAlloy | 75 |
| `MakeAmmo_72x30mmKwazaariCharged_Conc` | 3 Plasteel, 9 Steel | 21 | 19 QuasarAlloy | 95 |
| `MakeAmmo_72x30mmKwazaariCharged_Ion` | 3 Plasteel, 10 Steel | 22 | 15 QuasarAlloy, 40 Neutronium | 155 |
| `MakeAmmo_82x38mmKwazaariCharged` | 3 Plasteel, 10 Steel | 22 | 15 QuasarAlloy | 75 |
| `MakeAmmo_82x38mmKwazaariCharged_Conc` | 4 Plasteel, 12 Steel | 28 | 19 QuasarAlloy | 95 |
| `MakeAmmo_82x38mmKwazaariCharged_Ion` | 4 Plasteel, 13 Steel | 29 | 15 QuasarAlloy, 40 Neutronium | 155 |
| `MakeAmmo_11x55mmKwazaariCharged` | 8 Plasteel, 24 Steel | 56 | 137 QuasarAlloy | 688 |
| `MakeAmmo_11x55mmKwazaariCharged_Conc` | 9 Plasteel, 29 Steel | 65 | 171 QuasarAlloy | 858 |
| `MakeAmmo_11x55mmKwazaariCharged_Ion` | 10 Plasteel, 31 Steel | 71 | 137 QuasarAlloy, 81 Neutronium | 850 |
| `MakeAmmo_12GaugeKwazaariCharged` | 5 Plasteel, 15 Steel | 35 | 91 QuasarAlloy | 457 |
| `MakeAmmo_12GaugeKwazaariCharged_Slug` | 5 Plasteel, 15 Steel | 35 | 91 QuasarAlloy | 457 |
| `MakeAmmo_12GaugeKwazaariCharged_Ion` | 6 Plasteel, 20 Steel | 44 | 91 QuasarAlloy, 81 Neutronium | 619 |
| `MakeAmmo_7x22mmCaseless_FMJ` | 1 Plasteel, 3 Steel | 7 | 2 Plasteel, 10 Steel | 18 |
| `MakeAmmo_7x22mmCaseless_AP` | 1 Plasteel, 3 Steel | 7 | 2 Plasteel, 10 Steel | 18 |
| `MakeAmmo_7x22mmCaseless_HP` | 1 Plasteel, 3 Steel | 7 | 2 Plasteel, 10 Steel | 18 |
| `MakeAmmo_6x32mmCaseless_FMJ` | 1 Plasteel, 3 Steel | 7 | 2 Plasteel, 6 Steel | 14 |
| `MakeAmmo_6x32mmCaseless_AP` | 1 Plasteel, 4 Steel | 8 | 2 Plasteel, 6 Steel | 14 |
| `MakeAmmo_6x32mmCaseless_HP` | 1 Plasteel, 3 Steel | 7 | 2 Plasteel, 6 Steel | 14 |
| `MakeAmmo_6x32mmCaseless_Incendiary` | 1 Plasteel, 4 Steel | 8 | 2 Plasteel, 6 Steel, 2 Prometheum | 20 |
| `MakeAmmo_6x32mmCaseless_HE` | 1 Plasteel, 4 Steel | 8 | 2 Plasteel, 6 Steel, 4 FSX | 30 |
| `MakeAmmo_6x32mmCaseless_Sabot` | 1 Plasteel, 4 Steel | 8 | 2 Plasteel, 6 Steel | 14 |
| `MakeAmmo_75x40mmCaseless_FMJ` | 1 Plasteel, 4 Steel | 8 | 3 Plasteel, 12 Steel | 24 |
| `MakeAmmo_75x40mmCaseless_AP` | 1 Plasteel, 5 Steel | 9 | 3 Plasteel, 12 Steel | 24 |
| `MakeAmmo_75x40mmCaseless_Incendiary` | 2 Plasteel, 5 Steel | 13 | 3 Plasteel, 12 Steel, 2 Prometheum | 30 |
| `MakeAmmo_75x40mmCaseless_HE` | 2 Plasteel, 5 Steel | 13 | 3 Plasteel, 12 Steel, 4 FSX | 40 |
| `MakeAmmo_75x40mmCaseless_Sabot` | 2 Plasteel, 5 Steel | 13 | 3 Plasteel, 12 Steel | 24 |
| `MakeAmmo_10x60mmCaseless_FMJ` | 3 Plasteel, 9 Steel | 21 | 6 Plasteel, 22 Steel | 46 |
| `MakeAmmo_10x60mmCaseless_AP` | 4 Plasteel, 11 Steel | 27 | 6 Plasteel, 22 Steel | 46 |
| `MakeAmmo_10x60mmCaseless_Incendiary` | 4 Plasteel, 12 Steel | 28 | 6 Plasteel, 22 Steel, 4 Prometheum | 58 |
| `MakeAmmo_10x60mmCaseless_HE` | 4 Plasteel, 12 Steel | 28 | 6 Plasteel, 22 Steel, 9 FSX | 82 |
| `MakeAmmo_10x60mmCaseless_Sabot` | 4 Plasteel, 13 Steel | 29 | 6 Plasteel, 22 Steel | 46 |
| `MakeAmmo_16GaugeCaseless_Buck` | 2 Plasteel, 6 Steel | 14 | 6 Plasteel, 25 Steel | 49 |
| `MakeAmmo_16GaugeCaseless_Slug` | 2 Plasteel, 6 Steel | 14 | 6 Plasteel, 25 Steel | 49 |
| `MakeAmmo_16GaugeCaseless_Beanbag` | 1 Plasteel, 4 Steel | 8 | 6 Plasteel, 22 Steel | 46 |
| `MakeAmmo_16GaugeCaseless_EMP` | 2 Plasteel, 8 Steel | 16 | 6 Plasteel, 25 Steel, 18 FSX | 121 |
| `MakeAmmo_7x24mmHypersonic_FMJ` | 2 Plasteel, 5 Steel | 13 | 3 Plasteel, 12 Steel | 24 |
| `MakeAmmo_7x24mmHypersonic_AP` | 2 Plasteel, 6 Steel | 14 | 3 Plasteel, 12 Steel | 24 |
| `MakeAmmo_7x24mmHypersonic_HP` | 2 Plasteel, 5 Steel | 13 | 3 Plasteel, 12 Steel | 24 |
| `MakeAmmo_65x36mmHypersonic_FMJ` | 2 Plasteel, 6 Steel | 14 | 3 Plasteel, 12 Steel | 24 |
| `MakeAmmo_65x36mmHypersonic_AP` | 2 Plasteel, 7 Steel | 15 | 3 Plasteel, 12 Steel | 24 |
| `MakeAmmo_65x36mmHypersonic_Incendiary` | 2 Plasteel, 8 Steel | 16 | 3 Plasteel, 12 Steel, 4 Prometheum | 36 |
| `MakeAmmo_65x36mmHypersonic_HE` | 2 Plasteel, 8 Steel | 16 | 3 Plasteel, 12 Steel, 8 FSX | 56 |
| `MakeAmmo_65x36mmHypersonic_Sabot` | 3 Plasteel, 8 Steel | 20 | 3 Plasteel, 12 Steel | 24 |
| `MakeAmmo_8x44mmHypersonic_FMJ` | 2 Plasteel, 8 Steel | 16 | 5 Plasteel, 21 Steel | 41 |
| `MakeAmmo_8x44mmHypersonic_AP` | 3 Plasteel, 9 Steel | 21 | 5 Plasteel, 21 Steel | 41 |
| `MakeAmmo_8x44mmHypersonic_Incendiary` | 3 Plasteel, 10 Steel | 22 | 5 Plasteel, 21 Steel, 4 Prometheum | 53 |
| `MakeAmmo_8x44mmHypersonic_HE` | 3 Plasteel, 10 Steel | 22 | 5 Plasteel, 21 Steel, 8 FSX | 73 |
| `MakeAmmo_8x44mmHypersonic_Sabot` | 3 Plasteel, 11 Steel | 23 | 5 Plasteel, 21 Steel | 41 |
| `MakeAmmo_11x68mmHypersonic_AP` | 7 Plasteel, 22 Steel | 50 | 10 Plasteel, 41 Steel | 81 |
| `MakeAmmo_11x68mmHypersonic_Incendiary` | 8 Plasteel, 24 Steel | 56 | 10 Plasteel, 41 Steel, 8 Prometheum | 105 |
| `MakeAmmo_11x68mmHypersonic_HE` | 8 Plasteel, 24 Steel | 56 | 10 Plasteel, 41 Steel, 17 FSX | 149 |
| `MakeAmmo_11x68mmHypersonic_Sabot` | 8 Plasteel, 26 Steel | 58 | 10 Plasteel, 41 Steel | 81 |
| `MakeAmmo_14GaugeHypersonic_Buck` | 4 Plasteel, 12 Steel | 28 | 11 Plasteel, 45 Steel | 89 |
| `MakeAmmo_14GaugeHypersonic_Slug` | 4 Plasteel, 12 Steel | 28 | 11 Plasteel, 45 Steel | 89 |
| `MakeAmmo_14GaugeHypersonic_Beanbag` | 3 Plasteel, 8 Steel | 20 | 10 Plasteel, 41 Steel | 81 |
| `MakeAmmo_14GaugeHypersonic_EMP` | 5 Plasteel, 15 Steel | 35 | 11 Plasteel, 45 Steel, 33 FSX | 221 |
