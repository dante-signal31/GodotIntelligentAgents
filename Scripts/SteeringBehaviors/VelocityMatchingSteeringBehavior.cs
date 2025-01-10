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
    [Export] public float TimeToMatch { get; set; }
    
    private Vector2 _targetVelocity;
    private Vector2 _currentVelocity;
    private Vector2 _currentAcceleration;
    private bool _currentAccelerationUpdateIsNeeded;
    
    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        if (Target == null) return new SteeringOutput(Vector2.Zero, 0);
        
        _targetVelocity = Target.Velocity;
        _currentVelocity = args.CurrentVelocity;
        float stopSpeed = args.StopSpeed;
        float deltaTime = (float) args.DeltaTime;
        float maximumAcceleration = args.MaximumAcceleration;
        float maximumDeceleration = args.MaximumDeceleration;

        Vector2 neededAcceleration = (_targetVelocity - _currentVelocity) / TimeToMatch;
        
        // if braking, then target velocity is in the opposite direction than current.
        bool braking = _targetVelocity == Vector2.Zero || 
                       neededAcceleration.Dot(_currentVelocity) < 0;
        
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
        
        Vector2 newVelocity = _currentVelocity + _currentAcceleration * deltaTime;
        
        return new SteeringOutput(newVelocity, 0);
    }
}
