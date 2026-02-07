using System.Numerics;
using Raylib_cs;
using GatherAndGrow.Game;
using Color = Raylib_cs.Color;

namespace GatherAndGrow.UI;

public class MainMenu
{
    public enum MenuScreen
    {
        Main,
        HostWaiting,
        JoinInput
    }

    public MenuScreen CurrentScreen { get; set; } = MenuScreen.Main;
    public string StatusMessage { get; set; } = "";
    public int PlayerCount { get; set; } = 1;
    public string HostSteamId { get; set; } = "";

    // Text input for join screen
    public string JoinInput { get; set; } = "";

    public enum MenuAction
    {
        None,
        Host,
        Back,
        Start,
        DirectConnect
    }

    public MenuAction Draw(bool steamOk)
    {
        Raylib.DrawRectangle(0, 0, GameConstants.WindowWidth, GameConstants.WindowHeight, new Color(20, 40, 20, 255));

        string title = "Gather & Grow";
        int titleW = Raylib.MeasureText(title, 48);
        Raylib.DrawText(title, (GameConstants.WindowWidth - titleW) / 2, 80, 48, Color.White);

        string subtitle = "A Multiplayer Resource Gathering Game";
        int subW = Raylib.MeasureText(subtitle, 20);
        Raylib.DrawText(subtitle, (GameConstants.WindowWidth - subW) / 2, 140, 20, Color.LightGray);

        string steamStatus = steamOk ? "Steam: Connected" : "Steam: Not Connected";
        var steamColor = steamOk ? Color.Green : Color.Red;
        Raylib.DrawText(steamStatus, 10, GameConstants.WindowHeight - 30, 16, steamColor);

        if (!string.IsNullOrEmpty(StatusMessage))
        {
            int msgW = Raylib.MeasureText(StatusMessage, 16);
            Raylib.DrawText(StatusMessage, (GameConstants.WindowWidth - msgW) / 2, GameConstants.WindowHeight - 60, 16, Color.Yellow);
        }

        switch (CurrentScreen)
        {
            case MenuScreen.Main:
                return DrawMainScreen(steamOk);
            case MenuScreen.HostWaiting:
                return DrawHostScreen();
            case MenuScreen.JoinInput:
                return DrawJoinScreen();
        }

        return MenuAction.None;
    }

    private MenuAction DrawMainScreen(bool steamOk)
    {
        int btnW = 200;
        int btnH = 50;
        int centerX = (GameConstants.WindowWidth - btnW) / 2;

        if (steamOk)
        {
            if (DrawButton("Host Game", centerX, 250, btnW, btnH))
                return MenuAction.Host;

            if (DrawButton("Join Game", centerX, 320, btnW, btnH))
            {
                CurrentScreen = MenuScreen.JoinInput;
                JoinInput = "";
                StatusMessage = "";
                return MenuAction.None;
            }
        }
        else
        {
            Raylib.DrawText("Steam must be running to play.", centerX - 40, 280, 20, Color.Red);
        }

        return MenuAction.None;
    }

    private MenuAction DrawHostScreen()
    {
        int centerX = GameConstants.WindowWidth / 2;

        string waitText = $"Waiting for players... ({PlayerCount}/4)";
        int waitW = Raylib.MeasureText(waitText, 24);
        Raylib.DrawText(waitText, centerX - waitW / 2, 220, 24, Color.White);

        // Show SteamID for joining
        if (!string.IsNullOrEmpty(HostSteamId))
        {
            Raylib.DrawText("Your Steam ID (share with others to join):", centerX - 200, 270, 16, Color.LightGray);

            // Draw the ID in a box for easy copying
            int idW = Raylib.MeasureText(HostSteamId, 24);
            int boxX = centerX - idW / 2 - 15;
            int boxW = idW + 30;
            Raylib.DrawRectangle(boxX, 295, boxW, 40, new Color(30, 60, 30, 255));
            Raylib.DrawRectangleLines(boxX, 295, boxW, 40, Color.Green);
            Raylib.DrawText(HostSteamId, centerX - idW / 2, 303, 24, Color.Green);
        }

        int btnW = 200;
        int btnH = 50;

        if (DrawButton("Start Game", centerX - btnW / 2, 370, btnW, btnH))
            return MenuAction.Start;

        if (DrawButton("Back", centerX - btnW / 2, 440, btnW, btnH))
            return MenuAction.Back;

        return MenuAction.None;
    }

