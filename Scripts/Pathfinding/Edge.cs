using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

[Tool]
public partial class Edge: Resource
{
    [Export] public float Cost;
    [Export] public GraphNode EndNode;
}