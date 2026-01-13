using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Implements a pathfinding algorithm based on Dijkstra's algorithm to find the shortest
/// path between nodes in a graph. It calculates the least-cost path from a starting
/// position to a target position by exploring nodes systematically based on their cost
/// to be reached from the starting position.
/// </summary>
[Tool]
public partial class DijkstraPathFinder: HeuristicPathFinder<NodeRecord>
{
    /// <summary>
    /// A specialized collection of node records used in the Dijkstra pathfinding
    /// algorithm. This collection manages nodes to be explored in priority order
    /// based on their accumulated path cost, ensuring that the lowest-cost nodes
    /// are processed first.
    /// </summary>
    protected class DijkstraPrioritizedNodeRecordSet: PrioritizedNodeRecordSet
    {
        public override void Add(NodeRecord record)
        {
            PriorityQueue.Enqueue(record, record.CostSoFar);
            NodeRecordDict[record.Node] = record;
        }
    }
    
    private readonly DijkstraPrioritizedNodeRecordSet _openRecordSet = new ();
    
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
    public override Path FindPath(Vector2 targetPosition)
    {
        // Nodes not fully explored yet, ordered by the cost to get them from the
        // start node.
        _openRecordSet.Clear();
        
        // Nodes already fully explored. We use a dictionary to keep track of the
        // information gathered from each node, including the connection to get there,
        // while exploring the graph.
        ClosedDict.Clear();
        
        // Get graph nodes associated with the start and target positions. 
        CurrentStartNode = Graph.GetNodeAtPosition(GlobalPosition);
        PositionNode targetNode = Graph.GetNodeAtPosition(targetPosition);
        
        // You get to the start node from nowhere (null) and at no cost (0).
        NodeRecord startRecord = new NodeRecord
        {
            Node = CurrentStartNode, 
            Connection = null, 
            CostSoFar = 0
        };
        _openRecordSet.Add(startRecord);

        // Loop until we reach the target node or no more nodes are available to explore.
        NodeRecord current = NodeRecord.NodeRecordNull;
        while (_openRecordSet.Count > 0)
        {
            // Explore prioritizing the node with the lowest cost to be reached.
            current = _openRecordSet.Get();
            if (current == null) break;
            
            // If the current record is already in the ClosedDict, but in the ClosedDict
            // it is with a lower cost, it means that the recovered current record it's a
            // duplicated record left behind by the "lazy removal" node record set. So we
            // discard it and recover the next record from _openRecordSet.
            if (ClosedDict.ContainsKey(current.Node) && 
                current.CostSoFar >= ClosedDict[current.Node].CostSoFar) continue;

            // If we reached the end node, then our exploration is complete.
            if (current.Node == targetNode)
            {
                ClosedDict[current.Node] = current;
                break;
            }

            // Get all the connections of the current node and take note of the nodes
            // those connections lead to into the openSet to explore those nodes later.
            foreach (GraphConnection graphConnection in current.Node.Connections.Values)
            {
                // Where does that connection lead us?
                PositionNode endNode = Graph.GetNodeById(graphConnection.EndNodeId);
                // Calculate the cost to reach the end node from the current node.
                float endNodeCost = current.CostSoFar + graphConnection.Cost;
                
                // If that connection leads to a node fully explored at a lower cost,
                // skip it because we are not going to improve the path already discovered
                // to get that node.
                if (ClosedDict.ContainsKey(endNode) && 
                    ClosedDict[endNode].CostSoFar <= endNodeCost) continue;
                
                NodeRecord endNodeRecord;
                if (_openRecordSet.Contains(endNode))
                {
                    endNodeRecord = _openRecordSet[endNode];
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
                }
                // Add the node to the openSet to assess it fully again.
                //
                // If the record already existed in the open set, this new addition
                // will create a duplicate in the PriorityQueue, but as its cost is
                // lower than the old record, this new record will be located before 
                // in the queue. When the old record is finally recovered, its cost
                // will be higher than the one stored in ClosedDict, so it will be
                // discarded. This is a way to "lazily remove" records from the queue
                // with higher costs and avoid the performance penalty of actually
                // removing the old record from the queue (only possible rebuilding
                // the entire queue without the old record) to add the new record.
                _openRecordSet.Add(endNodeRecord);
            }
            
            // As we've finished looking at the connections of the current node, mark it
            // as fully explored, including it in the closed list.
            ClosedDict[current.Node] = current;
        }
        
        // If we get here and the current record does not point to the targetNode, then
        // we've fully explored the graph without finding a valid path to get the target.
        if (current?.Node == null || current.Node != targetNode) return null;
        
        // As we've got the target node, analyze the closedDict to follow back connections
        // from the target node to start node to build the path.
        Path foundPath = BuildPath(ClosedDict, CurrentStartNode, targetNode);
        return foundPath;
    }
}
