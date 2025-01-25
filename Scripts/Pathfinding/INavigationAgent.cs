using Godot;
using Godot.Collections;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Basic interface every navigation agent must implement.
/// </summary>
public interface INavigationAgent
{
    /// <summary>
    /// Position to navigate to.
    /// </summary>
    Vector2 TargetPosition { get; set; }
    
    /// <summary>
    /// Agent radius.
    /// </summary>
    float Radius { get; set; }
    
    /// <summary>
    /// This agent is ready to navigate.
    /// </summary>
    public bool IsReady { get; }

    /// <summary>
    /// Whether we can get a path to the target.
    /// </summary>
    public bool IsTargetReachable();

    /// <summary>
    /// <p>This agent has reached its target position.</p>
    /// <p>It may not always be possible to reach the target position. If target is not
    /// reachable, then path should get us to the nearest point to target.</p>
    /// </summary>
    public bool IsTargetReached();

    /// <summary>
    /// <p>Returns true if the navigation path's final position has been reached.</p>
    /// <p>If target is not rechable, then paths final position is the nearest point
    /// to target.</p>
    /// </summary>
    public bool IsNavigationFinished();
    
    /// <summary>
    /// List of path positions to target.
    /// </summary>
    public Vector2[] PathToTarget { get; }
    
    /// <summary>
    /// <p>It is the last position in the path to target.<p>
    /// <p>If path is reachable, then this is the target position. If not, then it is
    /// the nearest reachable point to target.</p>
    /// </summary>
    public Vector2 PathFinalPosition { get; }

    /// <summary>
    /// Remaining distance to reach the target, following current path.
    /// </summary>
    public float DistanceToTarget();
    
    /// <summary>
    /// Next position to reach in the current path to target
    /// </summary>
    /// <returns>Next position in global space.</returns>
    public Vector2 GetNextPathPosition();
    
}