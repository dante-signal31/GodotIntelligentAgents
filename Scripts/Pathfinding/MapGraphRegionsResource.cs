using Godot;
using Godot.Collections;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

[Tool]
[GlobalClass]
public partial class MapGraphRegionsResource: Resource
{
    [Export] public Dictionary<Vector2I, PositionNode> Nodes = new();
}