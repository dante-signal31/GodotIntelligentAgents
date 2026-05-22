using GodotGameAIbyExample.Scripts.Pathfinding;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// A constraint limits the ability of an agent to move directly towards a goal. They
/// usually represent obstacles that the agent cannot cross or conditions that the agent
/// must comply with.
/// </summary>
public interface IPipelineConstraint
{
    /// <summary>
    /// Check if the given goal will violate the constraint.
    /// </summary>
    /// <param name="pathToGoal">Proposed path to get the goal.</param>
    /// <param name="goal">Potential goal.</param>
    /// <param name="args">Current agent movement status.</param>
    /// <returns>True if the given goal can violate this constraint.
    /// Otherwise, false.</returns>
    public bool IsViolated(
        Path pathToGoal, 
        PipelineGoal goal, 
        SteeringBehaviorArgs args);

    /// <summary>
    /// Suggest a new goal that will not violate this constraint. Returned goal should
    /// be as near as possible to the given goal.
    /// <remarks>
    /// The suggested goal should be a new PipelineGoal instance. Given goal must not be
    /// modified.
    /// </remarks>
    /// </summary>
    /// <param name="pathToGoal">Ideal path to get the goal.</param>
    /// <param name="goal">Ideal goal.</param>
    /// <param name="args">Current agent movement status.</param>
    /// <returns>An alternative goal, as similar as possible to the ideal goal, but that
    /// does not violate the constraint.</returns>
    public PipelineGoal SuggestGoal(
        Path pathToGoal, 
        PipelineGoal goal, 
        SteeringBehaviorArgs args);
}