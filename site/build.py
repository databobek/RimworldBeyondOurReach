"""Build the Beyond Our Reach player site.

    python site/build.py            # writes site/_build
    python site/build.py --serve    # builds, then serves it on http://localhost:8000
    python site/build.py --out DIR  # writes somewhere else (CI uses this)

Pages are the Markdown files in site/content. They can call the helpers in
Helpers below, e.g. {{ weapon_tables() }} or {{ screenshot("name", "caption") }},
to pull generated tables and pictures into the text.
"""
from __future__ import annotations

import argparse
import dataclasses
import functools
import http.server
import re
import shutil
import sys
from pathlib import Path

import markdown
from jinja2 import Environment, FileSystemLoader, StrictUndefined, select_autoescape
from lxml import etree
from markupsafe import Markup, escape

SITE = Path(__file__).resolve().parent
ROOT = SITE.parent
sys.path.insert(0, str(SITE))

from gen.defs import (CE_MOD_NAME, CE_PACKAGE_ID, DLC_NAMES, DLC_PACKAGE_IDS, DefDB, LoadFolder, Scenario,  # noqa: E402
                      make_scenarios, read_load_folders)
from gen.external import BAMBA_DIR, find_ce, refresh, refresh_bamba  # noqa: E402
from gen.extract import MOD_NAMES, TIERS, Extractor  # noqa: E402
from gen.textures import Textures  # noqa: E402

GEN = SITE / "gen"
EXTERNAL_PARENTS = GEN / "external_parents.xml"
BUILD_MARKER = ".bor-site-build"  # written into the output so a rebuild only ever deletes its own folder

# Alt text for the Workshop banners: their headline, since the text is baked into the image.
BANNER_ALT = {
    "production": "This mod introduces production facilities for each tier, along with materials specific to each tier.",
    "bionics": "This mod introduces over 200 bionics, based on original and mod-introduced ones.",
    "weapons": "This mod introduces weapons of various shapes and sizes: ballistic, gauss, laser guns and stun weaponry.",
    "armor": "This mod also introduces armors to complement the weaponry.",
    "turrets": "Defensive capabilities introduced via turrets.",
    "research": "Tremendous amount of research to keep your smart pawns busy.",
    "badzaari": "Roaming gangs of psychopaths itching to get their hands on your flesh.",
    "monoswords": "Forgotten technologies of monoswording.",
    "medicine": "New and improved ways of treatment and reduced consciousness.",
    "neutronium": "Neutronium: a new underground resource used by your scientists to weave fabric into matter.",
    "artifacts": "Progression gated not only by research, but by analyzable artifacts.",
}

# Top bar groups and the pages inside each, in reading order.
NAV = [
    ("Factions", [("factions", "Factions")]),
    ("Tech", [("research", "Research & artifacts"), ("materials", "Materials & neutronium"),
              ("buildings", "Buildings")]),
    ("Gear", [("weapons", "Weapons"), ("turrets", "Turrets"), ("armor", "Armor"),
              ("apparel", "Apparel"), ("mechs", "Mechs & vehicles")]),
    ("Bionics & medicine", [("bionics", "Bionics"), ("medicine", "Medicine & drugs")]),
    ("Tenebral road", [("tenebral-road", "The Tenebral road"), ("orientis", "Orientis")]),
    ("Integrations", [("integrations", "DLCs & mods")]),
    ("Recommended Bamba Integrations", [("bamba-integrations", "Recommended Bamba Integrations")]),
    ("Sneaky mechanics", [("sneaky-mechanics", "Sneaky mechanics")]),
    ("Credits", [("credits", "Credits")]),
]

DISCORD = "https://discord.gg/Pgp89VwsKw"
GITHUB = "https://github.com/databobek/RimworldBeyondOurReach"
SITE_URL = "https://databobek.github.io/RimworldBeyondOurReach/"  # for link previews
SCREENSHOT_EXTS = (".png", ".jpg", ".jpeg", ".webp")


