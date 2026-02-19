using Godot;
using Godot.Collections;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

[Tool]
[GlobalClass]
public partial class RegionGraphResource: Resource
{
    /// <summary>
    /// Serializable key for a dictionary to store paths from a node to any other region.
    /// </summary>
    public partial class FromNodeToRegionPathsKey : Resource
    {
        [Export] public uint FromNodeId;
        [Export] public uint ToRegionId;
    }

    /// <summary>
    /// Path positions across a region to link two other regions.
    /// </summary>
    public partial class InterRegionPath : Resource
    {
        [Export] public Array<Vector2> PathPositions;
        [Export] public float Cost;
    }
    
    /// <summary>
    /// Dictionary mapping region ID to region node.
    /// </summary>
    [Export] public Dictionary<uint, RegionNode> RegionIdToRegionNode = new();
    
    /// <summary>
    /// Dictionary mapping an array of path positions for every node to any other region. 
    /// </summary>
    [Export] public Dictionary<FromNodeToRegionPathsKey, InterRegionPath> FromNodeToRegionPaths = new();
    
}