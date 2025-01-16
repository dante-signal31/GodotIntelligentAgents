using Godot;

namespace GodotGameAIbyExample.Scripts.Extensions;

public static class RandomExtensions
{
    /// <summary>
    /// Get a normalized vector in a random direction.
    /// </summary>
    /// <returns>Normalized vector.</returns>
    public static Vector2 GetRandomNormalizedVector()
    {
        float x = (float)GD.RandRange(-1f, 1f);
        float y = (float)GD.RandRange(-1f, 1f);
        return new Vector2(x, y).Normalized();
    }

    /// <summary>
    /// <p>Get a random position in the border of a circle.</p>
    /// <p>Circle is in local space, so it's centered at (0, 0).</p>
    /// </summary>
    /// <param name="radius">Circle radius</param>
    /// <returns>Random point at the border of the circle.</returns>
    public static Vector2 GetRandomPointInCircumference(
        float radius = 1.0f)
    {
        return GetRandomNormalizedVector() * radius;
    }

    /// <summary>
    /// <p>Get a random position inside a circle.</p>
    /// <p>Circle is in local space, so it's centered at (0, 0).</p>
    /// </summary>
    /// <param name="radius">Circle radius</param>
    /// <returns>Random point inside the circle.</returns>
    public static Vector2 GetRandomPointInsideCircle(float radius = 1.0f)
    {
        return GetRandomNormalizedVector() * (float) GD.RandRange(0f, radius);
    }
}