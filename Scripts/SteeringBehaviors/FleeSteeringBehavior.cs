using Godot;
using System;
using System.Collections.Generic;
using GodotGameAIbyExample.addons.InteractiveRanges.CircularRange;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;

// It must be marked as Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
/// <summary>
/// <p>Flee steering behaviour makes the agent go away from another GameObject marked
/// as threath.</p>
/// </summary>
public partial class FleeSteeringBehavior : Node2D, ISteeringBehavior
{
    private const float MinimumPanicDistance = 0.3f;
    
    [ExportCategory("CONFIGURATION:")]
    private Node2D _threath;
    /// <summary>
    /// Object to flee from.
    /// </summary>
    [Export] public Node2D Threath
    {
        get => _threath;
        set
        {
            _threath = value;
            if (_seekSteeringBehavior != null) _seekSteeringBehavior.Target = Threath;
        }
    }

    private float _panicDistance;

    /// <summary>
    /// Minimum distance to threath before fleeing.
    /// </summary>
    [Export]
    public float PanicDistance
    {
        get => _panicDistance;
        set
        {
            float newValue = Mathf.Max(MinimumPanicDistance, value);
            if (!Mathf.IsEqualApprox(newValue, _panicDistance))
            {
                _panicDistance = newValue;
                if (_circularRange != null) _circularRange.Radius = _panicDistance;
            }
        }
    }

    private SeekSteeringBehavior _seekSteeringBehavior;
    private CircularRange _circularRange;

    public override void _Ready()
    {
        _seekSteeringBehavior = this.FindChild<SeekSteeringBehavior>();
        _seekSteeringBehavior.Target = Threath;
        _circularRange = this.FindChild<CircularRange>();
        _circularRange.Radius = PanicDistance;
    }

    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        if (args.CurrentAgent.GlobalPosition.DistanceTo(Threath.GlobalPosition) > PanicDistance)
        { // Out of panic distance, so we stop accelerating.
            return new SteeringOutput(Vector2.Zero, 0);
        }
        else
        { // Threath inside panic distance, so run in the opposite direction seek would advise.
            SteeringOutput approachSteeringOutput = _seekSteeringBehavior.GetSteering(args);
            SteeringOutput fleeSteeringOutput = new SteeringOutput(
                -approachSteeringOutput.Linear, 
                approachSteeringOutput.Angular);
            return fleeSteeringOutput;
        }
    }

    public override string[] _GetConfigurationWarnings()
    {
        _seekSteeringBehavior = this.FindChild<SeekSteeringBehavior>();
        _circularRange = this.FindChild<CircularRange>();

        List<string> warnings = new();
        
        if (_seekSteeringBehavior == null)
        {
            warnings.Add("This node needs a child of type SeekSteeringBehavior to work.");
        }
        
        if (_circularRange == null)
        {
            warnings.Add("This node needs a child of type CircularRange to work.");
        }
        return warnings.ToArray();
    }
}
