using Godot;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// Interface for Steering Behaviors that follow a target in some way.
/// </summary>
public interface ITargeter
{
    /// <summary>
    /// Target for this steering behavior.
    /// </summary>
    public Node2D Target { get; set; }
}