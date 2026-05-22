using GodotGameAIbyExample.Scripts.Pathfinding;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// The actuator determines how the character will achieve its current subgoal attending
/// the physical capabilities of the agent.
/// <remarks>
/// The actuator also determines which channels of the goal take precedence or
/// are ignored.
/// </remarks>
/// </summary>
public interface IPipelineActuator
{
    /// <summary>
    /// Get the path to the given goal.
    /// </summary>
    /// <param name="goal">The goal to achieve.</param>
    /// <param name="args">The current status of the agent.</param>
    /// <returns>The best steering output available to achieve the goal.</returns>
    public Path GetPath(PipelineGoal goal, SteeringBehaviorArgs args);
    public SteeringOutput GetOutput(PipelineGoal goal, SteeringBehaviorArgs args);
}