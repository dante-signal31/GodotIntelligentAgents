using System;
using Godot;

namespace GodotGameAIbyExample.Scripts.Tools;

public static class RandomPointGenerator
{
    /// <summary>
    /// <p>Get a random point in a circle of given radius.<p>
    ///
    /// <p>Circle is in local space, so it's centered at (0, 0).</p>
    /// </summary>
    /// <param name="radius">Radius for the circle.</param>
    /// <returns>A point inside the circle (in local space).</returns>
    public static Vector2 GetRandomPointInCircle(float radius)
    {
        Random random = new Random();
        float angle = (float)(random.NextDouble() * 2 * Math.PI);
        float distance = (float)(Math.Sqrt(random.NextDouble()) * radius);
        float x = distance * Mathf.Cos(angle);
        float y = distance * Mathf.Sin(angle);
        return new Vector2(x, y);
    }
}