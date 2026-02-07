# Prompt: Build a Multiplayer Resource Gathering Game

Build a 2D top-down multiplayer resource gathering game using **C# (.NET 8)**, **Raylib-cs**, and **Facepunch.Steamworks** (Steam P2P networking). The game should support 2–4 players on a LAN using the `steam_appid.txt` set to `480` (Spacewar) for local testing. Target platform is macOS but keep it cross-platform compatible.

---

## Project Setup

- Create a new .NET 8 console project
- Add NuGet packages: `Raylib-cs` and `Facepunch.Steamworks`
- Create a `steam_appid.txt` file in the project root containing `480`
- Copy `steam_appid.txt` to output directory via `.csproj`:
  ```xml
  <ItemGroup>
    <None Update="steam_appid.txt" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>
  ```
- Window size: 1280x720, target 60 FPS
- Game title: "Gather & Grow"

---

## Core Game Architecture

Use a simple **host/client model** over Steam P2P:

- **Lobby system**: Host creates a Steam lobby, clients browse and join. Use `SteamMatchmaking` for lobby creation/discovery.
- **Networking**: Use `SteamNetworking.P2P` (legacy P2P) or `SteamNetworkingSockets` for sending/receiving game state. Keep it simple — legacy P2P (`SendP2PPacket` / `ReadP2PPacket`) is fine.
- **Authority**: The host is authoritative. Clients send input/actions to host, host validates and broadcasts world state to all clients.
- **Tick rate**: 20 updates/sec for network sync is fine for this type of game.

### Networking Message Types (use a simple enum + binary serialization):
- `PlayerJoined` — new player connected
- `PlayerLeft` — player disconnected
- `PlayerMove` — position update
- `GatherRequest` — client wants to gather a resource
- `GatherResult` — host confirms resource gathered, updates inventory
- `WorldState` — periodic full state sync (all resource nodes + player positions + inventories)
- `ToolUpgraded` — player upgraded a tool

---

## Game State

### World / Map
- Fixed map size: 2000x2000 pixels with camera following local player
- Randomly generate resource nodes at game start (host generates, syncs to clients):
  - **Trees** (wood icon — draw as green circles): 30–40 nodes
  - **Iron Deposits** (gray squares): 15–20 nodes
  - **Gold Deposits** (yellow squares): 8–12 nodes
- Each resource node has:
  - `Position` (Vector2)
  - `ResourceType` (Wood, Iron, Gold)
  - `RemainingAmount` (int) — Trees: 5, Iron: 8, Gold: 12
  - `RespawnTimer` — after depleted, respawn in 30 seconds
- Draw a simple grass-green background. Add some visual variety with random darker green patches.

### Player
- Represented as a colored circle (each player gets a unique color: Blue, Red, Green, Purple)
- Movement: WASD, speed = 200 pixels/sec
- Properties:
  - `SteamId` (ulong)
  - `Position` (Vector2)
  - `Inventory` — dictionary of ResourceType → int count
  - `Tools` — dictionary of ResourceType → ToolLevel (1–3)
  - `GatheringTarget` — nullable reference to a resource node being gathered
  - `GatherProgress` — float 0 to 1

### Gathering Mechanic
- Walk near a resource node (within 50px) and press **E** to start gathering
- Gathering takes time based on tool level:
  - Level 1 (default): 3.0 seconds per unit
  - Level 2: 1.5 seconds per unit
  - Level 3: 0.75 seconds per unit
- Show a progress bar above the player while gathering
- Player must stand still while gathering — moving cancels it
- Each gather tick extracts 1 unit from the node

### Tool Upgrades
- Press **TAB** to open/close an upgrade panel (simple overlay)
- Upgrades cost resources:

| Tool | Level 2 Cost | Level 3 Cost |
|------|-------------|-------------|
| Axe (Wood) | 10 Wood, 5 Iron | 20 Wood, 10 Iron, 5 Gold |
| Pickaxe (Iron) | 10 Wood, 5 Iron | 15 Wood, 15 Iron, 5 Gold |
| Gold Pick (Gold) | 10 Wood, 10 Iron | 20 Wood, 15 Iron, 10 Gold |

