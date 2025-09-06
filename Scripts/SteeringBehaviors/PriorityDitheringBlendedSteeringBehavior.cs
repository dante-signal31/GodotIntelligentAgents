using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

// It must be marked as Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
/// <summary>
/// This steering behavior takes a set of other steering behaviors and blend their outputs
/// using probabilities.
/// </summary>
public partial class PriorityDitheringBlendedSteeringBehavior: 
    Node2D, 
    ISteeringBehavior, 
    IGizmos
{
    private struct RealRandomBehavior
    {
        public ISteeringBehavior SteeringBehavior;
        public float Probability;
        public Color DebugColor;
    }

    /// <summary>
    /// A collection of randomized steering behaviors configured for this object.
    /// </summary>
    [ExportCategory("CONFIGURATION:")]
    
    /// <summary>
    /// A collection of random steering behaviors to be used by the
    /// PriorityDitheringBlendedSteeringBehavior class for combined steering calculations.
    /// </summary>
    /// <remarks>
    /// Each entry in the collection represents a specific steering behavior along with
    /// its corresponding priority, allowing for fine-grained control over the influence of
    /// individual steering behaviors. The combination of these behaviors
    /// determines the final steering output.
    /// </remarks>
    /// <value>
    /// An array of <see cref="RandomBehavior"/> objects, where each object contains a
    /// reference to a steering behavior and its associated priority.
    /// </value>
    [Export] public Array<RandomBehavior> RandomBehaviors
    {
        get; 
        private set;
    } = new();
    

    [ExportCategory("DEBUG:")]
    /// <summary>
    /// Show gizmos.
    /// </summary>
    [Export] public bool ShowGizmos { get; set; }

    /// <summary>
    /// Colors for this object's gizmos.
    ///</summary>
    [Export] public Color GizmosColor { get; set; }
    
    private readonly List<RealRandomBehavior> _nodeRandomBehaviors = new();
    private SteeringOutput _currentSteering;
    
    public override void _Ready()
    {
        if (Engine.IsEditorHint()) return;
        
        // Resolve all node paths into real nodes.
        foreach (var randomBehavior in RandomBehaviors)
        {
            ISteeringBehavior currentSteeringBehavior = 
                GetNode<ISteeringBehavior>(randomBehavior.SteeringBehavior);
            RealRandomBehavior currentWeightedBehavior = new RealRandomBehavior
            {
                SteeringBehavior = currentSteeringBehavior,
                Probability = randomBehavior.Probability,
                DebugColor = randomBehavior.DebugColor
            };
            _nodeRandomBehaviors.Add(currentWeightedBehavior);
        }
    }
    
    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        foreach (var randomBehavior in _nodeRandomBehaviors)
        {
            if (GD.Randf() > randomBehavior.Probability) continue;
            SteeringOutput output = randomBehavior.SteeringBehavior.GetSteering(args);
            if (output.Equals(SteeringOutput.Zero)) continue;
            _currentSteering = output;
            return output;
        }
        return SteeringOutput.Zero;
    }
    
    public override void _Process(double delta)
    {
        if (ShowGizmos) DrawGizmos();
    }

    private void DrawGizmos()
    {
        QueueRedraw();
    }
    
    public override void _Draw()
    {
        if (!ShowGizmos || _currentSteering == null) return;
        
        // Draw current steering.
        DrawLine(
            Vector2.Zero, 
            ToLocal(GlobalPosition + _currentSteering.Linear), 
            GizmosColor);
    }
}