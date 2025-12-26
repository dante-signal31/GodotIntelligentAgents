using System.Collections.Generic;
using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

[Tool]
public abstract partial class PathFinder<T>: Node2D where T: NodeRecord, new()
{
    /// <summary>
    /// A collection of node records with priority-based access for use in pathfinding
    /// algorithms like Dijkstra or AStar.
    /// </summary>
    /// <remarks>
    /// This class maintains a set of node records along with their associated costs for
    /// traversing a graph. It provides functionality to add and remove nodes, check for
    /// node existence, and retrieve the node with the lowest cost value.
    /// </remarks>
    protected abstract class PrioritizedNodeSet
    {
        // Needed to keep ordered by cost the NodeRecords of the node pending to be
        // explored.
        public readonly PriorityQueue<T, float> PriorityQueue = new ();
        // Needed to keep track of the nodes still pending to be explored and to quickly
        // get their respective records.
        public readonly Dictionary<GraphNode, T> NodeRecordDict = new ();
        
        public int Count => NodeRecordDict.Count;
        public bool Contains(GraphNode node) => NodeRecordDict.ContainsKey(node);
        
        
        public static PrioritizedNodeSet operator -(
            PrioritizedNodeSet set, 
            T record)
        {
            set.NodeRecordDict.Remove(record.Node);
            return set;
        }
        
        public T this[GraphNode node]
        {
            get => NodeRecordDict[node];
            set => NodeRecordDict[node] = value;
        }

        /// <summary>
        /// Extracts and removes the node record with the lowest cost value
        /// from the prioritized set. 
        /// </summary>
        /// <returns>
        /// The node record with the lowest cost value or a null if there are
        /// no valid records available in the set.
        /// </returns>
        public T ExtractLowestCostNodeRecord()
        {
            bool validNodeRecordFound = false;
            T recoveredNodeRecord = new();
            
            do
            {
                if (PriorityQueue.Count == 0) break;
                recoveredNodeRecord = PriorityQueue.Dequeue();
                // Note: .NET's PriorityQueue doesn't support efficient removal by value.
                // We only remove it from the dictionary when operator -= is used. So,
                // when dequeuing, we must check if the node still exists in
                // _nodeRecordDict before processing. If it doesn't, it means that we
                // have just dequeued a node that was actually removed from the set, so
                // we skip it and dequeue the next element.
                if (NodeRecordDict.ContainsKey(recoveredNodeRecord.Node))
                {
                    validNodeRecordFound = true;
                    // Dequeue actually removes the extracted element from the queue, so
                    // we must remove it from the internal dictionary to keep coherence.
                    NodeRecordDict.Remove(recoveredNodeRecord.Node);
                }
                    
            } while (!validNodeRecordFound);

            if (!validNodeRecordFound) return null;
            return recoveredNodeRecord;
        }
    }
    
    [ExportCategory("DEBUG:")]
    [Export] private bool ShowGizmos { get; set; } = true;
    [Export] private uint ExploredNodeGizmoRadius { get; set; }= 10;
    [Export] private Color ExploredNodeColor { get; set; } = Colors.Black;
    [Export] private uint PathNodeGizmoRadius { get; set; }= 10;
    [Export] private Color PathNodeColor { get; set; } = Colors.GreenYellow;
    [Export] private Vector2 GizmoTextOffset { get; set; }= new(10, 10);
    [Export] private Color TextColor { get; set; } = Colors.White;

    public MapGraph Graph { get; set; }
    protected Dictionary<GraphNode, T> ClosedDict;
    protected GraphNode CurrentStartNode;
    protected Path FoundPath;

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
    /// <param name="graph">
    /// The graph containing the nodes and connections used for pathfinding.
    /// </param>
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
    protected Path BuildPath(
        MapGraph graph,
        Dictionary<GraphNode, T> closedDict,
        GraphNode startNode,
        GraphNode targetNode)
    {
        List<GraphConnection> path = new();
        T pointer = closedDict[targetNode];

        // Traverse the closedDict backwards to build the path from target to start.
        while (pointer.Node != startNode)
        {
            path.Add(pointer.Connection);
            GraphNode endA = graph.Nodes[pointer.Connection.StartNodeKey];
            pointer = closedDict[endA];
        }

        // As Connections have been stored from target to start order, we must reverse
        // the list to get the path from start to target.
        path.Reverse();
        
        // Now that the Connection list is in correct order, we can build the Path
        // following Connections and taking note of their EndNode positions.
        FoundPath = new Path();
        FoundPath.Loop = false;
        FoundPath.ShowGizmos = ShowGizmos;
        foreach (GraphConnection connection in path)
        {
            GraphNode endB = graph.Nodes[connection.EndNodeKey];
            FoundPath.TargetPositions.Add(endB.Position);
        }
        
        return FoundPath;
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
        foreach (GraphNode exploredNode in ClosedDict.Keys)  
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
                Vector2I fromNodeKey = ClosedDict[exploredNode].Connection.StartNodeKey;
                GraphNode fromNode = Graph.Nodes[fromNodeKey];
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
        
        if (FoundPath == null) return;
        
        // Draw found path.
        foreach (Vector2 position in FoundPath.TargetPositions)
        {
            DrawCircle(
                ToLocal(position), 
                PathNodeGizmoRadius, 
                PathNodeColor, 
                filled: true);
        }
    }
}