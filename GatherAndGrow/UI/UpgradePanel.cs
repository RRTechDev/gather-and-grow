using Raylib_cs;
using GatherAndGrow.Game;

namespace GatherAndGrow.UI;

public class UpgradePanel
{
    public bool IsOpen { get; set; }
    private float _flashTimer;
    private int _flashToolIndex = -1;

    // Upgrade costs: [toolIndex][levelIndex] -> (Wood, Iron, Gold)
    // levelIndex 0 = upgrade to level 2, levelIndex 1 = upgrade to level 3
    private static readonly (int Wood, int Iron, int Gold)[][] UpgradeCosts = new[]
    {
        // Axe
        new (int, int, int)[]
        {
            (10, 5, 0),   // Level 1 -> 2
            (20, 10, 5)   // Level 2 -> 3
        },
        // Pickaxe
        new (int, int, int)[]
        {
            (10, 5, 0),   // Level 1 -> 2
            (15, 15, 5)   // Level 2 -> 3
        },
        // Gold Pick
        new (int, int, int)[]
        {
            (10, 10, 0),  // Level 1 -> 2
            (20, 15, 10)  // Level 2 -> 3
        }
    };

    public static (int Wood, int Iron, int Gold)? GetUpgradeCost(ToolType tool, int currentLevel)
    {
        if (currentLevel >= 3) return null;
        int toolIndex = (int)tool;
        int levelIndex = currentLevel - 1;
        return UpgradeCosts[toolIndex][levelIndex];
    }

    public static bool CanAfford(Player player, ToolType tool)
    {
        var cost = GetUpgradeCost(tool, player.ToolLevels[tool]);
        if (cost == null) return false;
        var c = cost.Value;
        return player.Inventory[ResourceType.Wood] >= c.Wood &&
               player.Inventory[ResourceType.Iron] >= c.Iron &&
               player.Inventory[ResourceType.Gold] >= c.Gold;
    }

    public void TriggerFlash(int toolIndex)
    {
        _flashToolIndex = toolIndex;
        _flashTimer = 0.5f;
    }

    public void Update(float dt)
    {
        if (_flashTimer > 0)
            _flashTimer -= dt;
    }

    /// <summary>
    /// Returns the ToolType if user requested an upgrade via click or key, else null.
    /// </summary>
    public ToolType? Draw(GameState state)
    {
        if (!IsOpen) return null;
        if (!state.Players.TryGetValue(state.LocalSteamId, out var localPlayer))
            return null;

        ToolType? upgradeRequest = null;

        // Overlay background
        Raylib.DrawRectangle(0, 0, GameConstants.WindowWidth, GameConstants.WindowHeight, new Color(0, 0, 0, 150));

        int panelW = 500;
        int panelH = 300;
        int panelX = (GameConstants.WindowWidth - panelW) / 2;
        int panelY = (GameConstants.WindowHeight - panelH) / 2;

        Raylib.DrawRectangle(panelX, panelY, panelW, panelH, new Color(30, 30, 30, 240));
        Raylib.DrawRectangleLines(panelX, panelY, panelW, panelH, Color.White);
        Raylib.DrawText("Tool Upgrades", panelX + 20, panelY + 15, 24, Color.White);
        Raylib.DrawText("Press 1/2/3 or click to upgrade. TAB to close.", panelX + 20, panelY + 45, 14, Color.LightGray);

        string[] toolNames = { "Axe (Wood)", "Pickaxe (Iron)", "Gold Pick (Gold)" };
        ToolType[] tools = { ToolType.Axe, ToolType.Pickaxe, ToolType.GoldPick };

        for (int i = 0; i < 3; i++)
        {
            int rowY = panelY + 80 + i * 70;
            int level = localPlayer.ToolLevels[tools[i]];
            bool isFlashing = _flashToolIndex == i && _flashTimer > 0;

            // Tool name and level
            var nameColor = isFlashing ? Color.Yellow : Color.White;
            Raylib.DrawText($"{toolNames[i]}", panelX + 20, rowY, 20, nameColor);
            Raylib.DrawText($"Level {level}/3", panelX + 20, rowY + 22, 16, Color.LightGray);

            if (level < 3)
            {
                var cost = GetUpgradeCost(tools[i], level)!.Value;
                bool affordable = CanAfford(localPlayer, tools[i]);
                var costColor = affordable ? Color.Green : Color.Red;

                string costText = $"{cost.Wood}W";
                if (cost.Iron > 0) costText += $", {cost.Iron}I";
                if (cost.Gold > 0) costText += $", {cost.Gold}G";

                Raylib.DrawText($"Cost: {costText}", panelX + 220, rowY, 16, costColor);

                // Upgrade button
                var btnRect = new Rectangle(panelX + 380, rowY, 100, 35);
                var btnColor = affordable ? new Color(0, 150, 0, 255) : new Color(100, 100, 100, 255);
                Raylib.DrawRectangleRec(btnRect, btnColor);
                Raylib.DrawRectangleLinesEx(btnRect, 1, Color.White);

                string btnText = $"[{i + 1}] Upgrade";
                int textW = Raylib.MeasureText(btnText, 14);
                Raylib.DrawText(btnText, (int)(btnRect.X + (btnRect.Width - textW) / 2), (int)(btnRect.Y + 10), 14, Color.White);

                // Click detection
                if (affordable && Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    var mousePos = Raylib.GetMousePosition();
                    if (Raylib.CheckCollisionPointRec(mousePos, btnRect))
                    {
                        upgradeRequest = tools[i];
                    }
                }

                // Keyboard shortcut
                if (affordable)
                {
                    var key = i switch
                    {
                        0 => KeyboardKey.One,
                        1 => KeyboardKey.Two,
                        2 => KeyboardKey.Three,
                        _ => KeyboardKey.Zero
                    };
                    if (Raylib.IsKeyPressed(key))
                    {
                        upgradeRequest = tools[i];
                    }
                }
            }
            else
            {
                Raylib.DrawText("MAX LEVEL", panelX + 220, rowY, 20, Color.Gold);
            }
        }

        return upgradeRequest;
    }
}
