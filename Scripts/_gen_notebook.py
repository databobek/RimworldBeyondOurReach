"""Generates tier1_balance_scaler.ipynb next to this file."""
import json, pathlib, uuid

def cell_id():
    return uuid.uuid4().hex[:16]

def md(source: str):
    return {"cell_type": "markdown", "id": cell_id(), "metadata": {},
            "source": source}

def code(source: str):
    return {"cell_type": "code", "id": cell_id(), "metadata": {},
            "execution_count": None, "outputs": [],
            "source": source}

# ─────────────────────────────────────────────────────────────────────────────
C0 = md("""\
# Tier 1 Balance Scaler

Scales numeric values in all Tier 1 XML files by configurable per-node-type factors.

## Usage
1. **Cell 1** – run once; installs deps, sets up paths and helpers.
2. **Cell 2** – discover every numeric node type across Tier 1 files.
3. **Cell 3** – edit `NODE_SCALING` to control which nodes change and by how much.
4. **Cell 4** – dry-run: preview all changes without writing any files.
5. **Cell 5** – set `APPLY = True` then run to write changes (`.bak` backups created automatically).
6. **Cell 6** – re-run to verify all changes were applied.

> **Rebuilding the mapping:** re-run Cell 2, then call `build_default_mapping()` and paste the result into Cell 3.\
""")

# ─────────────────────────────────────────────────────────────────────────────
C1 = code("""\
# ── Cell 1: Setup & Constants ────────────────────────────────────────────────
import subprocess, sys

for _pkg in ('lxml', 'pandas'):
    subprocess.check_call([sys.executable, '-m', 'pip', 'install', '--quiet', _pkg])

from pathlib import Path
import lxml.etree as ET
import pandas as pd
import shutil, re

# Notebook lives in Scripts/; VS Code sets cwd to the notebook's directory.
SCRIPT_DIR = Path().resolve()
MOD_ROOT   = SCRIPT_DIR.parent / '1.6'

# Folders (or single files) that define Tier 1 content.
TIER1_ROOTS: list = [
    MOD_ROOT / 'Common/Defs/ThingDefs_Items/Tier1',
    MOD_ROOT / 'Common/Defs/ThingDefs_Buildings/tier1',
    MOD_ROOT / 'Common/Defs/RecipeDefs/Tier1',
    MOD_ROOT / 'Mods/TerraPumps/Patches/Defs/tier1',
    MOD_ROOT / 'Mods/Ideology/Patches/tier1.xml',   # single file
]

def collect_xml_files(roots: list) -> list:
    \"\"\"Return all .xml files reachable from *roots* (folders recurse; files used as-is).\"\"\"
    result = []
    for root in roots:
        p = Path(root)
        if p.is_file() and p.suffix.lower() == '.xml':
            result.append(p)
        elif p.is_dir():
            result.extend(sorted(p.rglob('*.xml')))
        else:
            print(f'\\u26a0  Not found / skipped: {p}')
    return result

TIER1_FILES = collect_xml_files(TIER1_ROOTS)
print(f'Found {len(TIER1_FILES)} Tier1 XML files:')
for _f in TIER1_FILES:
    print(f'  {_f.relative_to(MOD_ROOT.parent)}')\
""")

