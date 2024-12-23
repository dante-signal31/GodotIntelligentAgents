using Godot;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// <p>Basic template for a steering behavior.</p>
///
/// <p>It should be an interface, but Godot does not allow to use interfaces in
/// inspector fields. So, I've used an abstract class to be able to pass its
/// implementations through inspector fields.</p>
///
/// <p> Actually you cannot export an abstract class to the inspector, unless you
/// make it partial and inherit from Node. </p>
/// </summary>
public abstract partial class SteeringBehavior: Node
{
    /// <summary>
    /// Get new steering as an object with linear velocity and rotation.
    /// </summary>
    /// <param name="args">Current agent state.</param>
    /// <returns>An object with new linear velocity and rotation as properties.</returns>
    public abstract SteeringOutput GetSteering(SteeringBehaviorArgs args);
}