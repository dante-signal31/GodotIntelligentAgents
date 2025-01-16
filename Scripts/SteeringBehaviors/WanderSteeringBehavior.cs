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
/// <p> Node to offer a wandering steering behaviour. </p>
/// <p> Wandering steering behaviour makes the agent move randomly around the scene. </p>
/// </summary>
public partial class WanderSteeringBehavior : Node2D, ISteeringBehavior
{
    [ExportCategory("CONFIGURATION:")]
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
    
    /// <summary>
    /// This is the radius of the constraining circle. KEEP IT UNDER wanderDistance!
    /// </summary>
    [Export] public float WanderRadius { get; set; }
    
    /// <summary>
    /// This is the distance the wander circle is projected in front of the agent.
    /// KEEP IT OVER wanderRadius!
    /// </summary>
    [Export] public float WanderDistance { get; set; }
    
    /// <summary>
    /// Maximum amount of random displacement that can be added to the target each
    /// second. KEEP IT OVER wanderRadius.
    /// </summary>
    [Export] public float WanderJitter { get; set; }
    
    /// <summary>
    /// Time in seconds to recalculate the wander position.
    /// </summary>
    [Export] public float WanderRecalculationTime { get; set; }
    
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
    
    private SteeringBehaviorArgs _currentSteeringBehaviorArgs;
    private Node2D _positionMarker;
    private Vector2 _wanderLocalPosition;
    // Actually, it could be an ArriveSteeringBehavior too. Anything that gets you from
    // current position to a desired position.
    private SeekSteeringBehavior _seekSteeringBehavior;
    private Timer _updateTimer;
    
    public override void _EnterTree()
    {
        // _positionMarker will be the target of _seekSteeringBehavior.
        _positionMarker = new Node2D();
        _wanderLocalPosition =
            RandomExtensions.GetRandomPointInCircumference(WanderRadius);
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
        _updateTimer = this.FindChild<Timer>();
        _updateTimer.ProcessCallback = Timer.TimerProcessCallback.Physics;
        _updateTimer.WaitTime = WanderRecalculationTime;
        _updateTimer.Timeout += UpdateWanderPosition;
        _updateTimer.Start();
    }

    /// <summary>
    /// Update the wander position based on the given steering behavior arguments.
    /// </summary>
    /// <param name="args">Steering behavior arguments</param>
    private void UpdateWanderPosition()
    {
        if (_currentSteeringBehaviorArgs == null) return;
        
        SteeringBehaviorArgs args = _currentSteeringBehaviorArgs;
        
        // Add random displacement over an area of a circle of radius wanderJitter. This
        // circle is around current wander local position.
        _wanderLocalPosition += RandomExtensions.GetRandomPointInsideCircle(1f) * 
                                WanderJitter;
        
        // Reproject this new vector back onto a unit circle. This circle is around
        // current agent.
        _wanderLocalPosition = _wanderLocalPosition.Normalized() * WanderRadius;
        
        // Create a targetLocal into a position WanderDist distance in front of the agent.
        // Remember X local axis is our forward axis.
        Vector2 targetLocal = _wanderLocalPosition + new Vector2(WanderDistance, 0);

        // Place targetLocal as relative to agent.
        _positionMarker.GlobalPosition = ToGlobal(targetLocal);
    }
    
    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        _currentSteeringBehaviorArgs = args;

        return _seekSteeringBehavior.GetSteering(args);
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        _seekSteeringBehavior = this.FindChild<SeekSteeringBehavior>();
        _updateTimer = this.FindChild<Timer>();

        List<string> warnings = new();
        
        if (_seekSteeringBehavior == null)
        {
            warnings.Add("This node needs a child of type SeekSteeringBehavior to work.");
        }

        if (_updateTimer == null)
        {
            warnings.Add("This node needs a child of type Timer to work.");
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
        if (!PositionGizmoVisible || _positionMarker == null || Engine.IsEditorHint()) return;
        
        // Draw current heading target.
        DrawCircle(
            ToLocal(_positionMarker.GlobalPosition),
            PositionGizmoRadius,
            PositionGizmoColor,
            filled: false);
        
        // Draw heading from current agent to the current heading target.
        DrawLine(
            Vector2.Zero, 
            ToLocal(_positionMarker.GlobalPosition),
            PositionGizmoColor);
    }
}
