using Godot;
using System;
using System.Collections.Generic;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;

// It must be marked as Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
/// <summary>
/// <p>Node to offer a evade steering behaviour.</p>
/// <p>Evade steering behaviour makes the agent go away from another node marked
/// as threath.</p>
/// </summary>
public partial class EvadeSteeringBehavior : Node2D, ISteeringBehavior
{
    [ExportCategory("CONFIGURATION")]
    /// <summary>
    /// Agent to run from.
    /// </summary>
    [Export] public MovingAgent Threat { get; set; }

    private float _panicDistance = 1.0f;
    /// <summary>
    /// Minimum distance to threat before fleeing.
    /// </summary>
    [Export] public float PanicDistance
    {
        get => _panicDistance;
        set
        {
            _panicDistance = value;
            if (_fleeSteeringBehavior != null) 
                _fleeSteeringBehavior.PanicDistance = value;
        }
    } 
    
    [ExportCategory("DEBUG:")]
    /// <summary>
    /// Make visible position marker.
    /// </summary>
    [Export] private bool PredictedPositionMarkerVisible { get; set; }
    
    private FleeSteeringBehavior _fleeSteeringBehavior;
    private Node2D _predictedPositionMarker;
    private MovingAgent _currentAgent;
    
    private Color AgentColor => _currentAgent.AgentColor;
    private Color ThreatColor => Threat.AgentColor;

    public override void _Draw()
    {
        if (_predictedPositionMarker == null || 
            !PredictedPositionMarkerVisible ||
            Engine.IsEditorHint()) return;
        DrawLine(
            Vector2.Zero, 
            ToLocal(_predictedPositionMarker.GlobalPosition), 
            AgentColor);
        DrawCircle(
            ToLocal(_predictedPositionMarker.GlobalPosition),
            30f, 
            AgentColor, 
            filled: false);
        DrawLine(
            ToLocal(Threat.GlobalPosition), 
            ToLocal(_predictedPositionMarker.GlobalPosition), 
            ThreatColor);
    }

    public override void _EnterTree()
    {
        _predictedPositionMarker = new Node2D();
        // Find out who is our father.
        _currentAgent = this.FindAncestor<MovingAgent>();
    }
    
    public override void _Ready()
    {
        _predictedPositionMarker.GlobalPosition = Threat.GlobalPosition;
        _fleeSteeringBehavior = this.FindChild<FleeSteeringBehavior>();
        _fleeSteeringBehavior.Threat = _predictedPositionMarker;
        _fleeSteeringBehavior.PanicDistance = PanicDistance;
    }

    public override void _ExitTree()
    {
        _predictedPositionMarker.QueueFree();
    }

    public override void _Process(double delta)
    {
        if (PredictedPositionMarkerVisible) DrawGizmos();
    }

    private void DrawGizmos()
    {
        QueueRedraw();
    }
    
    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        if (Threat == null) return new SteeringOutput(Vector2.Zero, 0);
        
        Vector2 currentPosition = args.CurrentAgent.GlobalPosition;
        float maximumSpeed = args.MaximumSpeed;
        
        Vector2 toThreat = Threat.GlobalPosition - currentPosition;
        
        // The look-ahead time is proportional to the distance between the evader
        // and the pursuer; and is inversely proportional to the sum of the
        // agent's velocities
        float lookAheadTime = toThreat.Length() / (maximumSpeed + Threat.CurrentSpeed);

        _predictedPositionMarker.GlobalPosition =
            Threat.GlobalPosition + Threat.Velocity * lookAheadTime;
        
        return _fleeSteeringBehavior.GetSteering(args);
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        _fleeSteeringBehavior = this.FindChild<FleeSteeringBehavior>();

        List<string> warnings = new();
        
        if (_fleeSteeringBehavior == null)
        {
            warnings.Add("This node needs a child of type FleeSteeringBehavior to work.");
        }
        
        return warnings.ToArray();
    }
}
