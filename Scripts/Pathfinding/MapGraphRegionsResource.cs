using Godot;
using Godot.Collections;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// This class serves as a resource for associating graph nodes with their respective
/// regions within a map. 
/// </summary>
[Tool]
[GlobalClass]
public partial class MapGraphRegionsResource: Resource
{
    /// <summary>
    /// Dictionary mapping node ID to region ID.
    /// </summary>
    [Export] public Dictionary<uint, uint> NodesIdToRegionsId = new();
}