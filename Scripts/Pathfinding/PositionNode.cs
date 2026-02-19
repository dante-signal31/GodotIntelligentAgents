using System.Collections.Generic;
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
        return HasConnection(orientation) ? Connections[(uint)orientation]: null;
    }

    public Dictionary<Orientation, GraphConnection> GetConnections()
    {
        Dictionary<Orientation, GraphConnection> currentConnections = new();
        foreach (KeyValuePair<uint, GraphConnection> connection in Connections)
        {
            currentConnections[(Orientation)connection.Key] = connection.Value;
        }
        return currentConnections;
    }
    
    public void AddConnection(
        uint endNodeId, 
        float cost, 
        Orientation orientation)
    {
        base.AddConnection(endNodeId, cost, (uint)orientation);
    }
}