- Click an upgrade button (or press 1/2/3 corresponding to tool) to upgrade if you have enough resources
- Play a simple visual flash effect on upgrade

---

## UI / HUD

All drawn with Raylib primitives and `DrawText` — no external assets needed.

- **Top-left**: Player inventory display (icon + count for each resource)
- **Top-right**: Connected players list with names/colors
- **Bottom-center**: Current tool levels (Axe Lv.1, Pickaxe Lv.1, etc.)
- **Center**: Gathering progress bar (when active)
- **TAB overlay**: Upgrade panel with costs and buttons
- **Main Menu**: Simple screen with "Host Game" and "Join Game" buttons
  - Join Game: show list of available lobbies, click to join
  - Show Steam connection status

---

## Camera

- 2D camera (`Camera2D` in Raylib) follows the local player
- Smooth lerp follow (lerp factor ~0.1)
- Show minimap in bottom-right corner (200x200 px) showing all resource nodes as colored dots and players as their colored dots

---

## Game Flow

1. **Main Menu** → Player chooses Host or Join
2. **Host**: Creates Steam lobby (max 4 players), generates world, waits for players
3. **Join**: Browses lobbies, joins one, receives world state from host
4. **Gameplay**: All players gather, upgrade, compete for limited resources
5. **Win Condition** (optional/simple): First player to reach Level 3 on ALL tools wins. Show a victory banner and return to menu after 5 seconds.

---

## File Structure

```
GatherAndGrow/
├── GatherAndGrow.csproj
├── steam_appid.txt
├── Program.cs              — entry point, game loop
├── Game/
│   ├── GameState.cs        — world state, resource nodes, players
│   ├── Player.cs           — player data class
│   ├── ResourceNode.cs     — resource node class
│   ├── Camera.cs           — camera follow logic
│   └── Constants.cs        — all magic numbers in one place
├── Network/
│   ├── NetworkManager.cs   — Steam init, lobby, P2P send/receive
│   ├── MessageType.cs      — enum of message types
│   └── PacketSerializer.cs — simple binary serialize/deserialize
├── UI/
│   ├── HUD.cs              — in-game HUD drawing
│   ├── MainMenu.cs         — menu screen
│   └── UpgradePanel.cs     — TAB overlay
└── Rendering/
    ├── WorldRenderer.cs    — draw map, resources, players
    └── MinimapRenderer.cs  — draw minimap
```

---

## Important Implementation Notes

1. **Steam Initialization**: Call `SteamClient.Init(480)` on startup. Call `SteamClient.RunCallbacks()` every frame. Call `SteamClient.Shutdown()` on exit.
2. **Lobby Discovery**: Use `SteamMatchmaking.LobbyList.RequestAsync()` to find lobbies. Filter by game name metadata.
3. **Set lobby metadata** so you can identify your game's lobbies: `lobby.SetData("game", "GatherAndGrow")`
4. **P2P Networking**: Use `SteamNetworking.SendP2PPacket()` with `P2PSend.Reliable` for important messages (gather results, upgrades) and `P2PSend.UnreliableNoDelay` for position updates.
5. **Keep it simple**: Use `BinaryWriter`/`BinaryReader` with `MemoryStream` for packet serialization. First byte is always the `MessageType` enum.
6. **Resource contention**: If two players try to gather the last unit of a resource simultaneously, the host decides who gets it (first request wins).
7. **All drawing uses Raylib primitives only** — `DrawCircle`, `DrawRectangle`, `DrawText`, `DrawRectangleLines`, etc. No textures or asset files needed.
8. **Make sure `SteamClient.RunCallbacks()` and `SteamNetworking.ReadP2PPacket()` are called in the main loop.**

---

## Build & Run Instructions

After building, to test multiplayer locally:
1. Make sure Steam is running and logged in
2. Run the first instance → choose "Host Game"
3. Run the second instance from the same build output directory → choose "Join Game"
4. Both should connect via Steam P2P using app ID 480

Generate the complete, working project. Make sure it compiles and runs. Keep the code clean with comments explaining the networking flow.
