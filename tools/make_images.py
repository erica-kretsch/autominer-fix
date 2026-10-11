"""Generate About/Preview.png and About/thumb.png.

LaunchPad needs both to register the mod. Run from the repo root:

    python tools/make_images.py

Needs Pillow. The picture is a section through a vertical pit: vanilla's ragged wall, where
the corner of every other layer is left standing, beside the fixed mod's clean one.
"""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent.parent
FONT_DIRS = [Path("C:/Windows/Fonts"), Path("/usr/share/fonts/truetype/dejavu")]

BG_TOP = (22, 27, 36)
BG_BOTTOM = (12, 15, 21)
TERRAIN = (112, 82, 58)
TERRAIN_DARK = (84, 61, 42)
PIT = (10, 12, 16)
GRID = (30, 36, 46)
AMBER = (255, 165, 0)
RED = (255, 84, 72)
GREEN = (96, 214, 130)
TEXT = (232, 236, 242)
MUTED = (150, 160, 175)


def font(size, bold=False):
    names = ["segoeuib.ttf", "arialbd.ttf", "DejaVuSans-Bold.ttf"] if bold else [
        "segoeui.ttf",
        "arial.ttf",
        "DejaVuSans.ttf",
    ]
    for d in FONT_DIRS:
        for n in names:
            if (d / n).exists():
                return ImageFont.truetype(str(d / n), size)
    return ImageFont.load_default()


def gradient(w, h):
    img = Image.new("RGB", (w, h))
    px = ImageDraw.Draw(img)
    for y in range(h):
        t = y / (h - 1)
        px.line(
            [(0, y), (w, y)],
            fill=tuple(round(a + (b - a) * t) for a, b in zip(BG_TOP, BG_BOTTOM)),
        )
    return img


def draw_pit(d, x, y, cell, cols, rows, skipped):
    """A pit `cols` wide and `rows` deep in terrain; `skipped` rows leave a corner standing."""
    pad = cell * 2
    w, h = cols * cell, rows * cell
    d.rectangle([x - pad, y - cell, x + w + pad, y + h + cell], fill=TERRAIN)
    # Faint strata so the terrain doesn't read as a flat block.
    for i in range(0, rows + 2):
        yy = y - cell + i * cell
        d.line([(x - pad, yy), (x + w + pad, yy)], fill=TERRAIN_DARK, width=max(1, cell // 14))
    d.rectangle([x, y, x + w, y + h], fill=PIT)
    for c in range(1, cols):
        d.line([(x + c * cell, y), (x + c * cell, y + h)], fill=GRID)
    for r in range(1, rows):
        d.line([(x, y + r * cell), (x + w, y + r * cell)], fill=GRID)
    flagged = []
    for r in skipped:
        cx0 = x + (cols - 1) * cell
        cy0 = y + r * cell
        d.rectangle([cx0, cy0, cx0 + cell, cy0 + cell], fill=TERRAIN)
        d.rectangle([cx0, cy0, cx0 + cell, cy0 + cell], outline=TERRAIN_DARK)
        flagged.append((cx0, cy0))
    return flagged, (x, y, w, h)


def render(width, height):
    s = width / 1280
    img = gradient(width, height)
    d = ImageDraw.Draw(img)

    d.text((64 * s, 44 * s), "Autominer Fix", font=font(round(84 * s), True), fill=TEXT)
    d.text(
        (68 * s, 150 * s),
        "No more skipped voxels in vertical pits",
        font=font(round(34 * s)),
        fill=AMBER,
    )

    cell = round(40 * s)
    cols, rows = 4, 6
    top = round(268 * s)
    panels = [
        ("Vanilla", round(320 * s), [1, 3, 5], RED),
        ("Fixed", round(800 * s), [], GREEN),
    ]
    for label, x, skipped, colour in panels:
        flagged, (px, py, pw, ph) = draw_pit(d, x, top, cell, cols, rows, skipped)
        for fx, fy in flagged:
            d.rectangle(
                [fx - 3 * s, fy - 3 * s, fx + cell + 3 * s, fy + cell + 3 * s],
                outline=RED,
                width=max(2, round(4 * s)),
            )
        d.text((px - 2 * cell, py + ph + cell + 16 * s), label, font=font(round(40 * s), True), fill=colour)
        if skipped:
            d.text(
                (px - 2 * cell, py + ph + cell + 64 * s),
                "corner left standing every other layer",
                font=font(round(24 * s)),
                fill=MUTED,
            )
        else:
            d.text(
                (px - 2 * cell, py + ph + cell + 64 * s),
                "every voxel cleared",
                font=font(round(24 * s)),
                fill=MUTED,
            )
    return img.convert("RGB")


def main():
    about = ROOT / "About"
    about.mkdir(exist_ok=True)
    render(1280, 720).save(about / "Preview.png", optimize=True)
    render(640, 360).save(about / "thumb.png", optimize=True)
    print("wrote", about / "Preview.png", about / "thumb.png")


if __name__ == "__main__":
    main()