def fmt_num(v, digits: int = 2) -> str:
    """1234.50 -> "1,234.5", 10000 with digits=0 -> "10,000", -0.3 -> "−0.3"."""
    if v is None:
        return "—"
    s = f"{v:,.{digits}f}"
    if "." in s:
        s = s.rstrip("0").rstrip(".")
    return s.replace("-", "−")


def fmt_duration(seconds) -> str:
    """90 -> "90 s", 600 -> "10 min", 3600 -> "1 h"."""
    if seconds is None:
        return "—"
    if seconds < 120:
        return f"{fmt_num(seconds, 1)} s"
    if seconds < 7200:
        return f"{fmt_num(seconds / 60, 1)} min"
    return f"{fmt_num(seconds / 3600, 1)} h"


def fmt_pct(v) -> str:
    return "—" if v is None else f"{v * 100:,.0f}%"


def fmt_armor(a) -> str:
    """Vanilla armor: a percentage, or base plus material × multiplier for stuffed apparel."""
    if not a:
        return "—"
    if a.get("stuff"):
        extra = f"{a['base'] * 100:.0f}% + " if a["base"] else ""
        return f"{extra}material ×{fmt_num(a['stuff'])}"
    return fmt_pct(a["base"])


def fmt_armor_ce(a, unit: str) -> str:
    if not a:
        return "—"
    if a.get("stuff"):
        extra = f"{fmt_num(a['base'])} + " if a["base"] else ""
        return f"{extra}material ×{fmt_num(a['stuff'])}"
    return f"{fmt_num(a['base'])} {unit}"


_RICH_TAG = re.compile(r"</?(color|b|i|size)(=[^>]*)?>", re.I)


def clean_desc(text: str | None) -> str:
    """Game descriptions use literal \\n and Unity rich-text tags."""
    if not text:
        return ""
    return _RICH_TAG.sub("", text.replace("\\n", "\n")).strip()


def fmt_desc(text: str | None) -> Markup:
    paras = [p.strip() for p in re.split(r"\n\s*\n", clean_desc(text)) if p.strip()]
    return Markup("<br><br>").join(Markup("<br>").join(escape(x) for x in p.split("\n")) for p in paras)


def fmt_desc_plain(text: str | None) -> str:
    return " ".join(clean_desc(text).split())


def slugify(s: str) -> str:
    return re.sub(r"[^a-z0-9]+", "-", s.lower()).strip("-")


def split_front_matter(text: str) -> tuple[dict, str]:
    meta: dict[str, str] = {}
    if text.startswith("---\n"):
        head, _, body = text[4:].partition("\n---\n")
        for line in head.splitlines():
            key, sep, value = line.partition(":")
            if sep:
                meta[key.strip()] = value.strip()
        return meta, body
    return meta, text