# ─────────────────────────────────────────────────────────────────────────────
C2 = code("""\
# ── Cell 2: Discovery ────────────────────────────────────────────────────────
# Scans every Tier 1 XML, collects (parent_tag / child_tag) pairs whose text
# is a plain number or a min~max range.  Also defines build_default_mapping().

_NUMERIC_RE = re.compile(r'^-?\\d+(?:\\.\\d+)?$')
_RANGE_RE   = re.compile(r'^-?\\d+(?:\\.\\d+)?~-?\\d+(?:\\.\\d+)?$')


def _is_numeric_text(text: str) -> bool:
    t = text.strip()
    return bool(_NUMERIC_RE.match(t) or _RANGE_RE.match(t))


def _find_def_name(el) -> str:
    \"\"\"Walk up the lxml tree to find the nearest defName or Name attribute.\"\"\"
    node = el
    while node is not None:
        for attr in ('Name', 'defName'):
            v = node.get(attr)
            if v:
                return v
        dn = node.find('defName')
        if dn is not None and dn.text:
            return dn.text.strip()
        node = node.getparent()
    return '?'


def scan_numeric_nodes(files: list) -> tuple:
    \"\"\"
    Returns (summary_df, raw_records_list).
    summary_df  – grouped view: one row per unique parent_tag/child_tag pair.
    raw_records – full detail list used by build_default_mapping().
    \"\"\"
    records = []
    for fpath in files:
        try:
            tree = ET.parse(str(fpath))
        except ET.XMLSyntaxError as exc:
            print(f'\\u26a0  Parse error in {fpath.name}: {exc}')
            continue
        rel = str(fpath.relative_to(MOD_ROOT.parent))
        for el in tree.getroot().iter():
            if not isinstance(el.tag, str):
                continue
            text = (el.text or '').strip()
            if not _is_numeric_text(text):
                continue
            parent = el.getparent()
            if parent is None:
                continue
            records.append({
                'parent_tag':  parent.tag,
                'child_tag':   el.tag,
                'value':       text,
                'has_neg':     text.startswith('-') or (
                                   '~' in text and any(p.startswith('-') for p in text.split('~'))),
                'is_float':    '.' in text,
                'file':        rel,
                'def_name':    _find_def_name(el),
            })

    if not records:
        print('No numeric nodes found.')
        return pd.DataFrame(), []

    df = pd.DataFrame(records)
    summary = (
        df.groupby(['parent_tag', 'child_tag'])
        .agg(
            count    = ('value', 'count'),
            examples = ('value', lambda x: ', '.join(sorted(set(x))[:6])),
            has_neg  = ('has_neg', 'any'),
        )
        .sort_values(['parent_tag', 'child_tag'])
        .reset_index()
    )
    summary['path'] = summary['parent_tag'] + '/' + summary['child_tag']
    return summary, records


_disc_summary, _disc_records = scan_numeric_nodes(TIER1_FILES)

print(f'Discovered {len(_disc_summary)} unique parent/child numeric pairs:\\n')
pd.set_option('display.max_rows', 300)
pd.set_option('display.max_colwidth', 60)
display(_disc_summary[['path', 'count', 'examples', 'has_neg']])


def build_default_mapping(factor: float = 0.83334) -> dict:
    \"\"\"
    Auto-generate a NODE_SCALING dict from the current discovery results.
    Every discovered path is mapped to *factor*.
    Re-run Cell 2 first, then call this to rebuild/reset the mapping in Cell 3.
    \"\"\"
    return {row['path']: factor for _, row in _disc_summary.iterrows()}


# Uncomment to print a ready-to-paste NODE_SCALING stub:
# import pprint; pprint.pprint(build_default_mapping())\
""")