    private MenuAction DrawJoinScreen()
    {
        int centerX = GameConstants.WindowWidth / 2;

        Raylib.DrawText("Enter Host's Steam ID:", centerX - 120, 220, 20, Color.White);
        Raylib.DrawText("(The host can see their ID on the waiting screen)", centerX - 200, 250, 16, Color.LightGray);

        // Text input box
        int boxX = centerX - 200;
        int boxW = 400;
        int boxY = 285;
        int boxH = 40;
        Raylib.DrawRectangle(boxX, boxY, boxW, boxH, new Color(30, 60, 30, 255));
        Raylib.DrawRectangleLines(boxX, boxY, boxW, boxH, Color.White);

        // Handle keyboard input
        HandleTextInput();

        // Draw the input text
        string displayText = JoinInput.Length > 0 ? JoinInput : "Type Steam ID here...";
        var textColor = JoinInput.Length > 0 ? Color.White : Color.DarkGray;
        Raylib.DrawText(displayText, boxX + 10, boxY + 10, 20, textColor);

        // Blinking cursor
        if ((int)(Raylib.GetTime() * 2) % 2 == 0)
        {
            int cursorX = boxX + 10 + Raylib.MeasureText(JoinInput, 20);
            Raylib.DrawText("|", cursorX, boxY + 10, 20, Color.White);
        }

        int btnW = 150;
        int btnH = 40;
        int btnY = boxY + boxH + 30;

        if (DrawButton("Connect", centerX - btnW - 10, btnY, btnW, btnH))
        {
            if (JoinInput.Length > 0)
                return MenuAction.DirectConnect;
        }

        if (DrawButton("Back", centerX + 10, btnY, btnW, btnH))
        {
            CurrentScreen = MenuScreen.Main;
            StatusMessage = "";
            return MenuAction.None;
        }

        // Also allow Enter key to connect
        if (Raylib.IsKeyPressed(KeyboardKey.Enter) && JoinInput.Length > 0)
            return MenuAction.DirectConnect;

        return MenuAction.None;
    }

    private void HandleTextInput()
    {
        // Handle character input (digits only for Steam ID)
        int key = Raylib.GetCharPressed();
        while (key > 0)
        {
            if (key >= '0' && key <= '9' && JoinInput.Length < 20)
            {
                JoinInput += (char)key;
            }
            key = Raylib.GetCharPressed();
        }

        // Backspace
        if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && JoinInput.Length > 0)
        {
            JoinInput = JoinInput[..^1];
        }

        // Handle paste (Cmd+V on macOS)
        if ((Raylib.IsKeyDown(KeyboardKey.LeftSuper) || Raylib.IsKeyDown(KeyboardKey.RightSuper)) && Raylib.IsKeyPressed(KeyboardKey.V))
        {
            unsafe
            {
                var clipboard = Raylib.GetClipboardText_();
                if (clipboard != null)
                {
                    string pasted = new string(clipboard);
                    // Only keep digits
                    foreach (char c in pasted)
                    {
                        if (c >= '0' && c <= '9' && JoinInput.Length < 20)
                            JoinInput += c;
                    }
                }
            }
        }
    }

    private bool DrawButton(string text, int x, int y, int w, int h)
    {
        var rect = new Rectangle(x, y, w, h);
        bool hovered = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), rect);

        var bgColor = hovered ? new Color(80, 120, 80, 255) : new Color(50, 90, 50, 255);
        Raylib.DrawRectangleRec(rect, bgColor);
        Raylib.DrawRectangleLinesEx(rect, 2, Color.White);

        int textW = Raylib.MeasureText(text, 20);
        Raylib.DrawText(text, x + (w - textW) / 2, y + (h - 20) / 2, 20, Color.White);

        return hovered && Raylib.IsMouseButtonPressed(MouseButton.Left);
    }
}
