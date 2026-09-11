#!/usr/bin/env bash

# deploy_to_itch.sh - Deploy Unity builds to itch.io using Butler
set -e

BUTLER_BIN="$HOME/.local/bin/butler"
if ! command -v butler &> /dev/null && [ -f "$BUTLER_BIN" ]; then
    BUTLER="$BUTLER_BIN"
elif command -v butler &> /dev/null; then
    BUTLER="butler"
else
    echo "❌ Butler CLI not found! Please install butler first."
    exit 1
fi

TARGET="$1"
if [ -z "$TARGET" ]; then
    echo "Usage: ./deploy_to_itch.sh <username>/<game-slug>"
    echo "Example: ./deploy_to_itch.sh reihannudin24/alif"
    echo ""
    read -p "Enter your itch.io project target (username/game-slug): " TARGET
fi

if [ -z "$TARGET" ]; then
    echo "❌ Target cannot be empty."
    exit 1
fi

echo "=========================================="
echo "🚀 Deploying Alif to itch.io: $TARGET"
echo "=========================================="

# Check login status
echo "🔍 Checking Butler login status..."
if ! "$BUTLER" login status &> /dev/null; then
    echo "🔐 You need to log in to itch.io first."
    echo "Running: butler login"
    "$BUTLER" login
fi

DEPLOYED=0

# 1. macOS Build
path=""
if [ -d "Build/macOS/Alif.app" ]; then path="Build/macOS/Alif.app"; elif [ -d "Build/Alif.app" ]; then path="Build/Alif.app"; fi
if [ -n "$path" ]; then
    echo ""
    echo "📦 Pushing macOS build ($path) to channel 'mac'..."
    "$BUTLER" push "$path" "$TARGET:mac"
    DEPLOYED=$((DEPLOYED+1))
fi

# 2. WebGL Build
web_path=""
if [ -d "web_alif" ]; then
    web_path="web_alif"
elif [ -d "Build/WebGL" ]; then
    web_path="Build/WebGL"
fi

if [ -n "$web_path" ]; then
    echo ""
    echo "🌐 Pushing WebGL build ($web_path) to channel 'html5'..."
    "$BUTLER" push "$web_path" "$TARGET:html5"
    DEPLOYED=$((DEPLOYED+1))
fi

# 3. Windows Build
if [ -d "Build/Windows" ]; then
    echo ""
    echo "💻 Pushing Windows build (Build/Windows) to channel 'windows'..."
    "$BUTLER" push Build/Windows "$TARGET:windows"
    DEPLOYED=$((DEPLOYED+1))
fi

if [ "$DEPLOYED" -eq 0 ]; then
    echo "⚠️ No builds found in Build/ directory!"
    echo "Please build your game first in Unity (or run Alif -> Build in the Unity menu)."
else
    echo ""
    echo "=========================================="
    echo "✅ Deployment complete!"
    echo "Check your project page at: https://$TARGET.itch.io/"
    echo "=========================================="
fi
