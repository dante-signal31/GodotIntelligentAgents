using Godot;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

// It must be marked as Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
/// <summary>
/// <p>Seek steering behaviour makes the agent move towards a target position.</p>
/// </summary>
public partial class SeekSteeringBehavior: Node, ISteeringBehavior, ITargeter
{
    [ExportCategory("CONFIGURATION:")]
    /// <summary>
    /// Point this agent is going to.
    /// </summary>
    [Export] public Node2D Target { get; set; }
    /// <summary>
    /// Distance at which we give our goal as reached and we stop our agent.
    /// </summary>
    [Export] public float ArrivalDistance { get; set; } = .1f;
    
    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        if (Target == null) return new SteeringOutput(Vector2.Zero, 0);
        
        Vector2 targetPosition = Target.GlobalPosition;
        Vector2 currentPosition = args.CurrentAgent.GlobalPosition;
        float maximumSpeed = args.MaximumSpeed;
        
        Vector2 toTarget = targetPosition - currentPosition;
        
        Vector2 newVelocity = toTarget.Length() > ArrivalDistance? 
            toTarget.Normalized() * maximumSpeed:
            Vector2.Zero;
        
        return new SteeringOutput(newVelocity, 0);
    }
    
}