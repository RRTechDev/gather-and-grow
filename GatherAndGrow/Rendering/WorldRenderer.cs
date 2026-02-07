using System.Numerics;
using Raylib_cs;
using GatherAndGrow.Game;

namespace GatherAndGrow.Rendering;

public class WorldRenderer
{
    private readonly List<Vector2> _grassPatches = new();

    public WorldRenderer()
    {
        // Pre-generate random darker grass patches
        var rng = new Random(42);
        for (int i = 0; i < 200; i++)
        {
            _grassPatches.Add(new Vector2(
                (float)rng.NextDouble() * GameConstants.MapWidth,
                (float)rng.NextDouble() * GameConstants.MapHeight
            ));
        }
    }

    public void Draw(GameState state)
    {
        // Grass background
        Raylib.DrawRectangle(0, 0, GameConstants.MapWidth, GameConstants.MapHeight, new Color(34, 139, 34, 255));

        // Darker grass patches
        var darkGrass = new Color(28, 120, 28, 255);
        foreach (var patch in _grassPatches)
        {
            Raylib.DrawCircleV(patch, 30f, darkGrass);
        }

        // Map border
        Raylib.DrawRectangleLines(0, 0, GameConstants.MapWidth, GameConstants.MapHeight, Color.DarkGray);

        // Resource nodes
        foreach (var node in state.ResourceNodes)
        {
            DrawNode(node);
        }

        // Players
        foreach (var player in state.Players.Values)
        {
            DrawPlayer(player, state);
        }
    }

    private void DrawNode(ResourceNode node)
    {
        byte alpha = node.IsDepleted ? (byte)60 : (byte)255;

        switch (node.Type)
        {
            case ResourceType.Wood:
            {
                var color = new Color((byte)34, (byte)100, (byte)34, alpha);
                var trunkColor = new Color((byte)101, (byte)67, (byte)33, alpha);
                // Tree trunk
                Raylib.DrawCircleV(node.Position, GameConstants.NodeRadius * 0.4f, trunkColor);
                // Tree canopy
                Raylib.DrawCircleV(node.Position, GameConstants.NodeRadius, color);
                break;
            }
            case ResourceType.Iron:
            {
                var color = new Color((byte)160, (byte)160, (byte)170, alpha);
                float size = GameConstants.NodeRadius * 1.4f;
                Raylib.DrawRectangle(
                    (int)(node.Position.X - size / 2),
                    (int)(node.Position.Y - size / 2),
                    (int)size, (int)size, color);
                break;
            }
            case ResourceType.Gold:
            {
                var color = new Color((byte)255, (byte)215, (byte)0, alpha);
                float size = GameConstants.NodeRadius * 1.4f;
                Raylib.DrawRectangle(
                    (int)(node.Position.X - size / 2),
                    (int)(node.Position.Y - size / 2),
                    (int)size, (int)size, color);
                break;
            }
        }

        // Amount text
        if (!node.IsDepleted)
        {
            string text = node.RemainingAmount.ToString();
            int textW = Raylib.MeasureText(text, 14);
            Raylib.DrawText(text, (int)node.Position.X - textW / 2, (int)node.Position.Y - 7, 14, Color.White);
        }
        else
        {
            // Respawn timer
            string text = $"{node.RespawnTimer:F0}s";
            int textW = Raylib.MeasureText(text, 12);
            Raylib.DrawText(text, (int)node.Position.X - textW / 2, (int)node.Position.Y - 6, 12, Color.Gray);
        }
    }

    private void DrawPlayer(Player player, GameState state)
    {
        // Player circle
        Raylib.DrawCircleV(player.Position, GameConstants.PlayerRadius, player.Color);
        Raylib.DrawCircleLines((int)player.Position.X, (int)player.Position.Y, GameConstants.PlayerRadius, Color.White);

        // Name above
        int nameW = Raylib.MeasureText(player.Name, 14);
        Raylib.DrawText(player.Name,
            (int)player.Position.X - nameW / 2,
            (int)(player.Position.Y - GameConstants.PlayerRadius - 20),
            14, Color.White);

        // Gather progress bar
        if (player.GatheringNodeId.HasValue && player.GatherProgress > 0f)
        {
            float barWidth = 40f;
            float barHeight = 6f;
            float barX = player.Position.X - barWidth / 2;
            float barY = player.Position.Y + GameConstants.PlayerRadius + 5;

            Raylib.DrawRectangle((int)barX, (int)barY, (int)barWidth, (int)barHeight, Color.DarkGray);
            Raylib.DrawRectangle((int)barX, (int)barY, (int)(barWidth * player.GatherProgress), (int)barHeight, Color.Yellow);
            Raylib.DrawRectangleLines((int)barX, (int)barY, (int)barWidth, (int)barHeight, Color.White);
        }

        // "Press E" prompt — only show for local player near a resource
        if (player.SteamId == state.LocalSteamId && !player.GatheringNodeId.HasValue)
        {
            var nearestId = state.FindNearestNode(player.Position, GameConstants.GatherRange);
            if (nearestId.HasValue)
            {
                string prompt = "Press E to gather";
                int promptW = Raylib.MeasureText(prompt, 16);
                Raylib.DrawText(prompt,
                    (int)player.Position.X - promptW / 2,
                    (int)(player.Position.Y + GameConstants.PlayerRadius + 15),
                    16, Color.White);
            }
        }
    }
}
