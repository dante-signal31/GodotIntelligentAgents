using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Graph node implementation with four edges.
/// </summary>
[Tool]
public partial class PositionNode: GraphNode
{
    [Export] public Vector2 Position;
}