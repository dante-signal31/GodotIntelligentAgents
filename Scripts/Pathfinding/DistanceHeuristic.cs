using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

[Tool]
public partial class DistanceHeuristic: Node, IAStarHeuristic
{
    public float EstimateCostToTarget(Vector2 startPosition, Vector2 targetPosition)
    {
        return startPosition.DistanceTo(targetPosition);
    }
}