# ─────────────────────────────────────────────────────────────────────────────
C3 = code("""\
# ── Cell 3: Scaling Config (EDITABLE) ────────────────────────────────────────
# Edit this cell to control which nodes are scaled and by how much.
#
# Key format:
#   "parent_tag/child_tag"  — exact parent→child match
#   "parent_tag/*"          — wildcard: any numeric child of parent_tag
#
# To rebuild from scratch: re-run Cell 2, then call build_default_mapping().

STAT_FACTOR: float = 0.83334
# COST_FACTOR applies to costList ingredient quantities and recipe ingredient
# counts.  Set a different value here if you want separate cost scaling,
# or set it equal to STAT_FACTOR to scale everything the same way.
COST_FACTOR: float = STAT_FACTOR  # ← adjust as needed

NODE_SCALING: dict = {

    # ── Combat / Armor ────────────────────────────────────────────────────────
    'statBases/ArmorRating_Sharp':           STAT_FACTOR,
    'statBases/ArmorRating_Blunt':           STAT_FACTOR,
    'statBases/ArmorRating_Heat':            STAT_FACTOR,
    'statBases/MaxHitPoints':                STAT_FACTOR,
    'statBases/Insulation_Cold':             STAT_FACTOR,
    'statBases/Insulation_Heat':             STAT_FACTOR,
    'statBases/StuffPower_Armor_Sharp':      STAT_FACTOR,
    'statBases/StuffPower_Armor_Blunt':      STAT_FACTOR,
    'statBases/StuffPower_Armor_Heat':       STAT_FACTOR,
    'statBases/StuffPower_Insulation_Cold':  STAT_FACTOR,
    'statBases/StuffPower_Insulation_Heat':  STAT_FACTOR,
    'statBases/SharpDamageMultiplier':       STAT_FACTOR,
    'statBases/BluntDamageMultiplier':       STAT_FACTOR,
    'statFactors/*':                         STAT_FACTOR,   # wildcard: all stuff statFactors

    # ── Market Value ──────────────────────────────────────────────────────────
    'statBases/MarketValue':                 STAT_FACTOR,

    # ── Medical ───────────────────────────────────────────────────────────────
    'statBases/MedicalPotency':              STAT_FACTOR,
    'statBases/MedicalQualityMax':           STAT_FACTOR,

    # ── Weapon / Projectile ───────────────────────────────────────────────────
    'projectile/damageAmountBase':           STAT_FACTOR,
    'projectile/stoppingPower':              STAT_FACTOR,
    'projectile/armorPenetrationBase':       STAT_FACTOR,
    'projectile/explosionRadius':            STAT_FACTOR,
    'li/range':                              STAT_FACTOR,
    'li/burstShotCount':                     STAT_FACTOR,
    # 'li/warmupTime':             STAT_FACTOR,  # lower=faster fire (buff) — opt-in
    # 'li/ticksBetweenBurstShots': STAT_FACTOR,  # lower=faster burst (buff) — opt-in
    # 'projectile/speed':          STAT_FACTOR,  # lower=slower projectile  — opt-in
    # Accuracy stats (all reduce hit chance — enable to nerf aim):
    # 'statBases/AccuracyTouch':   STAT_FACTOR,
    # 'statBases/AccuracyShort':   STAT_FACTOR,
    # 'statBases/AccuracyMedium':  STAT_FACTOR,
    # 'statBases/AccuracyLong':    STAT_FACTOR,
    # 'statBases/RangedWeapon_Cooldown':      STAT_FACTOR,  # lower=faster (buff) — opt-in
    # 'statBases/ShootingAccuracyTurret':     STAT_FACTOR,

    # ── Work & Crafting ───────────────────────────────────────────────────────
    'statBases/WorkToMake':                  STAT_FACTOR,
    'statBases/WorkToBuild':                 STAT_FACTOR,

    # ── Costs ─────────────────────────────────────────────────────────────────
    'costList/*':   COST_FACTOR,   # wildcard: all direct children of <costList>
    'li/count':     COST_FACTOR,   # recipe ingredient counts
    # 'products/*': COST_FACTOR,   # recipe output quantities       — opt-in

    # ── Drug / Medical Comps ──────────────────────────────────────────────────
    'li/addictiveness':                      STAT_FACTOR,
    'li/existingAddictionSeverityOffset':    STAT_FACTOR,
    'li/needLevelOffset':                    STAT_FACTOR,
    'li/overdoseSeverityOffset':             STAT_FACTOR,
    'li/largeOverdoseChance':                STAT_FACTOR,
    'li/offset':                             STAT_FACTOR,   # hediff stage capacity offsets

    # ── Buildings / Comps ─────────────────────────────────────────────────────
    'li/basePowerConsumption':               STAT_FACTOR,
    'li/storedEnergyMax':                    STAT_FACTOR,
    'building/heatPerTickWhileWorking':      STAT_FACTOR,
    'ThingDef/fertility':                    STAT_FACTOR,
    # 'li/consumptionPerTick':  STAT_FACTOR,  # neutronium pipe consumption — opt-in
    # 'li/radius':              STAT_FACTOR,  # pump effective radius       — opt-in
    # 'li/daysToRadius':        STAT_FACTOR,  # pump growth rate            — opt-in

    # ── Equipped Stat Offsets ─────────────────────────────────────────────────
    # Wildcard — applies to ALL children including negative offsets.
    # Negative values become less negative (smaller penalty), per design.
    'equippedStatOffsets/*':                 STAT_FACTOR,
}

print(f'NODE_SCALING: {len(NODE_SCALING)} entries | STAT_FACTOR={STAT_FACTOR} | COST_FACTOR={COST_FACTOR}')\
""")

