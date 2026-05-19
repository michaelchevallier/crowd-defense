#!/usr/bin/env python3
"""
V3 Pixel Diff — compare V3 screenshots vs golden baselines.
Usage: python3 tools/v3-pixel-diff.py [--update-baselines]
Output: Library/V3PixelDiff/report.json + diff PNGs
"""
import os
import sys
import json
import shutil
from datetime import datetime, timezone
from pathlib import Path

try:
    from PIL import Image, ImageChops, ImageFilter
except ImportError:
    print("ERROR: Pillow not found. Run: pip3 install Pillow")
    sys.exit(2)

SCREENSHOTS_DIR = Path("Library/V3Screenshots")
BASELINES_DIR   = Path("tools/golden-screenshots")
OUTPUT_DIR      = Path("Library/V3PixelDiff")
REPORT_PATH     = OUTPUT_DIR / "report.json"

UPDATE_BASELINES = "--update-baselines" in sys.argv

MAGENTA_THRESHOLD   = 0.50   # flag if >50% pixels are rgb(255,0,255)
UNIFORM_THRESHOLD   = 0.90   # flag if >90% pixels share the same color
DIFF_PIXEL_EPSILON  = 10     # per-channel tolerance for "different" pixel


def pixel_stats(img: Image.Image) -> dict:
    """Return magenta ratio + uniform color ratio for a PIL image."""
    rgb = img.convert("RGB")
    pixels = list(rgb.getdata())
    total = len(pixels)
    if total == 0:
        return {"magenta_ratio": 0, "uniform_ratio": 0, "dominant_color": None}

    magenta = sum(1 for r, g, b in pixels if r > 200 and g < 30 and b > 200)
    from collections import Counter
    counts = Counter(pixels)
    top_color, top_count = counts.most_common(1)[0]

    return {
        "magenta_ratio": round(magenta / total, 4),
        "uniform_ratio": round(top_count / total, 4),
        "dominant_color": list(top_color),
    }


def diff_images(img_a: Image.Image, img_b: Image.Image):
    """Return (diff_ratio, diff_image). Images are resized to match if needed."""
    if img_a.size != img_b.size:
        img_b = img_b.resize(img_a.size, Image.LANCZOS)
    a = img_a.convert("RGB")
    b = img_b.convert("RGB")
    diff = ImageChops.difference(a, b)
    pixels_diff = list(diff.getdata())
    changed = sum(
        1 for r, g, b in pixels_diff
        if r > DIFF_PIXEL_EPSILON or g > DIFF_PIXEL_EPSILON or b > DIFF_PIXEL_EPSILON
    )
    ratio = round(changed / len(pixels_diff), 4)
    # Amplify diff for visibility
    amplified = diff.point(lambda x: min(255, x * 4))
    return ratio, amplified


def process_scene(png_path: Path, scene_name: str) -> dict:
    """Analyse one screenshot; compare to baseline if present."""
    result = {"scene": scene_name, "path": str(png_path)}
    try:
        img = Image.open(png_path)
        stats = pixel_stats(img)
        result["magenta_ratio"]  = stats["magenta_ratio"]
        result["uniform_ratio"]  = stats["uniform_ratio"]
        result["dominant_color"] = stats["dominant_color"]
        result["flags"] = []

        if stats["magenta_ratio"] > MAGENTA_THRESHOLD:
            result["flags"].append("MAGENTA_PATCH — probable URP error shader")
        if stats["uniform_ratio"] > UNIFORM_THRESHOLD:
            result["flags"].append("UNIFORM_COLOR — probable render error")

        baseline = BASELINES_DIR / png_path.name
        if UPDATE_BASELINES or not baseline.exists():
            BASELINES_DIR.mkdir(parents=True, exist_ok=True)
            shutil.copy2(png_path, baseline)
            result["baseline"] = "created"
            result["diff_ratio"] = None
        else:
            baseline_img = Image.open(baseline)
            diff_ratio, diff_img = diff_images(img, baseline_img)
            result["diff_ratio"] = diff_ratio
            result["baseline"] = "compared"
            diff_out = OUTPUT_DIR / f"diff-{scene_name}.png"
            diff_img.save(diff_out)
            result["diff_png"] = str(diff_out)
            if diff_ratio > 0.05:
                result["flags"].append(f"DIFF_HIGH — {diff_ratio*100:.1f}% pixels changed vs baseline")

        result["status"] = "FAIL" if result["flags"] else "PASS"
    except Exception as ex:
        result["status"] = "ERROR"
        result["error"] = str(ex)
    return result


def main():
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    if not SCREENSHOTS_DIR.exists():
        print(f"ERROR: {SCREENSHOTS_DIR} not found — run V3ScreenshotBatch first")
        sys.exit(2)

    pngs = sorted(SCREENSHOTS_DIR.glob("*.png"))
    if not pngs:
        print(f"No PNGs found in {SCREENSHOTS_DIR}")
        sys.exit(2)

    scenes = []
    for png in pngs:
        scene_name = png.stem
        print(f"  Processing {scene_name} ...", end=" ", flush=True)
        r = process_scene(png, scene_name)
        flag_str = ", ".join(r.get("flags", [])) or "ok"
        print(f"{r['status']} ({flag_str})")
        scenes.append(r)

    total  = len(scenes)
    passed = sum(1 for s in scenes if s.get("status") == "PASS")
    failed = total - passed
    verdict = "PASS" if failed == 0 else "FAIL"

    report = {
        "timestamp": datetime.now(timezone.utc).isoformat(),
        "verdict": verdict,
        "passed": passed,
        "failed": failed,
        "baselines_dir": str(BASELINES_DIR),
        "scenes": scenes,
    }

    with open(REPORT_PATH, "w") as f:
        json.dump(report, f, indent=2)

    print(f"\n[V3PixelDiff] {verdict} — {passed}/{total} scenes clean")
    print(f"Report: {REPORT_PATH}")
    sys.exit(0 if verdict == "PASS" else 1)


if __name__ == "__main__":
    main()
