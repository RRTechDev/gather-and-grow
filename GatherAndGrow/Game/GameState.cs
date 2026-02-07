using System.Numerics;

namespace GatherAndGrow.Game;

public enum GamePhase
{
    MainMenu,
    InLobby,
    Playing,
    Victory
}

public class GameState
{
    public List<ResourceNode> ResourceNodes { get; set; } = new();
    public Dictionary<ulong, Player> Players { get; set; } = new();
    public bool IsHost { get; set; }
    public ulong LocalSteamId { get; set; }
    public ulong? WinnerId { get; set; }
    public float VictoryTimer { get; set; }
    public GamePhase Phase { get; set; } = GamePhase.MainMenu;

    private readonly Random _rng = new();

    public void GenerateWorld()
    {
        ResourceNodes.Clear();
        int id = 0;

        // Wood nodes
        for (int i = 0; i < GameConstants.WoodNodeCount; i++)
        {
            ResourceNodes.Add(new ResourceNode
            {
                Id = id++,
                Type = ResourceType.Wood,
                Position = RandomPosition(),
                RemainingAmount = GameConstants.WoodNodeAmount,
                MaxAmount = GameConstants.WoodNodeAmount
            });
        }

        // Iron nodes
        for (int i = 0; i < GameConstants.IronNodeCount; i++)
        {
            ResourceNodes.Add(new ResourceNode
            {
                Id = id++,
                Type = ResourceType.Iron,
                Position = RandomPosition(),
                RemainingAmount = GameConstants.IronNodeAmount,
                MaxAmount = GameConstants.IronNodeAmount
            });
        }

        // Gold nodes
        for (int i = 0; i < GameConstants.GoldNodeCount; i++)
        {
            ResourceNodes.Add(new ResourceNode
            {
                Id = id++,
                Type = ResourceType.Gold,
                Position = RandomPosition(),
                RemainingAmount = GameConstants.GoldNodeAmount,
                MaxAmount = GameConstants.GoldNodeAmount
            });
        }
    }

    private Vector2 RandomPosition()
    {
        float margin = 100f;
        return new Vector2(
            margin + (float)_rng.NextDouble() * (GameConstants.MapWidth - margin * 2),
            margin + (float)_rng.NextDouble() * (GameConstants.MapHeight - margin * 2)
        );
    }

    public int? FindNearestNode(Vector2 position, float maxRange)
    {
        int? nearestId = null;
        float nearestDist = float.MaxValue;

        for (int i = 0; i < ResourceNodes.Count; i++)
        {
            var node = ResourceNodes[i];
            if (node.IsDepleted) continue;

            float dist = Vector2.Distance(position, node.Position);
            if (dist <= maxRange && dist < nearestDist)
            {
                nearestDist = dist;
                nearestId = i;
            }
        }

        return nearestId;
    }
}
