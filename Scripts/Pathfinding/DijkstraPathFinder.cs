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
    protected class DijkstraPrioritizedNodeSet: PrioritizedNodeSet
    {
        public static DijkstraPrioritizedNodeSet operator +(
            DijkstraPrioritizedNodeSet set, 
            NodeRecord record)
        {
            set.PriorityQueue.Enqueue(record, record.CostSoFar);
            set.NodeRecordDict[record.Node] = record;
            return set;
        }
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
    public override Path FindPath(Vector2 targetPosition)
    {
        // Nodes not fully explored yet, ordered by the cost to get them from the
        // start node.
        DijkstraPrioritizedNodeSet openSet = new ();
        // Nodes already fully explored. We use a dictionary to keep track of the
        // information gathered from each node, including the connection to get there,
        // while exploring the graph.
        ClosedDict.Clear();
        
        // Get graph nodes associated with the start and target positions. 
        CurrentStartNode = Graph.GetNodeAtPosition(GlobalPosition);
        GraphNode targetNode = Graph.GetNodeAtPosition(targetPosition);
        
        // You get to the start node from nowhere (null) and at no cost (0).
        openSet += new NodeRecord {
            Node = CurrentStartNode,
            Connection = null,
            CostSoFar = 0,
        };

        // Loop until we reach the target node or no more nodes are available to explore.
        NodeRecord current = NodeRecord.NodeRecordNull;
        while (openSet.Count > 0)
        {
            // Explore prioritizing the node with the lowest cost to be reached.
            current = openSet.ExtractLowestCostNodeRecord();
            if (current == null) break;

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
                GraphNode endNode = Graph.Nodes[graphConnection.EndNodeKey];
                // If that connection leads to a node fully explored, skip it.
                if (ClosedDict.ContainsKey(endNode)) continue;
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
            ClosedDict[current.Node] = current;
        }
        
        // If we get here and the current record does not point to the targetNode, then
        // we've fully explored the graph without finding a valid path to get the target.
        if (current?.Node == null || current.Node != targetNode) return null;
        
        // As we've got the target node, analyze the closedDict to follow back connections
        // from the target node to start node to build the path.
        Path foundPath = BuildPath(Graph, ClosedDict, CurrentStartNode, targetNode);
        return foundPath;
    }
}
