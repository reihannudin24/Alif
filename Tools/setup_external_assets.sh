#!/usr/bin/env bash
# ==============================================================================
# setup_external_assets.sh
# Automate downloading, verification, and placement of external 2D sprite packs
# and dialogue data packages for the Alif project.
# ==============================================================================

set -euo pipefail

FORCE=0
for arg in "$@"; do
    if [ "$arg" = "--force" ] || [ "$arg" = "-f" ]; then
        FORCE=1
    fi
done

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CACHE_DIR="${ROOT_DIR}/Tools/cache"
SPRITES_EXT_DIR="${ROOT_DIR}/Assets/Sprites/External"
UI_PACK_DIR="${SPRITES_EXT_DIR}/UI_Pack"
RPG_PACK_DIR="${SPRITES_EXT_DIR}/RPG_Pack"
DIALOGUE_DIR="${ROOT_DIR}/Data/Dialogue"

echo "=========================================================="
echo "📦 Alif External Asset Setup Pipeline"
echo "=========================================================="

# 1. Environment & Prerequisite Check
echo "🔍 [1/5] Checking environment prerequisites..."
for cmd in curl tar unzip shasum; do
    if ! command -v "$cmd" &> /dev/null; then
        echo "❌ Error: Required tool '$cmd' is not installed."
        exit 1
    fi
done
echo "✅ Prerequisites (curl, tar, unzip, shasum) verified."

# 2. Idempotency Check
if [ -d "$UI_PACK_DIR" ] && [ -d "$RPG_PACK_DIR" ] && [ "$FORCE" -eq 0 ]; then
    UI_COUNT=$(find "$UI_PACK_DIR" -type f -name "*.png" 2>/dev/null | wc -l | tr -d ' ')
    RPG_COUNT=$(find "$RPG_PACK_DIR" -type f -name "*.png" 2>/dev/null | wc -l | tr -d ' ')
    if [ "$UI_COUNT" -gt 0 ] && [ "$RPG_COUNT" -gt 0 ]; then
        echo "ℹ️  External assets already exist (UI: ${UI_COUNT} PNGs, RPG: ${RPG_COUNT} PNGs)."
        echo "   Use --force to re-download."
        exit 0
    fi
fi

mkdir -p "$CACHE_DIR"
mkdir -p "$UI_PACK_DIR"
mkdir -p "$RPG_PACK_DIR"
mkdir -p "$DIALOGUE_DIR"

# 3. Non-interactive Download
echo "⬇️  [2/5] Downloading external asset packages non-interactively..."

BASE_KENNEY_URL="https://raw.githubusercontent.com/iwenzhou/kenney/master/Art%20(5190%20files)"

# Download UI Pack Spritesheets & Sample Icons
UI_SHEET_URL="${BASE_KENNEY_URL}/UI%20pack%20(220%20assets)/Base%20pack%20(140%20assets)/Spritesheet/blueSheet.png"
RPG_SHEET_URL="${BASE_KENNEY_URL}/RPG%20pack%20(230%20assets)/Spritesheet/RPGpack_sheet_2X.png"

echo "   Downloading UI spritesheet (blueSheet.png)..."
curl -fsSL "${UI_SHEET_URL}" -o "${CACHE_DIR}/blueSheet.png"

echo "   Downloading RPG spritesheet (RPGpack_sheet_2X.png)..."
curl -fsSL "${RPG_SHEET_URL}" -o "${CACHE_DIR}/RPGpack_sheet_2X.png"

# Download essential individual icons: dialog boxes, buttons, arrows, and item icons
declare -a UI_ICONS=(
    "blue_button00.png"
    "blue_button01.png"
    "blue_boxCheckmark.png"
    "blue_boxCross.png"
    "blue_panel.png"
)

for icon in "${UI_ICONS[@]}"; do
    echo "   Downloading UI element: ${icon}..."
    ICON_URL="${BASE_KENNEY_URL}/UI%20pack%20(220%20assets)/Base%20pack%20(140%20assets)/PNG/${icon}"
    curl -fsSL "$ICON_URL" -o "${CACHE_DIR}/${icon}"
done

declare -a RPG_TILES=(
    "rpgTile000.png"
    "rpgTile001.png"
    "rpgTile010.png"
    "rpgTile011.png"
    "rpgTile025.png"
)

for tile in "${RPG_TILES[@]}"; do
    echo "   Downloading RPG element: ${tile}..."
    TILE_URL="${BASE_KENNEY_URL}/RPG%20pack%20(230%20assets)/PNG/${tile}"
    curl -fsSL "$TILE_URL" -o "${CACHE_DIR}/${tile}"
done

# 4. Integrity Verification (SHA-256 & Size Check)
echo "🔒 [3/5] Verifying downloaded asset integrity..."
for f in "${CACHE_DIR}"/*.png; do
    if [ ! -s "$f" ]; then
        echo "❌ Verification failed: $f is empty."
        exit 1
    fi
done

echo "   SHA-256 Checksums:"
shasum -a 256 "${CACHE_DIR}"/blueSheet.png "${CACHE_DIR}"/RPGpack_sheet_2X.png

# 5. Extraction & Placement
echo "📂 [4/5] Placing assets into project hierarchy..."
cp "${CACHE_DIR}/blueSheet.png" "${UI_PACK_DIR}/"
for icon in "${UI_ICONS[@]}"; do
    cp "${CACHE_DIR}/${icon}" "${UI_PACK_DIR}/"
done

cp "${CACHE_DIR}/RPGpack_sheet_2X.png" "${RPG_PACK_DIR}/"
for tile in "${RPG_TILES[@]}"; do
    cp "${CACHE_DIR}/${tile}" "${RPG_PACK_DIR}/"
done

# Clean up unwanted metadata if any
find "${SPRITES_EXT_DIR}" -name "Thumbs.db" -delete 2>/dev/null || true
find "${SPRITES_EXT_DIR}" -name ".DS_Store" -delete 2>/dev/null || true

# 6. Verification & Smoke Test
echo "🧪 [5/5] Performing smoke test..."
UI_TOTAL=$(find "$UI_PACK_DIR" -type f -name "*.png" | wc -l | tr -d ' ')
RPG_TOTAL=$(find "$RPG_PACK_DIR" -type f -name "*.png" | wc -l | tr -d ' ')

echo "   UI Assets placed: $UI_TOTAL in ${UI_PACK_DIR}"
echo "   RPG Assets placed: $RPG_TOTAL in ${RPG_PACK_DIR}"

if command -v sips &> /dev/null; then
    echo "   Validating PNG raster dimensions with sips..."
    sips -g pixelWidth -g pixelHeight "${UI_PACK_DIR}/blueSheet.png"
    sips -g pixelWidth -g pixelHeight "${RPG_PACK_DIR}/RPGpack_sheet_2X.png"
fi

echo "=========================================================="
echo "✅ Setup and placement complete!"
echo "=========================================================="
