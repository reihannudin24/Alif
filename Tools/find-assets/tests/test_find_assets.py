#!/usr/bin/env python3
"""
Unit tests for find_assets.py.
"""

import os
import sys
import unittest
from pathlib import Path
from PIL import Image

# Add Tools/find-assets to path
tools_find_assets_dir = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(tools_find_assets_dir))

import find_assets


class TestFindAssets(unittest.TestCase):
    def setUp(self):
        self.repo_root = tools_find_assets_dir.parent.parent
        self.scratch_dir = tools_find_assets_dir / "tests" / "tmp"
        self.scratch_dir.mkdir(parents=True, exist_ok=True)

        # Create small 16x16 test sprite
        self.test_img_path = self.scratch_dir / "test_sprite.png"
        img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
        # Draw red 8x8 square in top left, blue 8x8 square in bottom right
        for y in range(8):
            for x in range(8):
                img.putpixel((x, y), (255, 0, 0, 255))
        for y in range(8, 16):
            for x in range(8, 16):
                img.putpixel((x, y), (0, 0, 255, 255))
        img.save(self.test_img_path)

    def tearDown(self):
        if self.scratch_dir.exists():
            for p in self.scratch_dir.glob("*"):
                try:
                    p.unlink()
                except Exception:
                    pass
            try:
                self.scratch_dir.rmdir()
            except Exception:
                pass

    def test_search_local_and_catalog(self):
        res = find_assets.search_assets("button", self.repo_root, category="ui")
        self.assertGreater(res["local_matches_count"], 0)
        self.assertGreater(res["catalog_matches_count"], 0)
        top_local = res["local_results"][0]
        self.assertIn("button", top_local["name"].lower())

    def test_inspect_asset(self):
        info = find_assets.inspect_asset(self.test_img_path)
        self.assertEqual(info["file"], "test_sprite.png")
        self.assertEqual(info["dimensions"]["width"], 16)
        self.assertEqual(info["dimensions"]["height"], 16)
        self.assertTrue(info["has_alpha"])
        self.assertGreaterEqual(len(info["dominant_palette"]), 2)

    def test_recolor_image(self):
        with Image.open(self.test_img_path) as img:
            # Recolor red (#ff0000) to green (#00ff00)
            recolored = find_assets.recolor_image(img, {"#ff0000": "#00ff00"}, tolerance=20)
            px = recolored.getpixel((2, 2))
            self.assertEqual(px, (0, 255, 0, 255))
            # Blue square should remain unchanged
            blue_px = recolored.getpixel((12, 12))
            self.assertEqual(blue_px, (0, 0, 255, 255))

    def test_tint_image(self):
        with Image.open(self.test_img_path) as img:
            tinted = find_assets.tint_image(img, "#ffffff", strength=0.5)
            r, g, b, a = tinted.getpixel((2, 2))
            self.assertGreater(r, 200)
            self.assertGreater(g, 100) # Blended towards white
            self.assertGreater(b, 100)

    def test_scale_pixel_art(self):
        with Image.open(self.test_img_path) as img:
            scaled = find_assets.scale_pixel_art(img, "2x")
            self.assertEqual(scaled.size, (32, 32))
            # Test nearest neighbor crispness
            self.assertEqual(scaled.getpixel((0, 0)), (255, 0, 0, 255))

    def test_slice_image(self):
        with Image.open(self.test_img_path) as img:
            sliced = find_assets.slice_image(img, 0, 0, 8, 8)
            self.assertEqual(sliced.size, (8, 8))
            self.assertEqual(sliced.getpixel((4, 4)), (255, 0, 0, 255))

    def test_composite_images(self):
        base = Image.new("RGBA", (32, 32), (50, 50, 50, 255))
        overlay = Image.new("RGBA", (8, 8), (255, 255, 0, 255))
        comp = find_assets.composite_images(base, overlay, position="center")
        self.assertEqual(comp.size, (32, 32))
        # Center should have overlay color
        self.assertEqual(comp.getpixel((16, 16)), (255, 255, 0, 255))

    def test_full_modify_pipeline(self):
        out_path = self.scratch_dir / "modified_output.png"
        res = find_assets.modify_asset(
            input_path=self.test_img_path,
            output_path=out_path,
            tint_color="#00ff00",
            scale_spec="2x"
        )
        self.assertTrue(out_path.exists())
        self.assertTrue(out_path.with_suffix(".png.meta").exists())
        self.assertEqual(res["dimensions"]["width"], 32)
        self.assertEqual(res["dimensions"]["height"], 32)


if __name__ == "__main__":
    unittest.main()
