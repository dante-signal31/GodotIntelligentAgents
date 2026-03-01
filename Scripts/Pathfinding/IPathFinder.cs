using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Defines the contract for a pathfinding interface. Classes implementing this interface
/// are responsible for navigating a graph structure to find a path to a target position.
/// </summary>
public interface IPathFinder
{
    public IPositionGraph Graph { get; set; }
    
    /// <summary>
    /// Finds a path in the defined graph to the specified target position.
    /// </summary>
    /// <param name="targetPosition">
    /// The target position in the graph to which a path should be found.
    /// </param>
    /// <param name="fromPosition">Initial point to calculate a path from. If left to
    /// default, then the agent's current position will be used.</param>
    /// <returns>
    /// A <c>Path</c> object representing the sequence of nodes leading to the target
    /// position.
    /// </returns>
    public Path FindPath(Vector2 targetPosition, Vector2 fromPosition=default);
}