using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Represents a connection between two nodes in a graph.
/// </summary>
[Tool]
public partial class GraphConnection: Resource
{
    [Export] public float Cost;
    // In my first approach I used direct references to Start and End nodes. But it
    // happened that it didn't serialize well. Randomly, some GraphConnections serialized 
    // their EndNode fields to null. After some investigation, I found out that
    // serialization algorithms don't like cyclic graphs. My graph implemented cycles:
    // MapGraphResource -> GraphNode A -> GraphConnection -> EndNode (GraphNode B) ->
    // Connections -> ... -> Return to GraphNode A. So I had to break cycles. That's why
    // now I used Vector2I keys to serialize GraphConnections and not direct references
    // instead.
    [Export] public Vector2I StartNodeKey;
    [Export] public Vector2I EndNodeKey;
}