class Helpers:
    """Functions the Markdown pages can call. Each returns a placeholder that
    is swapped for real HTML after Markdown runs, so Markdown never mangles it."""

    def __init__(self, builder: "Builder"):
        self.b = builder
        self.blocks: dict[str, str] = {}
        self.uses_modes = False

    def _stash(self, html: str) -> Markup:
        key = f"BORBLOCK{len(self.b.all_blocks)}X"
        self.b.all_blocks[key] = html
        return Markup(f"\n\n{key}\n\n")

    def _macro(self, name: str, **kw) -> Markup:
        macro = getattr(self.b.macros, name)
        return self._stash(str(macro(**kw)))

    def _groups(self, kind: str, tier: str | None):
        """All groups, or just one tech line shown expanded (for a page about that line)."""
        groups = self.b.data(kind)
        if tier is None:
            return groups
        picked = [dataclasses.replace(g, open=True) for g in groups if g.tier == tier]
        if not picked:
            self.b.problems.append(f"{self.b.current_page}: no {kind} in tech line {tier!r}")
        return picked

    def weapon_tables(self, tier: str | None = None):
        self.uses_modes = True
        return self._macro("weapon_tables", groups=self._groups("ranged", tier))

    def melee_tables(self, tier: str | None = None):
        self.uses_modes = True
        return self._macro("melee_tables", groups=self._groups("melee", tier))

    def turret_tables(self, tier: str | None = None):
        self.uses_modes = True
        return self._macro("turret_tables", groups=self._groups("turrets", tier))

    def apparel_tables(self, kind: str, tier: str | None = None):
        self.uses_modes = True
        return self._macro("apparel_tables", groups=self._groups(kind, tier))

    def gallery(self, kind: str, costs: bool = False):
        return self._macro("gallery", groups=self.b.data(kind), costs=costs)

    def abilities(self, *def_names: str):
        return self._macro("abilities", items=self.b.ex.abilities(list(def_names)))

    def made_at(self, bench: str):
        items = self.b.ex.made_at(bench)
        if not items:
            self.b.problems.append(f"{self.b.current_page}: made_at({bench!r}) found nothing")
        return self._macro("things", items=items, costs=False)

    def value(self, tag: str, def_name: str, path: str, fmt: str = "num") -> Markup:
        """A number from a def, for use inside a sentence. fmt: num, pct, days (from ticks),
        seconds (from ticks), across (a radius as a diameter)."""
        d = self.b.ex.all.get(tag, def_name) or self.b.ex.v.get(tag, def_name)
        v = d.num(path) if d is not None else None
        if v is None:
            self.b.problems.append(f"{self.b.current_page}: value({tag}, {def_name}, {path}) not found")
            return Markup("?")
        out = {
            "num": lambda x: fmt_num(x),
            "pct": lambda x: f"{x * 100:g}%",
            "days": lambda x: fmt_num(x / 60000, 1),
            "seconds": lambda x: fmt_num(x / 60, 1),
            "across": lambda x: fmt_num(x * 2),
        }[fmt](v)
        return Markup(escape(out))

    def keyed_quote(self, key: str, name: str = "your colonist"):
        text = self.b.keyed().get(key)
        if text is None:
            self.b.problems.append(f"{self.b.current_page}: no Keyed string {key}")
            return Markup("")
        # The pawn's name fills {0}; capitalize it where it starts a sentence.
        text = re.sub(r"(^|[.!?][\"”]?\s+)\{0\}", lambda m: m.group(1) + name[:1].upper() + name[1:], text)
        return self._macro("quote", text=text.replace("{0}", name))

    def def_quote(self, tag: str, def_name: str):
        """A def's own in-game description, quoted."""
        d = self.b.ex.all.get(tag, def_name) or self.b.ex.v.get(tag, def_name)
        if d is None or not d.description:
            self.b.problems.append(f"{self.b.current_page}: def_quote({tag}, {def_name}) has no description")
            return Markup("")
        return self._macro("quote", text=d.description)

    def emblem(self, tex_path: str, *badges: str):
        icon = self.b.tex.export(self.b.tex.find(tex_path))
        if icon is None:
            self.b.problems.append(f"{self.b.current_page}: emblem texture {tex_path} not found")
        return self._macro("emblem", icon=icon, badges=list(badges))

    def armor_abilities(self):
        return self._macro("armor_abilities", rows=self.b.ex.armor_abilities())

    def bamba_guns(self):
        self.uses_modes = True
        return self._macro("weapon_tables", groups=self._open(self.b.bamba.ranged_weapons()))

    def bamba_melee(self):
        self.uses_modes = True
        return self._macro("melee_tables", groups=self._open(self.b.bamba.melee_weapons()))

    @staticmethod
    def _open(groups):
        return [dataclasses.replace(g, open=True) for g in groups]

    def style_pics(self, *tex_paths: str):
        """A row of small textures, for a table cell."""
        imgs = []
        for t in tex_paths:
            url = self.b.tex.export(self.b.tex.find(t))
            if url is None:
                self.b.problems.append(f"{self.b.current_page}: style texture {t} not found")
                continue
            imgs.append(f'<img src="{url}" alt="" loading="lazy">')
        return Markup('<span class="style-pics">' + "".join(imgs) + "</span>")

    def portrait(self, def_name: str, caption: str = "", facing: str | None = None):
        pic = self.b.ex.portrait(def_name, facing)
        return self._macro("portrait", pic=pic, caption=caption) if pic else Markup("")

    def research_tables(self):
        return self._macro("research_tables", groups=self.b.data("research"))

    def kwazaari_favors(self):
        return self._macro("favors", rows=self.b.ex.kwazaari_favors())

    def faction(self, def_name: str):
        f = self.b.ex.faction(def_name)
        return self._macro("faction", f=f) if f else Markup("")

    def things(self, *def_names: str):
        return self._macro("things", items=self.b.ex.things(list(def_names)), costs=True)

    def banner(self, name: str, caption: str = ""):
        src = SITE / "banners" / f"{name}.png"
        if not src.is_file():
            self.b.problems.append(f"missing banner {src.relative_to(ROOT)}")
            return Markup("")
        self.b.copy_asset(src, f"assets/banners/{name}.png")
        alt = BANNER_ALT.get(name) or f"Beyond Our Reach banner: {name.replace('-', ' ')}"
        return self._macro("banner", src=f"assets/banners/{name}.png", caption=caption, alt=alt)

    def screenshot(self, name: str, caption: str = ""):
        for ext in SCREENSHOT_EXTS:
            src = SITE / "screenshots" / f"{name}{ext}"
            if src.is_file():
                url = f"assets/screenshots/{name}{ext}"
                self.b.copy_asset(src, url)
                return self._macro("screenshot", src=url, caption=caption)
        self.b.missing_shots.append((self.b.current_page, name, caption))
        return Markup("")

    def requirements(self):
        return self._macro("requirements", deps=self.b.dependencies())

    def integration_list(self):
        return self._macro("integration_list", mods=self.b.integrations())


