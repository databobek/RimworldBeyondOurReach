"""
Scans BeyondOurReach (1.6/ only) and AllBambaGuns for CE weapon/ammo/bullet data and
produces four CSVs:

1. weapon_ammo_map.csv  - weapon defName -> magazineSize, ammoSet, range
2. ammoset_bullets.csv  - AmmoSetDef defName -> bullet defName (one row per ammoTypes entry)
3. bullet_stats.csv     - bullet defName -> damage/penetration/speed/secondaryDamage
4. weapon_full_map.csv  - unified gun + ammoset + bullet stats, one row per (weapon, bullet)

Run from anywhere; paths are resolved relative to this file's location.
"""

import csv
import re
from pathlib import Path

from lxml import etree

SCRIPT_DIR = Path(__file__).resolve().parent
ROOTS = [
    SCRIPT_DIR.parent / "1.6",
    SCRIPT_DIR.parent.parent / "AllBambaGuns",
]

AMMOSET_XPATH_RE = re.compile(
    r'CombatExtended\.AmmoSetDef\[(defName|similarTo)\s*=\s*["\']([^"\']+)["\']\]/ammoTypes'
)

debug_msg = True


def debug(message):
    if debug_msg:
        print(message)


def find_xml_files(roots):
    files = []
    for root in roots:
        if not root.exists():
            debug(f"WARNING: root does not exist, skipping: {root}")
            continue
        for path in sorted(root.rglob("*.xml")):
            if path.name.endswith(".bak"):
                continue
            files.append(path)
    debug(f"Found {len(files)} XML files to scan across {len(roots)} root(s)")
    return files


def parse_file(path):
    try:
        return etree.parse(str(path))
    except etree.XMLSyntaxError as e:
        debug(f"WARNING: failed to parse {path}: {e}")
        return None


def elements(parent):
    """Iterate direct child Elements of parent, skipping comments/PIs."""
    for child in parent:
        if isinstance(child.tag, str):
            yield child


def find_child(parent, tag):
    if parent is None:
        return None
    for child in elements(parent):
        if child.tag == tag:
            return child
    return None


def child_text(parent, tag):
    child = find_child(parent, tag)
    if child is not None and child.text is not None:
        return child.text.strip()
    return None


def line_of(el):
    return el.sourceline if el is not None else None


# ---------------------------------------------------------------------------
# Pass 1: weapon_ammo_map.csv
# ---------------------------------------------------------------------------

WEAPON_PARENT_TAGS = {"Operation", "li", "ThingDef"}


def find_defname_via_ancestor_xpath(ammo_user_el):
    """Fallback: walk up ancestors looking for a sibling <xpath> containing defName="X"."""
    node = ammo_user_el.getparent()
    while node is not None:
        xpath_el = find_child(node, "xpath")
        if xpath_el is not None and xpath_el.text:
            m = re.search(r'defName\s*=\s*["\']([^"\']+)["\']', xpath_el.text)
            if m:
                return m.group(1)
        node = node.getparent()
    return None


def extract_range(parent):
    props = find_child(parent, "Properties")
    range_val = child_text(props, "range")
    if range_val:
        return range_val

    verbs = find_child(parent, "verbs")
    if verbs is not None:
        for li in elements(verbs):
            if li.tag == "li":
                range_val = child_text(li, "range")
                if range_val:
                    return range_val

    stat_bases = find_child(parent, "statBases")
    range_val = child_text(stat_bases, "Range")
    if range_val:
        return range_val

    return ""