# ─────────────────────────────────────────────────────────────────────────────
C4 = code("""\
# ── Cell 4: Dry-Run Preview ───────────────────────────────────────────────────
# Computes all changes without writing any files.


def scale_value(text: str, factor: float) -> str:
    \"\"\"
    Scale a numeric XML text value by *factor*.

    - Integer original (no '.'): result coalesced to nearest int.
    - Float original (has '.'):  result rounded to 2 decimal places.
    - Range (min~max):           each part treated independently by the above rules.
    \"\"\"
    def _scale_part(part: str) -> str:
        v = float(part)
        scaled = v * factor
        return f'{scaled:.2f}' if '.' in part else str(int(round(scaled)))

    text = text.strip()
    if '~' in text:
        lo, hi = text.split('~', 1)
        return f'{_scale_part(lo)}~{_scale_part(hi)}'
    return _scale_part(text)


def resolve_factor(parent_tag: str, child_tag: str, mapping: dict):
    \"\"\"
    Look up the scaling factor for (parent_tag, child_tag).
    Checks exact 'parent/child' first, then wildcard 'parent/*'.
    Returns None if no match.
    \"\"\"
    exact    = f'{parent_tag}/{child_tag}'
    wildcard = f'{parent_tag}/*'
    return mapping.get(exact, mapping.get(wildcard))


def preview_changes(files: list, mapping: dict) -> pd.DataFrame:
    \"\"\"
    Walk all files and collect every node that would be changed.
    No files are written.
    \"\"\"
    rows = []
    for fpath in files:
        try:
            tree = ET.parse(str(fpath))
        except ET.XMLSyntaxError as exc:
            print(f'\\u26a0  Parse error: {fpath.name}: {exc}')
            continue
        rel = str(fpath.relative_to(MOD_ROOT.parent))
        for el in tree.getroot().iter():
            if not isinstance(el.tag, str):
                continue
            text = (el.text or '').strip()
            if not _is_numeric_text(text):
                continue
            parent = el.getparent()
            if parent is None:
                continue
            factor = resolve_factor(parent.tag, el.tag, mapping)
            if factor is None:
                continue
            new_text = scale_value(text, factor)
            if new_text == text:
                continue
            rows.append({
                'file':   rel,
                'def':    _find_def_name(el),
                'path':   f'{parent.tag}/{el.tag}',
                'old':    text,
                'new':    new_text,
                'factor': factor,
            })

    cols = ['file', 'def', 'path', 'old', 'new', 'factor']
    return pd.DataFrame(rows, columns=cols) if rows else pd.DataFrame(columns=cols)


_preview_df = preview_changes(TIER1_FILES, NODE_SCALING)
files_hit   = _preview_df['file'].nunique() if not _preview_df.empty else 0
print(f'Dry-run: {len(_preview_df)} node(s) would change across {files_hit} file(s)\\n')
pd.set_option('display.max_rows', 500)
pd.set_option('display.max_colwidth', 90)
display(_preview_df)\
""")

