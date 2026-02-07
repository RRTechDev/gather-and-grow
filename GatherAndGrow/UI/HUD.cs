using Raylib_cs;
using GatherAndGrow.Game;

namespace GatherAndGrow.UI;

public class HUD
{
    public void Draw(GameState state)
    {
        if (!state.Players.TryGetValue(state.LocalSteamId, out var localPlayer))
            return;

        DrawInventory(localPlayer);
        DrawPlayerList(state);
        DrawToolLevels(localPlayer);
    }

    private void DrawInventory(Player player)
    {
        int x = 10;
        int y = 10;

        Raylib.DrawRectangle(x, y, 180, 100, new Color(0, 0, 0, 150));
        Raylib.DrawRectangleLines(x, y, 180, 100, Color.White);
        Raylib.DrawText("Inventory", x + 10, y + 5, 16, Color.White);

        // Wood
        Raylib.DrawCircle(x + 25, y + 35, 8, new Color(34, 139, 34, 255));
        Raylib.DrawText($"Wood: {player.Inventory[ResourceType.Wood]}", x + 40, y + 28, 16, Color.White);

        // Iron
        Raylib.DrawRectangleRec(new Rectangle(x + 17, y + 49, 16, 16), new Color(160, 160, 170, 255));
        Raylib.DrawText($"Iron: {player.Inventory[ResourceType.Iron]}", x + 40, y + 50, 16, Color.White);

        // Gold
        Raylib.DrawRectangleRec(new Rectangle(x + 17, y + 71, 16, 16), new Color(255, 215, 0, 255));
        Raylib.DrawText($"Gold: {player.Inventory[ResourceType.Gold]}", x + 40, y + 72, 16, Color.White);
    }

    private void DrawPlayerList(GameState state)
    {
        int x = GameConstants.WindowWidth - 200;
        int y = 10;
        int playerCount = state.Players.Count;
        int height = 25 + playerCount * 22;

        Raylib.DrawRectangle(x, y, 190, height, new Color(0, 0, 0, 150));
        Raylib.DrawRectangleLines(x, y, 190, height, Color.White);
        Raylib.DrawText("Players", x + 10, y + 5, 16, Color.White);

        int row = 0;
        foreach (var player in state.Players.Values)
        {
            int rowY = y + 25 + row * 22;
            Raylib.DrawCircle(x + 18, rowY + 8, 6, player.Color);
            Raylib.DrawText(player.Name, x + 30, rowY, 16, Color.White);
            row++;
        }
    }

    private void DrawToolLevels(Player player)
    {
        int totalWidth = 360;
        int x = (GameConstants.WindowWidth - totalWidth) / 2;
        int y = GameConstants.WindowHeight - 40;

        Raylib.DrawRectangle(x, y, totalWidth, 35, new Color(0, 0, 0, 150));
        Raylib.DrawRectangleLines(x, y, totalWidth, 35, Color.White);

        string[] toolNames = { "Axe", "Pickaxe", "Gold Pick" };
        ToolType[] tools = { ToolType.Axe, ToolType.Pickaxe, ToolType.GoldPick };

        for (int i = 0; i < 3; i++)
        {
            int tx = x + 10 + i * 120;
            int level = player.ToolLevels[tools[i]];
            Raylib.DrawText($"{toolNames[i]} Lv{level}", tx, y + 8, 16, Color.White);
        }

        // TAB hint
        Raylib.DrawText("[TAB] Upgrades", x + totalWidth + 10, y + 8, 14, Color.LightGray);
    }
}
