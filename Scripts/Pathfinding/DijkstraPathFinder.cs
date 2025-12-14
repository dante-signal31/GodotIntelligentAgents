using System.Collections.Generic;
using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Implements a pathfinding algorithm based on Dijkstra's algorithm to find the shortest
/// path between nodes in a graph. It calculates the least-cost path from a starting
/// position to a target position by exploring nodes systematically based on their cost.
/// </summary>
[Tool]
public partial class DijkstraPathFinder: Node2D, IPathFinder
{
    /// <summary>
    /// Structure needed for the Dijkstra algorithm to keep track of the calculations
    /// to reach every node.
    /// </summary>
    private record struct NodeRecord
    {
        public GraphNode Node;
        public GraphConnection Connection;
        public float CostSoFar;
    }
    
    private static readonly NodeRecord NodeRecordNull = new NodeRecord
    {
        Node = null,
        Connection = null,
        CostSoFar = 0
    };

    /// <summary>
    /// A collection of node records with priority-based access for use in pathfinding
    /// algorithms like Dijkstra.
    /// </summary>
    /// <remarks>
    /// This class maintains a set of node records along with their associated costs for
    /// traversing a graph. It provides functionality to add and remove nodes, check for
    /// node existence, and retrieve the node with the lowest cost value.
    /// </remarks>
    private class PrioritizedNodeSet
    {
        // Needed to keep ordered by cost the NodeRecords of the node pending to be
        // explored.
        private readonly PriorityQueue<NodeRecord, float> _priorityQueue = new ();
        // Needed to keep track of the nodes still pending to be explored and to quickly
        // get their respective records.
        private readonly Dictionary<GraphNode, NodeRecord> _nodeRecordDict = new ();
        
        public int Count => _nodeRecordDict.Count;
        public bool Contains(GraphNode node) => _nodeRecordDict.ContainsKey(node);
        
        public static PrioritizedNodeSet operator +(
            PrioritizedNodeSet set, 
            NodeRecord record)
        {
            set._priorityQueue.Enqueue(record, record.CostSoFar);
            set._nodeRecordDict[record.Node] = record;
            return set;
        }

        public static PrioritizedNodeSet operator -(
            PrioritizedNodeSet set, 
            NodeRecord record)
        {
            set._nodeRecordDict.Remove(record.Node);
            return set;
        }
        
        public NodeRecord this[GraphNode node]
        {
            get => _nodeRecordDict[node];
            set => _nodeRecordDict[node] = value;
        }

