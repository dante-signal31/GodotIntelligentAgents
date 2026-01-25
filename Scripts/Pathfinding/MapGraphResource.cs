using Godot;
using Godot.Collections;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Represents a resource containing a graph structure for pathfinding purposes.
/// This class is designed to hold a collection of <see cref="PositionNode"/> objects
/// indexed by their positions in a 2D space.
/// </summary>
/// <remarks>
/// The resource is intended to be used alongside the <see cref="MapGraph"/> class,
/// which acts as a container and manager for the graph's operations.
/// </remarks>
/// <seealso cref="PositionNode"/>
/// <seealso cref="MapGraph"/>
[Tool]
[GlobalClass]
public partial class MapGraphResource: Resource
{ 
    [Export] public Dictionary<Vector2I, PositionNode> ArrayPositionsToNodes = new();
    // Nodes store GraphNodes indexed by their array position in the spatial grid.
    // We need something to map node ids to array positions. That's what this
    // dictionary does.
    [Export] public Dictionary<uint, Vector2I> NodeIdsToArrayPositions = new();
}