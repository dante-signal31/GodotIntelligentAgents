
namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// Basic template for a steering behavior.
/// </summary>
public interface ISteeringBehavior
{
    /// <summary>
    /// Get new steering as an object with linear velocity and rotation.
    /// </summary>
    /// <param name="args">Current agent state.</param>
    /// <returns>An object with new linear velocity and rotation as properties.</returns>
    public SteeringOutput GetSteering(SteeringBehaviorArgs args);
}