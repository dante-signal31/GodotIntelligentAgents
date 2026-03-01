using Godot;
using Godot.Collections;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

[Tool]
[GlobalClass]
public partial class RegionGraphResource: Resource
{
    
    
    /// <summary>
    /// Dictionary mapping region ID to region node.
    /// </summary>
    [Export] public Dictionary<uint, RegionNode> RegionIdToRegionNode = new();
    
    /// <summary>
    /// Dictionary mapping position to region node.
    /// </summary>
    [Export] public Dictionary<Vector2, RegionNode> PositionToRegionNode = new();
    
    /// <summary>
    /// Dictionary mapping an array of path positions for every node to any other region. 
    /// </summary>
    [Export] public Dictionary<long, InterRegionPath> FromNodeToRegionPaths = new();

    /// <summary>
    /// Godot can only serialize Dictionaries if their keys are simple variant types. So,
    /// if my key is composed of two variables, I need to convert them to a single ulong.
    /// This method does that.
    /// </summary>
    /// <param name="fromNodeId">Node ID we are starting from.</param>
    /// <param name="toRegionId">Region ID where we want to go.</param>
    /// <returns></returns>
    public static long GetFromNodeToRegionKey(uint fromNodeId, uint toRegionId)
    {
        return (long)fromNodeId << 32 | toRegionId;
    }

    /// <summary>
    /// Splits a combined key into its constituent parts: the node ID and the region ID.
    /// </summary>
    /// <param name="key">The combined key containing both the node ID and the
    /// region ID.</param>
    /// <param name="fromNodeId">The extracted node ID component of the key.</param>
    /// <param name="toRegionId">The extracted region ID component of the key.</param>
    public static void SplitKey(long key, out uint fromNodeId, out uint toRegionId)
    {
        fromNodeId = (uint)((ulong)key >> 32);
        toRegionId = (uint)key;
    }
}