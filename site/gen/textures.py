"""Find a def's in-game texture and export a small web copy of it."""
from __future__ import annotations

import hashlib
import math
from dataclasses import dataclass
from pathlib import Path

from PIL import Image

from .defs import Def

ICON_SIZE = 128  # px, longest side
PORTRAIT_SIZE = 640  # px, longest side of a whole mech or vehicle (shown at about half that)


@dataclass
class Layer:
    """One texture of a composed picture, placed the way the game draws it."""
    src: Path
    size: tuple[float, float]  # drawSize, in cells
    offset: tuple[float, float] = (0.0, 0.0)  # in cells: x to the right, z up
    turn: float = 0.0  # degrees clockwise
    depth: float = 0.0  # lower is drawn first


class Textures:
    def __init__(self, root: Path, folder_paths: list[str], out_dir: Path):
        dirs = [root / "Textures"]
        dirs += [root / p / "Textures" for p in folder_paths if p]
        self.dirs = [d for d in dirs if d.is_dir()]
        self.out_dir = out_dir
        self._exported: dict[Path, str] = {}
        # Lower-case index, so a texPath whose case differs from the file still
        # resolves on Linux (the Pages build) the same way it does on Windows.
        self._files: list[dict[str, Path]] = []
        self._folders: list[dict[str, list[Path]]] = []
        for d in self.dirs:
            files: dict[str, Path] = {}
            folders: dict[str, list[Path]] = {}
            for p in sorted(d.rglob("*.png")):
                rel = p.relative_to(d).with_suffix("").as_posix().lower()
                files.setdefault(rel, p)
                if not p.stem.endswith("_m"):
                    folders.setdefault(rel.rpartition("/")[0], []).append(p)
            self._files.append(files)
            self._folders.append(folders)

    def find(self, tex_path: str | None, graphic_class: str | None = None) -> Path | None:
        """Resolve a texPath the way the common graphic classes do:
        Graphic_Single uses the file, Graphic_Multi its _south side,
        Graphic_StackCount the last (fullest) file in the folder, and the
        random classes the first."""
        if not tex_path:
            return None
        key = tex_path.replace("\\", "/").strip("/").lower()
        for files, folders in zip(self._files, self._folders):
            for suffix in ("", "_south", "_east", "_north"):
                if key + suffix in files:
                    return files[key + suffix]
            if key in folders:
                return folders[key][-1] if "StackCount" in (graphic_class or "") else folders[key][0]
        return None

    def find_side(self, tex_path: str | None, facing: str) -> Path | None:
        """The side of a multi-sided texture that faces `facing` ("south", "east"...), else what find() picks."""
        if tex_path:
            key = tex_path.replace("\\", "/").strip("/").lower() + "_" + facing
            for files in self._files:
                if key in files:
                    return files[key]
        return self.find(tex_path)

    def compose(self, name: str, layers: list[Layer]) -> dict | None:
        """Draw the layers into one picture: each texture stretched to its drawSize, turned, centred on its offset,
        lowest depth first. Writes a copy no bigger than PORTRAIT_SIZE and returns its URL and size."""
        if not layers:
            return None
        images = []
        for layer in layers:
            with Image.open(layer.src) as im:
                images.append(im.convert("RGBA"))
        # Work at the sharpest texture's own scale, so nothing is blown up before the final shrink.
        per_cell = max(im.width / layer.size[0] for im, layer in zip(images, layers))
        placed = []
        for im, layer in zip(images, layers):
            im = im.resize((max(1, round(layer.size[0] * per_cell)), max(1, round(layer.size[1] * per_cell))), Image.LANCZOS)
            if layer.turn:
                im = im.rotate(-layer.turn, resample=Image.BICUBIC, expand=True)
            x = layer.offset[0] * per_cell - im.width / 2
            y = -layer.offset[1] * per_cell - im.height / 2
            placed.append((layer.depth, x, y, im))
        left = min(x for _, x, _, _ in placed)
        top = min(y for _, _, y, _ in placed)
        width = math.ceil(max(x + im.width for _, x, _, im in placed) - left)
        height = math.ceil(max(y + im.height for _, _, y, im in placed) - top)
        canvas = Image.new("RGBA", (width, height))
        for _, x, y, im in sorted(placed, key=lambda p: p[0]):
            canvas.alpha_composite(im, (round(x - left), round(y - top)))
        bbox = canvas.getbbox()
        if bbox:
            canvas = canvas.crop(bbox)
        canvas.thumbnail((PORTRAIT_SIZE, PORTRAIT_SIZE), Image.LANCZOS)
        key = repr([(l.src.as_posix(), l.size, l.offset, l.turn, l.depth) for l in layers])
        file = f"{name}-{hashlib.sha1(key.encode()).hexdigest()[:8]}.png"
        self.out_dir.mkdir(parents=True, exist_ok=True)
        canvas.save(self.out_dir / file, optimize=True)
        return {"src": f"assets/icons/{file}", "width": canvas.width, "height": canvas.height}

    def for_def(self, d: Def | None) -> Path | None:
        if d is None:
            return None
        p = self.find(d.text("uiIconPath"))
        return p or self.find(d.text("graphicData/texPath"), d.text("graphicData/graphicClass"))

    def export(self, src: Path | None) -> str | None:
        """Write a downscaled copy into the site and return its URL."""
        if src is None:
            return None
        if src in self._exported:
            return self._exported[src]
        digest = hashlib.sha1(src.as_posix().encode()).hexdigest()[:8]
        name = f"{src.stem}-{digest}.png"
        dst = self.out_dir / name
        dst.parent.mkdir(parents=True, exist_ok=True)
        with Image.open(src) as im:
            im = im.convert("RGBA")
            bbox = im.getbbox()  # trim empty margins so icons fill their slot
            if bbox:
                im = im.crop(bbox)
            im.thumbnail((ICON_SIZE, ICON_SIZE), Image.LANCZOS)
            im.save(dst, optimize=True)
        url = f"assets/icons/{name}"
        self._exported[src] = url
        return url

    def icon(self, d: Def | None) -> str | None:
        return self.export(self.for_def(d))
