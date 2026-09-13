#!/usr/bin/env python3
"""
CLI entry point for Alif Design Asset Generation & Validation.
Usage:
    ./Tools/alif asset-gen generate-spec MarketBazaar --theme market
    ./Tools/alif asset-gen create-tile stall_fruit --type market_stall
    ./Tools/alif asset-gen create-emote emote_surprise --type alert
    ./Tools/alif asset-gen validate-all
"""

import argparse
import sys
from pathlib import Path

# Add parent directory to path so relative package import works
current_dir = Path(__file__).resolve().parent
if str(current_dir) not in sys.path:
    sys.path.insert(0, str(current_dir))

try:
    from generator import AssetGenerator
    from validator import SpecValidator
except ImportError:
    from .generator import AssetGenerator
    from .validator import SpecValidator


def main():
    parser = argparse.ArgumentParser(
        description="Alif Design Asset Generation & Validation Tool",
        formatter_class=argparse.RawDescriptionHelpFormatter
    )
    subparsers = parser.add_subparsers(dest="command", help="Asset command to execute")

    # generate-spec
    p_spec = subparsers.add_parser("generate-spec", help="Generate a valid TileRPG world spec")
    p_spec.add_argument("name", help="Name of the level (e.g. MarketBazaar)")
    p_spec.add_argument("--theme", choices=["market", "garden", "station", "kampoeng"], default="market", help="Level theme")
    p_spec.add_argument("--width", type=int, default=12, help="Grid width in tiles")
    p_spec.add_argument("--height", type=int, default=8, help="Grid height in tiles")
    p_spec.add_argument("--cell-size", type=float, default=1.0, help="World unit cell size")
    p_spec.add_argument("--output", type=str, default=None, help="Custom output path for spec JSON")

    # create-tile
    p_tile = subparsers.add_parser("create-tile", help="Generate a 64x64 pixel art tile with paired .meta")
    p_tile.add_argument("name", help="Tile asset name (e.g. tile_fruit_stall)")
    p_tile.add_argument("--type", choices=["market_stall", "crate", "tree", "flower", "station_bench", "stone"], default="stone")
    p_tile.add_argument("--output", type=str, default=None, help="Custom output path")

    # create-emote
    p_emote = subparsers.add_parser("create-emote", help="Generate an EmoteBubble sprite with paired .meta")
    p_emote.add_argument("name", help="Emote asset name (e.g. emote_alert)")
    p_emote.add_argument("--type", choices=["alert", "heart", "question", "sparkle", "sweat"], default="alert")
    p_emote.add_argument("--output", type=str, default=None, help="Custom output path")

    # create-dialogue
    p_dial = subparsers.add_parser("create-dialogue", help="Generate a Yarn Spinner dialogue file with paired .meta")
    p_dial.add_argument("name", help="Dialogue node name (e.g. BuSiti_MarketWelcome)")
    p_dial.add_argument("--speaker", default="Bu Siti", help="Speaker character name")
    p_dial.add_argument("--lines", nargs="+", default=["Selamat datang di warung!", "Mau beli apa hari ini?"], help="Lines spoken")

    # validate-all
    subparsers.add_parser("validate-all", help="Validate all TileRPG specs and check for missing .meta files")

    # validate-spec
    p_val_spec = subparsers.add_parser("validate-spec", help="Validate a specific TileRPG spec JSON file")
    p_val_spec.add_argument("spec_path", help="Path to spec JSON file")

    args = parser.parse_args()
    if not args.command:
        parser.print_help()
        sys.exit(1)

    alif_root = current_dir.parent.parent
    generator = AssetGenerator(alif_root)
    validator = SpecValidator(alif_root)

    if args.command == "generate-spec":
        out_path = Path(args.output) if args.output else (alif_root / f"Assets/AI/TileRpg/Specs/{args.name}.json")
        try:
            spec = generator.generate_tilerpg_spec(
                name=args.name,
                theme=args.theme,
                width=args.width,
                height=args.height,
                cell_size=args.cell_size,
                output_file=out_path
            )
            print(f"✅ Generated TileRPG spec: {out_path}")
            print(f"   Dimensions: {spec['width']}x{spec['height']} (cellSize: {spec['cellSize']})")
            print(f"   Spawn: ({spec['spawn']['x']}, {spec['spawn']['y']}) | Exit: ({spec['exit']['x']}, {spec['exit']['y']})")
            print(f"   Palette entries: {len(spec['palette'])}")
        except Exception as e:
            print(f"❌ Error generating spec: {e}", file=sys.stderr)
            sys.exit(1)

    elif args.command == "create-tile":
        out_path = Path(args.output) if args.output else None
        p = generator.create_procedural_tile(args.name, args.type, out_path)
        print(f"✅ Created tile: {p} (+ matching .meta)")

    elif args.command == "create-emote":
        out_path = Path(args.output) if args.output else None
        p = generator.create_reaction_emote(args.name, args.type, out_path)
        print(f"✅ Created emote: {p} (+ matching .meta)")

    elif args.command == "create-dialogue":
        p = generator.create_yarn_dialogue(
            node_title=args.name,
            speaker=args.speaker,
            lines=args.lines,
            choices=[("Tentu, saya ingin melihat menu.", f"{args.name}_Menu"), ("Lain kali saja ya, Bu.", f"{args.name}_Decline")]
        )
        print(f"✅ Created Yarn dialogue: {p} (+ matching .meta)")

    elif args.command == "validate-spec":
        spec_file = Path(args.spec_path)
        valid, errors = validator.validate_spec_file(spec_file, require_assets=True)
        if valid:
            print(f"✅ Spec {spec_file.name} is completely valid!")
        else:
            print(f"❌ Spec {spec_file.name} failed validation:", file=sys.stderr)
            for err in errors:
                print(f"   - {err}", file=sys.stderr)
            sys.exit(1)

    elif args.command == "validate-all":
        print("========================================")
        print("🔍 Validating TileRPG Specs & Assets")
        print("========================================")
        spec_results = validator.validate_all_specs()
        print(f"TileRPG Specs: {spec_results['passed']}/{spec_results['total']} passed.")
        for name, details in spec_results["details"].items():
            status_icon = "✅" if details["status"] == "PASS" else "❌"
            print(f"  {status_icon} {name}")
            for err in details["errors"]:
                print(f"     -> {err}")

        missing_meta = validator.find_missing_meta_files()
        if missing_meta:
            print(f"\n⚠️  Found {len(missing_meta)} assets missing .meta files:")
            for m in missing_meta[:10]:
                print(f"  - {m}")
            if len(missing_meta) > 10:
                print(f"  ... and {len(missing_meta) - 10} more.")
        else:
            print("\n✅ All assets under Assets/ have paired .meta files.")

        if spec_results["failed"] > 0:
            sys.exit(1)


if __name__ == "__main__":
    main()
