using Godot;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

// Tool attribute is needed to be able to debug editor. Otherwise, while debugging 
// the editor, cast from Resource to WeightedBehavior will fail.
[GlobalClass, Tool]
public partial class WeightedBehavior: Resource
{
    [Export] public NodePath SteeringBehavior { get; set; }
    [Export] public float Weight { get; set; }
    [Export] public Color DebugColor { get; set; }
}