def scrape_weapons(files, rows):
    for path in files:
        tree = parse_file(path)
        if tree is None:
            continue
        root = tree.getroot()
        for ammo_user in root.iter("AmmoUser"):
            if not isinstance(ammo_user.tag, str):
                continue
            ammo_set = child_text(ammo_user, "ammoSet")
            if not ammo_set:
                continue
            parent = ammo_user.getparent()
            if parent is None or parent.tag not in WEAPON_PARENT_TAGS:
                continue

            def_name = child_text(parent, "defName")
            if not def_name:
                def_name = find_defname_via_ancestor_xpath(ammo_user)
            if not def_name:
                debug(
                    f"WARNING: could not resolve defName for AmmoUser at "
                    f"{path}:{line_of(ammo_user)}"
                )
                def_name = ""

            label = child_text(parent, "label") or ""
            magazine_size = child_text(ammo_user, "magazineSize") or ""
            range_val = extract_range(parent)

            rows.append(
                {
                    "defName": def_name,
                    "label": label,
                    "magazineSize": magazine_size,
                    "ammoSet": ammo_set,
                    "range": range_val,
                    "sourceFile": str(path.relative_to(SCRIPT_DIR.parent.parent)),
                    "sourceLine": line_of(ammo_user),
                }
            )


# ---------------------------------------------------------------------------
# Pass 2: ammoset_bullets.csv
# ---------------------------------------------------------------------------


class AmmoSetRecord:
    def __init__(self, def_name, label="", similar_to="", defined_in_repo=True, source_file=""):
        self.def_name = def_name
        self.label = label
        self.similar_to = similar_to
        self.defined_in_repo = defined_in_repo
        self.source_file = source_file
        self.ammo_types = []  # list of (ammoDefName, bulletDefName)

    def append_ammo_types(self, pairs):
        existing = set(self.ammo_types)
        for pair in pairs:
            if pair not in existing:
                self.ammo_types.append(pair)
                existing.add(pair)

    def replace_ammo_types(self, pairs):
        self.ammo_types = list(pairs)


def scrape_ammosets(files):
    ammosets = {}  # defName -> AmmoSetRecord
    similar_to_index = {}  # similarTo value -> defName
    pending_merges = []  # (target_kind, target_value, class_attr, pairs, source_file)

    for path in files:
        tree = parse_file(path)
        if tree is None:
            continue
        root = tree.getroot()
        rel_path = str(path.relative_to(SCRIPT_DIR.parent.parent))

        # Full AmmoSetDef definitions
        for ammoset_el in root.iter("CombatExtended.AmmoSetDef"):
            def_name = child_text(ammoset_el, "defName")
            if not def_name:
                continue
            label = child_text(ammoset_el, "label") or ""
            similar_to = child_text(ammoset_el, "similarTo") or ""
            record = ammosets.get(def_name)
            if record is None:
                record = AmmoSetRecord(def_name, label, similar_to, True, rel_path)
                ammosets[def_name] = record
            if similar_to:
                similar_to_index[similar_to] = def_name

            ammo_types_el = find_child(ammoset_el, "ammoTypes")
            if ammo_types_el is not None:
                pairs = [
                    (child.tag, (child.text or "").strip())
                    for child in elements(ammo_types_el)
                ]
                record.append_ammo_types(pairs)

        # Additive patch operations targeting ammoTypes
        for tag_name in ("li", "Operation"):
            for op_el in root.iter(tag_name):
                if not isinstance(op_el.tag, str):
                    continue
                op_class = op_el.get("Class", "")
                if op_class not in ("PatchOperationAdd", "PatchOperationReplace"):
                    continue
                xpath_el = find_child(op_el, "xpath")
                if xpath_el is None or not xpath_el.text:
                    continue
                m = AMMOSET_XPATH_RE.search(xpath_el.text)
                if not m:
                    continue
                target_kind, target_value = m.group(1), m.group(2)
                value_el = find_child(op_el, "value")
                if value_el is None:
                    continue
                pairs = [
                    (child.tag, (child.text or "").strip())
                    for child in elements(value_el)
                ]
                if not pairs:
                    continue
                pending_merges.append((target_kind, target_value, op_class, pairs, rel_path))

    # Resolve and apply pending merges (may target defs not yet seen depending on file order,
    # but all full defs are already collected above since we finished the file loop).
    for target_kind, target_value, op_class, pairs, rel_path in pending_merges:
        if target_kind == "defName":
            resolved_name = target_value
        else:  # similarTo
            resolved_name = similar_to_index.get(target_value, target_value)

        record = ammosets.get(resolved_name)
        if record is None:
            record = AmmoSetRecord(
                resolved_name, defined_in_repo=False, source_file=rel_path
            )
            ammosets[resolved_name] = record

        if op_class == "PatchOperationReplace":
            record.replace_ammo_types(pairs)
        else:
            record.append_ammo_types(pairs)

    return ammosets


