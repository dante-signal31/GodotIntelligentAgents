using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Defines the contract for a pathfinding interface. Classes implementing this interface
/// are responsible for navigating a graph structure to find a path to a target position.
/// </summary>
public interface IPathFinder
{
    public MapGraph Graph { get; set; }
    
    public Path FindPath(Vector2 targetPosition);
}