class Builder:
    def __init__(self, out: Path):
        self.out = out
        self.problems: list[str] = []
        self.missing_shots: list[tuple[str, str, str]] = []
        self.all_blocks: dict[str, str] = {}
        self.current_page = ""
        self._data: dict[str, object] = {}

        folders = read_load_folders(ROOT)
        scenarios = make_scenarios(folders)
        if not EXTERNAL_PARENTS.is_file():
            self.problems.append(f"{EXTERNAL_PARENTS.relative_to(ROOT)} is missing; run with --refresh-parents "
                                 "where the game is installed, or stats inherited from vanilla/CE will be blank")
        dbs = {name: DefDB(ROOT, folders, scenarios[name], EXTERNAL_PARENTS) for name in ("vanilla", "ce", "all")}
        for db in dbs.values():
            self.problems += [f"[{db.scenario.name}] {w}" for w in db.warnings]
        self.folders = folders
        self.tex = Textures(ROOT, [f.path for f in folders], out / "assets" / "icons")
        self.ex = Extractor(dbs, folders, self.tex, hard_deps=self.hard_dependencies())

        self.bamba = self._load_bamba(out)

        self.env = Environment(
            loader=FileSystemLoader(str(SITE / "templates")),
            autoescape=select_autoescape(["html"]),
            undefined=StrictUndefined,
            trim_blocks=True,
            lstrip_blocks=True,
        )
        self.env.filters.update(num=fmt_num, duration=fmt_duration, pct=fmt_pct, armor=fmt_armor, armor_ce=fmt_armor_ce,
                                desc=fmt_desc, desc_plain=fmt_desc_plain, slug=slugify)
        self.env.globals.update(tiers=TIERS)
        self.macros = self.env.get_template("macros.html").module
        self.md_env = Environment(autoescape=False, undefined=StrictUndefined)

    # ---------- data ----------

    def _load_bamba(self, out: Path) -> Extractor | None:
        """BOR's guns and melee weapons from Guns For All and Melee For All, copied into gen/bamba (see refresh_bamba)."""
        root = GEN / BAMBA_DIR
        if not root.is_dir():
            self.problems.append(f"{root.relative_to(ROOT)} is missing; run with --refresh-bamba")
            return None
        ce = CE_PACKAGE_ID
        folders = [LoadFolder("guns", (), (), ()), LoadFolder("guns/CE", (ce,), (), ()),
                   LoadFolder("melee", (), (), ()), LoadFolder("melee/CE", (ce,), (), ())]
        bor = frozenset({"bambaryla.bor.redux", "bambaryla.bor", "bamba.allbambamelee.tiered", "bambaryla.allbambaguns"})
        plain = Scenario("vanilla", DLC_PACKAGE_IDS | bor, DLC_NAMES)
        with_ce = Scenario("ce", DLC_PACKAGE_IDS | bor | {ce}, DLC_NAMES | {CE_MOD_NAME})
        dbs = {"vanilla": DefDB(root, folders, plain, EXTERNAL_PARENTS),
               "ce": DefDB(root, folders, with_ce, EXTERNAL_PARENTS)}
        dbs["all"] = dbs["vanilla"]
        for db in dbs.values():
            self.problems += [f"[bamba {db.scenario.name}] {w}" for w in db.warnings]
        tex = Textures(root, [f.path for f in folders], out / "assets" / "icons")
        return Extractor(dbs, folders, tex, hard_deps=self.hard_dependencies())

    def data(self, kind: str):
        if kind not in self._data:
            ex = self.ex
            make = {
                "ranged": ex.ranged_weapons, "melee": ex.melee_weapons, "turrets": ex.turrets,
                "armor": lambda: ex.apparel("armor"), "apparel": lambda: ex.apparel("apparel"),
                "bionics": ex.bionics, "medicine": ex.medicine, "buildings": ex.buildings,
                "research": ex.research,
            }[kind]
            self._data[kind] = make()
        return self._data[kind]

    def dependencies(self) -> list[dict]:
        about = etree.parse(str(ROOT / "About" / "About.xml")).getroot()
        out = []
        for li in about.findall("modDependencies/li"):
            url = (li.findtext("steamWorkshopUrl") or li.findtext("downloadUrl") or "").strip()
            if url.startswith("steam://url/CommunityFilePage/"):
                url = "https://steamcommunity.com/sharedfiles/filedetails/?id=" + url.rsplit("/", 1)[-1]
            out.append({"name": (li.findtext("displayName") or "").strip(), "url": url})
        return out

    @staticmethod
    def hard_dependencies() -> set[str]:
        about = etree.parse(str(ROOT / "About" / "About.xml")).getroot()
        return {(li.findtext("packageId") or "").strip().lower() for li in about.findall("modDependencies/li")}

    def integrations(self) -> list[dict]:
        seen, out = self.hard_dependencies(), []
        for f in self.folders:
            for pid in f.required_ids:
                if pid in seen or pid.startswith("ludeon."):
                    continue
                seen.add(pid)
                out.append({"id": pid, "name": MOD_NAMES.get(pid, pid)})
        return sorted(out, key=lambda m: m["name"].lower())

    def keyed(self) -> dict[str, str]:
        """English Keyed strings (game UI text), from the root and every load folder."""
        if "_keyed" not in self._data:
            out: dict[str, str] = {}
            bases = [ROOT / "Languages" / "English" / "Keyed"]
            bases += [ROOT / f.path / "Languages" / "English" / "Keyed" for f in self.folders if f.path]
            for base in bases:
                for p in sorted(base.glob("*.xml")) if base.is_dir() else []:
                    for el in etree.parse(str(p)).getroot():
                        if isinstance(el.tag, str) and el.text:
                            out[el.tag] = el.text
            self._data["_keyed"] = out
        return self._data["_keyed"]

    def workshop_url(self) -> str:
        pid = (ROOT / "About" / "PublishedFileId.txt").read_text(encoding="utf-8").strip()
        return f"https://steamcommunity.com/sharedfiles/filedetails/?id={pid}"

    # ---------- output ----------

    def copy_asset(self, src: Path, rel: str):
        dst = self.out / rel
        dst.parent.mkdir(parents=True, exist_ok=True)
        if not dst.exists() or dst.stat().st_mtime < src.stat().st_mtime:
            shutil.copy2(src, dst)

    def pages(self) -> list[dict]:
        flat = []
        for group, entries in NAV:
            for slug, title in entries:
                flat.append({"slug": slug, "title": title, "group": group})
        return flat

    def render_markdown(self, slug: str) -> tuple[dict, str, list, bool]:
        self.current_page = slug
        meta, body = split_front_matter((SITE / "content" / f"{slug}.md").read_text(encoding="utf-8"))
        helpers = Helpers(self)
        g = {name: getattr(helpers, name) for name in dir(helpers)
             if not name.startswith("_") and callable(getattr(helpers, name))}
        text = self.md_env.from_string(body).render(**g)
        md = markdown.Markdown(extensions=["toc", "tables", "attr_list", "md_in_html"],
                               extension_configs={"toc": {"toc_depth": "2-3"}})
        html = md.convert(text)
        for key, block in self.all_blocks.items():
            html = html.replace(f"<p>{key}</p>", block).replace(key, block)
        toc = [{"id": t["id"], "name": t["name"]} for t in md.toc_tokens if t["level"] == 2]
        return meta, html, toc, helpers.uses_modes

    def prepare_out(self):
        """Empty the output folder, but only if this builder made it."""
        out = self.out
        if out in (ROOT, SITE) or out in ROOT.parents:
            raise SystemExit(f"Refusing to build into {out}: pick an empty or new folder.")
        if out.exists():
            if not (out / BUILD_MARKER).is_file() and any(out.iterdir()):
                raise SystemExit(f"Refusing to delete {out}: it isn't empty and wasn't made by this builder.")
            shutil.rmtree(out)
        (out / "assets").mkdir(parents=True)
        (out / BUILD_MARKER).write_text("Output of site/build.py. Safe to delete.\n", encoding="utf-8")

    def build(self):
        self.prepare_out()
        shutil.copytree(SITE / "static", self.out / "assets", dirs_exist_ok=True)
        self.copy_asset(ROOT / ".github" / "assets" / "logo.png", "assets/logo.png")
        self.copy_asset(SITE / "screenshots" / "faction-solar.png", "assets/preview.png")
        self.copy_asset(ROOT / "Textures" / "BOR" / "Things" / "Factions" / "ModIcon.png", "assets/favicon.png")

        common = {
            "nav": NAV,
            "workshop": self.workshop_url(),
            "discord": DISCORD,
            "github": GITHUB,
            "site_url": SITE_URL,
        }
        flat = self.pages()
        cards = []
        for i, page in enumerate(flat):
            meta, html, toc, uses_modes = self.render_markdown(page["slug"])
            group_pages = next(entries for g, entries in NAV if g == page["group"])
            out_html = self.env.get_template("page.html").render(
                **common, page=page, meta=meta, content=Markup(html), toc=toc, active_group=page["group"],
                uses_modes=uses_modes, group_pages=group_pages,
                prev=flat[i - 1] if i > 0 else None,
                next=flat[i + 1] if i + 1 < len(flat) else None,
            )
            (self.out / f"{page['slug']}.html").write_text(out_html, encoding="utf-8")
            cards.append({**page, "summary": meta.get("summary", ""), "card": self.card_image(meta.get("card", ""))})

        meta, html, _, _ = self.render_markdown("index")
        index = self.env.get_template("home.html").render(**common, meta=meta, content=Markup(html), cards=cards,
                                                          active_group="")
        (self.out / "index.html").write_text(index, encoding="utf-8")
        (self.out / ".nojekyll").write_text("", encoding="utf-8")

    def card_image(self, spec: str) -> dict:
        """Front matter "card:" picks the home page thumbnail:
        banner:<name>, icon:<ThingDef>, faction:<FactionDef> or shot:<screenshot>."""
        kind, _, value = spec.partition(":")
        if kind == "banner" and (SITE / "banners" / f"{value}.png").is_file():
            self.copy_asset(SITE / "banners" / f"{value}.png", f"assets/banners/{value}.png")
            return {"kind": "banner", "src": f"assets/banners/{value}.png"}
        if kind in ("icon", "faction"):
            srcs = []
            for name in value.split(","):
                name = name.strip()
                d = self.ex.faction(name) if kind == "faction" else (self.ex.things([name]) or [None])[0]
                if d and d.get("icon"):
                    srcs.append(d["icon"])
            if srcs:
                return {"kind": "icons", "srcs": srcs}
        if kind == "tex":
            srcs = [self.tex.export(self.tex.find(p.strip())) for p in value.split(",")]
            if all(srcs):
                return {"kind": "icons", "srcs": srcs}
        if kind == "shot":
            for ext in SCREENSHOT_EXTS:
                src = SITE / "screenshots" / f"{value}{ext}"
                if src.is_file():
                    self.copy_asset(src, f"assets/screenshots/{value}{ext}")
                    return {"kind": "banner", "src": f"assets/screenshots/{value}{ext}"}
        return {"kind": "none"}

    def report(self) -> int:
        """Print problems and missing screenshots; return the problem count."""
        notes = self.problems + self.ex.notes
        if notes:
            print(f"{len(notes)} problem(s):")
            for n in notes:
                print("  -", n)
        if self.missing_shots:
            print(f"{len(self.missing_shots)} screenshot(s) not added yet (put them in site/screenshots/):")
            for page, name, caption in self.missing_shots:
                print(f"  - {name}.png  ({page}: {caption})")
        print(f"Built {self.out}")
        return len(notes)


