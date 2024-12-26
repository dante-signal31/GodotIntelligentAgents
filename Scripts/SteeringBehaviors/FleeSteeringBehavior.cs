using Godot;
using System;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;


[Tool]
// It must be marked as Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
public partial class FleeSteeringBehavior : Node, ISteeringBehavior
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
    [Export] public float PanicDistance {
        get => _panicDistance;
        set => _panicDistance = Mathf.Max(MinimumPanicDistance, value);
    }
    
    private SeekSteeringBehavior _seekSteeringBehavior;

    public override void _Ready()
    {
        base._Ready();
        if (Engine.IsEditorHint()) return;
        _seekSteeringBehavior = this.FindChild<SeekSteeringBehavior>();
        _seekSteeringBehavior.Target = Threath;
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
        
        if (_seekSteeringBehavior == null)
        {
            return new[] {"This node needs a child of type SeekSteeringBehavior to work."};
        }

        return Array.Empty<string>();
    }
}
