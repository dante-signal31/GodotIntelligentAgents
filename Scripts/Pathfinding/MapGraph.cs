using System;
using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Tools;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Spatial graph implementation using a quad grid.
/// <remarks>
/// Ideally, a graph does not need to have any spatial structure, but if you are
/// going to use the graph for pathfinding purposes, it is recommended to use a spatial
/// structure to improve performance. Otherwise, you will be forced to iterate over all
/// nodes in the graph every time you want to find the nearest node to a given position.
/// </remarks>
/// </summary>
[Tool]
public partial class MapGraph: Node2D
{
    [ExportCategory("CONFIGURATION:")]
    [Export] public Vector2I MapSize { get; set; } = new(1920, 1080);

    [Export] public Vector2I CellResolution { get; set; }= new(18, 10);
    
    [Export(PropertyHint.Layers2DPhysics)] public uint ObstaclesLayers { get; set; } = 1;
    
    [ExportCategory("WIRING:")]
    [Export] private TileMapLayer _walkableTilemapLayer;

    /// <summary>
    /// MapGraph serialized backend.
    /// </summary>
    [Export] public MapGraphResource GraphResource = new();
    
    [ExportToolButton("Bake Graph")]
    private Callable GenerateGraphButton => Callable.From(GenerateGraph);
    
    [ExportCategory("DEBUG:")]
    [Export] public bool ShowGizmos { get; set; }
    [Export] public Color GridColor { get; set; } = Colors.Yellow;
    [Export] public int NodeRadius { get; set; } = 10;
    [Export] public Color NodeColor { get; set; } = Colors.Orange;
    
    public Vector2 CellSize => MapSize / (Vector2) CellResolution;
    
    /// <summary>
    /// Just a shortcut to the graph nodes dictionary inside GraphResource.
    /// </summary>
    public Godot.Collections.Dictionary<Vector2I, PositionNode> Nodes => GraphResource.Nodes;
    
    private Vector2 NodeGlobalPosition(Vector2I nodeArrayPosition) => 
        nodeArrayPosition * CellSize + CellSize / 2;

    private Vector2I GlobalToArrayPosition(Vector2 globalPosition)
    {
        return (Vector2I) (globalPosition / CellSize);
    } 
    
    public PositionNode GetNodeAtPosition(Vector2 globalPosition)
    {
        Vector2I arrayPosition = GlobalToArrayPosition(globalPosition);
        if (!GraphResource.Nodes.ContainsKey(arrayPosition)) return null;
        return GraphResource.Nodes[GlobalToArrayPosition(globalPosition)];
    }
    
    private Vector2I GetArrayPositionById(uint nodeId) => GraphResource.NodeArrayPositionsById[nodeId];
    
    public PositionNode GetNodeById(uint nodeId) => 
        GraphResource.Nodes[GetArrayPositionById(nodeId)];

    private CleanAreaChecker _cleanAreaChecker;

    /// <summary>
    /// Returns the relative array position of a neighboring node based on the specified
    /// orientation.
    /// <param name="orientation">The orientation indicating the direction of the neighbor
    /// (North, East, South, or West).</param>
    /// <return>The relative array position as a Vector2I representing the change in
    /// coordinates for the specified orientation.</return>
    /// </summary>
    private Vector2I GetNeighborRelativeArrayPosition(Orientation orientation)
    {
        Vector2 relativePosition = Vector2.Zero;
        switch (orientation)
        {
            case Orientation.North:
                relativePosition = Vector2.Up;
                break;
            case Orientation.East:
                relativePosition = Vector2.Left;
                break;
            case Orientation.South:
                relativePosition = Vector2.Down;
                break;
            case Orientation.West:
                relativePosition = Vector2.Right;
                break;
        }
        return (Vector2I) relativePosition;
    }

