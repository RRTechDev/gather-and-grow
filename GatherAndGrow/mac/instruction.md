# macOS Setup Instructions for Gather & Grow

## The Problem

Facepunch.Steamworks 2.3.3 (the NuGet package) only ships `steam_api64.dll` for Windows. On macOS, you need to provide the native Steam library yourself. Additionally, the Steam client's bundled library uses a newer SDK version than what Facepunch expects, so a **shim library** is needed to bridge the version gap.

## What's in this folder

| File | Purpose |
|------|---------|
| `steam_shim.c` | C source for the compatibility shim. Maps old Facepunch API names to the newer Steam SDK. |
| `build_native.sh` | Build script that finds the real Steam library, compiles the shim, and places both in the right locations. |

## Prerequisites

- **macOS** (tested on Apple Silicon M1, also works on Intel)
- **Steam** installed and running
- **.NET 9 SDK** (`dotnet --version` should print 9.x)
- **Xcode Command Line Tools** (`xcode-select --install` if needed)

## Quick Setup

```bash
# From the GatherAndGrow directory:
./mac/build_native.sh
dotnet run
```

That's it! The build script handles everything automatically.

## Manual Setup (if the script doesn't work)

### Step 1: Find the real Steam library

The Steam client includes `libsteam_api.dylib` at:
```
~/Library/Application Support/Steam/Steam.AppBundle/Steam/Contents/MacOS/Frameworks/Steam Helper.app/Contents/MacOS/libsteam_api.dylib
```

Copy it to the project root:
```bash
cp "~/Library/Application Support/Steam/Steam.AppBundle/Steam/Contents/MacOS/Frameworks/Steam Helper.app/Contents/MacOS/libsteam_api.dylib" ./libsteam_api.dylib
```

### Step 2: Remove quarantine

macOS Gatekeeper may block unsigned libraries:
```bash
xattr -d com.apple.quarantine libsteam_api.dylib
```

### Step 3: Compile the shim

```bash
mkdir -p native
cc -shared \
    -o native/libsteam_api64.dylib \
    mac/steam_shim.c \
    -arch arm64 -arch x86_64 \
    -Wl,-reexport_library,libsteam_api.dylib \
    -L. -lsteam_api
```

### Step 4: Run

```bash
dotnet run
```

## How it works

Facepunch.Steamworks P/Invokes into a library named `steam_api64`. On macOS, this resolves to `libsteam_api64.dylib`.

The shim (`libsteam_api64.dylib`) does three things:

1. **Re-exports all symbols** from the real `libsteam_api.dylib` using the `-reexport_library` linker flag. This gives Facepunch access to all ~900 flat API functions.

2. **Provides `SteamAPI_Init()`** — The newer Steam SDK replaced `SteamAPI_Init()` (returns bool) with `SteamAPI_InitFlat()` (returns int + error message). The shim wraps InitFlat to look like the old Init.

3. **Maps versioned interface accessors** — Facepunch expects older interface versions (e.g. `SteamAPI_SteamUser_v020`), but the newer SDK exports newer versions (e.g. `SteamAPI_SteamUser_v023`). The shim creates thin wrapper functions that redirect old names to new ones.

4. **Stubs removed flat API methods** — ~80 flat API functions were removed from the newer SDK (e.g. `SteamAPI_ISteamUserStats_RequestCurrentStats`). The shim provides stub implementations that return safe default values.

## Troubleshooting

**"Steam: Not Connected"** — Make sure Steam is running before launching the game.

**"Library not loaded: libsteam_api.dylib"** — Run `./mac/build_native.sh` to copy the library.

**"code signature invalid"** — Run:
```bash
xattr -d com.apple.quarantine libsteam_api.dylib
xattr -d com.apple.quarantine native/libsteam_api64.dylib
```

**Build errors in steam_shim.c** — Make sure Xcode Command Line Tools are installed:
```bash
xcode-select --install
```

## Architecture

```
GatherAndGrow/
├── libsteam_api.dylib          # Real Steam library (copied from Steam client)
├── native/
│   ├── libsteam_api64.dylib    # Compiled shim (what Facepunch loads)
│   └── steam_shim.c            # Shim source (same as mac/steam_shim.c)
├── mac/
│   ├── instruction.md          # This file
│   ├── steam_shim.c            # Shim source (reference copy)
│   └── build_native.sh         # Build script
└── GatherAndGrow.csproj        # References both .dylib files for CopyToOutputDirectory
```

The `.csproj` file includes entries to copy both dylibs to the build output:
```xml
<None Update="libsteam_api.dylib">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</None>
<None Include="native/libsteam_api64.dylib">
    <Link>libsteam_api64.dylib</Link>
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</None>
```
