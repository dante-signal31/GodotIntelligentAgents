using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Implements the Breadth-First Search (BFS) algorithm for pathfinding.
/// </summary>
/// <remarks>
/// Actually, a Breath-First pathfinder only guarantees to find the shortest path
/// if every connection has the same cost. With scenes with variable costs, the
/// algorithm will ignore that some connections are cheaper than others. I.e., a 
/// Breath-First pathfinder will always find the path with fewer nodes, not the
/// cost-cheaper one.
/// </remarks>
[Tool]
public partial class BreathFirstPathFinder: NotInformedPathFinder<NodeRecordQueue> { }