def serve(out: Path, port: int):
    handler = functools.partial(http.server.SimpleHTTPRequestHandler, directory=str(out))
    with http.server.ThreadingHTTPServer(("127.0.0.1", port), handler) as httpd:
        print(f"Serving on http://localhost:{port}/ (Ctrl+C to stop)")
        httpd.serve_forever()


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--out", type=Path, default=SITE / "_build")
    ap.add_argument("--serve", nargs="?", const=8000, type=int, metavar="PORT")
    ap.add_argument("--strict", action="store_true", help="exit with an error if the build reports problems (CI)")
    ap.add_argument("--refresh-parents", action="store_true",
                    help="re-copy the vanilla/DLC/CE base defs into gen/external_parents.xml (needs the game installed)")
    ap.add_argument("--refresh-bamba", action="store_true",
                    help="re-copy BOR's guns and melee weapons from Guns For All and Melee For All into gen/bamba")
    ap.add_argument("--ce-dir", type=Path, help="Combat Extended folder for --refresh-parents (found automatically)")
    args = ap.parse_args()
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    if args.refresh_parents:
        game = ROOT.parents[1]
        if not (game / "Data" / "Core" / "Defs").is_dir():
            raise SystemExit(f"No RimWorld install at {game}; --refresh-parents needs the game's Data folder.")
        ce = args.ce_dir or find_ce(ROOT.parents[3])
        count, missing = refresh(ROOT, game, ce, EXTERNAL_PARENTS)
        print(f"Wrote {count} base defs to {EXTERNAL_PARENTS.relative_to(ROOT)} (Combat Extended: {ce or 'not found'})")
        if missing:
            print(f"{len(missing)} parent(s) come from other mods and stay unresolved: {', '.join(missing)}")
    if args.refresh_bamba:
        mods = ROOT.parent
        files, textures, missing = refresh_bamba(mods, GEN / BAMBA_DIR)
        print(f"Copied {files} def files and {textures} textures to {(GEN / BAMBA_DIR).relative_to(ROOT)}")
        if missing:
            print("Not found: " + "; ".join(missing))
    b = Builder(args.out.resolve())
    b.build()
    problems = b.report()
    if args.strict and problems:
        raise SystemExit(f"--strict: {problems} problem(s)")
    if args.serve:
        serve(args.out.resolve(), args.serve)


if __name__ == "__main__":
    main()
