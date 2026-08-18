#!/usr/bin/env python3
"""Regenerate Languages/English/DefInjected/** from Def XML files under 1.6/Common/Defs.

For every non-abstract Def with a <defName>, this pulls whichever translatable
fields are known for that Def type (see FIELD_MAP, derived from the field
usage already established in this repo's existing DefInjected data) and
writes them to Languages/English/DefInjected/<DefType>/<same relative path
as the source file under 1.6/Common/Defs>.

Def types with no entry in FIELD_MAP are left alone entirely (their existing
DefInjected folder, if any, is not touched) and reported as skipped.

This is a full regenerate: for every Def type actually encountered while
scanning 1.6/Common/Defs, the corresponding DefInjected/<DefType>/ folder is
wiped and rebuilt from the current source. Only run this when you're ready to
review/commit the resulting diff.
"""
from __future__ import annotations

import xml.etree.ElementTree as ET
from pathlib import Path
from xml.sax.saxutils import escape

REPO_ROOT = Path(__file__).resolve().parent.parent
DEFS_ROOT = REPO_ROOT / "1.6" / "Common" / "Defs"
OUT_ROOT = REPO_ROOT / "Languages" / "English" / "DefInjected"

# DefType tag -> ordered list of translatable field names to extract.
FIELD_MAP: dict[str, list[str]] = {
    "ChemicalDef": ["label"],
    "CultureDef": ["label", "description"],
    "DesignationCategoryDef": ["label"],
    "DesignatorDropdownGroupDef": ["label"],
    "FactionDef": ["label", "description"],
    "HediffDef": ["label", "description", "labelNoun"],
    "JobDef": ["reportString"],
    "JoyKindDef": ["label"],
    "NeedDef": ["label", "description"],
    "PawnKindDef": ["label", "labelPlural"],
    "RecipeDef": ["label", "description", "jobString"],
    "ResearchProjectDef": ["label", "description", "discoveredLetterTitle", "discoveredLetterText"],
    "ResearchTabDef": ["label", "generalTitle", "generalDescription"],
    "RoyalTitleDef": ["label", "description"],
    "RoyalTitlePermitDef": ["label", "description"],
    "StatDef": ["label", "description"],
    "TerrainDef": ["label", "description"],
    "TerrainTemplateDef": ["label", "description"],
    "ThingCategoryDef": ["label"],
    "ThingDef": ["label", "description"],
    "TraderKindDef": ["label"],
    "WorkGiverDef": ["label", "verb"],
    "XenotypeDef": ["label", "description"],
}


def unescape_newlines(text: str) -> str:
    return text.replace("\\n", "\n")


def is_abstract(elem: ET.Element) -> bool:
    return elem.get("Abstract", "").strip().lower() == "true"


def extract_file(xml_path: Path) -> dict[str, list[tuple[str, str, str]]]:
    """Returns {defType: [(defName, field, value), ...]} for one source file."""
    try:
        root = ET.parse(xml_path).getroot()
    except ET.ParseError as e:
        print(f"  ! parse error in {xml_path}: {e}")
        return {}
    if root.tag != "Defs":
        return {}

    by_type: dict[str, list[tuple[str, str, str]]] = {}
    for def_elem in root:
        def_type = def_elem.tag
        if not isinstance(def_type, str) or def_type not in FIELD_MAP:
            continue
        if is_abstract(def_elem):
            continue
        def_name_elem = def_elem.find("defName")
        if def_name_elem is None or not (def_name_elem.text or "").strip():
            continue
        def_name = def_name_elem.text.strip()

        entries: list[tuple[str, str, str]] = []
        for field in FIELD_MAP[def_type]:
            field_elem = def_elem.find(field)
            if field_elem is None or not (field_elem.text or "").strip():
                continue
            entries.append((def_name, field, unescape_newlines(field_elem.text)))
        by_type.setdefault(def_type, [])
        if entries:
            by_type[def_type].extend(entries)
    return by_type


def write_language_data(out_path: Path, entries: list[tuple[str, str, str]]) -> None:
    out_path.parent.mkdir(parents=True, exist_ok=True)
    lines = ["<?xml version='1.0' encoding='utf-8'?>", "<LanguageData>"]
    last_def_name = None
    for def_name, field, value in entries:
        if last_def_name is not None and def_name != last_def_name:
            lines.append("")
        key = f"{def_name}.{field}"
        lines.append(f"  <{key}>{escape(value)}</{key}>")
        last_def_name = def_name
    lines.append("</LanguageData>")
    out_path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main() -> None:
    per_file_out: dict[tuple[str, Path], list[tuple[str, str, str]]] = {}
    seen_types: set[str] = set()

    for xml_path in sorted(DEFS_ROOT.rglob("*.xml")):
        rel = xml_path.relative_to(DEFS_ROOT)
        for def_type, entries in extract_file(xml_path).items():
            seen_types.add(def_type)
            if entries:
                per_file_out.setdefault((def_type, rel), []).extend(entries)

    # Full regenerate: clear every Def type folder we actually scanned before rewriting.
    for def_type in seen_types:
        type_dir = OUT_ROOT / def_type
        if type_dir.exists():
            for f in type_dir.rglob("*.xml"):
                f.unlink()

    written_files = 0
    total_keys = 0
    for (def_type, rel), entries in sorted(per_file_out.items(), key=lambda kv: (kv[0][0], str(kv[0][1]))):
        out_path = OUT_ROOT / def_type / rel
        write_language_data(out_path, entries)
        written_files += 1
        total_keys += len(entries)

    for def_type in seen_types:
        type_dir = OUT_ROOT / def_type
        if type_dir.exists():
            for sub in sorted((p for p in type_dir.rglob("*") if p.is_dir()), reverse=True):
                if not any(sub.iterdir()):
                    sub.rmdir()

    skipped_types = set()
    for xml_path in DEFS_ROOT.rglob("*.xml"):
        try:
            root = ET.parse(xml_path).getroot()
        except ET.ParseError:
            continue
        if root.tag != "Defs":
            continue
        for def_elem in root:
            if isinstance(def_elem.tag, str) and def_elem.tag not in FIELD_MAP:
                skipped_types.add(def_elem.tag)

    print(f"Scanned {len(list(DEFS_ROOT.rglob('*.xml')))} XML files under {DEFS_ROOT}")
    print(f"Wrote {written_files} DefInjected files, {total_keys} translation keys "
          f"across {len(seen_types)} Def types.")
    if skipped_types:
        print("Def types with no known translatable-field mapping (left untouched):")
        for t in sorted(skipped_types):
            print(f"  - {t}")


if __name__ == "__main__":
    main()
