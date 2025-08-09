using Godot;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

// Tool attribute is needed to be able to debug editor. Otherwise, while debugging 
// the editor, cast from Resource to WeightedBehavior will fail.
/// <summary>
/// Represents a random steering behavior resource that specifies a
/// particular steering behavior, its associated probability, and a color for debugging
/// purposes. This class is utilized in configuring prioritized dithering blended steering
/// behaviors in game AI.
/// </summary>
[GlobalClass, Tool]
public partial class RandomBehavior: Resource
{
    [Export] public NodePath SteeringBehavior { get; set; }
    [Export(PropertyHint.Range, "0, 1, 0.01")] 
    public float Probability { get; set; }
    [Export] public Color DebugColor { get; set; }
}