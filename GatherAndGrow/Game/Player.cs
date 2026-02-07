using System.Numerics;
using Raylib_cs;

namespace GatherAndGrow.Game;

public enum ToolType
{
    Axe,
    Pickaxe,
    GoldPick
}

public class Player
{
    public ulong SteamId { get; set; }
    public string Name { get; set; } = "";
    public Vector2 Position { get; set; }
    public Raylib_cs.Color Color { get; set; }
    public Dictionary<ResourceType, int> Inventory { get; set; } = new()
    {
        { ResourceType.Wood, 0 },
        { ResourceType.Iron, 0 },
        { ResourceType.Gold, 0 }
    };
    public Dictionary<ToolType, int> ToolLevels { get; set; } = new()
    {
        { ToolType.Axe, 1 },
        { ToolType.Pickaxe, 1 },
        { ToolType.GoldPick, 1 }
    };
    public int? GatheringNodeId { get; set; }
    public float GatherProgress { get; set; }

    public static readonly Raylib_cs.Color[] PlayerColors = new[]
    {
        Raylib_cs.Color.Blue,
        Raylib_cs.Color.Red,
        new Raylib_cs.Color(0, 200, 0, 255),   // Green
        new Raylib_cs.Color(160, 32, 240, 255)  // Purple
    };
}
