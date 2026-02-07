using System.Numerics;
using Raylib_cs;

namespace GatherAndGrow.Game;

public class GameCamera
{
    public Camera2D Camera;

    public GameCamera()
    {
        Camera = new Camera2D
        {
            Offset = new Vector2(GameConstants.WindowWidth / 2f, GameConstants.WindowHeight / 2f),
            Target = new Vector2(GameConstants.MapWidth / 2f, GameConstants.MapHeight / 2f),
            Rotation = 0f,
            Zoom = 1f
        };
    }

    public void Update(Vector2 targetPosition)
    {
        // Smooth lerp follow
        Camera.Target = Vector2.Lerp(Camera.Target, targetPosition, GameConstants.CameraLerp);

        // Clamp to map bounds so camera doesn't show outside the map
        float halfW = GameConstants.WindowWidth / (2f * Camera.Zoom);
        float halfH = GameConstants.WindowHeight / (2f * Camera.Zoom);

        Camera.Target = new Vector2(
            Math.Clamp(Camera.Target.X, halfW, GameConstants.MapWidth - halfW),
            Math.Clamp(Camera.Target.Y, halfH, GameConstants.MapHeight - halfH)
        );
    }
}
