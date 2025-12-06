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
    [Export] public Dictionary<Orientation, Edge> Edges = new();
    
    public void AddEdge(
        GraphNode endNode, 
        float cost, 
        Orientation orientation, 
        bool bidirectional = true)
    {
        Edge edge = new();
        edge.EndNode = endNode;
        edge.Cost = cost;
        Edges[orientation] = edge;

        if (!bidirectional) return;

        // In my example the paths between nodes are bidirectional, so we need to add
        // an edge in both directions. To do this, we update the other node's edge to
        // point to us.
        switch (orientation)
        {
            // bidirectional argument must be false in this call to avoid infinite
            // recursion.
            case Orientation.North: 
                edge.EndNode.AddEdge(this, cost, Orientation.South, false); 
                break;
            case Orientation.East:
                edge.EndNode.AddEdge(this, cost, Orientation.West, false); 
                break;
            case Orientation.South:
                edge.EndNode.AddEdge(this, cost, Orientation.North, false);
                break;
            case Orientation.West:
                edge.EndNode.AddEdge(this, cost, Orientation.East, false);
                break;
        }
    }
}