using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

[Tool]
public partial class SquaredDistanceHeuristic: Node, IAStarHeuristic
{
    public float EstimateCostToTarget(Vector2 startPosition, Vector2 targetPosition)
    {
        return startPosition.DistanceSquaredTo(targetPosition);
    }
}