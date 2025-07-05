using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Sensors;
using Timer = System.Timers.Timer;

// It must be marked as Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

[Tool]
/// <summary>
/// Steering behavior to avoid walls and obstacles.
/// </summary>
public partial class WallAvoiderSteeringBehavior : Node2D, ISteeringBehavior
{
    /// <summary>
    /// Data about the closest hit.
    /// </summary>
    private struct ClosestHitData
    {
        public float Distance;
        public RayCastHit Hit;
        public int detectionSensorIndex;
    }
    
    [ExportCategory("CONFIGURATION:")]

    private Node2D _target;
    /// <summary>
    /// Target to go avoiding other agents.
    /// </summary>
    [Export] public Node2D Target
    {
        get => _target;
        set
        {
            if (_target == value) return;
            _target = value;
            if (_targeter == null) return;
            _targeter.Target = value;
        }
    }
    
    /// <summary>
    /// Timeout started, in seconds, after no further collision detected, before
    /// resuming travel to target.
    /// </summary>
    [Export] public float AvoidanceTimeout { get; set; } = 0.5f;
    
    [ExportCategory("DEBUG:")]
    /// <summary>
    /// Show gizmos.
    /// </summary>
    [Export] public bool ShowGizmos { get; private set; }
    
    /// <summary>
    /// Colors for this object's gizmos.
    /// </summary>
    [Export] public Color GizmoColor { get; private set; } = Colors.White;
    
    private WhiskersSensor _whiskersSensor;
    private bool _obstacleDetected;
    private ITargeter _targeter;
    private ISteeringBehavior _steeringBehavior;
    private RayCastHit _closestHit;
    private Vector2 _avoidVector;
    private MovingAgent _currentAgent;
    private System.Timers.Timer _avoidanceTimer;
    private bool _waitingForAvoidanceTimeout;
    private SteeringOutput _currentSteering;
    
    public override void _EnterTree()
    {
        // Find out who our father is.
        _currentAgent = this.FindAncestor<MovingAgent>();
        SetTimer();
    }

    private void SetTimer()
    {
        _avoidanceTimer = new Timer(AvoidanceTimeout * 1000);
        _avoidanceTimer.AutoReset = false;
        _avoidanceTimer.Elapsed += OnTimerTimeout;
    }
    
    public override void _Ready()
    {
        _targeter = this.FindChild<ITargeter>();
        _steeringBehavior = (ISteeringBehavior)_targeter;
        _targeter.Target = Target;
        
        _whiskersSensor = this.FindChild<WhiskersSensor>();
        if (_whiskersSensor == null) return;
        // _whiskersSensor.SensorsLayersMask = LayersToAvoid;
        _whiskersSensor.Connect(
            WhiskersSensor.SignalName.ObjectDetected,
            new Callable(this, MethodName.OnObstacleDetected));
        _whiskersSensor.Connect(
            WhiskersSensor.SignalName.NoObjectDetected,
            new Callable(this, MethodName.OnNoObstacleDetected));
    }
    
    private void OnTimerTimeout(object sender, System.Timers.ElapsedEventArgs e)
    {
        StopAvoidanceTimer();
        _waitingForAvoidanceTimeout = false;
    }

    private void StartAvoidanceTimer()
    {
        _avoidanceTimer.Interval = AvoidanceTimeout * 1000;
        _avoidanceTimer.Enabled = true;
    }

