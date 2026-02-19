using System.Collections.Generic;
using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

public abstract partial class PathFinder<T>: Node2D, IPathFinder 
    where T: NodeRecord, new()
{
    [ExportCategory("DEBUG:")]
    [Export] private bool ShowGizmos { get; set; } = true;
    [Export] private uint ExploredNodeGizmoRadius { get; set; }= 10;
    [Export] private Color ExploredNodeColor { get; set; } = Colors.Black;
    [Export] private uint PathNodeGizmoRadius { get; set; }= 10;
    [Export] private Color PathNodeColor { get; set; } = Colors.GreenYellow;
    [Export] private Vector2 GizmoTextOffset { get; set; }= new(10, 10);
    [Export] private Color TextColor { get; set; } = Colors.White;
    
    /// <summary>
    /// Graph modeling the environment.
    /// </summary>
    public MapGraph Graph { get; set; }
    
    /// <summary>
    /// Agent starting node.
    /// </summary>
    protected PositionNode CurrentStartNode;
    
    /// <summary>
    /// Dictionary containing nodes and their corresponding recorded data after the
    /// exploration process.
    /// </summary>
    public Dictionary<PositionNode, T> ClosedDict;
    
    /// <summary>
    /// The currently found path across the graph to the target node.
    /// </summary>
    private Path _foundPath;
    
    public override void _Ready()
    {
        ClosedDict = new();
    }
    
    /// <summary>
    /// Finds a path from the current position to the specified target position
    /// within the provided graph using Dijkstra's algorithm.
    /// </summary>
    /// <param name="targetPosition">
    /// The target position on the map to which the path is to be found.
    /// </param>
    /// <returns>
    /// A path object representing the sequence of nodes from the start position
    /// to the target position. Returns null if no valid path exists to the target.
    /// </returns>
    public abstract Path FindPath(Vector2 targetPosition);
    
    /// <summary>
    /// Constructs a path from the start node to the target node by traversing
    /// the closed dictionary in reverse and building a sequence of connections.
    /// </summary>
    /// <param name="closedDict">
    /// A dictionary containing nodes and their corresponding recorded data,
    /// used to trace the path from the target node to the start node.
    /// </param>
    /// <param name="startNode">
    /// The initial node where the pathfinding process starts.
    /// </param>
    /// <param name="targetNode">
    /// The final node where the pathfinding process ends.
    /// </param>
    /// <returns>
    /// A Path object representing the ordered sequence of positions
    /// from the start node to the target node.
    /// </returns>
    public Path BuildPath(
        Dictionary<PositionNode, T> closedDict,
        PositionNode startNode,
        PositionNode targetNode)
    {
        List<GraphConnection> path = new();
        T pointer = closedDict[targetNode];

        // Traverse the closedDict backwards to build the path from target to start.
        while (pointer.Node != startNode)
        {
            path.Add(pointer.Connection);
            PositionNode endA = Graph.GetNodeById(pointer.Connection.StartNodeId);
            pointer = closedDict[endA];
        }

        // As Connections have been stored from target to start order, we must reverse
        // the list to get the path from start to target.
        path.Reverse();
        
        // Now that the Connection list is in correct order, we can build the Path
        // following Connections and taking note of their EndNode positions.
        _foundPath = new Path();
        _foundPath.Loop = false;
        _foundPath.ShowGizmos = ShowGizmos;
        foreach (GraphConnection connection in path)
        {
            PositionNode endB = Graph.GetNodeById(connection.EndNodeId);
            _foundPath.TargetPositions.Add(endB.Position);
        }
        
        return _foundPath;
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

        // Draw explored nodes.
        foreach (PositionNode exploredNode in ClosedDict.Keys)  
        {
            DrawCircle(
                ToLocal(exploredNode.Position), 
                ExploredNodeGizmoRadius, 
                ExploredNodeColor, 
                filled: true);
            
            // Mark every node with the smallest cost to get there from the start node
            // and the local orientation of the connection to get there.
            Vector2 gizmoBorder = new Vector2(
                ExploredNodeGizmoRadius, 
                ExploredNodeGizmoRadius);
            Vector2 textPosition = exploredNode.Position - gizmoBorder - 
                                   GizmoTextOffset;
            if (exploredNode == CurrentStartNode)
            {
                // I must cancel the draw transform matrix just before drawing the text
                // to avoid the text rotating when its node rotates along the agent it is 
                // attached to.
                DrawSetTransformMatrix(GlobalTransform.AffineInverse());
                DrawString(
                    ThemeDB.FallbackFont, 
                    textPosition, 
                    "Start", 
                    modulate: TextColor);
                // Once the text is drawn, restore the original transform matrix.
                DrawSetTransformMatrix(Transform2D.Identity);
            }
            else
            {
                PositionNode fromNode = 
                    Graph.GetNodeById(ClosedDict[exploredNode].Connection.StartNodeId);
                Vector2 relativePosition = exploredNode.Position - fromNode.Position;
                // Connection orientation from the receiving node (the explored node)
                // perspective.
                string connectionOrientation;
                if (Mathf.IsEqualApprox(relativePosition.X, 0f))
                {
                    connectionOrientation = relativePosition.Y > 0f ? "N" : "S";
                }
                else
                {
                    connectionOrientation = relativePosition.X > 0f ? "W" : "E";
                }
                string nodeInfoText = 
                    $"{connectionOrientation}{ClosedDict[exploredNode].CostSoFar}";
                // I must cancel the draw transform matrix just before drawing the text
                // to avoid the text rotating when its node rotates along the agent it is 
                // attached to.
                DrawSetTransformMatrix(GlobalTransform.AffineInverse());
                DrawString(
                    ThemeDB.FallbackFont, 
                    textPosition, 
                    nodeInfoText, 
                    modulate: TextColor);
                // Once the text is drawn, restore the original transform matrix.
                DrawSetTransformMatrix(Transform2D.Identity);
            }
        }
        
        if (_foundPath == null) return;
        
        // Draw found path.
        foreach (Vector2 position in _foundPath.TargetPositions)
        {
            DrawCircle(
                ToLocal(position), 
                PathNodeGizmoRadius, 
                PathNodeColor, 
                filled: true);
        }
    }
}