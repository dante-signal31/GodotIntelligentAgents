namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// A targeter generates the top-level goal for an agent.
/// </summary>
public interface IPipelineTargeter
{
    /// <summary>
    /// Get goal for this targeter.
    /// </summary>
    /// <param name="args">Current steering behavior args passed from
    /// PipelineSteeringBehavior's getSteering() method.</param>
    /// <remarks>
    /// Channels already defined by a targeter cannot be overwritten by another targeter.
    /// This way we keep coherent those goals registered by different targeters.
    /// </remarks>
    /// <returns>Goal aggregated from every pipeline targeter.</returns>
    public PipelineGoal GetGoal(SteeringBehaviorArgs args);
}