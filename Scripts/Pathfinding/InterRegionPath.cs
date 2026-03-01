using Godot;
using Godot.Collections;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Path positions across a region to link two other regions.
/// </summary>
public partial class InterRegionPath : Resource
{
    [Export] public Array<Vector2> PathPositions;
    [Export] public float Cost;
}
