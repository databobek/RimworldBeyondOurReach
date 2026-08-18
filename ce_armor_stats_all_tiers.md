# BOR Combat Extended — Armor Stats by Tier (CE-only overrides)

Only values coming from **Combat Extended (CE)** patches are included:
- Files that live in the CE-only patch folder `1.6/Mods/CombatExtended/Patches/additions/` (`tier1armourcepatch.xml`, `tier1_apparel_helmets.xml`, `tier2armourcepatch.xml`, `tier2_apparel_helmets.xml`, `tier3armourcepatch.xml`).
- The `PatchOperationFindMod` **"Combat Extended" `match`** branch inside `1.6/Mods/Biotech/Patches/quasar_mechlord.xml` and `tenebral_mechlord.xml` (their non-CE base-game stats are excluded).

**"—"** = this stat is not touched by the CE patch (value is inherited from a base/vanilla ThingDef defined elsewhere, out of scope here).

---

## Tier I — Solar/Sunsteel (`additions/tier1armourcepatch.xml`, `additions/tier1_apparel_helmets.xml`)

### Body Armor

| Armor | MaxHitPoints | ArmorRating_Sharp | ArmorRating_Blunt | Notes |
| --- | --- | --- | --- | --- |
| `BOR_Bionic_Assembly_Harness` | — | — | — | CE patch only adds Bulk/WornBulk + per-part `PartialArmorExt` multipliers; no flat stat override |
| `BOR_Sunforge_Welders_Coat` | — | 3 | 1.88 | — |
| `BOR_Solar_Apparel_FlakPants` | — | 3 | 1.88 | — |
| `BOR_Solar_Apparel_PowerArmor` | 625 | 25 | 56 | — |
| `BOR_Solar_Apparel_ArmorRecon` | 500 | 20 | 43 | — |
| `BOR_Solar_Apparel_ArchangelArmor` | 500 | 18 | 38.7 | Requires Royalty DLC (`MayRequire="ludeon.rimworld.royalty"`) |

### Helmets

| Armor | MaxHitPoints | ArmorRating_Sharp | ArmorRating_Blunt | Notes |
| --- | --- | --- | --- | --- |
| `BOR_Solar_Apparel_PowerArmorHelmet` | 300 | 20 | 45 | — |
| `BOR_Solar_Apparel_ArmorHelmetRecon` | 238 | 15 | 33 | — |
| `BOR_Solar_Apparel_ArchangelHelmet` | 238 | 13.5 | 29.7 | Requires Royalty DLC |

---

## Tier II — Quasar (`additions/tier2armourcepatch.xml`, `additions/tier2_apparel_helmets.xml`, `Biotech/Patches/quasar_mechlord.xml` CE branch)

### Body Armor

| Armor | MaxHitPoints | ArmorRating_Sharp | ArmorRating_Blunt | Notes |
| --- | --- | --- | --- | --- |
| `BOR_Quasar_Assembly_Carapace` | — | — | — | CE patch only adds Bulk/WornBulk + `PartialArmorExt` (6.25 Sharp/Blunt, Shoulder/Stomach/Spine only); no flat stat override |
| `BOR_Quasar_Masterforge_Mantle` | — | 3 | 2.81 | — |
| `BOR_Quasar_Apparel_FlakPants` | — | 3 | 2.81 | — |
| `ApparelArmorPowerQuasarBase` (abstract) | 938 | 38 | 83 | Shared base stats for Quasar power-armor suits |
| `BOR_Quasar_Apparel_ArmorHelmetRecon` | 750 | 30 | 63 | Note: despite the "Helmet" defName this entry is patched with CarryWeight/MoveSpeed offsets — a body-armor role, not a helmet |
| `BOR_Quasar_Apparel_ArmorRecon` | 357 | 23 | 49 | — |
| `BOR_Apparel_QuasarMechlordSuit` | 475 | 18 | 25 | From `quasar_mechlord.xml`'s CE `match` branch (base/non-CE stats: MaxHitPoints not set, Sharp 0.8, Blunt 0.6 — excluded here) |

### Helmets

| Armor | MaxHitPoints | ArmorRating_Sharp | ArmorRating_Blunt | Notes |
| --- | --- | --- | --- | --- |
| `ApparelArmorHelmetPowerQuasarBase` (abstract) | 450 | 30 | 68 | Shared base stats for Quasar power-armor helmets |
| `BOR_Apparel_QuasarArmorHelmetMechlordHelmet` | 325 | 19 | 30 | From `quasar_mechlord.xml`'s CE `match` branch (base/non-CE MaxHitPoints was 250 — excluded here) |

---

## Tier III — Tenebral (`additions/tier3armourcepatch.xml`, `Biotech/Patches/tenebral_mechlord.xml` CE branch)

### Body Armor

| Armor | MaxHitPoints | ArmorRating_Sharp | ArmorRating_Blunt | Notes |
| --- | --- | --- | --- | --- |
| `BOR_Tenebral_Apparel_PowerArmor` | 1687 | 107 | 240 | — |
| `BOR_Apparel_TenebralMechlordSuit` | 675 | 30 | 50 | From `tenebral_mechlord.xml`'s CE `match` branch (base/non-CE stats: MaxHitPoints not set, Sharp 1.2, Blunt 1.0 — excluded here) |

### Helmets

| Armor | MaxHitPoints | ArmorRating_Sharp | ArmorRating_Blunt | Notes |
| --- | --- | --- | --- | --- |
| `BOR_Apparel_TenebralArmorHelmetMechlordHelmet` | 490 | 30 | 45 | From `tenebral_mechlord.xml`'s CE `match` branch (base/non-CE MaxHitPoints was 450 — excluded here) |

---

## Cross-tier progression at a glance

### Body Armor / Suits

| Tier | Armor | MaxHitPoints | ArmorRating_Sharp | ArmorRating_Blunt |
| --- | --- | --- | --- | --- |
| I | `BOR_Solar_Apparel_PowerArmor` | 625 | 25 | 56 |
| I | `BOR_Solar_Apparel_ArmorRecon` | 500 | 20 | 43 |
| II | `ApparelArmorPowerQuasarBase` | 938 | 38 | 83 |
| II | `BOR_Apparel_QuasarMechlordSuit` | 475 | 18 | 25 |
| II | `BOR_Quasar_Apparel_ArmorRecon` | 357 | 23 | 49 |
| III | `BOR_Tenebral_Apparel_PowerArmor` | 1687 | 107 | 240 |
| III | `BOR_Apparel_TenebralMechlordSuit` | 675 | 30 | 50 |

### Helmets

| Tier | Armor | MaxHitPoints | ArmorRating_Sharp | ArmorRating_Blunt |
| --- | --- | --- | --- | --- |
| I | `BOR_Solar_Apparel_PowerArmorHelmet` | 300 | 20 | 45 |
| I | `BOR_Solar_Apparel_ArmorHelmetRecon` | 238 | 15 | 33 |
| II | `ApparelArmorHelmetPowerQuasarBase` | 450 | 30 | 68 |
| II | `BOR_Apparel_QuasarArmorHelmetMechlordHelmet` | 325 | 19 | 30 |
| III | `BOR_Apparel_TenebralArmorHelmetMechlordHelmet` | 490 | 30 | 45 |

*Mechlord suits/helmets trade armor rating for `MechBandwidth`/`MechControlGroups` and are intentionally weaker (per tier) than the corresponding dedicated power-armor line.*
