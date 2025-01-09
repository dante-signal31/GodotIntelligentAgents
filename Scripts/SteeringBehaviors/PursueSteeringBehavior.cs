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
/// <p>Node to offer a Pursuit steering behaviour.</p>
/// <p>To pursue another agent if won't be enough to go to its current position. If that
/// agent displaces then we will only follow its trail. Instead, pursuer must predict
/// whare chased agent will be and aim to that position.</p>
/// </summary>
public partial class PursueSteeringBehavior : Node, ISteeringBehavior
{
    [ExportCategory("CONFIGURATION:")]
    /// <summary>
    /// Agent to pursue to.
    /// </summary>
    [Export] public MovingAgent Target { get; set; }
    /// <summary>
    /// Distance at which we give our goal as reached and we stop our agent.
    /// </summary>
    [Export] public float ArrivalDistance { get; set; }
    
    private float _aheadSemiConeDegrees;
    /// <summary>
    /// Degrees from forward vector inside which we consider an object is ahead.
    /// </summary>
    [Export(PropertyHint.Range, "0, 90")]
    public float AheadSemiConeDegrees
    {
        get => _aheadSemiConeDegrees;
        set
        {
            if (Mathf.IsEqualApprox(_aheadSemiConeDegrees, value)) return;
            _aheadSemiConeDegrees = Mathf.Clamp(value, 0, 90);
        }
    }
    
    private float _comingToUsSemiConeDegrees;
    /// <summary>
    /// Degrees from forward vector inside which we consider an object is going toward us.
    /// </summary>
    [Export(PropertyHint.Range, "0, 90")]
    public float ComingToUsSemiConeDegrees
    {
        get => _comingToUsSemiConeDegrees;
        set
        {
            if (Mathf.IsEqualApprox(_comingToUsSemiConeDegrees, value)) return;
            _comingToUsSemiConeDegrees = Mathf.Clamp(value, 0, 90);
        }
    }
    
    [ExportCategory("DEBUG:")]
    /// <summary>
    /// Make visible position marker.
    /// </summary>
    [Export] private bool PredictedPositionMarkerVisible { get; set; }
    
    private SeekSteeringBehavior _seekSteeringBehavior;
    private float _cosAheadSemiConeRadians;
    private float _cosComingToUsSemiConeRadians;
    private Node2D _predictedPositionMarker;
    private Color _agentColor;
    private Color _targetColor;
    
    public override void _EnterTree()
    {
        _cosAheadSemiConeRadians = Mathf.Cos(Mathf.DegToRad(AheadSemiConeDegrees));
        _cosAheadSemiConeRadians = Mathf.Cos(Mathf.DegToRad(ComingToUsSemiConeDegrees));
        _predictedPositionMarker = new Node2D();
    }

    public override void _ExitTree()
    {
        _predictedPositionMarker.QueueFree();
    }

    public override void _Ready()
    {
        _seekSteeringBehavior = this.FindChild<SeekSteeringBehavior>();
        _seekSteeringBehavior.Target = Target;
        _seekSteeringBehavior.ArrivalDistance = ArrivalDistance;
        _agentColor = this.FindAncestor<MovingAgent>().AgentColor;
        _targetColor = Target.AgentColor;
    }

    /// <summary>
    /// Whether target is coming to us.
    /// </summary>
    /// <param name="args">Our current data.</param>
    /// <returns>True if target has a velocity vector that is going toward us.</returns>
    private bool TargetIsComingToUs(SteeringBehaviorArgs args)
    {
        Vector2 currentPosition = args.CurrentAgent.GlobalPosition;
        Vector2 currentVelocity = args.CurrentAgent.Velocity;
        Vector2 targetPosition = Target.GlobalPosition;
        Vector2 targetVelocity = Target.Velocity;
        
        Vector2 directionToTarget = (targetPosition - currentPosition).Normalized();

        bool targetInFrontOfUs =
            currentVelocity.Dot(directionToTarget) > _cosAheadSemiConeRadians;
        bool targetComingToUs = currentVelocity.Dot(targetVelocity.Normalized()) <
                                (-1 * _cosComingToUsSemiConeRadians);

        return targetInFrontOfUs && targetComingToUs;
    }
    
    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        if (TargetIsComingToUs(args))
        {   // Target is coming to us so just go straight to it.
            _predictedPositionMarker.GlobalPosition = Target.GlobalPosition;
            _seekSteeringBehavior.Target = _predictedPositionMarker;
            return _seekSteeringBehavior.GetSteering(args);
        }
        else
        {   // Target is not coming to us so we must predict where it will be.
            // The look-ahead time is proportional to the distance between the chased
            // and the pursuer and is inversely proportional to the sum of the
            // agents velocities.
            Vector2 currentPosition = args.CurrentAgent.GlobalPosition;
            float currenSpeed = args.CurrentAgent.Velocity.Length();
            float targetSpeed = Target.Velocity.Length();
            Vector2 targetVelocity = Target.Velocity;
            float distanceToTarget = Target.GlobalPosition.DistanceTo(currentPosition);
            float lookAheadTime = distanceToTarget / (currenSpeed + targetSpeed);

            if (!float.IsInfinity(lookAheadTime))
            {
                _predictedPositionMarker.GlobalPosition = Target.GlobalPosition +
                                                          (targetVelocity *
                                                           lookAheadTime);
                _seekSteeringBehavior.Target = _predictedPositionMarker;
            }
            
            return _seekSteeringBehavior.GetSteering(args);
        }
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
}