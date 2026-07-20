#!/usr/bin/env python3
import argparse
import math
import os
import xml.etree.ElementTree as ET

ROOT_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
TARGET_FILES = [
    '1.6/Common/Defs/RecipeDefs/Tier1/Recipe_Neutronium.xml',
    '1.6/Common/Defs/RecipeDefs/Tier2/Recipe_Neutronium.xml',
    '1.6/Common/Defs/RecipeDefs/Tier3/Recipe_Neutronium.xml',
    '1.6/Common/Defs/RecipeDefs/Tier4/Recipe_Neutronium.xml',
]


def parse_args():
    parser = argparse.ArgumentParser(
        description='Scale workAmount values in the specified BOR Recipe_Neutronium.xml files by a multiplier.'
    )
    parser.add_argument(
        'factor',
        type=float,
        help='Multiplier to apply to each workAmount before rounding down to the nearest integer.',
    )
    return parser.parse_args()


def indent(elem, level=0):
    i = '\n' + level * '  '
    if len(elem):
        if not elem.text or not elem.text.strip():
            elem.text = i + '  '
        for child in elem:
            indent(child, level + 1)
        if not child.tail or not child.tail.strip():
            child.tail = i
    if level and (not elem.tail or not elem.tail.strip()):
        elem.tail = i


def process_file(path, factor):
    tree = ET.parse(path)
    root = tree.getroot()
    changed = 0

    for recipe in root.findall('.//RecipeDef'):
        work_amount = recipe.find('workAmount')
        if work_amount is None or work_amount.text is None:
            continue
        try:
            current = float(work_amount.text.strip())
        except ValueError:
            continue

        new_value = math.floor(current * factor)
        if new_value != int(current) or new_value != current:
            work_amount.text = str(new_value)
            changed += 1

    if changed:
        indent(root)
        tree.write(path, encoding='utf-8', xml_declaration=True)

    return changed


def main():
    args = parse_args()
    factor = args.factor

    if factor <= 0:
        raise SystemExit('Factor must be greater than zero.')

    for relative_path in TARGET_FILES:
        full_path = os.path.join(ROOT_DIR, relative_path)
        if not os.path.isfile(full_path):
            print(f'SKIP missing file: {relative_path}')
            continue

        changed_count = process_file(full_path, factor)
        print(f'{relative_path}: updated {changed_count} workAmount entries')


if __name__ == '__main__':
    main()
