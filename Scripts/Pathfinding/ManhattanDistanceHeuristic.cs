using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

[Tool]
public partial class ManhattanDistanceHeuristic: Node, IAStarHeuristic
{
    public float EstimateCostToTarget(Vector2 startPosition, Vector2 targetPosition)
    {
        float distanceX = Mathf.Abs(targetPosition.X - startPosition.X);
        float distanceY = Mathf.Abs(targetPosition.Y - startPosition.Y);
        float manhattanDistance = distanceX + distanceY;
        return manhattanDistance;
    }
}