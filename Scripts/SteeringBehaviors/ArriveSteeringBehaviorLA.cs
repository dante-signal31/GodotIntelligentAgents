using Godot;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

// It must be marked as Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
/// <summary>
/// <p> Arrive behavior is a Seek-like steering behaviour in which agent accelerates at
/// the startup and brakes gradually when approachs the end.</p>
/// <p> LA behavior implements a Linear-Acceleration approach. So, 
/// acceleration is given by a fixed acceleration values. In this case, that values are
/// maximum acceleration and maximum deceleration values from agent.</p>
/// </summary>
public partial class ArriveSteeringBehaviorLA: Node, ISteeringBehavior, ITargeter
{
    [ExportCategory("CONFIGURATION:")]
    /// <summary>
    /// Point this agent is going to.
    /// </summary>
    [Export] public Node2D Target { get; set; }

    /// <summary>
    /// At this distance from target agent will full stop.
    /// </summary>
    [Export] public float ArrivalDistance { get; set; }
    
    private Vector2 _startPosition;
    private bool _idle = true;
    private float _currentSpeed;
    private float _currentMaximumDeceleration;

    public float BrakingRadius =>
        GetBrakingRadius(_currentSpeed, _currentMaximumDeceleration);

    private float GetBrakingRadius(float speed, float deceleration)
    {
        return float.Pow(speed, 2) / (2 * deceleration);
    }
    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        Vector2 targetPosition = Target.GlobalPosition;
        Vector2 currentPosition = args.CurrentAgent.GlobalPosition;
        Vector2 currentVelocity = args.CurrentAgent.Velocity;
        _currentSpeed = currentVelocity.Length();
        float maximumSpeed = args.MaximumSpeed;
        float currentMaximumAcceleration = args.MaximumAcceleration;
        _currentMaximumDeceleration = args.MaximumDeceleration;
        float deltaTime = (float) args.DeltaTime;
        
        Vector2 toTarget = targetPosition - currentPosition;
        float distanceToTarget = toTarget.Length();
        
        float newSpeed = 0f;

        if (distanceToTarget >= ArrivalDistance &&
            distanceToTarget > BrakingRadius &&
            _currentSpeed < maximumSpeed)
        { // Acceleration phase.
            newSpeed = Mathf.Min(
                maximumSpeed,
                _currentSpeed + currentMaximumAcceleration * deltaTime);
        } 
        else if (distanceToTarget >= ArrivalDistance &&
                   distanceToTarget > BrakingRadius &&
                   _currentSpeed >= maximumSpeed)
        { // Full speed phase.
            newSpeed = maximumSpeed;
        }
        else if (distanceToTarget <= BrakingRadius &&
                 distanceToTarget >= ArrivalDistance)
        { // Braking phase.
            newSpeed = Mathf.Max(
                0, 
                _currentSpeed - _currentMaximumDeceleration * deltaTime);
        }
        else if (distanceToTarget < ArrivalDistance)
        { // Full stop phase.
            newSpeed = 0;
        }

        Vector2 newVelocity = toTarget.Normalized() * newSpeed;

        return new SteeringOutput(newVelocity, 0);
    }
}
