"""Load Beyond Our Reach's XML the way RimWorld does.

RimWorld merges every Defs file from the active load folders into one
document, applies the Patches files in load order, then resolves ParentName
inheritance. This module repeats those steps for a chosen set of active mods
(a "scenario"), so the site shows the numbers a player actually gets with or
without Combat Extended.

Vanilla's and Combat Extended's own defs are not in the repo. The base defs
BOR inherits from are copied into external_parents.xml by
`python site/build.py --refresh-parents` (run where the game and CE are
installed) and loaded first, so inheritance resolves on the Pages build too.

MayRequire / MayRequireAnyOf work as in the game: a def, list item, field or
patch operation whose mods aren't active in the scenario is dropped.

Two deliberate differences from the game:
- A failing operation inside a PatchOperationSequence does not stop the rest
  of the sequence. Here many targets (vanilla defs, other mods) are missing,
  and stopping would drop the BOR patches that come after them.
- Mods named in PatchOperationFindMod count as inactive unless the scenario
  lists them (DLCs and, for the CE scenario, Combat Extended).
"""
from __future__ import annotations

import copy
import re
from dataclasses import dataclass
from pathlib import Path

from lxml import etree

DLC_PACKAGE_IDS = frozenset({
    "ludeon.rimworld.royalty",
    "ludeon.rimworld.ideology",
    "ludeon.rimworld.biotech",
    "ludeon.rimworld.anomaly",
    "ludeon.rimworld.odyssey",
})
DLC_NAMES = frozenset({"Royalty", "Ideology", "Biotech", "Anomaly", "Odyssey"})
CE_PACKAGE_ID = "ceteam.combatextended"
CE_MOD_NAME = "Combat Extended"

_PARSER = etree.XMLParser(remove_comments=True, remove_blank_text=True)


@dataclass(frozen=True)
class Scenario:
    """Which mods count as active. Package ids are compared in lower case."""

    name: str
    package_ids: frozenset[str]
    mod_names: frozenset[str]

    def any_active(self, ids: list[str]) -> bool:
        return any(i in self.package_ids for i in ids)


@dataclass(frozen=True)
class LoadFolder:
    path: str  # relative to the mod root, "" for the root folder
    if_active: tuple[str, ...]  # any of these
    if_active_all: tuple[str, ...]  # all of these
    if_not_active: tuple[str, ...]  # none of these

    def is_active(self, scenario: Scenario) -> bool:
        if self.if_active and not scenario.any_active(list(self.if_active)):
            return False
        if self.if_active_all and not all(i in scenario.package_ids for i in self.if_active_all):
            return False
        if self.if_not_active and scenario.any_active(list(self.if_not_active)):
            return False
        return True

    @property
    def required_ids(self) -> tuple[str, ...]:
        return self.if_active + self.if_active_all


def _ids(value: str | None) -> tuple[str, ...]:
    if not value:
        return ()
    return tuple(i.strip().lower() for i in value.split(",") if i.strip())


def read_load_folders(root: Path) -> list[LoadFolder]:
    """Read LoadFolders.xml. Only the v1.7 list exists in this mod."""
    tree = etree.parse(str(root / "LoadFolders.xml"), _PARSER)
    version = tree.getroot().find("v1.7")
    if version is None:
        raise SystemExit("LoadFolders.xml has no <v1.7> list")
    folders = []
    for li in version.findall("li"):
        path = (li.text or "").strip().strip("/")
        folders.append(LoadFolder(
            path=path,
            if_active=_ids(li.get("IfModActive")),
            if_active_all=_ids(li.get("IfModActiveAll")),
            if_not_active=_ids(li.get("IfModNotActive")),
        ))
    return folders


def make_scenarios(folders: list[LoadFolder]) -> dict[str, Scenario]:
    """vanilla: DLCs only. ce: DLCs plus Combat Extended.
    all: every mod BOR integrates with except CE, for the galleries."""
    every = set()
    for f in folders:
        every.update(f.required_ids)
    every -= {CE_PACKAGE_ID}
    return {
        "vanilla": Scenario("vanilla", DLC_PACKAGE_IDS, DLC_NAMES),
        "ce": Scenario("ce", DLC_PACKAGE_IDS | {CE_PACKAGE_ID}, DLC_NAMES | {CE_MOD_NAME}),
        "all": Scenario("all", frozenset(every) | DLC_PACKAGE_IDS, DLC_NAMES),
    }


