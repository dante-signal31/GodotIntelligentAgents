using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

public interface IPositionNode
{
    /// <summary>
    /// Unique identifier of the node.
    /// </summary>
    public uint Id { get; }
    
    /// <summary>
    /// Global position of the node.
    /// </summary>
    public Vector2 Position { get; set; }
    
    /// <summary>
    /// Connections from this node to other nodes.
    /// </summary>
    public Godot.Collections.Dictionary<uint, GraphConnection> Connections { get; }
}