    /// <summary>
    /// Generates the graph representation based on the current map configuration and
    /// obstacle data. 
    /// </summary>
    private void GenerateGraph()
    {
        ClearGraph();
        for (int x = 0; x < CellResolution.X; x++)
        {
            for (int y = 0; y < CellResolution.Y; y++)
            {
                Vector2I nodeArrayPosition = new(x, y);
                Vector2 nodeGlobalPosition = NodeGlobalPosition(nodeArrayPosition);
                
                // If there is any obstacle at that position, we don't create any node.
                if (!_cleanAreaChecker.IsCleanArea(nodeGlobalPosition))
                    continue;
                
                // If the position is clean, create a node.
                PositionNode node = new();
                node.Position = nodeGlobalPosition;
                
                float nodeCost = GetPositionCost(nodeGlobalPosition);
                // Node cost is the cost to go through the node. So, its connection cost
                // is the half of the node cost. One half to enter the node and the other
                // half to exit it.
                float connectionCost = nodeCost / 2;
                
                // Populate new node's connections.
                foreach (Orientation orientation in Enum.GetValues<Orientation>())
                {
                    // If the newly created node is adjacent to an existing node, we
                    // create a connection between them.
                    Vector2I neighborArrayPosition =
                        GetNeighborRelativeArrayPosition(orientation) + 
                        nodeArrayPosition;
                    if (!GraphResource.Nodes.ContainsKey(neighborArrayPosition)) continue;
                    // Get the cost to enter the neighbor node.
                    Vector2 neighborGlobalPosition = 
                        NodeGlobalPosition(neighborArrayPosition);
                    float neighbourCost = GetPositionCost(neighborGlobalPosition);
                    float neighborConnectionCost = neighbourCost / 2;
                    // Direct connection between this node and the neighbor.
                    PositionNode neighborNode = GraphResource.Nodes[neighborArrayPosition];
                    node.AddConnection(
                        neighborNode.Id, 
                        connectionCost + neighborConnectionCost, 
                        orientation);
                    // Conversely, as our connections are bidirectional, we must set up
                    // also the reciprocal connection from the neighbor to this node. 
                    Orientation reciprocalOrientation = Orientation.North;
                    switch (orientation)
                    {
                        case Orientation.North: 
                            reciprocalOrientation = Orientation.South; 
                            break;
                        case Orientation.East:
                            reciprocalOrientation = Orientation.West; 
                            break;
                        case Orientation.South:
                            reciprocalOrientation = Orientation.North;
                            break;
                        case Orientation.West:
                            reciprocalOrientation = Orientation.East;
                            break;
                    }
                    neighborNode.AddConnection(
                        node.Id, 
                        connectionCost + neighborConnectionCost, 
                        reciprocalOrientation);
                }
                
                // Once the node is created and configured, we add it to the graph.
                AddNodeToGraph(nodeArrayPosition, node);
            }
        }
    }

    private void ClearGraph()
    {
        GraphResource.Nodes.Clear();
        GraphResource.NodeArrayPositionsById.Clear();
    }

    private void AddNodeToGraph(Vector2I nodeArrayPosition, PositionNode node)
    {
        GraphResource.Nodes.Add(nodeArrayPosition, node);
        GraphResource.NodeArrayPositionsById.Add(node.Id, nodeArrayPosition);
    }

    /// <summary>
    /// Calculates the movement cost associated with a specific global position on the
    /// map. This cost represents the effort required to traverse the tile at the given
    /// position.
    /// </summary>
    /// <param name="neighborGlobalPosition">The global position of the tile
    /// to be evaluated.</param>
    /// <return>The movement cost as a float, derived from the custom data associated
    /// with the tile.</return>
    private float GetPositionCost(Vector2 neighborGlobalPosition)
    {
        Vector2I tileArrayPosition = _walkableTilemapLayer.LocalToMap(
            _walkableTilemapLayer.ToLocal(neighborGlobalPosition));
        TileData tileData = _walkableTilemapLayer.GetCellTileData(tileArrayPosition);
        return (float) tileData.GetCustomData("Cost");
    }

    public override void _EnterTree()
    {
        _cleanAreaChecker = new CleanAreaChecker(
            (Mathf.Min(CellSize.X, CellSize.Y)/2)-5f, 
            ObstaclesLayers, 
            this);
    }    
    
    public override void _ExitTree()
    {
        _cleanAreaChecker.Dispose();
    }

    public override void _Process(double delta)
    {
        if (ShowGizmos) DrawGizmos();
    }

    private void DrawGizmos()
    {
        QueueRedraw();
    }
    
    public override void _Draw()
    {
        if (!ShowGizmos) return;
        
        Vector2 cellSize = CellSize;

        // Draw the grid.
        // Draw vertical lines.
        for (float x = 0; x < MapSize.X; x+=cellSize.X)
        {
            DrawLine(new Vector2(x, 0), new Vector2(x, MapSize.Y), GridColor);
        }
        // Draw horizontal lines.
        for (float y = 0; y < MapSize.Y; y += cellSize.Y)
        {
            DrawLine(new Vector2(0, y), new Vector2(MapSize.X, y), GridColor);
        }

        if (GraphResource.Nodes.Count == 0) return;
        
        // Draw nodes and their edges.
        foreach (KeyValuePair<Vector2I, PositionNode> nodeEntry in GraphResource.Nodes)
        {
            Vector2 cellPosition = NodeGlobalPosition(nodeEntry.Key);
            PositionNode node = nodeEntry.Value;
            DrawCircle(cellPosition, 10, NodeColor);
            foreach (Orientation orientation in Enum.GetValues<Orientation>())
            {
                if (node.Connections.ContainsKey(orientation))
                {
                    Vector2 otherNodeRelativePosition = 
                        GetNeighborRelativeArrayPosition(orientation);
                    Vector2 otherNodePosition = cellPosition + 
                                                otherNodeRelativePosition * CellSize;
                    DrawLine(
                        cellPosition, 
                        otherNodePosition, 
                        NodeColor);
                    DrawString(
                        ThemeDB.FallbackFont, 
                        cellPosition + (otherNodePosition - cellPosition) / 2, 
                        node.Connections[orientation].Cost.ToString("G"), 
                        modulate: NodeColor);
                }
            }
        }
    }
}