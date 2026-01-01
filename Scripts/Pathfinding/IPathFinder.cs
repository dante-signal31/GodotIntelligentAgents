using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Defines the contract for a pathfinding interface. Classes implementing this interface
/// are responsible for navigating a graph structure to find a path to a target position.
/// </summary>
public interface IPathFinder
{
    /// <summary>
    /// Represents a graph structure used for pathfinding and navigation. It is used to
    /// define the spatial configuration and connections between nodes for AI navigation.
    /// </summary>
    public MapGraph Graph { get; set; }

    /// <summary>
    /// Finds a path in the defined graph to the specified target position.
    /// </summary>
    /// <param name="targetPosition">
    /// The target position in the graph to which a path should be found.
    /// </param>
    /// <returns>
    /// A <c>Path</c> object representing the sequence of nodes leading to the target
    /// position.
    /// </returns>
    public Path FindPath(Vector2 targetPosition);
}