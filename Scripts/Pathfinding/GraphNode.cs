using System;
using Godot;
using Godot.Collections;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Graph node implementation with four edges.
/// </summary>
[Tool]
public partial class GraphNode: Resource
{
    [Export] public Vector2 Position;
    [Export] public Dictionary<Orientation, GraphConnection> Connections = new();
    
    public void AddConnection(
        Vector2I thisNodeKey,
        Vector2I endNodeKey, 
        float cost, 
        Orientation orientation)
    {
        GraphConnection graphConnection = new();
        graphConnection.StartNodeKey = thisNodeKey;
        graphConnection.EndNodeKey = endNodeKey;
        graphConnection.Cost = cost;
        Connections[orientation] = graphConnection;
    }
}