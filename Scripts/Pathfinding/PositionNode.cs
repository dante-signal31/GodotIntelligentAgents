using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Graph node implementation with four edges.
/// </summary>
[Tool]
public partial class PositionNode: GraphNode
{
    [Export] public Vector2 Position;

    public bool HasConnection(Orientation orientation)
    {
        return Connections.ContainsKey((uint)orientation);
    }

    public GraphConnection GetConnection(Orientation orientation)
    {
        return Connections[(uint)orientation];
    }
    
    public void AddConnection(
        uint endNodeId, 
        float cost, 
        Orientation orientation)
    {
        base.AddConnection(endNodeId, cost, (uint)orientation);
    }
}