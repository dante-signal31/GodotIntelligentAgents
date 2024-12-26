using Godot;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

[Tool]
// It must be marked as Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
public partial class ArriveSteeringBehavior: Node, ISteeringBehavior, ITargeter
{
    [ExportCategory("CONFIGURATION:")]
    [Export] private Node2D _target;
    /// <summary>
    /// Radius to start slowing down using deceleration curve.
    /// </summary>
    [Export] private float _brakingRadius;
    /// <summary>
    /// At this distance from target agent will full stop.
    /// </summary>
    [Export] private float _arrivalDistance;
    /// <summary>
    /// Deceleration curve.
    /// </summary>
    [Export] private Curve _decelerationCurve;
    /// <summary>
    /// At this distance from start, agent will be at full speed, finishing its
    /// acceleration curve.
    /// </summary>
    [Export] private float _accelerationRadius;
    /// <summary>
    /// Acceleration curve.
    /// </summary>
    [Export] private Curve _accelerationCurve;

    /// <summary>
    /// Point this agent is going to.
    /// </summary>
    public Node2D Target
    {
        get=> _target;
        set => _target = value;
    }
    
    /// <summary>
    /// Radius to start slowing down using deceleration curve.
    /// </summary>
    public float BrakingRadius
    {
        get => _brakingRadius;
        set => _brakingRadius = value;
    }
    
    /// <summary>
    /// At this distance from target agent will full stop.
    /// </summary>
    public float ArrivalDistance
    {
        get => _arrivalDistance;
        set => _arrivalDistance = value;
    }
    
    /// <summary>
    /// At this distance from start, agent will be at full speed, finishing its
    /// acceleration curve.
    /// </summary>
    public float AccelerationRadius
    {
        get => _accelerationRadius;
        set => _accelerationRadius = value;
    }
    
    private Vector2 _startPosition;
    private float _distanceFromStart;
    private bool _idle = true;
    
    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        Vector2 targetPosition = _target.GlobalPosition;
        Vector2 currentPosition = args.CurrentAgent.GlobalPosition;
        Vector2 currentVelocity = args.CurrentAgent.Velocity;
        float stopSpeed = args.CurrentAgent.StopSpeed;
        float maximumSpeed = args.MaximumSpeed;
        
        Vector2 toTarget = targetPosition - currentPosition;
        float distanceToTarget = toTarget.Length();
        
        float newSpeed = 0f;
        
        if (_idle && _distanceFromStart > 0) _distanceFromStart = 0;

        if (distanceToTarget >= _arrivalDistance &&
            _distanceFromStart < _accelerationRadius)
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
                    Mathf.InverseLerp(0, _accelerationRadius, _distanceFromStart)) *
                     maximumSpeed;
        }
        else if (distanceToTarget < _brakingRadius &&
                 distanceToTarget >= _arrivalDistance)
        {
            newSpeed = currentVelocity.Length() > stopSpeed ?
                _decelerationCurve.Sample(
                    Mathf.InverseLerp(_brakingRadius, 0, distanceToTarget)) * 
                maximumSpeed:
                0;
        }
        else if (distanceToTarget < _arrivalDistance)
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