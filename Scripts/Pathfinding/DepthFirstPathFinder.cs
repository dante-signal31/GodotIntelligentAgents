using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Implements a depth-first search (DFS) pathfinding algorithm for navigating a
/// graph structure.
/// <remarks>
/// Depth-first search does NOT guarantee to find the shortest path. Actually, when
/// it finds a path, it's likely to be suboptimal.
/// </remarks>
/// </summary>
[Tool]
public partial class DepthFirstPathFinder: NotInformedPathFinder<NodeRecordStack> { }