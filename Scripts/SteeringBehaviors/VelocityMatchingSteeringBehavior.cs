using Godot;
using System;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;

// It must be marked as Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
/// <summary>
/// <p> Velocity matching steering behaviour makes the agent get the same velocity than
/// a target Node2D. </p>
/// </summary>
public partial class VelocityMatchingSteeringBehavior : Node, ISteeringBehavior
{
    [ExportCategory("CONFIGURATION:")]
    [Export] public MovingAgent Target { get; set; }
    [Export] public float TimeToMatch { get; private set; }
    
    private Vector2 _targetVelocity;
    private Vector2 _currentVelocity;
    private Vector2 _currentAcceleration;
    private bool _currentAccelerationUpdateIsNeeded;
    
    private void UpdateTargetData()
    {
        // Acceleration should change in two cases:
        // 1. Target velocity has changed.
        // 2. Target velocity and current velocity are the same. So we should
        //    stop accelerating.
        if ((_currentVelocity != Target.Velocity) 
            ||
            Mathf.IsEqualApprox(_currentVelocity.Length(), Target.Velocity.Length())
            )
        {
            _targetVelocity = Target.Velocity;
            _currentAccelerationUpdateIsNeeded = true;
        }
    }
    
    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        if (Target == null) return new SteeringOutput(Vector2.Zero, 0);
        
        UpdateTargetData();

        _currentVelocity = args.CurrentVelocity;
        float stopSpeed = args.StopSpeed;
        
        float deltaTime = (float) args.DeltaTime;

        // if braking, then target velocity is in the opposite direction than current.
        bool braking = _targetVelocity == Vector2.Zero || 
                       _currentVelocity.Dot(_targetVelocity) < 0;
        
        if (_currentAccelerationUpdateIsNeeded)
        {
            // Millington executes this code section in every frame, but I
            // think that is an error. Doing that way targets velocity is never
            // reached because current gap between target and current velocity
            // is always divided by timeToMatch. So, my version is executing this
            // code section only when acceleration needs to really update
            // (target has changed its velocity or target velocity has been
            // reached and we no longer need an acceleration).
            float maximumAcceleration = args.MaximumAcceleration;
            float maximumDeceleration = args.MaximumDeceleration;
            Vector2 neededAcceleration = (_targetVelocity - _currentVelocity) / TimeToMatch;
            
            // Make sure velocity change is not greater than its maximum values.
            if (!braking && neededAcceleration.Length() > maximumAcceleration)
            {
                neededAcceleration = neededAcceleration.Normalized() * maximumAcceleration;
            }
            else if (braking && _currentVelocity.Length() <= stopSpeed)
            {
                return new SteeringOutput(Vector2.Zero, 0);
            }
            else if (braking && neededAcceleration.Length() > maximumDeceleration)
            {
                neededAcceleration = neededAcceleration.Normalized() * maximumDeceleration;
            }
            
            _currentAcceleration = neededAcceleration;
            _currentAccelerationUpdateIsNeeded = false;
        }
        
        Vector2 newVelocity = _currentVelocity + _currentAcceleration * deltaTime;
        // When we want to stop, a too high acceleration could invert Velocity direction
        // instead of reducing it to zero. This would cause the agent to move backwards
        // when we want it to stop. So, in those cases the acceleration overpass target
        // velocity, we'll clamp new velocity to target velocity.
        if ((braking && newVelocity.Dot(_currentVelocity) < 0) ||
            (!braking && newVelocity.Length() > _targetVelocity.Length()))
        {
            newVelocity = _targetVelocity;
        }
        
        return new SteeringOutput(newVelocity, 0);
    }
}
