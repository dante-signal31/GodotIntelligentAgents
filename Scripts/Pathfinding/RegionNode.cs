using Godot;
using Godot.Collections;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

[Tool]
public partial class RegionNode: GraphNode, IPositionNode
{
   /// <summary>
   /// Position of the region center.
   /// </summary>
   [Export] public Vector2 Position { get; set; }
   
   /// <summary>
   /// Region nodes that border another region.
   /// </summary>
   /// <remarks>
   /// Key is the region ID of the neighbor region.
   /// Value is an array of position node IDs that border the neighbor region.
   /// </remarks>
    [Export] public Dictionary<uint, Array<uint>> BoundaryNodes = new();
}