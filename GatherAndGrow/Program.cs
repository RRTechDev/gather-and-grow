using System.Numerics;
using Raylib_cs;
using GatherAndGrow.Game;
using GatherAndGrow.Network;
using GatherAndGrow.Rendering;
using GatherAndGrow.UI;

// --- Create systems ---
var gameState = new GameState();
var networkManager = new NetworkManager(gameState);
var camera = new GameCamera();
var worldRenderer = new WorldRenderer();
var minimapRenderer = new MinimapRenderer();
var hud = new HUD();
var upgradePanel = new UpgradePanel();
var mainMenu = new MainMenu();

// --- Init Steam ---
bool steamOk = networkManager.Init();

// --- Init Raylib ---
Raylib.InitWindow(GameConstants.WindowWidth, GameConstants.WindowHeight, "Gather & Grow");
Raylib.SetTargetFPS(60);

try
{
    while (!Raylib.WindowShouldClose())
    {
        float dt = Raylib.GetFrameTime();
        if (dt <= 0) dt = 0.0001f; // Guard first frame

        // Steam callbacks
        networkManager.RunCallbacks();
        networkManager.Update(dt);

        // --- Update ---
        switch (gameState.Phase)
        {
            case GamePhase.MainMenu:
            case GamePhase.InLobby:
                // Menu input handled in draw
                mainMenu.PlayerCount = networkManager.GetPlayerCount();
                break;

            case GamePhase.Playing:
                UpdatePlaying(dt);
                break;

            case GamePhase.Victory:
                gameState.VictoryTimer += dt;
                break;
        }

        upgradePanel.Update(dt);

        // --- Draw ---
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);

        switch (gameState.Phase)
        {
            case GamePhase.MainMenu:
            case GamePhase.InLobby:
                DrawMenu();
                break;

            case GamePhase.Playing:
                DrawPlaying();
                break;

            case GamePhase.Victory:
                DrawPlaying(); // Draw the world behind the victory banner
                DrawVictory();
                break;
        }

        Raylib.EndDrawing();
    }
}
finally
{
    networkManager.Shutdown();
    Raylib.CloseWindow();
}

// ============================================================
// Update logic
// ============================================================

void UpdatePlaying(float dt)
{
    if (!gameState.Players.TryGetValue(gameState.LocalSteamId, out var localPlayer))
        return;

    // Don't process movement input when upgrade panel is open
    if (upgradePanel.IsOpen) return;

    // WASD movement
    var moveDir = Vector2.Zero;
    if (Raylib.IsKeyDown(KeyboardKey.W)) moveDir.Y -= 1;
    if (Raylib.IsKeyDown(KeyboardKey.S)) moveDir.Y += 1;
    if (Raylib.IsKeyDown(KeyboardKey.A)) moveDir.X -= 1;
    if (Raylib.IsKeyDown(KeyboardKey.D)) moveDir.X += 1;

    if (moveDir != Vector2.Zero)
    {
        moveDir = Vector2.Normalize(moveDir);
        var newPos = localPlayer.Position + moveDir * GameConstants.PlayerSpeed * dt;

        // Clamp to map bounds
        newPos = new Vector2(
            Math.Clamp(newPos.X, GameConstants.PlayerRadius, GameConstants.MapWidth - GameConstants.PlayerRadius),
            Math.Clamp(newPos.Y, GameConstants.PlayerRadius, GameConstants.MapHeight - GameConstants.PlayerRadius)
        );

        localPlayer.Position = newPos;

        // Cancel gathering on move
        if (localPlayer.GatheringNodeId.HasValue)
        {
            localPlayer.GatheringNodeId = null;
            localPlayer.GatherProgress = 0;
        }

        // Send position to host/other players
        networkManager.SendPlayerMove(newPos);
    }

    // E to gather
    if (Raylib.IsKeyPressed(KeyboardKey.E) && !localPlayer.GatheringNodeId.HasValue)
    {
        var nearestId = gameState.FindNearestNode(localPlayer.Position, GameConstants.GatherRange);
        if (nearestId.HasValue)
        {
            networkManager.SendGatherRequest(nearestId.Value);
        }
    }

    // TAB to toggle upgrade panel
    if (Raylib.IsKeyPressed(KeyboardKey.Tab))
    {
        upgradePanel.IsOpen = !upgradePanel.IsOpen;
    }

    // Update camera
    camera.Update(localPlayer.Position);
}

// ============================================================
// Draw logic
// ============================================================

void DrawMenu()
{
    var action = mainMenu.Draw(steamOk);

    switch (action)
    {
        case MainMenu.MenuAction.Host:
            networkManager.HostGame();
            mainMenu.CurrentScreen = MainMenu.MenuScreen.HostWaiting;
            mainMenu.HostSteamId = gameState.LocalSteamId.ToString();
            mainMenu.StatusMessage = "";
            break;

        case MainMenu.MenuAction.Back:
            mainMenu.CurrentScreen = MainMenu.MenuScreen.Main;
            mainMenu.StatusMessage = "";
            break;

        case MainMenu.MenuAction.Start:
            networkManager.StartGame();
            break;

        case MainMenu.MenuAction.DirectConnect:
            if (ulong.TryParse(mainMenu.JoinInput, out var hostId))
            {
                networkManager.DirectConnect(hostId);
                mainMenu.StatusMessage = "Connecting...";
            }
            else
            {
                mainMenu.StatusMessage = "Invalid Steam ID";
            }
            break;

        case MainMenu.MenuAction.RetrySteam:
            steamOk = networkManager.Init();
            if (!steamOk)
                mainMenu.StatusMessage = "Still unable to connect to Steam.";
            else
                mainMenu.StatusMessage = "";
            break;
    }
}

void DrawPlaying()
{
    // World space
    Raylib.BeginMode2D(camera.Camera);
    worldRenderer.Draw(gameState);
    Raylib.EndMode2D();

    // Screen space
    hud.Draw(gameState);
    minimapRenderer.Draw(gameState, camera.Camera);

    // Upgrade panel (screen space overlay)
    var upgradeRequest = upgradePanel.Draw(gameState);
    if (upgradeRequest.HasValue)
    {
        networkManager.SendToolUpgradeRequest(upgradeRequest.Value);
        int toolIdx = (int)upgradeRequest.Value;
        upgradePanel.TriggerFlash(toolIdx);
    }

    // TAB toggle (also check in draw phase since upgrade panel might consume the key)
    // Already handled in UpdatePlaying
}

void DrawVictory()
{
    // Semi-transparent overlay
    Raylib.DrawRectangle(0, 0, GameConstants.WindowWidth, GameConstants.WindowHeight, new Color(0, 0, 0, 180));

    string winnerName = "Unknown";
    if (gameState.WinnerId.HasValue && gameState.Players.TryGetValue(gameState.WinnerId.Value, out var winner))
    {
        winnerName = winner.Name;
    }

    string text = $"{winnerName} wins!";
    int textW = Raylib.MeasureText(text, 48);
    Raylib.DrawText(text, (GameConstants.WindowWidth - textW) / 2, 250, 48, Color.Gold);

    string sub = "All tools upgraded to Level 3!";
    int subW = Raylib.MeasureText(sub, 24);
    Raylib.DrawText(sub, (GameConstants.WindowWidth - subW) / 2, 320, 24, Color.White);

    if (gameState.VictoryTimer > 3f)
    {
        string hint = "Press ESC to exit";
        int hintW = Raylib.MeasureText(hint, 20);
        Raylib.DrawText(hint, (GameConstants.WindowWidth - hintW) / 2, 380, 20, Color.LightGray);
    }
}
