using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

public interface IAStarHeuristic
{
    /// <summary>
    /// Heuristic to get an estimated cost to get to the target from a given position.
    /// </summary>
    /// <param name="startPosition">Start position.</param>
    /// <param name="targetPosition">Current position to get to.</param>
    /// <returns>An estimated cost.</returns>
    public float EstimateCostToTarget(Vector2 startPosition, Vector2 targetPosition);
}