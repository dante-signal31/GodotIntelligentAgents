using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

public interface IPathFinder
{
    public Path FindPath(MapGraph graph, Vector2 targetPosition);
}