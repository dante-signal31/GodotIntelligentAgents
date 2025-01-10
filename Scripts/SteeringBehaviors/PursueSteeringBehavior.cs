using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using GodotGameAIbyExample.addons.InteractiveRanges.ConeRange;
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
public partial class PursueSteeringBehavior : Node2D, ISteeringBehavior
{
    private const string aheadConeRangeName = "AheadConeRange";
    private const string comingToUsConeRangeName = "ComingToUsConeRange";
    
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
            if (_aheadConeRange != null) _aheadConeRange.SemiConeDegrees = value;
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
            if (_comingToUsConeRange != null) _comingToUsConeRange.SemiConeDegrees = value;
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
    private MovingAgent _currentAgent;
    private ConeRange _aheadConeRange;
    private ConeRange _comingToUsConeRange;
    
    private Color AgentColor => _currentAgent.AgentColor;
    private Color TargetColor => Target.AgentColor;
    
    public override void _EnterTree()
    {
        // Most methods use radians as input, but most humans understand better degrees. 
        // So, we accept degrees to configure scripts but convert them to radians to work.
        _cosAheadSemiConeRadians = Mathf.Cos(Mathf.DegToRad(AheadSemiConeDegrees));
        _cosComingToUsSemiConeRadians = Mathf.Cos(Mathf.DegToRad(ComingToUsSemiConeDegrees));
        // Create an invisible object as marker to place it at target predicted future
        // position. That marker will be used by seek steering behaviour as target.
        _predictedPositionMarker = new Node2D();
        _predictedPositionMarker.GlobalPosition = Target.GlobalPosition;
    }

    public override void _ExitTree()
    {
        _predictedPositionMarker.QueueFree();
    }

    public override void _Ready()
    {
        _currentAgent = this.FindAncestor<MovingAgent>();
        _seekSteeringBehavior = this.FindChild<SeekSteeringBehavior>();
        _seekSteeringBehavior.ArrivalDistance = ArrivalDistance;
        _seekSteeringBehavior.Target = _predictedPositionMarker;
        // Configure our gizmos.
        _aheadConeRange = (ConeRange) FindChild(aheadConeRangeName);
        _aheadConeRange.SemiConeDegrees = AheadSemiConeDegrees;
        _comingToUsConeRange = (ConeRange) FindChild(comingToUsConeRangeName);
        _comingToUsConeRange.SemiConeDegrees = ComingToUsSemiConeDegrees;
    }

    public override void _Process(double delta)
    {
        if (PredictedPositionMarkerVisible) DrawGizmos();
    }

    private void DrawGizmos()
    {
        QueueRedraw();
    }

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
            ToLocal(Target.GlobalPosition), 
            ToLocal(_predictedPositionMarker.GlobalPosition), 
            TargetColor);
    }

    /// <summary>
    /// Whether target is coming to us.
    /// </summary>
    /// <param name="args">Our current data.</param>
    /// <returns>True if target has a velocity vector that is going toward us.</returns>
    private bool TargetIsComingToUs(SteeringBehaviorArgs args)
    {
        Vector2 currentPosition = args.CurrentAgent.GlobalPosition;
        Vector2 currentDirection = args.CurrentAgent.Velocity.Normalized();
        Vector2 targetPosition = Target.GlobalPosition;
        Vector2 targetDirection = Target.Velocity.Normalized();
        
        Vector2 directionToTarget = (targetPosition - currentPosition).Normalized();

        bool targetInFrontOfUs =
            currentDirection.Dot(directionToTarget) > _cosAheadSemiConeRadians;
        bool targetComingToUs = currentDirection.Dot(targetDirection) <
                                (-1 * _cosComingToUsSemiConeRadians);

        return targetInFrontOfUs && targetComingToUs;
    }
    
    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        if (TargetIsComingToUs(args))
        {   // Target is coming to us so just go straight to it.
            _predictedPositionMarker.GlobalPosition = Target.GlobalPosition;
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

            // Avoid divide-by-zero error when both agents are stationary.
            if (float.IsInfinity(lookAheadTime))
                return new SteeringOutput(Vector2.Zero, 0);
            
            // Place the marker where we think the target will be at the look-ahead
            // time.
            _predictedPositionMarker.GlobalPosition = Target.GlobalPosition +
                                                          (targetVelocity *
                                                           lookAheadTime);
            
            // Let the seek steering behavior get to the new marker position.
            return _seekSteeringBehavior.GetSteering(args);
        }
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        _seekSteeringBehavior = this.FindChild<SeekSteeringBehavior>();
        List<ConeRange> coneRangesFound = this.FindChildren<ConeRange>();
        bool coneRangesNamesCorrect = coneRangesFound.Any(
                                          cr => cr.Name == aheadConeRangeName) && 
                                      coneRangesFound.Any(
                                          cr => cr.Name == comingToUsConeRangeName);

        List<string> warnings = new();
        
        if (_seekSteeringBehavior == null)
        {
            warnings.Add("This node needs a child of type SeekSteeringBehavior to work.");
        }
        
        if (coneRangesFound.Count != 2 || !coneRangesNamesCorrect)
        {
            warnings.Add($"This node needs 2 child ConeRange to work. " +
                         $"One called {aheadConeRangeName} and another " +
                         $"called {comingToUsConeRangeName}");
        }
        
        return warnings.ToArray();
    }
}
