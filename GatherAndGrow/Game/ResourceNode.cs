using System.Numerics;

namespace GatherAndGrow.Game;

public enum ResourceType
{
    Wood,
    Iron,
    Gold
}

public class ResourceNode
{
    public int Id { get; set; }
    public ResourceType Type { get; set; }
    public Vector2 Position { get; set; }
    public int RemainingAmount { get; set; }
    public int MaxAmount { get; set; }
    public float RespawnTimer { get; set; }

    public bool IsDepleted => RemainingAmount <= 0;
}
