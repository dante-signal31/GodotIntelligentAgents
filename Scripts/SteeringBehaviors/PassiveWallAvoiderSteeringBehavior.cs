using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Sensors;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

// It must be marked as Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
/// <summary>
/// Steering behavior to return an avoid vector to not to crash with walls and obstacles.
/// </summary>
public partial class PassiveWallAvoiderSteeringBehavior: 
    Node2D, 
    ISteeringBehavior, 
    IGizmos
{
    /// <summary>
    /// Data about the closest hit.
    /// </summary>
    private struct ClosestHitData
    {
        public float Distance;
        public RayCastHit Hit;
        public int DetectionSensorIndex;
    }

    private enum RelativeOrientation
    {
        Left, 
        Front, 
        Right
    }

    /// <summary>
    /// The cooldown time, in seconds, used to control the interval between activations
    /// of the passive wall-avoidance steering behavior.
    /// </summary>
    [ExportCategory("CONFIGURATION:")]
    [Export] private float _avoidDistance = 60f;
    [Export(PropertyHint.Range, "0.0, 1.0, 0.01")] 
    private float _longitudinalTolerance = 0.1f;
    // [Export(PropertyHint.Range, "0.0, 1.0, 0.01")] 
    // private float _minimumPushFactor = 0.5f;
    
    [ExportCategory("DEBUG:")]
    private bool _showGizmos;
    /// <summary>
    /// Show gizmos.
    /// </summary>
    [Export] public bool ShowGizmos
    {
        get => _showGizmos;
        set
        {
            _showGizmos = value;
            if (_whiskersSensor == null) return;
            _whiskersSensor.ShowGizmos = value;
        }
    }
    
    /// <summary>
    /// Colors for this object's gizmos.
    /// </summary>
    [Export] public Color GizmosColor { get; set; }
    
    private WhiskersSensor _whiskersSensor;    
    private bool _obstacleDetected;
    private RayCastHit _closestHit;
    private Vector2 _avoidVector;
    private MovingAgent _currentAgent;
    private SteeringOutput _currentSteering;
    private Vector2 _currentAvoidVector;
    private bool _calculationCooldownActive;
    
    public override void _EnterTree()
    {
        // Find out who our father is.
        _currentAgent = this.FindAncestor<MovingAgent>();
    }
    
    public override void _Ready()
    {
        _whiskersSensor = this.FindChild<WhiskersSensor>();
        if (_whiskersSensor == null) return;
        _whiskersSensor.Connect(
            WhiskersSensor.SignalName.ObjectDetected,
            new Callable(this, MethodName.OnObstacleDetected));
        _whiskersSensor.Connect(
            WhiskersSensor.SignalName.NoObjectDetected,
            new Callable(this, MethodName.OnNoObstacleDetected));
    }
    
    /// <summary>
    /// Method to bind to whisker's ColliderDetected event.
    /// </summary>
    /// <param name="_"></param>
    private void OnObstacleDetected(RayCastHit _)
    {
        _obstacleDetected = true;
    }
    
    /// <summary>
    /// Method to bind to whisker's NoColliderDetected event.
    /// </summary>
    private void OnNoObstacleDetected()
    {
        _obstacleDetected = false;
    }

    /// <summary>
    /// Determines the relative orientation of a given position with respect to the
    /// current agent.
    /// </summary>
    /// <param name="position">The position to evaluate relative to the agent.</param>
    /// <returns>A value of <see cref="RelativeOrientation"/> indicating whether the
    /// position is to the left, right, or in front of the agent.</returns>
    private RelativeOrientation GetRelativeOrientation(Vector2 position)
    {
        Vector2 relativePosition = position - _currentAgent.GlobalPosition;
        float crossProduct = _currentAgent.Forward.Cross(relativePosition.Normalized());
        if (Mathf.Abs(crossProduct) < _longitudinalTolerance) 
            return RelativeOrientation.Front;
        if (crossProduct > 0) return RelativeOrientation.Right;
        return RelativeOrientation.Left;
    }

    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        if (!_obstacleDetected)
        {
            _currentSteering = new SteeringOutput(linear: Vector2.Zero, angular: 0);
            return _currentSteering;
        }
        
        _avoidVector = GetAvoidVector(args);
        Vector2 recommendedTargetToAvoidObstacle = _closestHit.Position + _avoidVector;
        Vector2 vectorToGetRecommendedTarget =
            (recommendedTargetToAvoidObstacle - _currentAgent.GlobalPosition).Normalized();
        _currentSteering = new SteeringOutput(
            linear: vectorToGetRecommendedTarget * args.MaximumSpeed,
            angular: 0);
        return _currentSteering;
    }
    
    

    private Vector2 GetAvoidVector(SteeringBehaviorArgs args)
    {
        Vector2 avoidVector = Vector2.Zero;
        
        if (_obstacleDetected)
        {
            ClosestHitData closestHitData = GetClosestHit(args);
            _closestHit = closestHitData.Hit;
            
            float overShootFactor = GetOverShootFactor(
                closestHitData.DetectionSensorIndex, 
                closestHitData.Distance);

            // Buckland and Millington calculate avoidVector this way, but It is 
            // troublesome when the avoid-vector is longitudinal to the
            // current heading, because then avoidVector will stop the agent not make it
            // evade the wall. So, an additional lateral push must be added to the
            // avoidVector. 
            avoidVector = _closestHit.Normal * 
                          (args.MaximumSpeed * overShootFactor + _avoidDistance);
            
            Vector2 normalizedInverseAvoidVector = -avoidVector.Normalized();
            
            Vector2 normalizedHeading = args.CurrentVelocity == Vector2.Zero?
                _currentAgent.Forward:
                args.CurrentVelocity.Normalized();
            float longitudinalDisplacement = 1 -
                                             normalizedInverseAvoidVector.Dot(
                                                 normalizedHeading);
            
            // If avoidVector and CurrentVelocity are too aligned (avoidVector is
            // longitudinal to current heading), then we must add
            // a lateral push to avoid the obstacle.
            // avoidVector and currentVelocity are too aligned in two edge cases:
            // - When the agent approaches to a wall perpendicular to its heading.
            // - When one of the lateral sensors touches the end of a wall that is ç
            // perpendicular to agent heading.
            if (longitudinalDisplacement < _longitudinalTolerance)
            {
                // Calculate the right vector relative to our current Forward vector.
                //
                // TIP --------------------------
                // I could have done:
                // Vector2 rightVector = _currentAgent.Forward.Rotated(Mathf.Pi / 2).Normalized();
                // 
                // But when you are rotating exactly 90 degrees is more performant just
                // inverting the components.
                // To rotate clockwise (In Godot):
                // Vector2 rightVector = new Vector2(-_currentAgent.Forward.y, _currentAgent.Forward.x);
                // To rotate counterclockwise:
                // Vector2 rightVector = new Vector2(_currentAgent.Forward.y, -_currentAgent.Forward.x);
                Vector2 rightVector = new Vector2(
                    -_currentAgent.Forward.Y,
                    _currentAgent.Forward.X);

                // The detected obstacle is on the left or right side of the agent?
                switch (GetRelativeOrientation(_closestHit.Position))
                {
                    // If obstacle on the left, evade to the right.
                    case RelativeOrientation.Left: 
                        avoidVector = avoidVector.Length() * rightVector;
                        break;
                    // If obstacle on the right, evade to the left.
                    case RelativeOrientation.Right: 
                        avoidVector = avoidVector.Length() * rightVector * -1;
                        break;
                    // If obstacle on the front, evade to the right or left.
                    case RelativeOrientation.Front: 
                        float pushDirection = GD.Randf() < 0.5f ? 1 : -1;
                        avoidVector = avoidVector.Length() * rightVector * pushDirection;
                        break;
                }
            }
        }
        return avoidVector;
    }

    /// <summary>
    /// Get the closest hit data.
    /// </summary>
    /// <param name="args">SteeringBehaviorArgs arguments passed to this class
    /// <see cref="GetSteering"/> method.</param>
    /// <returns>Hit data.</returns>
    private ClosestHitData GetClosestHit(SteeringBehaviorArgs args)
    {
        ClosestHitData closestHit = new()
        {
            Distance = float.MaxValue,
            DetectionSensorIndex = -1
        };

        foreach ((RayCastHit hit, int index) in _whiskersSensor.DetectedHits)
        {
            float hitDistance = hit.Position.DistanceTo(args.Position);
            if (hitDistance < closestHit.Distance)
            {
                closestHit.Distance = hitDistance;
                closestHit.Hit = hit;
                closestHit.DetectionSensorIndex= index;
            }
        }

        return closestHit;
    }
    
    /// <summary>
    /// Calculates the overshoot factor based on the sensor index and the
    /// closest distance.
    /// </summary>
    /// <param name="sensorIndex">The index of the sensor used in the calculation.</param>
    /// <param name="closestDistance">The distance to the closest object detected
    /// by the sensor.</param>
    /// <returns>
    /// A normalized value representing the overshoot factor, ranging from 0 to 1.
    /// </returns>
    private float GetOverShootFactor(int sensorIndex, float closestDistance)
    {
        float sensorLength = _whiskersSensor.GetSensorLength(sensorIndex);
        float overShoot = sensorLength - closestDistance;
        float overShootFactor = Mathf.InverseLerp(0, sensorLength, overShoot);
        return overShootFactor;
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
        if (!ShowGizmos || 
            !_whiskersSensor.IsAnyObjectDetected || 
            _closestHit == null) return;
        
        DrawCircle(ToLocal(_closestHit.Position), 10.0f, GizmosColor);
        DrawLine(
            ToLocal(_closestHit.Position), 
            ToLocal(_closestHit.Position + _avoidVector), 
            GizmosColor);
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        WhiskersSensor whiskers= this.FindChild<WhiskersSensor>();
        
        List<string> warnings = new();
        
        if (whiskers == null)
        {
            warnings.Add("This node needs a child node of type " +
                         "WhiskersSensor to work properly.");  
        }
        
        return warnings.ToArray();
    }
}