"""Turn resolved defs into the rows and cards the site renders.

Every number shown on the site comes from here, read out of the same XML the
game loads. Three def sets are used:
- vanilla: base mod plus DLC folders, Combat Extended not loaded
- ce: the same with Combat Extended loaded
- all: every integration folder except CE, for the galleries
"""
from __future__ import annotations

import re
from dataclasses import dataclass, field

from .defs import CE_PACKAGE_ID, Def, DefDB, LoadFolder
from .textures import Layer, Textures

# Tech lines in display order. Spoiler lines start collapsed on the site.
TIERS = {
    "solar": "Solar",
    "kwazaari": "Kwazaari",
    "badzaari": "Badzaari",
    "tenebral": "Tenebral",
    "umbral": "Umbral",
    "orientis": "Orientis",
    "other": "General",
}
SPOILER_TIERS = frozenset({"umbral", "orientis"})
# A description outside the spoiler groups that mentions these gives away the
# Tenebral road; the site shows it only on request.
SPOILER_WORDS = re.compile(r"orientis|crystalli[sz]", re.I)

MOD_NAMES = {
    "ludeon.rimworld.royalty": "Royalty",
    "ludeon.rimworld.ideology": "Ideology",
    "ludeon.rimworld.biotech": "Biotech",
    "ludeon.rimworld.anomaly": "Anomaly",
    "ludeon.rimworld.odyssey": "Odyssey",
    "adaptive.storage.framework": "Adaptive Storage Framework",
    "assssssqwww.visualtweaks": "More Shader Visuals",
    "automatic.bionicicons": "Bionic Icons",
    "bamba.allbambaarmors": "Armors For All Post-Medieval Tech Levels",
    "bamba.allbambamelee.tiered": "Melee Weapons For All Post-Medieval Tech Levels",
    "ceteam.combatextended": "Combat Extended",
    "cf.chempowereddeepdrill": "Chemfuel Powered Deepdrill",
    "co.uk.epicguru.meleeanimation": "Melee Animation",
    "dubwise.dubsbadhygiene": "Dubs Bad Hygiene",
    "fox.simplelearning": "Simple Learning",
    "hobbes.bamba.evenmorelinkables16": "Even More Linkables",
    "hobbes.terrapumps": "TerraPumps",
    "issaczhuang.muzzleflash": "Muzzle Flash",
    "kentington.saveourship2": "Save Our Ship 2",
    "koth.modmedicinepatch": "Mod Medicine Patch",
    "lazyfridaystudio.uirethemed.core": "[LFS] UI Rethemed",
    "lts.i": "LTS Integrated Implants",
    "mlie.showmeyourhands": "Show Me Your Hands",
    "mrk.researchcommission": "Research Commission",
    "onofre150.me.vaccines": "Medical Expansion - Vaccines",
    "oskarpotocki.vanillafactionsexpanded.core": "Vanilla Expanded Framework",
    "shunthewitch.mechsuitframework": "Coventech Mechsuit Framework",
    "smashphil.vehicleframework": "Vehicle Framework",
    "sumghai.medpod": "MedPod",
    "vanillaexpanded.skills": "Vanilla Skills Expanded",
    "vanillaexpanded.vfesecurity": "Vanilla Furniture Expanded - Security",
    "vanillaracesexpanded.android": "Vanilla Races Expanded - Android",
    "vanillastorytellersexpanded.winstonwave": "Winston Waves",
    "vat.epoeforked": "EPOE-Forked",
}

# Name tokens first, then the file path. Order matters: _TIII before _TII before _TI.
_TIER_RULES = [
    ("orientis", re.compile(r"orientis|crusader")),
    ("umbral", re.compile(r"umbral|(^|[^a-z])tier[45]/|_(iv|v)\b")),
    ("badzaari", re.compile(r"badzaar")),
    ("tenebral", re.compile(r"tenebr|_tiii\b|(^|[^a-z])tier3/|_iii\b")),
    ("kwazaari", re.compile(r"quasar|kwazaar|_tii\b|(^|[^a-z])tier2/|_ii\b")),
    ("solar", re.compile(r"solar|sunforge|sunsteel|sunweave|kylar|_ti\b|(^|[^a-z])tier1/|_i\b")),
]