    private void StopAvoidanceTimer()
    {
        _avoidanceTimer.Enabled = false;
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
    
    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        if (_waitingForAvoidanceTimeout) return _currentSteering;
        
        SteeringOutput steeringToTargetVelocity = _steeringBehavior.GetSteering(args);
        _avoidVector = Vector2.Zero;
        if (_obstacleDetected)
        {
            ClosestHitData closestHitData = GetClosestHit(args);
            _closestHit = closestHitData.Hit;
            
            float overShootFactor = GetOverShootFactor(
                closestHitData.detectionSensorIndex, 
                closestHitData.Distance);

            // Buckland and Millington calculate avoidVector this way but I is 
            // troublesome when center sensor detect a wall perpendicular to current
            // heading, because then avoidVector will stop the agent not make it
            // evade the wall. So, an additional lateral push must be added to the
            // avoidVector. That lateral push should be at its maximum when detecting
            // sensor is in the center and minimum when detecting sensor is in the
            // right or left side.
            _avoidVector = _closestHit.Normal * (args.MaximumSpeed * overShootFactor);
            
            // Calculate relative side of detecting sensor.
            // 0 is right side, 1 is left side, 0.5 is center.
            float indexSide = Mathf.InverseLerp(
                0, 
                _whiskersSensor.SensorAmount-1,
                closestHitData.detectionSensorIndex);
            
            // Positive means the detecting sensor is in the right side of the
            // agent. Negative means the detecting sensor is in the left side of the
            // agent.
            float distanceFromCenterFactor = 0.5f - indexSide;
            
            // Minimum when near 1 (so, when detection is near left or right side) and
            // maximum when near 0 (so, when detection is near center).
            float pushFactor = Mathf.InverseLerp(
                1, 
                0, 
                Mathf.Abs(distanceFromCenterFactor));

            float pushDirection;
            if (Mathf.IsEqualApprox(indexSide, 0.5))
            {
                // Random direction when sensor detects in the center
                pushDirection = GD.Randf() < 0.5f ? 1 : -1;
            } 
            else if (indexSide > 0.5)
            {
                // Push to the right when sensor detects obstacle in the left side.
                pushDirection = 1;
            } 
            else
            {
                // Push to the left when sensor detects obstacle in the right side.
                pushDirection = -1;
            }
            
            // Calculate right vector relative to our current Forward vector.
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
            
            // Calculate the push vector.
            Vector2 pushVector = rightVector * 
                                 pushDirection * 
                                 pushFactor * 
                                 args.MaximumSpeed;
            
            // Add the push vector to the avoidVector.
            _avoidVector += pushVector;
            
            // Start avoid timer to avoid jittering.
            StartAvoidanceTimer();
            _waitingForAvoidanceTimeout = true;
        }

        _currentSteering = new SteeringOutput(
            linear: steeringToTargetVelocity.Linear + _avoidVector,
            angular: steeringToTargetVelocity.Angular);
        return _currentSteering;
    }

    /// <summary>
    /// Get the closest hit data.
    /// </summary>
    /// <param name="args">SteeringBehaviorArgs arguments passed to this class
    /// <see cref="GetSteering"/> method.</param>
    /// <returns>Hit data.</returns>
    private ClosestHitData GetClosestHit(SteeringBehaviorArgs args)
    {
        ClosestHitData closestHit = new();
        closestHit.Distance = float.MaxValue;
        closestHit.detectionSensorIndex = -1;

        foreach ((RayCastHit hit, int index) in _whiskersSensor.DetectedHits)
        {
            float hitDistance = hit.Position.DistanceTo(args.Position);
            if (hitDistance < closestHit.Distance)
            {
                closestHit.Distance = hitDistance;
                closestHit.Hit = hit;
                closestHit.detectionSensorIndex= index;
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
        
        DrawCircle(ToLocal(_closestHit.Position), 10.0f, GizmoColor);
        DrawLine(
            ToLocal(_closestHit.Position), 
            ToLocal(_closestHit.Position + _avoidVector), 
            GizmoColor);
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        WhiskersSensor whiskers= this.FindChild<WhiskersSensor>();
        
        ITargeter targeterBehavior = 
            this.FindChild<ITargeter>();
        
        List<string> warnings = new();
        
        if (whiskers == null)
        {
            warnings.Add("This node needs a child node of type " +
                         "WhiskersSensor to work properly.");  
        }
        
        if (targeterBehavior == null || !(targeterBehavior is ISteeringBehavior))
        {
            warnings.Add("This node needs a child of type both ISteeringBehavior and " +
                         "ITargeter to work. ");  
        }
        
        return warnings.ToArray();
    }
}