using Godot;
using System;
using System.Collections.Generic;
using Godot.Collections;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;

// It must be marked as Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
/// <summary>
/// <p> Node to offer a cohesion steering behaviour. </p>
/// <p> Cohesion steering behaviour makes the agent to place himself in the center of a
/// group of other agents. </p>
/// </summary>
public partial class CohesionSteeringBehavior : Node2D, ISteeringBehavior
{
    [ExportCategory("CONFIGURATION:")]
    /// <summary>
    /// List of agents to average their positions.
    /// </summary>
    [Export] public Array<Node2D> Targets { get; set; }

    private float _arrivalDistance;

    /// <summary>
    /// Distance at which we give our goal as reached and we stop our agent.
    /// </summary>
    [Export] public float ArrivalDistance
    {
        get => _arrivalDistance;
        set
        {
            _arrivalDistance = value;
            if (_seekSteeringBehavior != null)
                _seekSteeringBehavior.ArrivalDistance = value;
        }
    }
    
    [ExportCategory("DEBUG:")]
    /// <summary>
    /// Make position gizmos visible.
    /// </summary>
    [Export] private bool PositionGizmoVisible { get; set; }
    [Export] private Color PositionGizmoColor { get; set; }
    /// <summary>
    /// Radius for the position marker gizmo.
    /// </summary>
    [Export] private float PositionGizmoRadius { get; set; }
    
    /// <summary>
    /// <p>Average position, counting every agent's targets.</p>
    /// </summary>
    public Vector2 AveragePosition { get; private set; }
    
    private Node2D _positionMarker;
    // Actually, it could be an ArriveSteeringBehavior too. Anything that gets you from
    // current position to a desired position.
    private SeekSteeringBehavior _seekSteeringBehavior;
    private MovingAgent _currentAgent;
    
    public override void _EnterTree()
    {
        // _positionMarker will be the target of _seekSteeringBehavior.
        _positionMarker = new Node2D();
        // Find out who is our father.
        _currentAgent = this.FindAncestor<MovingAgent>();
    }
    
    public override void _ExitTree()
    {
        _positionMarker.QueueFree();
    }

    public override void _Ready()
    {
        _seekSteeringBehavior = this.FindChild<SeekSteeringBehavior>();
        _seekSteeringBehavior.Target = _positionMarker;
        _seekSteeringBehavior.ArrivalDistance = ArrivalDistance;
    }
    
    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        if (Targets == null || Targets.Count == 0 || _seekSteeringBehavior == null) 
            return new SteeringOutput(Vector2.Zero, 0);

        // Let's average position counting every agent's targets. 
        Vector2 positionSum = new();
        foreach (Node2D target in Targets)
        {
            positionSum += target.GlobalPosition;
        }
        AveragePosition = positionSum / Targets.Count;
        
        // Place our position marker in the average position to make
        // _seekSteeringBehavior makes us get there.
        _positionMarker.GlobalPosition = AveragePosition;
        
        return _seekSteeringBehavior.GetSteering(args);
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        _seekSteeringBehavior = this.FindChild<SeekSteeringBehavior>();

        List<string> warnings = new();
        
        if (_seekSteeringBehavior == null)
        {
            warnings.Add("This node needs a child of type SeekSteeringBehavior to work.");
        }
        return warnings.ToArray();
    }
    
    public override void _Process(double delta)
    {
        if (PositionGizmoVisible) DrawGizmos();
    }

    private void DrawGizmos()
    {
        QueueRedraw();
    }
    
    public override void _Draw()
    {
        if (!PositionGizmoVisible || Targets == null || Engine.IsEditorHint()) return;

        // Draw relationship between targets and center of mass.
        foreach (Node2D target in Targets)
        {
            DrawLine(
                ToLocal(target.GlobalPosition), 
                ToLocal(_positionMarker.GlobalPosition), 
                ((MovingAgent) target).AgentColor);
        }
        
        // Draw center of mass.
        DrawCircle(
            ToLocal(_positionMarker.GlobalPosition),
            PositionGizmoRadius,
            PositionGizmoColor,
            filled: false);
        
        // Draw heading from current agent to center of mass.
        DrawLine(
            Vector2.Zero, 
            ToLocal(_positionMarker.GlobalPosition),
            _currentAgent.AgentColor);
    }
}