def _xml_files(base: Path) -> list[Path]:
    if not base.is_dir():
        return []
    return sorted(p for p in base.rglob("*") if p.suffix.lower() == ".xml" and p.is_file())


def _child(el, tag):
    """Find a direct child by exact tag (ElementPath chokes on some tag names)."""
    for c in el:
        if c.tag == tag:
            return c
    return None


class Def:
    """Read-only view of one resolved def."""

    __slots__ = ("el", "db")

    def __init__(self, el, db: "DefDB"):
        self.el = el
        self.db = db

    @property
    def tag(self) -> str:
        return self.el.tag

    @property
    def def_name(self) -> str:
        return (self.el.findtext("defName") or "").strip()

    @property
    def src(self) -> str:
        """Load folder the def came from, e.g. "Common" or "Mods/Royalty"."""
        return self.el.get("_src", "")

    @property
    def file(self) -> str:
        return self.el.get("_file", "")

    @property
    def chain(self) -> list[str]:
        """ParentName chain, nearest first, including parents not in the repo."""
        c = self.el.get("_chain", "")
        return c.split(",") if c else []

    @property
    def cls(self) -> str:
        return self.el.get("Class", "")

    def text(self, path: str, default: str | None = None) -> str | None:
        v = self.el.findtext(path)
        if v is None:
            return default
        v = v.strip()
        return v if v else default

    def num(self, path: str, default: float | None = None) -> float | None:
        v = self.text(path)
        if v is None:
            return default
        try:
            return float(v)
        except ValueError:
            return default

    def items(self, path: str) -> list[str]:
        node = self.el.find(path)
        if node is None:
            return []
        return [(li.text or "").strip() for li in node if li.tag == "li" and (li.text or "").strip()]

    def node(self, path: str):
        return self.el.find(path)

    @property
    def label(self) -> str:
        inj = self.db.injected.get((self.tag, self.def_name), {})
        return inj.get("label") or self.text("label") or self.def_name

    @property
    def description(self) -> str:
        inj = self.db.injected.get((self.tag, self.def_name), {})
        return inj.get("description") or self.text("description") or ""

    def __repr__(self) -> str:
        return f"<{self.tag} {self.def_name}>"


