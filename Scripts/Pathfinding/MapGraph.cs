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

    /// <summary>
    /// MapGraph serialized backend.
    /// </summary>
    [Export] public MapGraphResource GraphResource = new();
    
    [ExportToolButton("GenerateGraph")]
    private Callable GenerateGraphButton => Callable.From(GenerateGraph);
    
    [ExportCategory("DEBUG:")]
    [Export] public bool ShowGizmos { get; set; }
    [Export] public Color GridColor { get; set; } = Colors.Yellow;
    [Export] public int NodeRadius { get; set; } = 10;
    [Export] public Color NodeColor { get; set; } = Colors.Orange;
    
    private Vector2 CellSize => MapSize / (Vector2) CellResolution;
    
    private Vector2 NodeGlobalPosition(Vector2I nodeArrayPosition) => 
        nodeArrayPosition * CellSize + CellSize / 2;

    private Vector2I GlobalToArrayPosition(Vector2 globalPosition)
    {
        return (Vector2I) (globalPosition / CellSize);
    } 
    
    public GraphNode GetNodeAtPosition(Vector2 globalPosition)
    {
        Vector2I arrayPosition = GlobalToArrayPosition(globalPosition);
        if (!GraphResource.Nodes.ContainsKey(arrayPosition)) return null;
        return GraphResource.Nodes[GlobalToArrayPosition(globalPosition)];
    }

    /// <summary>
    /// Just a shortcut to the graph nodes dictionary inside GraphResource.
    /// </summary>
    public Godot.Collections.Dictionary<Vector2I, GraphNode> Nodes => GraphResource.Nodes;

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
        GraphResource.Nodes.Clear();
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
                GraphNode node = new();
                node.Position = nodeGlobalPosition;
                
                // Populate new node's connections.
                foreach (Orientation orientation in Enum.GetValues<Orientation>())
                {
                    // If the newly created node is adjacent to an existing node, we
                    // create a connection between them.
                    Vector2I neighborArrayPosition =
                        GetNeighborRelativeArrayPosition(orientation) + 
                        nodeArrayPosition;
                    if (!GraphResource.Nodes.ContainsKey(neighborArrayPosition)) continue;
                    // Direct connection between this node and the neighbor.
                    node.AddConnection(
                        nodeArrayPosition,
                        neighborArrayPosition, 
                        1, 
                        orientation);
                    // Conversely, as our connections are bidirectional, we must set up
                    // also the reciprocal connection from the neighbor to this node. 
                    GraphNode neighborNode = GraphResource.Nodes[neighborArrayPosition];
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
                        neighborArrayPosition, 
                        nodeArrayPosition, 
                        1, 
                        reciprocalOrientation);
                }
                
                // Once the node is created and configured, we add it to the graph.
                GraphResource.Nodes.Add(nodeArrayPosition, node);
            }
        }
    }
    
    public override void _EnterTree()
    {
        _cleanAreaChecker = new CleanAreaChecker(
            (Mathf.Min(CellSize.X, CellSize.Y)/2)-10f, 
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
        foreach (KeyValuePair<Vector2I, GraphNode> nodeEntry in GraphResource.Nodes)
        {
            Vector2 cellPosition = NodeGlobalPosition(nodeEntry.Key);
            GraphNode node = nodeEntry.Value;
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
                }
            }
        }
    }
}