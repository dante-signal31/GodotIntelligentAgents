using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Represents an influence point within the map graph regions, defining a specific
/// position, influence magnitude, and a visual representation color for debugging
/// purposes.
/// </summary>
[Tool]
[GlobalClass]
public partial class RegionSeed : Resource
{
    [Export] public Vector2 Position = Vector2.Zero;
    [Export] public float Influence = 1.0f;
    [Export] public Color GizmoColor = Colors.White;
}