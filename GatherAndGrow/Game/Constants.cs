namespace GatherAndGrow.Game;

public static class GameConstants
{
    // Window
    public const int WindowWidth = 1280;
    public const int WindowHeight = 720;

    // Map
    public const int MapWidth = 3000;
    public const int MapHeight = 3000;

    // Player
    public const float PlayerSpeed = 200f;
    public const float PlayerRadius = 16f;

    // Gathering
    public const float GatherRange = 50f;
    public const float GatherTimeWood = 3.0f;
    public const float GatherTimeIron = 1.5f;
    public const float GatherTimeGold = 0.75f;

    // Resources
    public const int WoodNodeCount = 30;
    public const int IronNodeCount = 25;
    public const int GoldNodeCount = 15;
    public const int WoodNodeAmount = 8;
    public const int IronNodeAmount = 6;
    public const int GoldNodeAmount = 4;
    public const float RespawnTime = 30f;
    public const float NodeRadius = 20f;

    // Network
    public const float NetworkTickRate = 20f;
    public const float NetworkTickInterval = 1f / NetworkTickRate;

    // Camera
    public const float CameraLerp = 0.1f;

    // Minimap
    public const int MinimapSize = 200;
    public const float MinimapScale = 0.1f;
    public const int MinimapPadding = 10;
}
