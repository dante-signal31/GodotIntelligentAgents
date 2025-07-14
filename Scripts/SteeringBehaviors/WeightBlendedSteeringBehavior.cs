using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using GodotGameAIbyExample.Scripts.Extensions;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

// It must be marked as Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
/// <summary>
/// This steering behavior takes a set of other steering behaviors and blend their outputs
/// using weights.
/// </summary>
public partial class WeightBlendedSteeringBehavior : Node2D, ISteeringBehavior, IGizmos
{
    private bool _showGizmos;
    private Color _gizmosColor;
    
    private struct RealWeightedBehavior
    {
        public ISteeringBehavior SteeringBehavior;
        public float Weight;
        public Color DebugColor;
    }

    private struct WeightedOutput
    {
        public readonly SteeringOutput SteeringOutput;
        public readonly float Weight;
        public readonly Color DebugColor;
    
        public WeightedOutput(SteeringOutput steeringOutput, float weight, Color color)
        {
            SteeringOutput = steeringOutput;
            Weight = weight;
            DebugColor = color;
        }
    }


    [ExportCategory("CONFIGURATION:")]

    /// <summary>
    /// A collection of weighted steering behaviors to be used by the
    /// WeightBlendedSteeringBehavior class for combined steering calculations.
    /// </summary>
    /// <remarks>
    /// Each entry in the collection represents a specific steering behavior along with
    /// its corresponding weight, allowing for fine-grained control over the influence of
    /// individual steering behaviors. The weighted combination of these behaviors
    /// determines the final steering output.
    /// </remarks>
    /// <value>
    /// An array of <see cref="WeightedBehavior"/> objects, where each object contains a
    /// reference to a steering behavior and its associated weight.
    /// </value>
    [Export] public Array<WeightedBehavior> WeightedBehaviors
    {
        get; 
        private set;
    } = new();
    
    [ExportCategory("DEBUG:")]
    /// <summary>
    /// Show gizmos.
    /// </summary>
    [Export] public bool ShowGizmos
    {
        get => _showGizmos;
        set => _showGizmos = value;
    }

    /// <summary>
    /// Colors for this object's gizmos.
    /// </summary>
    [Export] public Color GizmosColor
    {
        get => _gizmosColor;
        set => _gizmosColor = value;
    }
    
    private readonly List<RealWeightedBehavior> _nodeWeightedBehaviors = new();
    private readonly List<WeightedOutput> _activeOutputs = new();
    
    private float _totalWeight;
    
    private SteeringOutput _currentSteering;
    
    private MovingAgent _currentAgent;

    public override void _EnterTree()
    {
        // Find out who our father is.
        _currentAgent = this.FindAncestor<MovingAgent>();
    }

    public override void _Ready()
    {
        // Resolve all node paths into real nodes and calculate the total weight.
        foreach (var weightedBehavior in WeightedBehaviors)
        {
            ISteeringBehavior currentSteeringBehavior = 
                GetNode<ISteeringBehavior>(weightedBehavior.SteeringBehavior);
            RealWeightedBehavior currentWeightedBehavior = new RealWeightedBehavior
            {
                SteeringBehavior = currentSteeringBehavior,
                Weight = weightedBehavior.Weight,
                DebugColor = weightedBehavior.DebugColor
            };
            _nodeWeightedBehaviors.Add(currentWeightedBehavior);
            _totalWeight += weightedBehavior.Weight;
        }
    }

    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        // We need two passes: one to get the active outputs
        // and one to blend them taking their relative weights for the active outputs.
        _activeOutputs.Clear();
        
        // First pass: get only active outputs.
        foreach (var weightedBehavior in _nodeWeightedBehaviors)
        {
            SteeringOutput output = weightedBehavior.SteeringBehavior.GetSteering(args);
            if (output.Equals(SteeringOutput.Zero)) continue;
            _activeOutputs.Add(
                new WeightedOutput(
                    output, 
                    weightedBehavior.Weight,
                    weightedBehavior.DebugColor));
        }
    
        // Second pass: blend them.
        _currentSteering = new SteeringOutput();
        foreach (WeightedOutput weightedOutput in _activeOutputs)
        {
            float outputRelativeWeight = weightedOutput.Weight / _totalWeight;
            _currentSteering += weightedOutput.SteeringOutput * outputRelativeWeight;
        }
    
        return _currentSteering;
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
        if (!ShowGizmos) return;
        
        // Draw first partial steerings.
        foreach (WeightedOutput weightedOutput in _activeOutputs)
        {
            float outputRelativeWeight = weightedOutput.Weight / _totalWeight;
            DrawLine(
                Vector2.Zero, 
                ToLocal(GlobalPosition + weightedOutput.SteeringOutput.Linear * outputRelativeWeight), 
                weightedOutput.DebugColor, width: 3f);
        }
        
        // Next draw total steering.
        DrawLine(
            Vector2.Zero, 
            ToLocal(GlobalPosition + _currentSteering.Linear), 
            GizmosColor);
    }
}