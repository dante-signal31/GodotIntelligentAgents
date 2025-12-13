using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

[Tool]
public partial class GraphConnection: Resource
{
    [Export] public float Cost;
    [Export] public GraphNode StartNode;
    [Export] public GraphNode EndNode;
}