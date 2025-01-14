using Godot;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

public class SteeringBehaviorArgs
{
    /// <summary>
    /// Owner of this steering.
    /// </summary>
    public MovingAgent CurrentAgent { get; }
    
    /// <summary>
    /// Maximum linear speed for this steering.
    /// </summary>
    public float MaximumSpeed { get; set; }
    
    /// <summary>
    /// Minimum linear speed under which agent is considered stopped.
    /// </summary>
    public float StopSpeed { get; set; }
    
    /// <summary>
    /// Current owner velocity vector.
    /// </summary>
    public Vector2 CurrentVelocity { get; set; }
    
    /// <summary>
    /// Maximum rotational speed for this steering.
    /// </summary>
    public float MaximumRotationalSpeed { get; set; }
    
    /// <summary>
    /// Rotation will stop when the difference in degrees between the current
    /// rotation and current forward vector is less than this value.
    /// </summary>
    public float StopRotationThreshold { get; set; }
    
    /// <summary>
    /// Maximum acceleration for this steering.
    /// </summary>
    public float MaximumAcceleration { get; set; }
    
    /// <summary>
    /// Maximum deceleration for this steering.
    /// </summary>
    public float MaximumDeceleration { get; set; }
    
    /// <summary>
    /// Delta time since last steering behavior update.
    /// </summary>
    public double DeltaTime { get; set; }

    /// <summary>
    /// This agent position.
    /// </summary>
    public Vector2 Position => CurrentAgent.GlobalPosition;

    /// <summary>
    /// This agent rotation in degrees (using Z as rotation axis because this is a
    /// 2D game).
    /// </summary>
    public float Orientation => CurrentAgent.GlobalRotationDegrees;

    public SteeringBehaviorArgs(
        MovingAgent currentAgent, 
        Vector2 currentVelocity,
        float maximumSpeed, 
        float stopSpeed, 
        float maximumRotationalSpeed, 
        float stopRotationThreshold,
        float maximumAcceleration,
        float maximumDeceleration, 
        float deltaTime)
    {
        CurrentVelocity = currentVelocity;
        MaximumSpeed = maximumSpeed;
        StopSpeed = stopSpeed;
        MaximumRotationalSpeed = maximumRotationalSpeed;
        StopRotationThreshold = stopRotationThreshold;  
        CurrentAgent = currentAgent;
        MaximumAcceleration = maximumAcceleration;
        MaximumDeceleration = maximumDeceleration;
        DeltaTime = deltaTime;
    }
}