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
    
    public void AddEdge(
        GraphNode endNode, 
        float cost, 
        Orientation orientation, 
        bool bidirectional = true)
    {
        GraphConnection graphConnection = new();
        graphConnection.StartNode = this;
        graphConnection.EndNode = endNode;
        graphConnection.Cost = cost;
        Connections[orientation] = graphConnection;

        if (!bidirectional) return;

        // In my example the paths between nodes are bidirectional, so we need to add
        // an edge in both directions. To do this, we update the other node's edge to
        // point to us.
        switch (orientation)
        {
            // bidirectional argument must be false in this call to avoid infinite
            // recursion.
            case Orientation.North: 
                graphConnection.EndNode.AddEdge(this, cost, Orientation.South, false); 
                break;
            case Orientation.East:
                graphConnection.EndNode.AddEdge(this, cost, Orientation.West, false); 
                break;
            case Orientation.South:
                graphConnection.EndNode.AddEdge(this, cost, Orientation.North, false);
                break;
            case Orientation.West:
                graphConnection.EndNode.AddEdge(this, cost, Orientation.East, false);
                break;
        }
    }
}