def build_ammoset_rows(ammosets):
    rows = []
    for record in ammosets.values():
        if not record.ammo_types:
            rows.append(
                {
                    "ammoSetDefName": record.def_name,
                    "ammoDefName": "",
                    "bulletDefName": "",
                    "definedInRepo": record.defined_in_repo,
                    "sourceFile": record.source_file,
                }
            )
            continue
        for ammo_def_name, bullet_def_name in record.ammo_types:
            rows.append(
                {
                    "ammoSetDefName": record.def_name,
                    "ammoDefName": ammo_def_name,
                    "bulletDefName": bullet_def_name,
                    "definedInRepo": record.defined_in_repo,
                    "sourceFile": record.source_file,
                }
            )
    return rows


# ---------------------------------------------------------------------------
# Pass 3: bullet_stats.csv
# ---------------------------------------------------------------------------

PROJECTILE_FIELDS = [
    "damageAmountBase",
    "armorPenetrationSharp",
    "armorPenetrationBlunt",
    "speed",
]


def scrape_bullets(files, bullet_def_names):
    bullets = {}  # defName -> dict of stats
    for path in files:
        tree = parse_file(path)
        if tree is None:
            continue
        root = tree.getroot()
        rel_path = str(path.relative_to(SCRIPT_DIR.parent.parent))
        for thing_def in root.iter("ThingDef"):
            if not isinstance(thing_def.tag, str):
                continue
            if thing_def.get("Abstract") == "true":
                continue
            def_name = child_text(thing_def, "defName")
            if not def_name or def_name not in bullet_def_names:
                continue
            projectile_el = find_child(thing_def, "projectile")
            if projectile_el is None:
                continue

            stats = {field: child_text(projectile_el, field) or "" for field in PROJECTILE_FIELDS}

            secondary_parts = []
            secondary_el = find_child(projectile_el, "secondaryDamage")
            if secondary_el is not None:
                for li in elements(secondary_el):
                    if li.tag != "li":
                        continue
                    def_ref = child_text(li, "def") or ""
                    amount = child_text(li, "amount") or ""
                    secondary_parts.append(f"{def_ref}:{amount}")
            stats["secondaryDamage"] = ";".join(secondary_parts)
            stats["sourceFile"] = rel_path
            stats["sourceLine"] = line_of(thing_def)
            bullets[def_name] = stats

    return bullets


def build_bullet_rows(bullet_def_names, bullets):
    rows = []
    for def_name in sorted(bullet_def_names):
        stats = bullets.get(def_name)
        if stats is None:
            rows.append(
                {
                    "bulletDefName": def_name,
                    "damageAmountBase": "",
                    "armorPenetrationSharp": "",
                    "armorPenetrationBlunt": "",
                    "speed": "",
                    "secondaryDamage": "",
                    "found": False,
                    "sourceFile": "",
                    "sourceLine": "",
                }
            )
            continue
        rows.append(
            {
                "bulletDefName": def_name,
                "damageAmountBase": stats["damageAmountBase"],
                "armorPenetrationSharp": stats["armorPenetrationSharp"],
                "armorPenetrationBlunt": stats["armorPenetrationBlunt"],
                "speed": stats["speed"],
                "secondaryDamage": stats["secondaryDamage"],
                "found": True,
                "sourceFile": stats["sourceFile"],
                "sourceLine": stats["sourceLine"],
            }
        )
    return rows


# ---------------------------------------------------------------------------
# Pass 4: weapon_full_map.csv (unified gun -> ammoset -> bullet, one row per bullet)
# ---------------------------------------------------------------------------