# ─────────────────────────────────────────────────────────────────────────────
C5 = code("""\
# ── Cell 5: Apply Changes ─────────────────────────────────────────────────────
# ⚠  Set APPLY = True to write changes to disk.

APPLY: bool = False   # ← flip to True when ready


def backup_file(path: Path) -> bool:
    \"\"\"Copy path → path.bak.  Skips if .bak is already newer.  Returns True if backed up.\"\"\"
    bak = path.with_suffix(path.suffix + '.bak')
    if bak.exists() and bak.stat().st_mtime >= path.stat().st_mtime:
        return False
    shutil.copy2(str(path), str(bak))
    return True


def apply_changes(files: list, mapping: dict, *, dry_run: bool = True) -> pd.DataFrame:
    \"\"\"
    Apply scaling to all matching nodes.  If dry_run=True nothing is written.
    Uses lxml for accurate parent-context matching, then writes via targeted
    byte-level substitution to preserve original file formatting exactly.
    \"\"\"
    all_rows = []

    for fpath in files:
        try:
            tree = ET.parse(str(fpath))
        except ET.XMLSyntaxError as exc:
            print(f'\\u26a0  Parse error: {fpath.name}: {exc}')
            continue

        rel       = str(fpath.relative_to(MOD_ROOT.parent))
        file_rows = []

        for el in tree.getroot().iter():
            if not isinstance(el.tag, str):
                continue
            text = (el.text or '').strip()
            if not _is_numeric_text(text):
                continue
            parent = el.getparent()
            if parent is None:
                continue
            factor = resolve_factor(parent.tag, el.tag, mapping)
            if factor is None:
                continue
            new_text = scale_value(text, factor)
            if new_text == text:
                continue
            file_rows.append({
                'file':     rel,
                'def':      _find_def_name(el),
                'path':     f'{parent.tag}/{el.tag}',
                '_tag':     el.tag,
                'old':      text,
                'new':      new_text,
                'factor':   factor,
            })

        if file_rows and not dry_run:
            # Detect original encoding from XML declaration
            raw       = fpath.read_bytes()
            decl_enc  = re.search(rb'encoding=[\"\\']([^\"\\']+)[\"\\']', raw[:100])
            enc       = decl_enc.group(1).decode() if decl_enc else 'utf-8'
            content   = raw.decode(enc)

            for row in file_rows:
                tag, old_v, new_v = row['_tag'], row['old'], row['new']
                # Match <tag>value</tag> with optional surrounding whitespace
                pat     = rf'(<{re.escape(tag)}>)\\s*{re.escape(old_v)}\\s*(</{re.escape(tag)}>)'
                content = re.sub(pat, rf'\\g<1>{new_v}\\g<2>', content)

            backed_up = backup_file(fpath)
            fpath.write_bytes(content.encode(enc))
            bak_note = ' (backup created)' if backed_up else ' (backup already exists)'
            print(f'  \\u2713 {rel}: {len(file_rows)} change(s){bak_note}')

        all_rows.extend(file_rows)

    cols = ['file', 'def', 'path', 'old', 'new', 'factor']
    df   = pd.DataFrame([{k: r[k] for k in cols} for r in all_rows], columns=cols) if all_rows \\
           else pd.DataFrame(columns=cols)
    return df


if not APPLY:
    print('APPLY = False — no files written.')
    print(f'Cell 4 dry-run shows {len(_preview_df)} pending change(s).')
    print('Set APPLY = True and re-run this cell to apply.')
else:
    print('Applying changes...\\n')
    _apply_df = apply_changes(TIER1_FILES, NODE_SCALING, dry_run=False)
    n_files   = _apply_df['file'].nunique() if not _apply_df.empty else 0
    print(f'\\nDone — {len(_apply_df)} change(s) written across {n_files} file(s).')
    display(_apply_df)\
""")

# ─────────────────────────────────────────────────────────────────────────────
C6 = code("""\
# ── Cell 6: Verification ──────────────────────────────────────────────────────
# Re-runs the dry-run against the (now-modified) files.
# If all changes were applied, no further changes should be pending.

_verify_df = preview_changes(TIER1_FILES, NODE_SCALING)

if _verify_df.empty:
    print('\\u2713 Verification passed — no further changes detected. All values are already scaled.')
else:
    print(f'\\u26a0  {len(_verify_df)} change(s) still pending (files may not have been written yet).')
    display(_verify_df)\
""")

# ─────────────────────────────────────────────────────────────────────────────
notebook = {
    "nbformat": 4,
    "nbformat_minor": 5,
    "metadata": {
        "kernelspec": {
            "display_name": "Python 3",
            "language": "python",
            "name": "python3"
        },
        "language_info": {
            "name": "python",
            "version": "3.11.0"
        }
    },
    "cells": [C0, C1, C2, C3, C4, C5, C6]
}

out = pathlib.Path(__file__).parent / 'tier1_balance_scaler.ipynb'
out.write_text(json.dumps(notebook, indent=1, ensure_ascii=False), encoding='utf-8')
print(f'Written: {out}')
