using System.Numerics;
using Raylib_cs;
using GatherAndGrow.Game;

namespace GatherAndGrow.Rendering;

public class MinimapRenderer
{
    public void Draw(GameState state, Camera2D camera)
    {
        int mmSize = GameConstants.MinimapSize;
        int mmX = GameConstants.WindowWidth - mmSize - GameConstants.MinimapPadding;
        int mmY = GameConstants.WindowHeight - mmSize - GameConstants.MinimapPadding;
        float scale = (float)mmSize / GameConstants.MapWidth;

        // Background
        Raylib.DrawRectangle(mmX, mmY, mmSize, mmSize, new Color(0, 0, 0, 180));
        Raylib.DrawRectangleLines(mmX, mmY, mmSize, mmSize, Color.White);

        // Resource dots
        foreach (var node in state.ResourceNodes)
        {
            if (node.IsDepleted) continue;

            var color = node.Type switch
            {
                ResourceType.Wood => new Color(34, 139, 34, 255),
                ResourceType.Iron => new Color(160, 160, 170, 255),
                ResourceType.Gold => new Color(255, 215, 0, 255),
                _ => Color.White
            };

            int dotX = mmX + (int)(node.Position.X * scale);
            int dotY = mmY + (int)(node.Position.Y * scale);
            Raylib.DrawCircle(dotX, dotY, 2f, color);
        }

        // Player dots
        foreach (var player in state.Players.Values)
        {
            int dotX = mmX + (int)(player.Position.X * scale);
            int dotY = mmY + (int)(player.Position.Y * scale);
            Raylib.DrawCircle(dotX, dotY, 3f, player.Color);

            // Highlight local player
            if (player.SteamId == state.LocalSteamId)
            {
                Raylib.DrawCircleLines(dotX, dotY, 4f, Color.White);
            }
        }

        // Viewport rectangle
        float vpX = (camera.Target.X - GameConstants.WindowWidth / (2f * camera.Zoom)) * scale;
        float vpY = (camera.Target.Y - GameConstants.WindowHeight / (2f * camera.Zoom)) * scale;
        float vpW = (GameConstants.WindowWidth / camera.Zoom) * scale;
        float vpH = (GameConstants.WindowHeight / camera.Zoom) * scale;

        Raylib.DrawRectangleLines(
            mmX + (int)vpX, mmY + (int)vpY,
            (int)vpW, (int)vpH, Color.White);
    }
}
