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
    private bool _isBraking;
    
    
    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        if (Target == null) return new SteeringOutput(Vector2.Zero, 0);
        
        
        _currentVelocity = args.CurrentVelocity;
        float stopSpeed = args.StopSpeed;
        float deltaTime = (float) args.DeltaTime;
        float maximumAcceleration = args.MaximumAcceleration;
        float maximumDeceleration = args.MaximumDeceleration;

        if (_targetVelocity != Target.Velocity)
        {
            _targetVelocity = Target.Velocity;
            
            // Millington recalculates neededAcceleration in every frame, but I
            // think that is an error. Doing that way apparently works but, actually,
            // target velocity is never entirely reached because current gap between
            // target and current velocity is always divided by timeToMatch.
            // So, my version calculates neededAcceleration only when target velocity
            // has changed. This way target velocity is accurately reached, although I
            // have to check for border cases where a minimum amount of acceleration can
            // make us overpass target velocity.
            Vector2 neededAcceleration = (_targetVelocity - _currentVelocity) / 
                                         TimeToMatch;
            
            // If braking, then target velocity is zero or the acceleration vector is
            // opposed to current velocity direction.
            _isBraking = _targetVelocity == Vector2.Zero || 
                         Mathf.IsEqualApprox(neededAcceleration.Normalized().Dot(
                             _currentVelocity.Normalized()), -1);
            
            // Make sure velocity change is not greater than its maximum values.
            if (!_isBraking && neededAcceleration.Length() > maximumAcceleration)
            {
                neededAcceleration = neededAcceleration.Normalized() * maximumAcceleration;
            }
            else if (_isBraking && _currentVelocity.Length() <= stopSpeed)
            {
                return new SteeringOutput(Vector2.Zero, 0);
            }
            else if (_isBraking && neededAcceleration.Length() > maximumDeceleration)
            {
                neededAcceleration = neededAcceleration.Normalized() * maximumDeceleration;
            }
        
            _currentAcceleration = neededAcceleration;
        }
        
        Vector2 frameAcceleration = _currentAcceleration * deltaTime;
        
        // Check for border cases, where just a minimum amount of acceleration can make 
        // us overpass target velocity.
        Vector2 newVelocity = new();
        // While brake, don't brake too much and invert direction.
        if (_isBraking && frameAcceleration.Length() > _currentVelocity.Length())
        { 
            newVelocity = Vector2.Zero;
        } 
        // While not accelerating, don't overpass target velocity
        else if (!_isBraking && 
                 frameAcceleration.Length() > 
                 (_targetVelocity - _currentVelocity).Length())
        {
            newVelocity = _targetVelocity;
        }
        // Normal case.
        else
        {
            newVelocity = _currentVelocity + frameAcceleration;
        }
        
        return new SteeringOutput(newVelocity, 0);
    }
}