class DefDB:
    """All defs of one scenario, patched and inheritance-resolved."""

    def __init__(self, root: Path, folders: list[LoadFolder], scenario: Scenario,
                 external: Path | None = None):
        self.root = root
        self.scenario = scenario
        self.folders = [f for f in folders if f.is_active(scenario)]
        self.doc = etree.Element("Defs")
        self.tree = etree.ElementTree(self.doc)
        self.warnings: list[str] = []
        self.missing_parents: set[str] = set()
        if external is not None and external.is_file():
            self._add_external(external)
        self._load("Defs", self._add_defs)
        self._load("Patches", self._apply_patch_file)
        self._resolve_inheritance()
        for el in self.doc:
            self._strip_unavailable(el)
        self.by_name: dict[tuple[str, str], Def] = {}
        for el in self.doc:
            if el.get("Abstract", "").lower() == "true":
                continue
            d = Def(el, self)
            if d.def_name:
                self.by_name[(d.tag, d.def_name)] = d
        self.injected = self._load_injected()

    # ---------- loading ----------

    def _load(self, kind: str, handler):
        """Visit every file of one kind in load order.

        As in RimWorld, a file in a later load folder replaces a file with the
        same relative path in an earlier folder."""
        winner: dict[str, tuple[LoadFolder, Path]] = {}
        for folder in self.folders:
            base = self.root / folder.path / kind
            for p in _xml_files(base):
                rel = p.relative_to(base).as_posix().lower()
                winner[rel] = (folder, p)
        order = {f.path: i for i, f in enumerate(self.folders)}
        for folder, p in sorted(winner.values(), key=lambda fp: (order[fp[0].path], fp[1].as_posix())):
            try:
                tree = etree.parse(str(p), _PARSER)
            except etree.XMLSyntaxError as e:
                self.warnings.append(f"{p}: {e}")
                continue
            handler(tree.getroot(), folder, p)

    def _rel(self, p: Path) -> str:
        return p.relative_to(self.root).as_posix()

    def _add_external(self, path: Path):
        """Vanilla/CE base defs: usable as parents, never shown themselves."""
        try:
            root = etree.parse(str(path), _PARSER).getroot()
        except etree.XMLSyntaxError as e:
            self.warnings.append(f"{path}: {e}")
            return
        for el in list(root):
            if isinstance(el.tag, str) and el.get("Name"):
                el.set("_src", "external")
                el.set("Abstract", "True")
                self.doc.append(el)

    def _available(self, el) -> bool:
        """The game's MayRequire (all listed mods) and MayRequireAnyOf (any one)."""
        need_all = _ids(el.get("MayRequire"))
        if need_all and not all(i in self.scenario.package_ids for i in need_all):
            return False
        need_any = _ids(el.get("MayRequireAnyOf"))
        if need_any and not self.scenario.any_active(list(need_any)):
            return False
        return True

    def _strip_unavailable(self, el):
        for child in list(el):
            if not isinstance(child.tag, str):
                continue
            if self._available(child):
                self._strip_unavailable(child)
            else:
                el.remove(child)

    def _add_defs(self, root, folder: LoadFolder, p: Path):
        if root.tag != "Defs":
            return
        for el in list(root):
            if not isinstance(el.tag, str) or not self._available(el):
                continue
            el.set("_src", folder.path)
            el.set("_file", self._rel(p))
            self.doc.append(el)

    def _apply_patch_file(self, root, folder: LoadFolder, p: Path):
        if root.tag != "Patch":
            return
        for op in root:
            if isinstance(op.tag, str) and op.tag == "Operation":
                self._apply(op, folder, p)

    # ---------- patching ----------

    def _select(self, op) -> list:
        xpath = (op.findtext("xpath") or "").strip()
        if not xpath:
            return []
        if not xpath.startswith("/"):
            xpath = "/" + xpath  # RimWorld evaluates against the document node
        try:
            found = self.tree.xpath(xpath)
        except etree.XPathError as e:
            self.warnings.append(f"bad xpath {xpath!r}: {e}")
            return []
        return [n for n in found if isinstance(n, etree._Element)]

    def _mark_new_defs(self, nodes, parent, folder: LoadFolder, p: Path):
        if parent is self.doc:
            for n in nodes:
                if not self._available(n):
                    parent.remove(n)
                    continue
                n.set("_src", folder.path)
                n.set("_file", self._rel(p))

    def _apply(self, op, folder: LoadFolder, p: Path) -> bool:
        if not self._available(op):
            return True  # skipped, like the game does
        cls = op.get("Class", "")
        if cls == "PatchOperationSequence":
            ok = True
            ops = _child(op, "operations")
            for li in (ops if ops is not None else []):
                if isinstance(li.tag, str) and li.tag == "li":
                    ok = self._apply(li, folder, p) and ok
            return ok
        if cls == "PatchOperationFindMod":
            names = [(li.text or "").strip() for li in op.findall("mods/li")]
            hit = any(n in self.scenario.mod_names for n in names)
            branch = _child(op, "match" if hit else "nomatch")
            return self._apply(branch, folder, p) if branch is not None else True
        if cls == "PatchOperationConditional":
            hit = bool(self._select(op))
            branch = _child(op, "match" if hit else "nomatch")
            return self._apply(branch, folder, p) if branch is not None else True
        if cls == "CombatExtended.PatchOperationMakeGunCECompatible":
            return self._make_gun_ce(op)

        nodes = self._select(op)
        if not nodes:
            return False
        value = _child(op, "value")
        values = [v for v in (value if value is not None else []) if isinstance(v.tag, str)]
        order = (op.findtext("order") or "").strip()

        def fresh():
            return [copy.deepcopy(v) for v in values]

        if cls == "PatchOperationAdd":
            for n in nodes:
                new = fresh()
                if order == "Prepend":
                    for i, v in enumerate(new):
                        n.insert(i, v)
                else:
                    n.extend(new)
                self._mark_new_defs(new, n, folder, p)
        elif cls == "PatchOperationReplace":
            for n in nodes:
                parent = n.getparent()
                if parent is None:
                    continue
                idx = parent.index(n)
                new = fresh()
                for i, v in enumerate(new):
                    parent.insert(idx + i, v)
                parent.remove(n)
                self._mark_new_defs(new, parent, folder, p)
        elif cls == "PatchOperationRemove":
            for n in nodes:
                parent = n.getparent()
                if parent is not None:
                    parent.remove(n)
        elif cls == "PatchOperationInsert":
            for n in nodes:
                parent = n.getparent()
                if parent is None:
                    continue
                pos = parent.index(n) + (1 if order == "Append" else 0)
                new = fresh()
                for i, v in enumerate(new):
                    parent.insert(pos + i, v)
                self._mark_new_defs(new, parent, folder, p)
        elif cls == "PatchOperationAddModExtension":
            for n in nodes:
                ext = _child(n, "modExtensions")
                if ext is None:
                    ext = etree.SubElement(n, "modExtensions")
                ext.extend(fresh())
        elif cls in ("PatchOperationAttributeAdd", "PatchOperationAttributeSet"):
            attr = (op.findtext("attribute") or "").strip()
            val = (op.findtext("value") or "").strip()
            for n in nodes:
                if cls == "PatchOperationAttributeSet" or n.get(attr) is None:
                    n.set(attr, val)
        elif cls == "PatchOperationAttributeRemove":
            attr = (op.findtext("attribute") or "").strip()
            for n in nodes:
                n.attrib.pop(attr, None)
        else:
            # Other mods' own operation classes (Muzzle Flash, SafePatcher...) only
            # touch visuals; note vanilla ones we don't handle so they get added.
            if cls.startswith("PatchOperation"):
                self.warnings.append(f"{self._rel(p)}: unsupported patch operation {cls!r}")
            return False
        return True

    def _make_gun_ce(self, op) -> bool:
        """Mirror of CE's PatchOperationMakeGunCECompatible.

        Merges statBases, replaces the verbs with the CE Properties, adds the
        AmmoUser and FireModes comps, and replaces weaponTags, costList and
        researchPrerequisite when given."""
        name = (op.findtext("defName") or "").strip()
        targets = self.tree.xpath(f'/Defs/ThingDef[defName="{name}"]') if name else []
        if not targets:
            return False
        for d in targets:
            stats = _child(op, "statBases")
            if stats is not None:
                dst = _child(d, "statBases")
                if dst is None:
                    dst = etree.SubElement(d, "statBases")
                for stat in stats:
                    old = _child(dst, stat.tag)
                    if old is not None:
                        dst.remove(old)
                    dst.append(copy.deepcopy(stat))
            props = _child(op, "Properties")
            if props is not None:
                old = _child(d, "verbs")
                if old is not None:
                    d.remove(old)
                verbs = etree.SubElement(d, "verbs", Inherit="False")
                li = etree.SubElement(verbs, "li", Class="CombatExtended.VerbPropertiesCE")
                for c in props:
                    li.append(copy.deepcopy(c))
            for tag, comp_cls in (("AmmoUser", "CombatExtended.CompProperties_AmmoUser"),
                                  ("FireModes", "CombatExtended.CompProperties_FireModes")):
                src = _child(op, tag)
                if src is None:
                    continue
                comps = _child(d, "comps")
                if comps is None:
                    comps = etree.SubElement(d, "comps")
                for old in [c for c in comps if c.get("Class") == comp_cls]:
                    comps.remove(old)
                li = etree.SubElement(comps, "li", Class=comp_cls)
                for c in src:
                    li.append(copy.deepcopy(c))
            for tag in ("weaponTags", "costList", "researchPrerequisite"):
                src = _child(op, tag)
                if src is None:
                    continue
                old = _child(d, tag)
                if old is not None:
                    d.remove(old)
                d.append(copy.deepcopy(src))
        return True

    # ---------- inheritance ----------

    def _resolve_inheritance(self):
        named = {}
        for el in self.doc:
            n = el.get("Name")
            if n:
                named[n] = el
        cache: dict[int, object] = {}
        chains: dict[int, list[str]] = {}

        def resolve(el, seen: tuple[str, ...] = ()):
            key = id(el)
            if key in cache:
                return cache[key], chains[key]
            parent_name = el.get("ParentName")
            if parent_name and parent_name in named and parent_name not in seen:
                parent, parent_chain = resolve(named[parent_name], seen + (parent_name,))
                result = copy.deepcopy(parent)
                for a in ("Abstract", "Name", "_chain"):
                    result.attrib.pop(a, None)
                for k, v in el.attrib.items():
                    result.set(k, v)
                _merge(result, el)
                chain = [parent_name] + parent_chain
            else:
                result = copy.deepcopy(el)
                chain = [parent_name] if parent_name else []
                if parent_name and parent_name not in named:
                    self.missing_parents.add(parent_name)
            cache[key] = result
            chains[key] = chain
            return result, chain

        resolved = []
        for el in list(self.doc):
            r, chain = resolve(el)
            r = copy.deepcopy(r)
            if chain:
                r.set("_chain", ",".join(chain))
            resolved.append(r)
        self.doc.clear()
        self.doc.extend(resolved)

    # ---------- translations ----------

    def _load_injected(self) -> dict[tuple[str, str], dict[str, str]]:
        """English DefInjected label/description overrides, like the game applies them."""
        out: dict[tuple[str, str], dict[str, str]] = {}
        bases = [self.root / "Languages" / "English" / "DefInjected"]
        bases += [self.root / f.path / "Languages" / "English" / "DefInjected" for f in self.folders if f.path]
        for base in bases:
            if not base.is_dir():
                continue
            for type_dir in base.iterdir():
                if not type_dir.is_dir():
                    continue
                for p in _xml_files(type_dir):
                    try:
                        root = etree.parse(str(p), _PARSER).getroot()
                    except etree.XMLSyntaxError as e:
                        self.warnings.append(f"{p}: {e}")
                        continue
                    for el in root:
                        if not isinstance(el.tag, str) or "." not in el.tag:
                            continue
                        def_name, _, field = el.tag.partition(".")
                        if field in ("label", "description") and (el.text or "").strip():
                            out.setdefault((type_dir.name, def_name), {})[field] = el.text.strip()
        return out

    # ---------- queries ----------

    def get(self, tag: str, def_name: str | None) -> Def | None:
        if not def_name:
            return None
        return self.by_name.get((tag, def_name))

    def all(self, tag: str) -> list[Def]:
        return [d for (t, _), d in self.by_name.items() if t == tag]


def _merge(current, child):
    """RimWorld's XmlInheritance overwrite rules, applied to child's elements:
    list items append, Inherit="False" replaces, leaves replace text, and
    other elements merge recursively."""
    for ch in child:
        if not isinstance(ch.tag, str):
            continue
        if (ch.get("Inherit") or "").lower() == "false":
            existing = _child(current, ch.tag)
            new = copy.deepcopy(ch)
            if existing is not None:
                current.replace(existing, new)
            else:
                current.append(new)
            continue
        if ch.tag == "li":
            current.append(copy.deepcopy(ch))
            continue
        existing = _child(current, ch.tag)
        if existing is None:
            current.append(copy.deepcopy(ch))
            continue
        for k, v in ch.attrib.items():
            existing.set(k, v)
        if len(ch) == 0:
            for c in list(existing):
                existing.remove(c)
            existing.text = ch.text
            continue
        _merge(existing, ch)


_NUM_RE = re.compile(r"^-?\d+(\.\d+)?([eE][+-]?\d+)?$")


def is_number(s: str | None) -> bool:
    return bool(s and _NUM_RE.match(s.strip()))
