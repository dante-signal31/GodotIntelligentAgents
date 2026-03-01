using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Interface provided by every position graph implementation to be used by the
/// pathfinding algorithm.
/// </summary>
public interface IPositionGraph
{
    IPositionNode GetNodeById(uint nodeId);
    
    IPositionNode GetNodeAtPosition(Vector2 position);
}