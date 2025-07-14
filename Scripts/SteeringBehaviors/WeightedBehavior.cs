using Godot;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

[GlobalClass]
public partial class WeightedBehavior: Resource
{
    [Export] public NodePath SteeringBehavior { get; set; }
    [Export] public float Weight { get; set; }
    [Export] public Color DebugColor { get; set; }
}