# Vanilla stat names shown in effect lists. BOR's own stats use their def labels.
STAT_LABELS = {
    "MoveSpeed": ("move speed", "abs"),
    "WorkSpeedGlobal": ("global work speed", "pct"),
    "ShootingAccuracyPawn": ("shooting accuracy", "abs"),
    "MeleeHitChance": ("melee hit chance", "abs"),
    "MeleeDodgeChance": ("melee dodge chance", "abs"),
    "AimingDelayFactor": ("aiming time", "pct"),
    "CarryingCapacity": ("carrying capacity", "abs"),
    "MedicalTendQuality": ("medical tend quality", "pct"),
    "MedicalTendSpeed": ("medical tend speed", "pct"),
    "MedicalSurgerySuccessChance": ("surgery success chance", "pct"),
    "ResearchSpeed": ("research speed", "pct"),
    "GlobalLearningFactor": ("learning rate", "pct"),
    "MiningSpeed": ("mining speed", "pct"),
    "MiningYield": ("mining yield", "pct"),
    "ConstructionSpeed": ("construction speed", "pct"),
    "ConstructSuccessChance": ("construct success chance", "pct"),
    "PlantWorkSpeed": ("plant work speed", "pct"),
    "PlantHarvestYield": ("plant harvest yield", "pct"),
    "SmeltingSpeed": ("smelting speed", "pct"),
    "GeneralLaborSpeed": ("general labor speed", "pct"),
    "SocialImpact": ("social impact", "pct"),
    "NegotiationAbility": ("negotiation ability", "pct"),
    "TradePriceImprovement": ("trade price improvement", "pct"),
    "PsychicSensitivity": ("psychic sensitivity", "pct"),
    "ToxicResistance": ("toxic resistance", "pct"),
    "ToxicEnvironmentResistance": ("toxic environment resistance", "pct"),
    "MentalBreakThreshold": ("mental break threshold", "pct"),
    "RestRateMultiplier": ("rest rate", "pct"),
    "EatingSpeed": ("eating speed", "pct"),
    "ImmunityGainSpeed": ("immunity gain speed", "pct"),
    "PainShockThreshold": ("pain shock threshold", "pct"),
    "Insulation_Cold": ("cold insulation", "temp"),
    "Insulation_Heat": ("heat insulation", "temp"),
    "ArmorRating_Sharp": ("sharp armor", "pct"),
    "ArmorRating_Blunt": ("blunt armor", "pct"),
    "ArmorRating_Heat": ("heat armor", "pct"),
    "MechBandwidth": ("mech bandwidth", "abs"),
    "MechControlGroups": ("mech control groups", "abs"),
    "MechRemoteRepairDistance": ("mech remote repair distance", "abs"),
    "MechFormingSpeed": ("mech gestation speed", "pct"),
    "MechRepairSpeed": ("mech repair speed", "pct"),
    "JumpRange": ("jump range", "abs"),
    "SlaveSuppressionOffsetWorn": ("slave suppression", "pct"),
    "VacuumResistance": ("vacuum resistance", "pct"),
    "VEF_MassCarryCapacity": ("caravan carry capacity", "abs"),
    "SlaveSuppressionOffset": ("slave suppression", "pct"),
    "CarryBulk": ("carry bulk", "abs"),
    "CarryWeight": ("carry weight", "abs"),
    "WornBulk": ("worn bulk", "abs"),
    "Suppressability": ("suppressability", "pct"),
    "SmokeSensitivity": ("smoke sensitivity", "pct"),
    "ReloadSpeed": ("reload speed", "pct"),
    "AimingAccuracy": ("aiming accuracy", "pct"),
    "MeleeCritChance": ("melee crit chance", "pct"),
    "MeleeParryChance": ("melee parry chance", "pct"),
    "MeleeDodgeChanceCE": ("melee dodge chance", "pct"),
    "NightVisionEfficiency": ("night vision", "pct"),
    "Comfort": ("comfort", "pct"),
}

DAMAGE_WORDS = {
    "Bullet": "", "Arrow": "", "Bomb": "explosive", "EMP": "EMP", "Stun": "stun",
    "Flame": "fire", "Burn": "burn", "Cut": "cut", "Stab": "stab", "Blunt": "blunt",
    "Frostbite": "frost", "Electrical": "electrical", "Smoke": "smoke", "Vaporize": "vaporize",
}


def tier_of(d: Def) -> str:
    name = f"{d.def_name} {d.label}".lower()
    for tier, rx in _TIER_RULES:
        if rx.search(name):
            return tier
    path = d.file.lower()
    for tier, rx in _TIER_RULES:
        if rx.search(path):
            return tier
    return "other"


_ACRONYMS = re.compile(r"\b(dmr|lmg|smg|emp)\b")


def cap(s: str | None) -> str:
    """Capitalize like the game's UI, and fix acronyms the defs write in lower case."""
    if not s:
        return ""
    s = _ACRONYMS.sub(lambda m: m.group(1).upper(), s)
    return s[:1].upper() + s[1:]


def humanize(name: str) -> str:
    name = re.sub(r"^BOR_", "", name)
    name = name.replace("_", " ")
    name = re.sub(r"(?<=[a-z])(?=[A-Z])", " ", name)
    return name.strip().lower()


def _f(el, path: str, default: float | None = None) -> float | None:
    if el is None:
        return default
    v = el.findtext(path)
    if v is None or not v.strip():
        return default
    try:
        return float(v)
    except ValueError:
        return default


def _verb(d: Def):
    verbs = d.node("verbs")
    if verbs is None:
        return None
    lis = [li for li in verbs if li.tag == "li"]
    for li in lis:
        if (li.findtext("defaultProjectile") or "").strip():
            return li
    return lis[0] if lis else None


def _comp(d: Def, cls: str):
    comps = d.node("comps")
    if comps is None:
        return None
    for li in comps:
        if li.get("Class") == cls:
            return li
    return None


def _uniq(xs):
    seen, out = set(), []
    for x in xs:
        if x and x not in seen:
            seen.add(x)
            out.append(x)
    return out


@dataclass
class Group:
    tier: str
    items: list = field(default_factory=list)
    open: bool = False  # shown expanded even if it's a spoiler tier (on its own page)

    @property
    def label(self) -> str:
        return TIERS.get(self.tier, cap(self.tier))

    @property
    def spoiler(self) -> bool:
        return self.tier in SPOILER_TIERS and not self.open


# Vanilla materials that show up in BOR cost lists (their defs aren't in the repo).
VANILLA_MATERIALS = {
    "Steel": "steel", "Plasteel": "plasteel", "ComponentIndustrial": "components",
    "ComponentSpacer": "advanced components", "Gold": "gold", "Silver": "silver", "Uranium": "uranium",
    "Jade": "jade", "Cloth": "cloth", "Hyperweave": "hyperweave", "DevilstrandCloth": "devilstrand",
    "Synthread": "synthread", "WoodLog": "wood", "Chemfuel": "chemfuel", "Neutroamine": "neutroamine",
    "MedicineIndustrial": "medicine", "MedicineUltratech": "glitterworld medicine", "Luciferium": "luciferium",
}


def group_by_tier(items: list[dict]) -> list[Group]:
    order = list(TIERS)
    groups: dict[str, Group] = {}
    for it in items:
        groups.setdefault(it["tier"], Group(it["tier"])).items.append(it)
    for g in groups.values():
        g.items.sort(key=lambda it: it["label"].lower())
    return sorted(groups.values(), key=lambda g: order.index(g.tier) if g.tier in order else len(order))


