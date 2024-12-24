using Godot;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

public partial class SeekSteeringBehavior: SteeringBehavior, ITargeter
{
    [ExportCategory("CONFIGURATION:")] 
    [Export] private Node2D _target;
    /// <summary>
    /// Distance at which we give our goal as reached and we stop our agent.
    /// </summary>
    [Export] private float _arrivalDistance = .1f;

    /// <summary>
    /// Point this agent is going to.
    /// </summary>
    public Node2D Target
    {
        get=> _target; 
        set=> _target = value;
    }
    
    /// <summary>
    /// Distance at which we give our goal as reached and we stop our agent.
    /// </summary>
    public float ArrivalDistance
    {
        get=> _arrivalDistance; 
        set=> _arrivalDistance = value;
    }
    
    public override SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        if (_target == null) return new SteeringOutput(Vector2.Zero, 0);
        
        Vector2 targetPosition = _target.GlobalPosition;
        Vector2 currentPosition = args.CurrentAgent.GlobalPosition;
        float maximumSpeed = args.MaximumSpeed;
        
        Vector2 toTarget = targetPosition - currentPosition;
        
        Vector2 newVelocity = toTarget.Length() > _arrivalDistance? 
            toTarget.Normalized() * maximumSpeed:
            Vector2.Zero;
        
        return new SteeringOutput(newVelocity, 0);
    }
    
}