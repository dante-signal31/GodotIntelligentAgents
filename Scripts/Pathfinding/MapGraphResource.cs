using Godot;
using Godot.Collections;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Represents a resource containing a graph structure for pathfinding purposes.
/// This class is designed to hold a collection of <see cref="GraphNode"/> objects
/// indexed by their positions in a 2D space.
/// </summary>
/// <remarks>
/// The resource is intended to be used alongside the <see cref="MapGraph"/> class,
/// which acts as a container and manager for the graph's operations.
/// </remarks>
/// <seealso cref="GraphNode"/>
/// <seealso cref="MapGraph"/>
[Tool]
[GlobalClass]
public partial class MapGraphResource: Resource
{ 
    [Export] public Dictionary<Vector2I, GraphNode> Nodes = new();
}