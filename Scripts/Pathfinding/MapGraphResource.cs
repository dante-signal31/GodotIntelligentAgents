using Godot;
using Godot.Collections;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

[Tool]
[GlobalClass]
public partial class MapGraphResource: Resource
{ 
    [Export] public Dictionary<Vector2I, GraphNode> Nodes = new();
}