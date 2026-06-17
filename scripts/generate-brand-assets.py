#!/usr/bin/env python3
"""Generate splash logo, app icon, and README UI preview assets."""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "src" / "SpatialLabsOptimizer" / "Assets"
README_ASSETS = ROOT / "docs" / "assets" / "readme"

BG = (18, 18, 18)
ACCENT = (204, 85, 0)
ACCENT_BRIGHT = (255, 140, 0)
PANEL = (30, 30, 30)
TEXT = (226, 232, 240)
MUTED = (148, 163, 184)
CARD = (51, 65, 85)


def _font(size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    for name in ("segoeui.ttf", "arial.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()


def draw_logo(size: int) -> Image.Image:
    img = Image.new("RGBA", (size, size), BG + (255,))
    draw = ImageDraw.Draw(img)
    margin = max(4, size // 10)
    draw.rounded_rectangle(
        (margin, margin, size - margin, size - margin),
        radius=max(4, size // 8),
        fill=ACCENT_BRIGHT + (255,),
    )
    font = _font(max(10, size // 4))
    label = "3D"
    bbox = draw.textbbox((0, 0), label, font=font)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    draw.text(((size - tw) / 2, (size - th) / 2 - 2), label, fill=BG + (255,), font=font)
    return img


def draw_store_placeholder(label: str, accent: tuple[int, int, int], width: int = 600, height: int = 900) -> Image.Image:
    img = Image.new("RGB", (width, height), (30, 30, 30))
    draw = ImageDraw.Draw(img)
    draw.rounded_rectangle((24, 24, width - 24, height - 24), radius=16, outline=accent, width=4)
    font = _font(max(18, width // 18))
    bbox = draw.textbbox((0, 0), label, font=font)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    draw.text(((width - tw) / 2, (height - th) / 2), label, fill=MUTED, font=font)
    return img


def draw_placeholder_cover(width: int = 600, height: int = 900) -> Image.Image:
    img = Image.new("RGB", (width, height), (30, 30, 30))
    draw = ImageDraw.Draw(img)
    draw.rounded_rectangle((24, 24, width - 24, height - 24), radius=16, outline=ACCENT_BRIGHT, width=4)
    font = _font(max(18, width // 18))
    label = "No cover art"
    bbox = draw.textbbox((0, 0), label, font=font)
    tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
    draw.text(((width - tw) / 2, (height - th) / 2), label, fill=MUTED, font=font)
    return img


def _header(draw: ImageDraw.ImageDraw, title: str, subtitle: str, width: int) -> None:
    draw.rounded_rectangle((24, 24, width - 24, 96), radius=10, fill=PANEL)
    draw.text((40, 36), title, fill=ACCENT, font=_font(26))
    draw.text((40, 68), subtitle, fill=MUTED, font=_font(14))


def draw_library_grid(width: int = 960, height: int = 540) -> Image.Image:
    img = Image.new("RGB", (width, height), BG)
    draw = ImageDraw.Draw(img)
    _header(draw, "Ready to Play", "Library grid · favorites · queue · pinned shelf", width)
    x, y = 40, 120
    for i in range(6):
        col, row = i % 3, i // 3
        left = x + col * 290
        top = y + row * 190
        draw.rounded_rectangle((left, top, left + 260, top + 170), radius=8, fill=CARD)
        draw.rectangle((left + 12, top + 12, left + 248, top + 110), fill=(15, 23, 42))
        draw.text((left + 12, top + 120), f"Game title {i + 1}", fill=TEXT, font=_font(14))
        if i == 0:
            draw.text((left + 12, top + 142), "Favorite · Pinned", fill=ACCENT, font=_font(12))
    return img


def draw_setup_wizard(width: int = 960, height: int = 540) -> Image.Image:
    img = Image.new("RGB", (width, height), BG)
    draw = ImageDraw.Draw(img)
    _header(draw, "Setup Wizard", "Display detection · toolchain install · legal consent", width)
    steps = ["Detect display", "Install tools", "Apply defaults", "Ready"]
    sx = 40
    for idx, step in enumerate(steps):
        color = ACCENT if idx == 1 else CARD
        draw.rounded_rectangle((sx, 130, sx + 200, 170), radius=8, fill=color)
        draw.text((sx + 12, 142), step, fill=TEXT, font=_font(14))
        sx += 220
    draw.rounded_rectangle((40, 210, width - 40, height - 40), radius=10, fill=PANEL)
    draw.text((60, 240), "Recommended profile: Balanced Acer RPG", fill=TEXT, font=_font(18))
    draw.text((60, 280), "Silent install progress: ReShade, UEVR, helpers", fill=MUTED, font=_font(14))
    draw.rounded_rectangle((60, 320, 260, 360), radius=8, fill=ACCENT)
    draw.text((88, 332), "Continue", fill=BG, font=_font(14))
    return img


def draw_launch_progress(width: int = 960, height: int = 540) -> Image.Image:
    img = Image.new("RGB", (width, height), BG)
    draw = ImageDraw.Draw(img)
    _header(draw, "Launch Progress", "Play in 3D · preset cache · safe launch overlay", width)
    draw.rounded_rectangle((40, 130, width - 40, height - 40), radius=10, fill=PANEL)
    draw.text((60, 160), "Cyberpunk 2077", fill=TEXT, font=_font(22))
    draw.text((60, 200), "Step 4 of 12 — Applying optimal defaults", fill=MUTED, font=_font(14))
    draw.rounded_rectangle((60, 240, width - 60, 268), radius=6, fill=(15, 23, 42))
    draw.rounded_rectangle((60, 240, width - 360, 268), radius=6, fill=ACCENT)
    draw.text((60, 290), "PCVR runtime: OpenXR:SteamVR", fill=ACCENT, font=_font(14))
    return img


def draw_settings(width: int = 960, height: int = 540) -> Image.Image:
    img = Image.new("RGB", (width, height), BG)
    draw = ImageDraw.Draw(img)
    _header(draw, "Global 3D Settings", "Depth · convergence · session profiles · theme", width)
    draw.text((40, 130), "Depth", fill=TEXT, font=_font(16))
    draw.rounded_rectangle((40, 158, width - 40, 176), radius=6, fill=(15, 23, 42))
    draw.rounded_rectangle((40, 158, width - 280, 176), radius=6, fill=ACCENT)
    draw.text((40, 200), "Cache top presets", fill=TEXT, font=_font(16))
    draw.rounded_rectangle((40, 228, 220, 266), radius=8, fill=ACCENT)
    draw.text((58, 242), "Run bulk cache", fill=BG, font=_font(14))
    draw.text((40, 290), "Session profile: LAN night — saved 2026-06-14", fill=MUTED, font=_font(14))
    draw.text((40, 320), "Streamer hotkey: Ctrl+Shift+3", fill=MUTED, font=_font(14))
    return img


def main() -> None:
    ASSETS.mkdir(parents=True, exist_ok=True)
    README_ASSETS.mkdir(parents=True, exist_ok=True)

    draw_logo(96).save(ASSETS / "SplashLogo.png")

    ico_sizes = [16, 32, 48, 256]
    ico_images = [draw_logo(size) for size in ico_sizes]
    ico_images[0].save(
        ASSETS / "AppIcon.ico",
        format="ICO",
        sizes=[(size, size) for size in ico_sizes],
        append_images=ico_images[1:],
    )
    draw_logo(256).save(README_ASSETS / "app-icon.png")
    draw_placeholder_cover(600, 900).save(ASSETS / "placeholder-cover.png")
    draw_store_placeholder("Epic Games", (40, 40, 40)).save(ASSETS / "placeholder-epic.png")
    draw_store_placeholder("GOG", (128, 0, 64)).save(ASSETS / "placeholder-gog.png")
    draw_store_placeholder("Ubisoft", (0, 120, 180)).save(ASSETS / "placeholder-ubisoft.png")

    screens = [
        ("library-grid.png", draw_library_grid),
        ("setup-wizard.png", draw_setup_wizard),
        ("launch-progress.png", draw_launch_progress),
        ("settings.png", draw_settings),
    ]
    for filename, renderer in screens:
        renderer().save(README_ASSETS / filename)

    print(f"Wrote app assets to {ASSETS}")
    print(f"Wrote README UI previews to {README_ASSETS}")


if __name__ == "__main__":
    main()