        /// <summary>
        /// Extracts and removes the node record with the lowest cost-so-far value
        /// from the prioritized set. 
        /// </summary>
        /// <returns>
        /// The node record with the lowest cost-so-far value or a null-equivalent record
        /// (NodeRecordNull) if there are no valid records available in the set.
        /// </returns>
        public NodeRecord ExtractLowestCostNodeRecord()
        {
            bool validNodeRecordFound = false;
            NodeRecord recoveredNodeRecord = new();
            
            do
            {
                if (_priorityQueue.Count == 0) break;
                recoveredNodeRecord = _priorityQueue.Dequeue();
                // Note: .NET's PriorityQueue doesn't support efficient removal by value.
                // We only remove it from the dictionary when operator -= is used. So,
                // when dequeuing, we must check if the node still exists in
                // _nodeRecordDict before processing. If it doesn't, it means that we
                // have just dequeued a node that was actually removed from the set, so
                // we skip it and dequeue the next element.
                if (_nodeRecordDict.ContainsKey(recoveredNodeRecord.Node))
                {
                    validNodeRecordFound = true;
                    // Dequeue actually removes the extracted element from the queue, so
                    // we must remove it from the internal dictionary to keep coherence.
                    _nodeRecordDict.Remove(recoveredNodeRecord.Node);
                }
                    
            } while (!validNodeRecordFound);

            if (!validNodeRecordFound) return NodeRecordNull;
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
    private Dictionary<GraphNode, NodeRecord> _closedDict;
    private GraphNode _currentStartNode;
    private Path _foundPath;

    public override void _Ready()
    {
        _closedDict = new();
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
    public Path FindPath(Vector2 targetPosition)
    {
        // Nodes not fully explored yet, ordered by the cost to get them from the
        // start node.
        PrioritizedNodeSet openSet = new ();
        // Nodes already fully explored. We use a dictionary to keep track of the
        // information gathered from each node, including the connection to get there,
        // while exploring the graph.
        _closedDict.Clear();
        
        // Get graph nodes associated with the start and target positions. 
        _currentStartNode = Graph.GetNodeAtPosition(GlobalPosition);
        GraphNode targetNode = Graph.GetNodeAtPosition(targetPosition);
        
        // You get to the start node from nowhere (null) and at no cost (0).
        openSet += new NodeRecord {
            Node = _currentStartNode,
            Connection = null,
            CostSoFar = 0,
        };

        // Loop until we reach the target node or no more nodes are available to explore.
        NodeRecord current = NodeRecordNull;
        while (openSet.Count > 0)
        {
            // Explore prioritizing the node with the lowest cost to be reached.
            current = openSet.ExtractLowestCostNodeRecord();
            if (current == NodeRecordNull) break;

            // If we reached the end node, then our exploration is complete.
            if (current.Node == targetNode)
            {
                _closedDict[current.Node] = current;
                break;
            }

            // Get all the connections of the current node and take note of the nodes
            // those connections lead to into the openSet to explore those nodes later.
            foreach (GraphConnection graphConnection in current.Node.Connections.Values)
            {
                // Where does that connection lead us?
                GraphNode endNode = Graph.Nodes[graphConnection.EndNodeKey];
                // If that connection leads to a node fully explored, skip it.
                if (_closedDict.ContainsKey(endNode)) continue;
                // Calculate the cost to reach the end node from the current node.
                float endNodeCost = current.CostSoFar + graphConnection.Cost;

                NodeRecord endNodeRecord;
                if (openSet.Contains(endNode))
                {
                    endNodeRecord = openSet[endNode];
                    // If the end node is already in the open set, but with a lower cost,
                    // it means that we are NOT found a better path to get to it. So skip
                    // it.
                    if (endNodeRecord.CostSoFar <= endNodeCost) continue;
                    // Otherwise, update the record with the lower cost and the connection
                    // to get there with that lower cost.
                    endNodeRecord.CostSoFar = endNodeCost;
                    endNodeRecord.Connection = graphConnection;
                }
                else
                {
                    // If the open set does not contain that node, it means we have
                    // discovered a new node. So include it in the open set to explore it 
                    // further later.
                    endNodeRecord = new NodeRecord
                    {
                        Node = endNode,
                        Connection = graphConnection,
                        CostSoFar = endNodeCost,
                    };
                    openSet += endNodeRecord;
                }
            }
            
            // As we've finished looking at the connections of the current node, mark it
            // as fully explored, including it in the closed list.
            _closedDict[current.Node] = current;
        }
        
        // If we get here and the current record does not point to the targetNode, then
        // we've fully explored the graph without finding a valid path to get the target.
        if (current.Node != targetNode) return null;
        
        // As we've got the target node, analyze the closedDict to follow back connections
        // from the target node to start node to build the path.
        Path foundPath = BuildPath(Graph, _closedDict, _currentStartNode, targetNode);
        return foundPath;
    }

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
    private Path BuildPath(
        MapGraph graph,
        Dictionary<GraphNode, NodeRecord> closedDict,
        GraphNode startNode,
        GraphNode targetNode)
    {
        List<GraphConnection> path = new();
        NodeRecord pointer = closedDict[targetNode];

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
        _foundPath = new Path();
        _foundPath.Loop = false;
        _foundPath.ShowGizmos = ShowGizmos;
        foreach (GraphConnection connection in path)
        {
            GraphNode endB = graph.Nodes[connection.EndNodeKey];
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
        foreach (GraphNode exploredNode in _closedDict.Keys)  
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
            if (exploredNode == _currentStartNode)
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
                Vector2I fromNodeKey = _closedDict[exploredNode].Connection.StartNodeKey;
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
                    $"{connectionOrientation}{_closedDict[exploredNode].CostSoFar}";
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
