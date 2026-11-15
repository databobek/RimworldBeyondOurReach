"""Generate chemical gene icons in the vanilla Biotech style.

RimWorld auto-generates dependency / resistant / impervious genes for every ChemicalDef
and loads their icons from Textures/UI/Icons/Genes/Chemicals/<ChemicalDef defName>/<template>.png.
Without those files the genes show the red-X placeholder.

Each icon is the drug's texture over a brown backdrop (heart = dependency, half shield =
resistant, full shield = impervious) inside a blue hexagon.

Usage:
    python Scripts/gene_icons.py                     # regenerate everything in DRUGS
    python Scripts/gene_icons.py <ChemicalDefName> <texture path relative to mod root>

To add a new drug, add a line to DRUGS and rerun.
"""
import math
import os
import sys

from PIL import Image, ImageDraw

MOD_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT_DIR = "Textures/UI/Icons/Genes/Chemicals"

# ChemicalDef defName -> drug texture (relative to mod root)
DRUGS = {
    "BOR_Chemical_Sunweave": "Textures/BOR/Things/Item/Drugs/Tier1/Happy/HappyA.png",
    "BOR_Sunsteel_Chemical": "Textures/BOR/Things/Item/Drugs/Tier1/Combat/Combat1A.png",
}

# Gene template defName (vanilla) -> backdrop shape
KINDS = {
    "ChemicalDependency": "heart",
    "AddictionResistant": "half_shield",
    "AddictionImmune": "shield",
}

SIZE = 256         # output size in px
SS = 4             # supersampling factor for smooth edges
ICON_SCALE = 0.56  # drug sprite size relative to the icon

HEX_FILL = (88, 150, 180, 255)
HEX_EDGE = (52, 98, 124, 255)
BACK = (126, 86, 58, 255)
BACK_EDGE = (78, 52, 34, 255)


def hexagon(cx, cy, r):
    """Flat-top hexagon, vertices on the left and right."""
    return [(cx + r * math.cos(math.radians(a)), cy + r * math.sin(math.radians(a)))
            for a in range(0, 360, 60)]


def heart(cx, cy, s):
    """Classic parametric heart curve."""
    pts = []
    for i in range(200):
        t = 2 * math.pi * i / 200
        x = 16 * math.sin(t) ** 3
        y = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
        pts.append((cx + x * s, cy - y * s))
    return pts


def shield(cx, cy, w, h, half=False):
    """Flat top, straight sides, curved taper to a point. half=True keeps only the left half."""
    pts = [(cx - w / 2, cy - h / 2), (cx + w / 2, cy - h / 2), (cx + w / 2, cy)]
    for i in range(1, 30):
        t = i / 30
        pts.append((cx + (w / 2) * (1 - t) ** 1.3, cy + (h / 2) * math.sin(t * math.pi / 2)))
    pts.append((cx, cy + h / 2))
    for i in range(1, 30):
        t = 1 - i / 30
        pts.append((cx - (w / 2) * (1 - t) ** 1.3, cy + (h / 2) * math.sin(t * math.pi / 2)))
    pts.append((cx - w / 2, cy))
    if half:
        pts = [(min(x, cx), y) for x, y in pts]
    return pts


def drug_sprite(path):
    img = Image.open(os.path.join(MOD_ROOT, path)).convert("RGBA")
    img = img.crop(img.getbbox())
    box = int(SIZE * SS * ICON_SCALE)
    img.thumbnail((box, box), Image.LANCZOS)
    return img


def make_icon(texture, shape):
    S = SIZE * SS
    c = S / 2
    canvas = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(canvas)

    d.polygon(hexagon(c, c, S * 0.47), fill=HEX_EDGE)
    d.polygon(hexagon(c, c, S * 0.47 - 7 * SS), fill=HEX_FILL)

    by = c + S * 0.02
    if shape == "heart":
        d.polygon(heart(c, by, S * 0.0175), fill=BACK_EDGE)
        d.polygon(heart(c, by, S * 0.0158), fill=BACK)
    else:
        half = shape == "half_shield"
        w, h = S * 0.48, S * 0.56
        d.polygon(shield(c, by, w, h, half), fill=BACK_EDGE)
        d.polygon(shield(c, by, w - 6 * SS, h - 6 * SS, half), fill=BACK)

    sprite = drug_sprite(texture)
    canvas.alpha_composite(sprite, (int(c - sprite.width / 2), int(c - sprite.height / 2 + S * 0.01)))
    return canvas.resize((SIZE, SIZE), Image.LANCZOS)


def generate(chemical, texture):
    out = os.path.join(MOD_ROOT, OUT_DIR, chemical)
    os.makedirs(out, exist_ok=True)
    for kind, shape in KINDS.items():
        path = os.path.join(out, kind + ".png")
        make_icon(texture, shape).save(path)
        print(os.path.relpath(path, MOD_ROOT))


if __name__ == "__main__":
    if len(sys.argv) == 3:
        generate(sys.argv[1], sys.argv[2])
    elif len(sys.argv) == 1:
        for chem, tex in DRUGS.items():
            generate(chem, tex)
    else:
        sys.exit(__doc__)
