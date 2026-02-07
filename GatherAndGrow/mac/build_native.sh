#!/bin/bash
# Build script for macOS Steam native shim library
# This creates libsteam_api64.dylib (the shim) that Facepunch.Steamworks loads.
# The shim re-exports symbols from the real libsteam_api.dylib and adds
# compatibility wrappers for the version differences.

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

# Step 1: Find the real libsteam_api.dylib from the Steam client
STEAM_LIB=""
SEARCH_PATHS=(
    "$PROJECT_DIR/libsteam_api.dylib"
    "/Users/$USER/Library/Application Support/Steam/Steam.AppBundle/Steam/Contents/MacOS/Frameworks/Steam Helper.app/Contents/MacOS/libsteam_api.dylib"
    "/Applications/Steam.app/Contents/MacOS/Frameworks/Steam Helper.app/Contents/MacOS/libsteam_api.dylib"
)

for path in "${SEARCH_PATHS[@]}"; do
    if [ -f "$path" ]; then
        STEAM_LIB="$path"
        break
    fi
done

if [ -z "$STEAM_LIB" ]; then
    echo "ERROR: Could not find libsteam_api.dylib"
    echo "Make sure Steam is installed, or copy libsteam_api.dylib to: $PROJECT_DIR/"
    echo ""
    echo "You can find it at:"
    echo "  ~/Library/Application Support/Steam/Steam.AppBundle/Steam/Contents/MacOS/Frameworks/Steam Helper.app/Contents/MacOS/libsteam_api.dylib"
    exit 1
fi

echo "Found Steam library: $STEAM_LIB"

# Step 2: Copy the real library to the project root (needed for build output)
DEST_LIB="$PROJECT_DIR/libsteam_api.dylib"
if [ "$STEAM_LIB" != "$DEST_LIB" ]; then
    echo "Copying to project: $DEST_LIB"
    cp "$STEAM_LIB" "$DEST_LIB"
fi

# Step 3: Remove quarantine attribute (macOS Gatekeeper)
echo "Removing quarantine attributes..."
xattr -d com.apple.quarantine "$DEST_LIB" 2>/dev/null || true

# Step 4: Compile the shim as a universal binary (arm64 + x86_64)
echo "Compiling steam_shim.c -> native/libsteam_api64.dylib ..."
mkdir -p "$PROJECT_DIR/native"
cc -shared \
    -o "$PROJECT_DIR/native/libsteam_api64.dylib" \
    "$SCRIPT_DIR/steam_shim.c" \
    -arch arm64 -arch x86_64 \
    -Wl,-reexport_library,"$DEST_LIB" \
    -L"$PROJECT_DIR" -lsteam_api

# Step 5: Remove quarantine from the shim too
xattr -d com.apple.quarantine "$PROJECT_DIR/native/libsteam_api64.dylib" 2>/dev/null || true

echo ""
echo "SUCCESS! Native libraries ready."
echo "  Real library: $DEST_LIB"
echo "  Shim library: $PROJECT_DIR/native/libsteam_api64.dylib"
echo ""
echo "You can now run: dotnet run"
