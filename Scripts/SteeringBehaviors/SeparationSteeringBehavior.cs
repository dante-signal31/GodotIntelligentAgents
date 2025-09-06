using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using GodotGameAIbyExample.addons.InteractiveRanges.CircularRange;
using GodotGameAIbyExample.Scripts.Extensions;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

// It must be marked as Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
/// <summary>
/// <p>Node to offer a separation steering behaviour.</p>
/// <p>Separation steering behaviour makes the agent go away from other nodes marked
/// as threat. It's similar to flee, but the threat is not a single node, but a group and
/// the repulsion force is inversely proportional to the distance.</p>
/// </summary>
public partial class SeparationSteeringBehavior : Node2D, ISteeringBehavior
{
    public enum SeparationAlgorithms
    {
        Linear,
        InverseSquare
    }

    [ExportCategory("CONFIGURATION")]
    /// <summary>
    /// List of agents to separate from.
    /// </summary>
    [Export] public Array<MovingAgent> Threats { get; set; } = new();
    
    private float _separationThreshold  = 100f;
    /// <summary>
    /// Below this threshold distance, separation will be applied.
    /// </summary>
    [Export] public float SeparationThreshold
    {
        get => _separationThreshold;
        set
        {   
            _separationThreshold = value;
            if (_circularRange != null) _circularRange.Radius = value;
        }
    }

    /// <summary>
    /// Chosen algorithm to calculate separation acceleration.
    /// </summary>
    [Export] public SeparationAlgorithms SeparationAlgorithm { get; set; } = SeparationAlgorithms.Linear;
    
    /// <summary>
    /// Coefficient for inverse square law separation algorithm.
    /// </summary>
    [Export] public float DecayCoefficient { get; set; } = 0.1f;
    
    [ExportCategory("DEBUG:")]
    /// <summary>
    /// Make visible velocity marker.
    /// </summary>
    [Export] private bool VelocityMarkerVisible { get; set; }
    [Export] private Color MarkerColor { get; set; }
    
    private Color AgentColor => _currentAgent.AgentColor;

    private MovingAgent _currentAgent;
    private CircularRange _circularRange;
    
    public override void _EnterTree()
    {
        // Find out who is our father.
        _currentAgent = this.FindAncestor<MovingAgent>();
    }

    public override void _Ready()
    {
        _circularRange = this.FindChild<CircularRange>();
        _circularRange.Radius = SeparationThreshold;
    }

    private float GetLinearSeparationStrength(
        float maximumAcceleration,
        float currentDistance, 
        float threshold)
    {
        return maximumAcceleration * (threshold - currentDistance) / threshold;    
    }

    private float GetInverseSquareLawSeparationStrength(
        float maximumAcceleration,
        float currentDistance,
        float k)
    {
        float normalizedDistance = currentDistance / SeparationThreshold;
        return Mathf.Min(k / Mathf.Pow(normalizedDistance, 2f), maximumAcceleration);    
    }
    
    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        if (Threats == null || Threats.Count == 0) 
            return new SteeringOutput(Vector2.Zero, 0);

        Vector2 newVelocity = args.CurrentVelocity;
        Vector2 currentPosition = args.Position;

        // Traverse every target and sum up their respective repulsion forces.
        foreach (MovingAgent target in Threats)
        {
            Vector2 toTarget = target.GlobalPosition - currentPosition;
            float distanceToTarget = toTarget.Length();

            // If the agent is close enough to the target, apply a repulsion force.
            if (distanceToTarget < SeparationThreshold)
            {
                float strengthAcceleration = SeparationAlgorithm switch
                {
                    SeparationAlgorithms.Linear =>
                        GetLinearSeparationStrength(
                            args.MaximumAcceleration,
                            distanceToTarget,
                            SeparationThreshold),
                    SeparationAlgorithms.InverseSquare =>
                        GetInverseSquareLawSeparationStrength(
                            args.MaximumAcceleration,
                            distanceToTarget,
                            DecayCoefficient),
                    _ => throw new ArgumentOutOfRangeException(),
                };
                Vector2 toTargetDirection = toTarget.Normalized();
                Vector2 repulsionDirection = -toTargetDirection;
                // Add current repulsion to previously added from other targets.
                newVelocity += repulsionDirection * strengthAcceleration * (float) args.DeltaTime;
            }
        }
        return new SteeringOutput(newVelocity, 0);
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        _circularRange = this.FindChild<CircularRange>();

        List<string> warnings = new();
        
        if (_circularRange == null)
        {
            warnings.Add("This node needs a child of type CircularRange to work.");
        }
        return warnings.ToArray();
    }
    
    public override void _Process(double delta)
    {
        if (VelocityMarkerVisible) DrawGizmos();
    }

    private void DrawGizmos()
    {
        QueueRedraw();
    }
    
    public override void _Draw()
    {
        if (!VelocityMarkerVisible ||
            Engine.IsEditorHint()) return;
        
        DrawLine(
            Vector2.Zero, 
            ToLocal(GlobalPosition + _currentAgent.Velocity), 
            MarkerColor);
    }
}