# Player site

The site at https://databobek.github.io/RimworldBeyondOurReach/ shows players what Beyond Our Reach adds. It is not part of the mod: `_PublisherPlus.xml` keeps this folder out of the Workshop upload.

## How it works

`build.py` reads the mod the way RimWorld does. It loads the defs from the folders in `LoadFolders.xml`, applies the patches, resolves inheritance, honors `MayRequire`, and applies the English DefInjected text. It does this three times, once per set of active mods:

| Set | Active mods | Loaded folders | Used for |
|---|---|---|---|
| vanilla | all DLCs | the root, `Common`, the DLC folders and anything else whose conditions hold (MedicineCare, Harbinger) | vanilla stats |
| ce | all DLCs plus Combat Extended | the same plus `Mods/CE` | Combat Extended stats |
| all | every mod BOR integrates with, except CE | every integration folder | the galleries |

Every stat, icon, research cost and Kwazaari favor on the site comes from those defs, so the tables stay correct when you rebalance. The written text lives in `content/` and has to be updated by hand when a mechanic changes.

Vanilla's, the DLCs' and Combat Extended's own defs aren't in the repo, but some BOR defs inherit stats from them (a mechanitor helmet's armor, a laser bullet's penetration). `gen/external_parents.xml` holds copies of those base defs. After a game or CE update, refresh it on a machine with the game installed and commit the result:

```
python site/build.py --refresh-parents
```

Bases that come from other mods (Adaptive Storage, Vanilla Expanded and so on) stay unresolved; the refresh lists them.

## Building and previewing

```
pip install -r site/requirements.txt
python site/build.py --serve
```

Then open http://localhost:8000. The build also lists problems it found, like a missing banner or an unknown def name, and which screenshots are still missing. It only ever deletes an output folder it created itself.

Pushing to `main` rebuilds and publishes the site through `.github/workflows/pages.yml`. One-time setup on GitHub: **Settings > Pages > Source: GitHub Actions**. Pages only works on a public repository with a free account.

The workflow builds with `--strict`, so a problem (a page naming a def that no longer exists, say) fails the run and the live site stays as it was. GitHub emails you when a run fails. The site is built from what's committed, so commit new content before or together with the pages that describe it.

## Where things live

| Path | What |
|---|---|
| `content/*.md` | One Markdown file per page. Front matter sets the home page card (`summary`, `card`). |
| `build.py` | The top bar groups and page order (`NAV`), the helpers pages can call, and the page rendering. |
| `gen/defs.py` | Loading, patching, inheritance and `MayRequire`. |
| `gen/external.py`, `gen/external_parents.xml` | The vanilla/DLC/CE base defs BOR inherits from (see above). |
| `gen/extract.py` | Turns defs into table rows and gallery cards. Tech line rules, stat labels and mod names are at the top. |
| `templates/` | Page layout (`base.html`, `page.html`, `home.html`) and the tables and galleries (`macros.html`). |
| `static/` | CSS, the Vanilla/CE switch script and the Audiowide font (OFL). |
| `banners/` | The Workshop infographics that still match the mod. |
| `screenshots/` | Your in-game screenshots. |

## Helpers a page can call

```
{{ weapon_tables() }}          ranged weapons, vanilla and CE
{{ weapon_tables("orientis") }}   one tech line only, shown expanded (also melee_tables, turret_tables, apparel_tables)
{{ melee_tables() }}           melee weapons
{{ turret_tables() }}          turrets
{{ apparel_tables("armor") }}  or "apparel"
{{ gallery("bionics") }}       or "medicine", "buildings"; gallery("buildings", costs=True) adds costs
{{ research_tables() }}        every research project
{{ kwazaari_favors() }}        scrip favors with cost and cooldown
{{ faction("BOR_SolarRepublic") }}
{{ things("BOR_Sunsteel", "BOR_Sunweave") }}   cards for any defs, with their costs
{{ made_at("BOR_Orientis_OfferingsBench") }}   cards for everything a bench's recipes make
{{ portrait("BOR_Helios", "caption") }}        a whole mech or vehicle, put together from its defs the way the game
                               draws it (turrets, mechsuit parts); facing="east" or "south" to turn it
{{ abilities("BOR_OrientisManifest") }}         ability cards: cooldown and the gear that grants them
{{ value("ThingDef", "BOR_Orientis_MeditationPod", "comps/li[@Class='...']/powerPerDay") }}
                               a number from a def, inside a sentence; fmt: num, pct, days, seconds, across
{{ keyed_quote("BOR_OrientisChoiceText") }}    quote an English Keyed string (game UI text)
{{ def_quote("XenotypeDef", "BOR_OrientisXenotype") }}   quote a def's in-game description
{{ emblem("BOR/Things/Factions/OrientisIcon", "badge", ...) }}   an icon with badges
{{ banner("weapons", "caption") }}             banners/weapons.png
{{ screenshot("helios", "caption") }}          screenshots/helios.png
{{ requirements() }}           from About.xml
{{ integration_list() }}       from LoadFolders.xml
```

Prefer `value()` over typing a number into the text: when the def changes, the page follows.

Umbral and Orientis groups start collapsed as spoilers on the general pages; the Orientis page itself shows its own gear expanded, under a spoiler warning at the top. That's set by `SPOILER_TIERS` in `gen/extract.py`. Any other item whose description mentions `SPOILER_WORDS` (Orientis, crystallization) shows its description only on request.

To hide written text, wrap it in `<details class="spoiler tier-orientis" markdown="1">` with a `<summary>` that gives nothing away, as `factions.md` and `tenebral-road.md` do. Don't put a `##` heading inside: the sidebar lists every `##` heading by name.

## Screenshots

Drop a PNG, JPG or WebP with one of these names into `screenshots/` and it appears on its page at the next build. Until then, nothing shows there. To add a new one, call `screenshot("name", "caption")` in a page.

| File name | Page | What to capture |
|---|---|---|
| `home-colony` | Home | A late-game colony running on Kwazaari technology |
| `faction-solar` | Factions | A Solar Republic trade caravan |
| `faction-kwazaari` | Factions | The Kwazaari console window with scrip and favors |
| `faction-tenebral` | Factions | A Tenebral raid |
| `strange-signal` | Research | A colonist investigating a strange signal |
| `dark-pact` | Research | A dark pact offered at the dark mirror |
| `neutronium-net` | Materials | Condensers, pipes and tanks |
| `forge-tab` | Materials | The forge tab with schematics queued |
| `buildings-solar` | Buildings | A Solar-tier workshop |
| `turrets` | Turrets | A turret line holding off a raid |
| `armor-lineup` | Armor | Marine armor from all three tiers |
| `mechs` | Mechs & vehicles | Solar and Kwazaari work mechs |
| `helios` | Mechs & vehicles | The Helios mechsuit |
| `combat-truck` | Mechs & vehicles | The Kwazaari combat truck |
| `harbinger` | Mechs & vehicles | The Umbral harbinger |
| `tenebral-pawn` | Tenebral road | A colonist with tenebral implants |
| `orientis-choice` | Orientis | The Light/Dark choice letter |
| `orientis-manifest` | Orientis | The manifest window |
| `orientis-meditation` | Orientis | Orientis colonists meditating |
| `orientis-shield` | Orientis | A meditation shield holding off a raid |
| `orientis-blessing` | Orientis | Choosing parts at the blessing bench |
| `orientis-crusaders` | Orientis | Orientis crusaders |

A home page card can use a screenshot too: set `card: shot:<name>` in the page's front matter.
