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
/// </summary>
public partial class ArriveSteeringBehavior: Node, ISteeringBehavior, ITargeter
{
    [ExportCategory("CONFIGURATION:")]
    /// <summary>
    /// Point this agent is going to.
    /// </summary>
    [Export] public Node2D Target { get; set; }
    /// <summary>
    /// Radius to start slowing down using deceleration curve.
    /// </summary>
    [Export] public float BrakingRadius { get; set; }
    /// <summary>
    /// At this distance from target agent will full stop.
    /// </summary>
    [Export] public float ArrivalDistance { get; set; }
    /// <summary>
    /// Deceleration curve.
    /// </summary>
    [Export] private Curve _decelerationCurve;
    /// <summary>
    /// At this distance from start, agent will be at full speed, finishing its
    /// acceleration curve.
    /// </summary>
    [Export] public float AccelerationRadius { get; set; }
    /// <summary>
    /// Acceleration curve.
    /// </summary>
    [Export] private Curve _accelerationCurve;
    
    
    private Vector2 _startPosition;
    private float _distanceFromStart;
    private bool _idle = true;
    
    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        Vector2 targetPosition = Target.GlobalPosition;
        Vector2 currentPosition = args.CurrentAgent.GlobalPosition;
        Vector2 currentVelocity = args.CurrentAgent.Velocity;
        float stopSpeed = args.CurrentAgent.StopSpeed;
        float maximumSpeed = args.MaximumSpeed;
        
        Vector2 toTarget = targetPosition - currentPosition;
        float distanceToTarget = toTarget.Length();
        
        float newSpeed = 0f;
        
        if (_idle && _distanceFromStart > 0) _distanceFromStart = 0;

        if (distanceToTarget >= ArrivalDistance &&
            _distanceFromStart < AccelerationRadius)
        {
            // Acceleration phase.
            if (_idle)
            {
                _startPosition = currentPosition;
                _idle = false;
            }

            _distanceFromStart = (currentPosition - _startPosition).Length();
            newSpeed =
                _accelerationCurve.Sample(
                    Mathf.InverseLerp(0, AccelerationRadius, _distanceFromStart)) *
                     maximumSpeed;
        }
        else if (distanceToTarget < BrakingRadius &&
                 distanceToTarget >= ArrivalDistance)
        {
            newSpeed = currentVelocity.Length() > stopSpeed ?
                _decelerationCurve.Sample(
                    Mathf.InverseLerp(BrakingRadius, 0, distanceToTarget)) * 
                maximumSpeed:
                0;
        }
        else if (distanceToTarget < ArrivalDistance)
        {
            // Full stop phase.
            newSpeed = 0;
            _idle = true;
        }
        else
        {
            // Full speed phase.
            newSpeed = maximumSpeed;
        }

        Vector2 newVelocity = toTarget.Normalized() * newSpeed;

        return new SteeringOutput(newVelocity, 0);
    }
}