def build_unified_rows(weapon_rows, ammosets, bullets):
    rows = []
    for weapon in weapon_rows:
        ammoset_name = weapon["ammoSet"]
        record = ammosets.get(ammoset_name)

        base = {
            "gunDefName": weapon["defName"],
            "gunLabel": weapon["label"],
            "magazineSize": weapon["magazineSize"],
            "range": weapon["range"],
            "gunSourceFile": weapon["sourceFile"],
        }

        if record is None or not record.ammo_types:
            rows.append(
                {
                    **base,
                    "ammoSetDefName": ammoset_name,
                    "ammoSetFound": False,
                    "ammoDefName": "",
                    "bulletDefName": "",
                    "damageAmountBase": "",
                    "armorPenetrationSharp": "",
                    "armorPenetrationBlunt": "",
                    "speed": "",
                    "secondaryDamage": "",
                    "bulletFound": False,
                }
            )
            continue

        for ammo_def_name, bullet_def_name in record.ammo_types:
            stats = bullets.get(bullet_def_name)
            rows.append(
                {
                    **base,
                    "ammoSetDefName": record.def_name,
                    "ammoSetFound": True,
                    "ammoDefName": ammo_def_name,
                    "bulletDefName": bullet_def_name,
                    "damageAmountBase": stats["damageAmountBase"] if stats else "",
                    "armorPenetrationSharp": stats["armorPenetrationSharp"] if stats else "",
                    "armorPenetrationBlunt": stats["armorPenetrationBlunt"] if stats else "",
                    "speed": stats["speed"] if stats else "",
                    "secondaryDamage": stats["secondaryDamage"] if stats else "",
                    "bulletFound": stats is not None,
                }
            )
    return rows


# ---------------------------------------------------------------------------
# CSV writing helper
# ---------------------------------------------------------------------------


def write_csv(rows, fieldnames, out_path):
    with open(out_path, "w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)
    debug(f"Wrote {len(rows)} rows to {out_path}")


def main():
    files = find_xml_files(ROOTS)

    weapon_rows = []
    scrape_weapons(files, weapon_rows)
    write_csv(
        weapon_rows,
        ["defName", "label", "magazineSize", "ammoSet", "range", "sourceFile", "sourceLine"],
        SCRIPT_DIR / "weapon_ammo_map.csv",
    )

    ammosets = scrape_ammosets(files)
    ammoset_rows = build_ammoset_rows(ammosets)
    write_csv(
        ammoset_rows,
        ["ammoSetDefName", "ammoDefName", "bulletDefName", "definedInRepo", "sourceFile"],
        SCRIPT_DIR / "ammoset_bullets.csv",
    )

    bullet_def_names = {
        row["bulletDefName"] for row in ammoset_rows if row["bulletDefName"]
    }
    bullets = scrape_bullets(files, bullet_def_names)
    bullet_rows = build_bullet_rows(bullet_def_names, bullets)
    write_csv(
        bullet_rows,
        [
            "bulletDefName",
            "damageAmountBase",
            "armorPenetrationSharp",
            "armorPenetrationBlunt",
            "speed",
            "secondaryDamage",
            "found",
            "sourceFile",
            "sourceLine",
        ],
        SCRIPT_DIR / "bullet_stats.csv",
    )

    unified_rows = build_unified_rows(weapon_rows, ammosets, bullets)
    write_csv(
        unified_rows,
        [
            "gunDefName",
            "gunLabel",
            "magazineSize",
            "range",
            "ammoSetDefName",
            "ammoSetFound",
            "ammoDefName",
            "bulletDefName",
            "damageAmountBase",
            "armorPenetrationSharp",
            "armorPenetrationBlunt",
            "speed",
            "secondaryDamage",
            "bulletFound",
            "gunSourceFile",
        ],
        SCRIPT_DIR / "weapon_full_map.csv",
    )


if __name__ == "__main__":
    main()
