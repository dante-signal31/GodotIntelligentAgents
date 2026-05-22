namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// A pipeline decomposer splits a high-level steering goal into smaller, incremental
/// steering goals that can be more easily achieved.
/// </summary>
public interface IPipelineDecomposer
{
    /// <summary>
    /// Decompose a high-level goal into smaller, incremental goals.
    /// </summary>
    /// <param name="goal">Higher level goal.</param>
    /// <param name="args">Movement status for the current agent.</param>
    /// <returns>A subgoal from the given goal.</returns>
    public PipelineGoal Decompose(PipelineGoal goal, SteeringBehaviorArgs args);
}