FACING_TURN = {"north": 0.0, "east": 90.0, "south": 180.0, "west": 270.0}  # degrees clockwise from north


def _vec(text: str | None) -> tuple[float, ...] | None:
    """A drawSize or offset: "4", "(4, 6)" or "(x, layer, z)"."""
    if not text:
        return None
    try:
        return tuple(float(v) for v in text.strip().strip("()").split(","))
    except ValueError:
        return None


def _range(text: str | None) -> tuple[float, float] | None:
    """A FloatRange like "1.5~2.5", or a single number."""
    if not text:
        return None
    lo, _, hi = text.partition("~")
    try:
        return float(lo), float(hi or lo)
    except ValueError:
        return None


class Extractor:
    def __init__(self, dbs: dict[str, DefDB], folders: list[LoadFolder], tex: Textures,
                 hard_deps: set[str] | frozenset[str] = frozenset()):
        self.v = dbs["vanilla"]
        self.ce = dbs["ce"]
        self.all = dbs["all"]
        self.folders = {f.path: f for f in folders}
        self.tex = tex
        self.hard_deps = {i.lower() for i in hard_deps}  # required mods never get a badge
        self._recipe_index: dict[int, tuple[dict, dict]] = {}
        self._altar_index: dict[int, dict[str, str]] = {}
        self.notes: list[str] = []

    # ---------- shared helpers ----------

    def _names(self, ids) -> list[str]:
        return _uniq(MOD_NAMES.get(i, i) for i in ids if i and i not in self.hard_deps)

    def requires(self, d: Def) -> list[str]:
        """DLC/mod badges: the load folder's IfModActive (any of) and IfModActiveAll,
        and the def's own MayRequire (all of) / MayRequireAnyOf."""
        out: list[str] = []
        f = self.folders.get(d.src)
        if f:
            any_of = self._names(f.if_active)
            if any_of:
                out.append(" or ".join(any_of))
            out += self._names(f.if_active_all)
        out += self._names(i.strip().lower() for i in (d.el.get("MayRequire") or "").split(","))
        any_of = self._names(i.strip().lower() for i in (d.el.get("MayRequireAnyOf") or "").split(","))
        if any_of:
            out.append(" or ".join(any_of))
        return _uniq(out)

    def _recipes(self, db: DefDB):
        key = id(db)
        if key not in self._recipe_index:
            by_product: dict[str, list[Def]] = {}
            for r in db.all("RecipeDef"):
                products = r.node("products")
                for p in (products if products is not None else []):
                    by_product.setdefault(p.tag, []).append(r)
            users: dict[str, list[str]] = {}
            for t in db.all("ThingDef"):
                for rn in t.items("recipes"):
                    users.setdefault(rn, []).append(t.def_name)
            self._recipe_index[key] = (by_product, users)
        return self._recipe_index[key]

    def _research_label(self, db: DefDB, name: str) -> str:
        r = db.get("ResearchProjectDef", name) or self.all.get("ResearchProjectDef", name)
        return cap(r.label) if r else humanize(name)

    def _thing_label(self, db: DefDB, name: str) -> str:
        t = db.get("ThingDef", name) or self.all.get("ThingDef", name)
        return cap(t.label) if t else humanize(name)

    def _altars(self, db: DefDB) -> dict[str, str]:
        """Items a dark archotech altar (a forge with a fixed grid) can make."""
        key = id(db)
        if key not in self._altar_index:
            made: dict[str, str] = {}
            for t in db.all("ThingDef"):
                comp = _comp(t, "BeyondOurReach.CompProperties_DarkAltar")
                if comp is None:
                    continue
                for li in comp.findall("entries/li"):
                    thing = (li.findtext("thing") or "").strip()
                    if thing:
                        made.setdefault(thing, t.def_name)
            self._altar_index[key] = made
        return self._altar_index[key]

    def making(self, d: Def, db: DefDB) -> dict:
        research: list[str] = []
        benches: list[str] = []
        if d.node("recipeMaker") is not None:
            research += [d.text("recipeMaker/researchPrerequisite")] + d.items("recipeMaker/researchPrerequisites")
            benches += d.items("recipeMaker/recipeUsers")
        by_product, users = self._recipes(db)
        for r in by_product.get(d.def_name, []):
            research += [r.text("researchPrerequisite")] + r.items("researchPrerequisites")
            benches += r.items("recipeUsers") + users.get(r.def_name, [])
        research += d.items("researchPrerequisites")
        altar = self._altars(db).get(d.def_name)
        if altar:
            benches.append(altar)
        return {
            "research": [self._research_label(db, x) for x in _uniq(research)],
            "benches": [self._thing_label(db, x) for x in _uniq(benches)],
        }

    def pawn_icon(self, d: Def) -> str | None:
        """Pawns (mechs) have no graphicData; their look lives on the PawnKindDef."""
        for pk in self.all.all("PawnKindDef"):
            if pk.text("race") != d.def_name:
                continue
            stages = pk.node("lifeStages")
            for li in reversed(list(stages) if stages is not None else []):
                tex_path = (li.findtext("bodyGraphicData/texPath") or "").strip()
                if tex_path:
                    return self.tex.export(self.tex.find(tex_path))
        return None

    def cost(self, d: Def, db: DefDB) -> list[str]:
        """The def's costList, e.g. ["138 kwazaari alloy", "50 gold"]."""
        out = []
        node = d.node("costList")
        for c in (node if node is not None else []):
            if not isinstance(c.tag, str):
                continue
            try:
                n = float((c.text or "").strip())
            except ValueError:
                continue
            thing = db.get("ThingDef", c.tag) or self.all.get("ThingDef", c.tag)
            label = thing.label if thing else VANILLA_MATERIALS.get(c.tag, humanize(c.tag))
            out.append(f"{n:g} {label}")
        stuff = d.num("costStuffCount")
        if stuff:
            cats = " or ".join(humanize(x) for x in d.items("stuffCategories")) or "material"
            out.append(f"{stuff:g} {cats}")
        return out

    def base_item(self, d: Def, db: DefDB) -> dict:
        icon = self.tex.icon(d)
        if icon is None and d.node("race") is not None:
            icon = self.pawn_icon(d)
        tier = tier_of(d)
        return {
            "def_name": d.def_name,
            "label": cap(d.label),
            "description": d.description,
            "spoiler_desc": tier not in SPOILER_TIERS and bool(SPOILER_WORDS.search(d.description)),
            "icon": icon,
            "tier": tier,
            "requires": self.requires(d),
            "cost": self.cost(d, db),
            **self.making(d, db),
        }

    def abilities(self, names: list[str]) -> list[dict]:
        """Ability cards: icon, text, cooldown (own or its group's) and the gear that grants it."""
        granted: dict[str, list[str]] = {}
        for t in self.all.all("ThingDef"):
            comps = t.node("comps")
            for li in (comps if comps is not None else []):
                given = [(x.text or "").strip() for x in li.findall("abilities/li")]
                given += [(li.findtext("abilityDef") or "").strip()]
                for a in given:
                    if a:
                        granted.setdefault(a, []).append(cap(t.label))
        out = []
        for n in names:
            d = self.all.get("AbilityDef", n)
            if d is None:
                self.notes.append(f"abilities(): no AbilityDef {n}")
                continue
            ticks = _range(d.text("cooldownTicksRange"))
            group = self.all.get("AbilityGroupDef", d.text("groupDef"))
            if ticks is None and group is not None and group.num("cooldownTicks"):
                ticks = (group.num("cooldownTicks"),) * 2
            out.append({
                "label": cap(d.label),
                "description": d.description,
                "icon": self.tex.export(self.tex.find(d.text("iconPath"))),
                "cooldown": (ticks[0] / 60, ticks[1] / 60) if ticks else None,
                "shared": group is not None,
                "granted_by": sorted(set(granted.get(n, []))),
            })
        return out

    def made_at(self, bench: str) -> list[dict]:
        """Everything the bench's recipes produce."""
        _, users = self._recipes(self.all)
        products = []
        for r in self.all.all("RecipeDef"):
            if bench in r.items("recipeUsers") or bench in users.get(r.def_name, []):
                node = r.node("products")
                products += [p.tag for p in (node if node is not None else []) if isinstance(p.tag, str)]
        items = []
        for name in _uniq(products):
            d = self.all.get("ThingDef", name)
            if d is not None:
                items.append(self.base_item(d, self.all))
        return sorted(items, key=lambda it: it["label"].lower())

    def damage_word(self, db: DefDB, name: str | None) -> str:
        if not name:
            return ""
        name = name.removesuffix("_Secondary")  # CE's secondary-damage variants (Bomb_Secondary...)
        if name in DAMAGE_WORDS:
            return DAMAGE_WORDS[name]
        dd = db.get("DamageDef", name) or self.all.get("DamageDef", name)
        return dd.label if dd else humanize(name)

    def stat_effects(self, d: Def, db: DefDB, path: str = "equippedStatOffsets") -> list[str]:
        node = d.node(path)
        out = []
        for st in (node if node is not None else []):
            if not isinstance(st.tag, str):
                continue
            try:
                val = float((st.text or "").strip())
            except ValueError:
                continue
            if val == 0:
                continue
            label, kind = STAT_LABELS.get(st.tag, (None, None))
            if label is None:
                sd = db.get("StatDef", st.tag) or self.all.get("StatDef", st.tag)
                label = sd.label if sd else humanize(st.tag)
                kind = "pct" if sd is not None and (sd.text("toStringStyle") or "").startswith("Percent") else "abs"
            sign = "+" if val > 0 else "−"
            if kind == "pct":
                out.append(f"{sign}{abs(val) * 100:g}% {label}")
            elif kind == "temp":
                out.append(f"{sign}{abs(val):g}°C {label}")
            else:
                out.append(f"{sign}{abs(val):g} {label}")
        return out

    # ---------- weapons ----------

    @staticmethod
    def is_ranged(d: Def) -> bool:
        if d.tag != "ThingDef":
            return False
        if any(d.node(x) is not None for x in ("apparel", "building", "race", "projectile")):
            return False
        if any("Turret" in c for c in d.chain):
            return False
        v = _verb(d)
        return v is not None and bool((v.findtext("defaultProjectile") or "").strip())

    @staticmethod
    def is_melee(d: Def) -> bool:
        if d.tag != "ThingDef":
            return False
        if any(d.node(x) is not None for x in ("apparel", "building", "race", "projectile", "verbs")):
            return False
        if d.node("tools") is None:
            return False
        cats = d.items("thingCategories")
        return "WeaponsMelee" in cats or any("Melee" in c for c in d.chain)

    def gun_vanilla(self, d: Def, db: DefDB) -> dict:
        v = _verb(d)
        proj = db.get("ThingDef", (v.findtext("defaultProjectile") or "").strip())
        dmg = proj.num("projectile/damageAmountBase") if proj else None
        ap = proj.num("projectile/armorPenetrationBase") if proj else None
        if ap is None and dmg is not None:
            ap = dmg * 0.015  # the game's default for projectiles
        return {
            "damage": dmg,
            "damage_type": self.damage_word(db, proj.text("projectile/damageDef") if proj else None),
            "ap": ap,
            "stopping": proj.num("projectile/stoppingPower", 0.5) if proj else None,
            "explosion": proj.num("projectile/explosionRadius") if proj else None,
            "burst": int(_f(v, "burstShotCount", 1)),
            "range": _f(v, "range"),
            "warmup": _f(v, "warmupTime"),
            "cooldown": d.num("statBases/RangedWeapon_Cooldown"),
            "accuracy": [d.num(f"statBases/Accuracy{r}") for r in ("Touch", "Short", "Medium", "Long")],
            "mass": d.num("statBases/Mass"),
        }

    def ce_projectile(self, db: DefDB, name: str | None) -> dict | None:
        p = db.get("ThingDef", name) if name else None
        if p is None:
            return None
        secondary = []
        sec = p.node("projectile/secondaryDamage")
        for li in (sec if sec is not None else []):
            amount = _f(li, "amount")
            kind = self.damage_word(db, (li.findtext("def") or "").strip()) or "extra"
            if amount:
                secondary.append(f"+{amount:g} {kind}")
        damage = p.num("projectile/damageAmountBase")
        unset = 0.0 if damage is not None else None  # CE's default when a projectile sets no penetration
        return {
            "projectile": p.def_name,
            "damage": damage,
            "damage_type": self.damage_word(db, p.text("projectile/damageDef")),
            "ap_sharp": p.num("projectile/armorPenetrationSharp", unset),
            "ap_blunt": p.num("projectile/armorPenetrationBlunt", unset),
            "pellets": p.num("projectile/pelletCount"),
            "explosion": p.num("projectile/explosionRadius"),
            "secondary": secondary,
        }

    def _ammo_types(self, aset: Def) -> list[tuple[str, str]]:
        node = aset.node("ammoTypes")
        out = []
        for c in (node if node is not None else []):
            if c.tag == "li":
                out.append(((c.findtext("ammo") or "").strip(), (c.findtext("projectile") or "").strip()))
            elif isinstance(c.tag, str):
                out.append((c.tag, (c.text or "").strip()))
        return out

    def _ammo_name(self, db: DefDB, ammo: Def | None, fallback: str) -> tuple[str, str]:
        if ammo is None:
            return humanize(fallback), ""
        cls = ammo.text("ammoClass")
        cat = db.get("CombatExtended.AmmoCategoryDef", cls) if cls else None
        if cat is not None:
            return cat.text("labelShort") or cap(cat.label), cap(cat.label)
        m = re.search(r"\(([^)]+)\)\s*$", ammo.label)
        if m:
            return m.group(1), ""
        return humanize(cls or ammo.def_name), ""

    def gun_ce(self, d: Def, db: DefDB) -> dict:
        v = _verb(d)
        ammo_comp = _comp(d, "CombatExtended.CompProperties_AmmoUser")
        fire = _comp(d, "CombatExtended.CompProperties_FireModes")
        patched = ammo_comp is not None or (v is not None and "CE" in (v.get("Class") or ""))
        aset = db.get("CombatExtended.AmmoSetDef", (ammo_comp.findtext("ammoSet") or "").strip()) if ammo_comp is not None else None
        ammo = []
        if aset is not None:
            for ammo_name, proj_name in self._ammo_types(aset):
                stats = self.ce_projectile(db, proj_name)
                if stats is None:
                    continue
                short, long_ = self._ammo_name(db, db.get("ThingDef", ammo_name), ammo_name)
                ammo.append({"name": short, "long": long_, **stats})
        default_proj = (v.findtext("defaultProjectile") or "").strip() if v is not None else ""
        default = next((a for a in ammo if a["projectile"] == default_proj), None)
        if default is None:
            default = ammo[0] if ammo else self.ce_projectile(db, default_proj)
        magazine = _f(ammo_comp, "magazineSize")
        return {
            "patched": patched,
            "caliber": aset.label if aset is not None else None,
            "magazine": int(magazine) if magazine else None,
            "reload": _f(ammo_comp, "reloadTime"),
            "burst": int(_f(v, "burstShotCount", 1)) if v is not None else None,
            "aimed_burst": _f(fire, "aimedBurstShotCount"),
            "range": _f(v, "range"),
            "warmup": _f(v, "warmupTime"),
            "cooldown": d.num("statBases/RangedWeapon_Cooldown"),
            "sights": d.num("statBases/SightsEfficiency"),
            "spread": d.num("statBases/ShotSpread"),
            "sway": d.num("statBases/SwayFactor"),
            "bulk": d.num("statBases/Bulk"),
            "mass": d.num("statBases/Mass"),
            "recoil": _f(v, "recoilAmount"),
            "default": default,
            "ammo": ammo,
        }

    def ranged_weapons(self) -> list[Group]:
        names = _uniq([d.def_name for d in self.v.all("ThingDef") if self.is_ranged(d)]
                      + [d.def_name for d in self.ce.all("ThingDef") if self.is_ranged(d)])
        rows = []
        for name in names:
            vd = self.v.get("ThingDef", name)
            cd = self.ce.get("ThingDef", name)
            d = vd or cd
            row = self.base_item(d, self.v if vd else self.ce)
            row["vanilla"] = self.gun_vanilla(vd, self.v) if vd else None
            row["ce"] = self.gun_ce(cd, self.ce) if cd else None
            if cd is None:
                row["requires"] = [r for r in row["requires"] if r != "Combat Extended"]
            rows.append(row)
        return group_by_tier(rows)

    def _tools(self, d: Def) -> list[dict]:
        node = d.node("tools")
        out = []
        for li in (node if node is not None else []):
            if li.tag != "li":
                continue
            caps = [(c.text or "").strip() for c in (li.find("capacities") if li.find("capacities") is not None else [])]
            power = _f(li, "power")
            ap = _f(li, "armorPenetration")
            if ap is None and power is not None:
                ap = power * 0.015  # the game's default for melee attacks
            out.append({
                "label": (li.findtext("label") or "").strip(),
                "kind": ", ".join(c.lower() for c in caps if c),
                "power": power,
                "cooldown": _f(li, "cooldownTime"),
                "ap": ap,
                "ap_sharp": _f(li, "armorPenetrationSharp"),
                "ap_blunt": _f(li, "armorPenetrationBlunt"),
            })
        return out

    def melee_weapons(self) -> list[Group]:
        names = _uniq([d.def_name for d in self.v.all("ThingDef") if self.is_melee(d)]
                      + [d.def_name for d in self.ce.all("ThingDef") if self.is_melee(d)])
        rows = []
        for name in names:
            vd = self.v.get("ThingDef", name)
            cd = self.ce.get("ThingDef", name)
            d = vd or cd
            row = self.base_item(d, self.v if vd else self.ce)
            stuffed = d.node("stuffCategories") is not None
            row["vanilla"] = {"tools": self._tools(vd), "stuffed": stuffed, "mass": vd.num("statBases/Mass")} if vd else None
            row["ce"] = {"tools": self._tools(cd), "stuffed": stuffed, "mass": cd.num("statBases/Mass"),
                         "bulk": cd.num("statBases/Bulk")} if cd else None
            rows.append(row)
        return group_by_tier(rows)

    # ---------- turrets ----------

    def turrets(self) -> list[Group]:
        rows = []
        names = _uniq([d.def_name for d in self.v.all("ThingDef") if d.text("building/turretGunDef")]
                      + [d.def_name for d in self.ce.all("ThingDef") if d.text("building/turretGunDef")])
        for name in names:
            vb = self.v.get("ThingDef", name)
            cb = self.ce.get("ThingDef", name)
            b = vb or cb
            row = self.base_item(b, self.v if vb else self.ce)
            gun_v = self.v.get("ThingDef", vb.text("building/turretGunDef")) if vb else None
            gun_c = self.ce.get("ThingDef", cb.text("building/turretGunDef")) if cb else None
            if not row["icon"]:
                row["icon"] = self.tex.icon(gun_v or gun_c)
            power = None
            comps = b.node("comps")
            for li in (comps if comps is not None else []):
                if "Power" in (li.get("Class") or "") and li.findtext("basePowerConsumption"):
                    power = _f(li, "basePowerConsumption")
            row["hp"] = b.num("statBases/MaxHitPoints")
            row["power"] = power
            row["size"] = b.text("size")
            row["gun_label"] = cap((gun_v or gun_c).label) if (gun_v or gun_c) else ""
            row["vanilla"] = self.gun_vanilla(gun_v, self.v) if gun_v is not None and _verb(gun_v) is not None else None
            row["ce"] = self.gun_ce(gun_c, self.ce) if gun_c is not None and _verb(gun_c) is not None else None
            # A turret aims and cycles on the building's timings, not the gun verb's.
            for stats, bd in ((row["vanilla"], vb), (row["ce"], cb)):
                if stats is None or bd is None:
                    continue
                stats["warmup_range"] = _range(bd.text("building/turretBurstWarmupTime"))
                cooldown = bd.num("building/turretBurstCooldownTime")
                if cooldown is not None and cooldown > 0:
                    stats["cooldown"] = cooldown
                stats["turret_accuracy"] = bd.num("statBases/ShootingAccuracyTurret")
            rows.append(row)
        return group_by_tier(rows)

    # ---------- apparel ----------

    @staticmethod
    def apparel_kind(d: Def) -> str:
        f = d.file.lower()
        return "apparel" if ("statboost" in f or "belt" in f or "backpack" in f) else "armor"

    def _armor_value(self, d: Def, stat: str) -> dict | None:
        base = d.num(f"statBases/{stat}")
        stuffed = d.node("stuffCategories") is not None
        mult = d.num("statBases/StuffEffectMultiplierArmor") if stat != "ArmorRating_Heat" else None
        if stuffed and mult:
            return {"base": base or 0.0, "stuff": mult}
        if base is None:
            return None
        return {"base": base, "stuff": None}

    def _partial(self, d: Def) -> list[str]:
        """CE partial armor: each value multiplies the item's own rating on the listed parts."""
        out = []
        ext = d.node("modExtensions")
        for li in (ext if ext is not None else []):
            if li.get("Class") != "CombatExtended.PartialArmorExt":
                continue
            stats = li.find("stats")
            for s in (stats if stats is not None else []):
                parts = [humanize(p.text or "") for p in (s.find("parts") if s.find("parts") is not None else [])]
                for st in s:
                    if st.tag not in ("ArmorRating_Sharp", "ArmorRating_Blunt"):
                        continue
                    try:
                        mult = float((st.text or "").strip())
                    except ValueError:
                        continue
                    word, unit = ("sharp", "mm") if st.tag.endswith("Sharp") else ("blunt", "MPa")
                    base = d.num(f"statBases/{st.tag}")
                    value = f" = {base * mult:.3g} {unit}" if base is not None else ""
                    out.append(f"{word} ×{mult:g}{value} on {', '.join(parts)}")
        return out

    def apparel(self, kind: str) -> list[Group]:
        names = _uniq([d.def_name for d in self.v.all("ThingDef") if d.node("apparel") is not None]
                      + [d.def_name for d in self.ce.all("ThingDef") if d.node("apparel") is not None])
        rows = []
        for name in names:
            vd = self.v.get("ThingDef", name)
            cd = self.ce.get("ThingDef", name)
            d = vd or cd
            if self.apparel_kind(d) != kind:
                continue
            row = self.base_item(d, self.v if vd else self.ce)
            row["stuffed"] = d.node("stuffCategories") is not None
            if vd is not None:
                row["vanilla"] = {
                    "sharp": self._armor_value(vd, "ArmorRating_Sharp"),
                    "blunt": self._armor_value(vd, "ArmorRating_Blunt"),
                    "heat": self._armor_value(vd, "ArmorRating_Heat"),
                    "cold": vd.num("statBases/Insulation_Cold"),
                    "hot": vd.num("statBases/Insulation_Heat"),
                    "covers": _uniq(humanize(x) for x in vd.items("apparel/bodyPartGroups")),
                    "effects": self.stat_effects(vd, self.v),
                    "mass": vd.num("statBases/Mass"),
                }
            else:
                row["vanilla"] = None
            if cd is not None:
                row["ce"] = {
                    "sharp": self._armor_value(cd, "ArmorRating_Sharp"),
                    "blunt": self._armor_value(cd, "ArmorRating_Blunt"),
                    "heat": self._armor_value(cd, "ArmorRating_Heat"),
                    "bulk": cd.num("statBases/Bulk"),
                    "worn_bulk": cd.num("statBases/WornBulk"),
                    "mass": cd.num("statBases/Mass"),
                    "covers": _uniq(humanize(x) for x in cd.items("apparel/bodyPartGroups")),
                    "partial": self._partial(cd),
                    "effects": self.stat_effects(cd, self.ce),
                }
            else:
                row["ce"] = None
            rows.append(row)
        return group_by_tier(rows)

    # ---------- galleries ----------

    def _gallery(self, defs: list[Def]) -> list[Group]:
        rows = [self.base_item(d, self.all) for d in defs]
        return group_by_tier(rows)

    def bionics(self) -> list[Group]:
        """One card per installable part. Parts with an item use the item's
        text; surgery-only parts (Umbral) use the hediff and its install recipe.
        Parts whose item uses vanilla's generic health-item texture get the
        tier box from BOR's Bionic Icons integration, as players see it there."""
        installs: dict[str, list[Def]] = {}
        for r in self.all.all("RecipeDef"):
            if r.text("addsHediff"):
                installs.setdefault(r.text("addsHediff"), []).append(r)
        parts = []
        for h in self.all.all("HediffDef"):
            is_part = h.node("addedPartProps") is not None or any(
                k in c for c in h.chain for k in ("AddedBodyPart", "Implant", "BOR_Added"))
            if is_part and (h.def_name in installs or h.text("spawnThingOnRemoved")):
                parts.append(h)
        # Several tenebral parts grow from one shared template item; those get
        # the part's own name and text instead of the template's.
        item_users: dict[str, int] = {}
        for h in parts:
            item_users[h.text("spawnThingOnRemoved") or ""] = item_users.get(h.text("spawnThingOnRemoved") or "", 0) + 1
        rows = []
        for h in parts:
            item = self.all.get("ThingDef", h.text("spawnThingOnRemoved"))
            row = self.base_item(item or h, self.all)
            if item is not None and item_users[item.def_name] > 1:
                row["label"] = cap(h.label)
                row["description"] = h.description or row["description"]
            if item is None:
                research = []
                for r in installs.get(h.def_name, []):
                    research += [r.text("researchPrerequisite")] + r.items("researchPrerequisites")
                row["research"] = [self._research_label(self.all, x) for x in _uniq(research)]
                row["benches"] = ["surgery"]
            if not row["description"]:
                row["description"] = h.description
            if not row["icon"]:
                box = {"solar": "Solar", "kwazaari": "Quasar", "tenebral": "Tenebral", "umbral": "Tenebral"}.get(row["tier"])
                row["icon"] = self.tex.export(self.tex.find(f"BOR/Boxes/{box}")) if box else None
            row["spoiler_desc"] = row["tier"] not in SPOILER_TIERS and bool(SPOILER_WORDS.search(row["description"]))
            rows.append(row)
        return group_by_tier(rows)

    def medicine(self) -> list[Group]:
        defs = [d for d in self.all.all("ThingDef")
                if d.node("ingestible/drugCategory") is not None
                or d.num("statBases/MedicalPotency") is not None
                or any(("Drug" in c or "Medicine" in c or "Vaccine" in c) for c in d.chain)]
        return self._gallery(defs)

    def buildings(self) -> list[Group]:
        # Anything placeable from the Architect menu; turrets have their own page.
        # (Integration buildings may inherit their category from another mod.)
        def is_building(d: Def) -> bool:
            if not d.text("designationCategory") or d.text("building/turretGunDef"):
                return False
            return all(d.node(x) is None for x in ("race", "apparel", "projectile"))
        return self._gallery([d for d in self.all.all("ThingDef") if is_building(d)])

    def things(self, names: list[str]) -> list[dict]:
        out = []
        for n in names:
            d = self.all.get("ThingDef", n) or self.ce.get("ThingDef", n)
            if d is None:
                self.notes.append(f"things(): no ThingDef {n}")
                continue
            out.append(self.base_item(d, self.all))
        return out

    # ---------- whole mechs and vehicles ----------

    def portrait(self, name: str, facing: str | None = None) -> dict | None:
        """A mech or vehicle drawn whole, the way the game puts it together: a vehicle with its turrets, a mechsuit
        with the parts its pawn kind is built with, anything else from its own graphic. Vehicles face east by
        default (side on), everything else south."""
        d = self.all.get("Vehicles.VehicleDef", name) or self.all.get("ThingDef", name)
        if d is None:
            self.notes.append(f"portrait(): no ThingDef or VehicleDef {name}")
            return None
        vehicle = d.tag == "Vehicles.VehicleDef"
        facing = facing or ("east" if vehicle else "south")
        if vehicle:
            layers = self._vehicle_layers(d, facing)
        elif d.node("race") is not None:
            layers = self._mechsuit_layers(d, facing)
        else:
            layers = [self._layer(d.node("graphicData"), facing)]
        pic = self.tex.compose(f"{name}-{facing}", [x for x in layers if x is not None])
        if pic is None:
            self.notes.append(f"portrait(): nothing to draw for {name}")
            return None
        return {**pic, "label": cap(d.label)}

    def _layer(self, graphic, facing: str, offset: tuple[float, float] = (0.0, 0.0), turn: float = 0.0,
               depth: float = 0.0, sideways: bool = False) -> Layer | None:
        """A Layer from a graphicData node. `sideways` swaps a non-square drawSize facing east or west, as vehicles do."""
        if graphic is None:
            return None
        tex_path = (graphic.findtext("texPath") or "").strip()
        src = self.tex.find_side(tex_path, facing)
        if src is None:
            self.notes.append(f"portrait(): texture {tex_path} not found")
            return None
        size = _vec(graphic.findtext("drawSize")) or (1.0,)
        w, h = (size[0], size[0]) if len(size) == 1 else size[:2]
        if sideways and facing in ("east", "west"):
            w, h = h, w
        return Layer(src, (w, h), offset, turn, depth)

    def _vehicle_layers(self, d: Def, facing: str) -> list[Layer | None]:
        """The hull, then every turret that has a graphic, pointing the way the vehicle faces."""
        layers = [self._layer(d.node("graphicData"), facing, sideways=True)]
        for t in d.el.iterfind("comps/li/turrets/li"):
            turret = self.all.get("Vehicles.VehicleTurretDef", (t.findtext("turretDef") or "").strip())
            graphic = turret.node("graphicData") if turret else None
            if graphic is None:  # drawn into the hull, like the truck's flank cannons
                continue
            x, z = (_vec(t.findtext(f"renderProperties/{facing}")) or (0.0, 0.0))[:2]
            depth = 1 + float((t.findtext("drawLayer") or "0").strip())
            layers.append(self._layer(graphic, facing, (x, z), FACING_TURN[facing], depth))
        return layers

    def _mechsuit_layers(self, d: Def, facing: str) -> list[Layer | None]:
        """Mechsuit Framework: the body from the pawn kind, then each prebuilt part the mech draws,
        placed by its body slot's (x, layer, z) offset for that facing."""
        kind = next((k for k in self.all.all("PawnKindDef") if k.text("race") == d.def_name), None)
        if kind is None:
            return []
        stages = kind.el.findall("lifeStages/li/bodyGraphicData")
        layers = [self._layer(stages[-1] if stages else None, facing)]
        body = self.all.get("BodyDef", d.text("race/body"))
        offsets = {}
        for slot in body.el.iter("li") if body is not None else []:
            at = _vec(slot.findtext(f"compDrawOffsets/{facing}"))
            if slot.findtext("slotID") and at and len(at) == 3:
                offsets[slot.findtext("slotID").strip()] = at
        for part in kind.el.iterfind("modExtensions/li/prebuiltComponents/li"):
            comp = self.all.get("taranchuk_mechsuits.MechComponentDef", (part.findtext("component") or "").strip())
            at = offsets.get((part.findtext("slotID") or "").strip())
            if comp is None or comp.text("drawOnMech") != "true" or at is None:
                continue
            # A turret drawn from one texture points north and turns to face the way the mech does;
            # a multi-sided one (the rocket pod) is drawn from the side that faces that way.
            turret = comp.el.find("comps/li[@Class='taranchuk_mechsuits.CompProperties_MechCompTurret']")
            turns = (turret is not None and turret.findtext("canRotate", "").strip() != "false"
                     and "Multi" not in (comp.text("equippedGraphicData/graphicClass") or ""))
            x, depth, z = at
            layers.append(self._layer(comp.node("equippedGraphicData"), facing, (x, z),
                                      FACING_TURN[facing] if turns else 0.0, depth))
        return layers

    # ---------- factions and research ----------

    def faction(self, name: str) -> dict | None:
        d = self.v.get("FactionDef", name)
        if d is None:
            self.notes.append(f"faction(): no FactionDef {name}")
            return None
        icon = self.tex.export(self.tex.find(d.text("factionIconPath")))
        return {"def_name": name, "label": d.label.title(), "icon": icon,
                "hidden": d.text("hidden") == "true", "enemy": d.text("permanentEnemy") == "true"}

    def kwazaari_favors(self) -> list[dict]:
        rows = []
        for d in self.v.all("BeyondOurReach.KwazaariFavorDef"):
            needs = self.requires(d)  # its load folder (Mods/Biotech...) and its own MayRequire
            any_of = self._names(i.lower() for i in d.items("requiredAnyOf"))
            if any_of:
                needs.append(" or ".join(any_of))
            else:  # whatever DLC the delivered things or pawns need
                ids = set()
                for el in d.el.iter():
                    if el is not d.el:
                        ids.update(i.strip().lower() for i in (el.get("MayRequire") or "").split(",") if i.strip())
                needs += self._names(sorted(ids))
            needs = ", ".join(_uniq(needs))
            rows.append({
                "label": cap(d.label),
                "description": d.description,
                "cost": d.num("cost"),
                "cooldown": d.num("cooldownDays"),
                "needs": needs,
                "order": d.num("order", 0),
            })
        return sorted(rows, key=lambda r: r["order"])

    def research(self) -> list[Group]:
        """Every project in the vanilla set, plus the ones that only exist with CE."""
        rows = []
        vanilla_names = {d.def_name for d in self.v.all("ResearchProjectDef")}
        ce_only = [d for d in self.ce.all("ResearchProjectDef") if d.def_name not in vanilla_names]
        for d in self.v.all("ResearchProjectDef") + ce_only:
            db = self.v if d.def_name in vanilla_names else self.ce
            cost = d.num("baseCost")
            prereq = [self._research_label(db, x) for x in d.items("prerequisites") + d.items("hiddenPrerequisites")]
            # 1E+15 projects can't be finished at a bench. Tenebral ones are
            # bargained for with a dark pact; the rest come through communion.
            special = None
            if cost is not None and cost >= 1e12:
                exts = d.node("modExtensions")
                pact = any("Pact" in c for c in d.chain) or any(
                    "DarkPact" in (li.get("Class") or "") for li in (exts if exts is not None else []))
                special = "dark pact" if pact else "communion"
            rows.append({
                "def_name": d.def_name,
                "label": cap(d.label),
                "description": d.description,
                "tier": tier_of(d),
                "cost": cost,
                "special": special,
                "prerequisites": prereq,
                "requires": self.requires(d),
            })
        groups = group_by_tier(rows)
        for g in groups:
            g.items.sort(key=lambda r: (r["special"] is not None, r["cost"] or 0, r["label"]))
        return groups
