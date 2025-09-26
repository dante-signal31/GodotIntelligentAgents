using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// This agent is supposed to act as a hinge of formation turns. To do so, it needs to be
/// the parent of the formation, so when this agent faces its target, the entire formation
/// will turn.
/// </summary>
[Tool]
public partial class UsherHingeAgent : MovingAgent
{
    [ExportCategory("USHER HINGE CONFIGURATION:")]
    private Node2D _targetToLookAt;

    [Export] public Node2D TargetToLookAt
    {
        get => _targetToLookAt;
        set
        {
            _targetToLookAt = value;
            if (_faceSteeringBehavior != null) _faceSteeringBehavior.Target = value;
        }
    }
    
    /// <summary>
    /// The agent maximum rotational speed in degrees.
    /// </summary>
    [Export] public new float MaximumRotationalDegSpeed { get; set; } = 1080;

    /// <summary>
    /// Rotation will stop when the difference in degrees between the current rotation
    /// and the current forward vector is less than this value.
    /// </summary>
    [Export] public new float StopRotationDegThreshold { get; set; } = 1;

    [ExportCategory("DEBUG:")]
    [Export] public bool ShowGizmos { get; set; }
    [Export] public Color GizmosColor { get; set; }
    [Export] public int GizmoRadius { get; set; } = 20;
    
    private FaceSteeringBehavior _faceSteeringBehavior;
    
    public override void _EnterTree()
    {
        base._EnterTree();
        _faceSteeringBehavior = this.FindChild<FaceSteeringBehavior>();
    }

    public override void _Ready()
    {
        base._Ready();
        if (_faceSteeringBehavior == null || TargetToLookAt == null) return;
        _faceSteeringBehavior.Target = TargetToLookAt;
    }

    /// <summary>
    /// Stops the rotation of the UsherHingeAgent by setting the reference to
    /// the target object to null. Once this method is called, the agent will
    /// no longer automatically align itself with any target direction, effectively
    /// halting all rotational behavior controlled by the FaceSteeringBehavior.
    /// </summary>
    public void StopRotation()
    {
        TargetToLookAt = null;
    }

    /// <summary>
    /// Sets the forward direction of the UsherHingeAgent.
    /// </summary>
    /// <param name="direction">The vector defining the new forward direction for the
    /// agent.</param>
    public void SetForward(Vector2 direction)
    {
        GlobalTransform = GlobalTransform with { X = direction };
    }
    
    protected override SteeringBehaviorArgs GetSteeringBehaviorArgs()
    {
        return new SteeringBehaviorArgs(
            this, 
            Vector2.Zero, 
            0, 
            0,
            MaximumRotationalDegSpeed,
            StopRotationDegThreshold,
            0,
            0,
            0);
    }
    
    protected override void UpdateSteeringBehaviorArgs(double delta)
    {
        // Update steering behavior args.
        _behaviorArgs.MaximumSpeed = 0;
        _behaviorArgs.StopSpeed = 0;
        _behaviorArgs.CurrentVelocity = Vector2.Zero;
        _behaviorArgs.MaximumRotationalSpeed = MaximumRotationalDegSpeed;
        _behaviorArgs.StopRotationThreshold = StopRotationDegThreshold;
        _behaviorArgs.MaximumAcceleration = 0;
        _behaviorArgs.MaximumDeceleration = 0;
        _behaviorArgs.DeltaTime = delta;
    }
    
    public override void _Process(double delta)
    {
        DrawGizmos();
    }

    private void DrawGizmos()
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!ShowGizmos) return;

        // Draw hinge position.
        DrawCircle(Vector2.Zero, GizmoRadius, GizmosColor, filled: true);
        
        // Draw the hinge forward vector.
        DrawLine(Vector2.Zero, Forward, GizmosColor);
        
        if (TargetToLookAt == null) return;
        
        // Draw the line to look target.
        DrawDashedLine(
            Vector2.Zero, 
            ToLocal(TargetToLookAt.GlobalPosition), 
            GizmosColor, 
            dash:2.0f);
        
        // Draw a Circle with an inner cross to mark look target position.
        Vector2 targetRelativePosition = ToLocal(TargetToLookAt.GlobalPosition);
        DrawCircle(
            targetRelativePosition, 
            GizmoRadius, 
            GizmosColor, 
            filled: false);
        DrawLine(
            targetRelativePosition with {X = targetRelativePosition.X - GizmoRadius}, 
            targetRelativePosition with {X = targetRelativePosition.X + GizmoRadius},
            GizmosColor);
        DrawLine(
            targetRelativePosition with {Y = targetRelativePosition.Y - GizmoRadius}, 
            targetRelativePosition with {Y = targetRelativePosition.Y + GizmoRadius},
            GizmosColor);
    }
    

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings());
        
        if (SteeringBehavior == null)
        {
            warnings.Add("This node needs a child node of type FaceSteeringBehavior " +
                         "to work.");
        }

        return warnings.ToArray();
    }
}