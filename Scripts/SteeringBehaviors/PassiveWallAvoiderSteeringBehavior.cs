using System.Collections.Generic;
using System.Timers;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Sensors;
using Timer = System.Timers.Timer;

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
    
    [ExportCategory("CONFIGURATION:")]
    [Export] private float _coolDownTime = 0.5f;
    
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
    private Timer _calculationCooldownTimer;
    private Vector2 _currentAvoidVector;
    private bool _calculationCooldownActive;
    
    public override void _EnterTree()
    {
        // Find out who our father is.
        _currentAgent = this.FindAncestor<MovingAgent>();
        SetCalculationCooldownTimer();
    }

    public override void _ExitTree()
    {
        StopCalculationCooldownTimer();
    }

    public override void _Ready()
    {
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

    private void SetCalculationCooldownTimer()
    {
        _calculationCooldownTimer = new Timer(_coolDownTime * 1000);
        _calculationCooldownTimer.AutoReset = false;
        _calculationCooldownTimer.Elapsed += OnCalculationCooldownTimerTimeout;
    }

    private void OnCalculationCooldownTimerTimeout(object sender, ElapsedEventArgs e)
    {
        _calculationCooldownActive = false;
    }

    private void StartCalculationCooldownTimer()
    {
        _calculationCooldownTimer.Stop();
        _calculationCooldownTimer.Start();
        _calculationCooldownActive = true;
    }

    private void StopCalculationCooldownTimer()
    {
        _calculationCooldownTimer.Stop();
        _calculationCooldownActive = false;
    }

    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        _avoidVector = GetAvoidVector(args);
        
        _currentSteering = new SteeringOutput(
            linear: _avoidVector,
            angular: 0);
        
        return _currentSteering;
    }

    private Vector2 GetAvoidVector(SteeringBehaviorArgs args)
    {
        // A cooldown is needed when avoiding an obstacle perpendicular to the agent
        // heading. Without a cooldown, lateral push will not have enough time to displace
        // the agent to avoid the obstacle.
        if (_calculationCooldownActive) return _currentAvoidVector;
        
        Vector2 avoidVector = Vector2.Zero;
        
        if (_obstacleDetected)
        {
            ClosestHitData closestHitData = GetClosestHit(args);
            _closestHit = closestHitData.Hit;
            
            float overShootFactor = GetOverShootFactor(
                closestHitData.DetectionSensorIndex, 
                closestHitData.Distance);

            // Buckland and Millington calculate avoidVector this way, but It is 
            // troublesome when the center sensor detects a wall perpendicular to the
            // current heading, because then avoidVector will stop the agent not make it
            // evade the wall. So, an additional lateral push must be added to the
            // avoidVector. That lateral push should be at its maximum when the detecting
            // sensor is in the center and minimum when the detecting sensor is on the
            // right or left side.
            avoidVector = _closestHit.Normal * (args.MaximumSpeed * overShootFactor);
            
            // Calculate relative side of detecting sensor.
            // 0 is the right side, 1 is the left side, 0.5 is center.
            float indexSide = Mathf.InverseLerp(
                0, 
                _whiskersSensor.SensorAmount-1,
                closestHitData.DetectionSensorIndex);
            
            // Positive means the detecting sensor is on the right side of the
            // agent. Negative means the detecting sensor is on the left side of the
            // agent.
            float distanceFromCenterFactor = 0.5f - indexSide;
            
            // Minimum when near 1 (so, when detection is near the left or right side)
            // and maximum when near 0 (so, when detection is near the center).
            float pushFactor = Mathf.InverseLerp(
                1, 
                0, 
                Mathf.Abs(distanceFromCenterFactor));

            float pushDirection;
            if (Mathf.IsEqualApprox(indexSide, 0.5))
            {
                // Random direction when the sensor detects in the center
                pushDirection = GD.Randf() < 0.5f ? 1 : -1;
            } 
            else if (indexSide > 0.5)
            {
                // Push to the right when the sensor detects an obstacle on the left side.
                pushDirection = 1;
            } 
            else
            {
                // Push to the left when the sensor detects an obstacle on the right side.
                pushDirection = -1;
            }
            
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
            
            // Calculate the push vector.
            Vector2 pushVector = rightVector * 
                                 pushDirection * 
                                 pushFactor * 
                                 args.MaximumSpeed;
            
            // Add the push vector to the avoidVector.
            avoidVector += pushVector;
        }
        
        _currentAvoidVector = avoidVector;
        StartCalculationCooldownTimer();
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