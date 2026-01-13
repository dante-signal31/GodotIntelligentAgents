using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Represents a base implementation for pathfinding algorithms
/// that do not rely on heuristic information to guide the search.
/// </summary>
public abstract partial class NotInformedPathFinder: PathFinder<NodeRecord>
{
    /// <summary>
    /// Finds and returns a path to the specified target position.
    /// Depending on the implementation, this uses a specific node collection
    /// type to determine the sequence of nodes to explore during pathfinding.
    /// </summary>
    /// <typeparam name="TN">The type of node collection to use for pathfinding.
    /// Must implement INodeCollection.</typeparam>
    /// <param name="targetPosition">The target position to find a path to.</param>
    /// <returns>A Path object representing the found path from the start position
    /// to the target position, or null if no valid path exists.</returns>
    protected Path FindPath<TN>(Vector2 targetPosition) 
        where TN: INodeRecordCollection<NodeRecord>, new()
    {
        // Nodes not fully explored yet, ordered as they were found.
        TN openQueue = new();
        
        // Nodes already fully explored. We use a dictionary to keep track of the
        // information gathered from each node, including the connection to get there,
        // while exploring the graph.
        ClosedDict.Clear();
        
        // Get graph nodes associated with the start and target positions. 
        CurrentStartNode = Graph.GetNodeAtPosition(GlobalPosition);
        PositionNode targetNode = Graph.GetNodeAtPosition(targetPosition);
        
        // You get to the start node from nowhere (null) and at no cost (0).
        var startRecord = new NodeRecord
        {
            Node = CurrentStartNode,
            Connection = null,
            CostSoFar = 0,
        };
        openQueue.Add(startRecord);
        
        // Loop until we reach the target node or no more nodes are available to explore.
        NodeRecord current = NodeRecord.NodeRecordNull;
        while (openQueue.Count > 0)
        {
            // Explore the pending node that was first discovered.
            current = openQueue.Get();
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
                PositionNode endNode = Graph.GetNodeById(graphConnection.EndNodeId);
                // If that connection leads to an already explored node, skip it.
                if (ClosedDict.ContainsKey(endNode)) continue;
                
                // Otherwise, calculate the cost to reach the end node from the current
                // node.
                float endNodeCost = current.CostSoFar + 1;
                // Include the discovered node in the open set to explore it further
                // later.
                NodeRecord endNodeRecord = new() 
                {
                    Node = endNode,
                    Connection = graphConnection,
                    CostSoFar = endNodeCost,
                };
                openQueue.Add(endNodeRecord);
            }
            
            // As we've finished looking at the connections of the current node, mark it
            // as fully explored, including it in the closed list.
            ClosedDict[current.Node] = current;
        }
        
        // If we get here and the current record does not point to the targetNode, then
        // we've fully explored the graph without finding a valid path to get the target.
        if (current?.Node == null || current.Node != targetNode) 
            return null;
        
        // As we've got the target node, analyze the closedDict to follow back connections
        // from the target node to start node to build the path.
        Path foundPath = BuildPath(ClosedDict, CurrentStartNode, targetNode);
        return foundPath;
    }
}