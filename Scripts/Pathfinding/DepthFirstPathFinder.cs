using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

[Tool]
public partial class DepthFirstPathFinder: PathFinder<NodeRecord>
{
    private class NodeStack
    {
        // Needed to keep ordered the NodeRecords pending to be explored. The order is
        // last-found-first-to-be-explored.
        private readonly Stack<NodeRecord> _stack = new ();
        
        // Needed to keep track of the nodes still pending to be explored and to quickly
        // get their respective records.
        private readonly Dictionary<GraphNode, NodeRecord> _nodeRecordDict = new ();
        
        public int Count => _nodeRecordDict.Count;
        public bool Contains(GraphNode node) => _nodeRecordDict.ContainsKey(node);
        
        public void Enqueue(NodeRecord record)
        {
            // I cannot use Contains() property because that only checks the dict and I 
            // need to find inconsistencies in the queue.
            bool nodeAlreadyInQueue =
                _stack.Any(queuedRecord => queuedRecord.Node == record.Node);
            
            // If the queue contains the node already, and it is active (so it is present
            // at the dict), then do nothing.
            if (nodeAlreadyInQueue && _nodeRecordDict.ContainsKey(record.Node)) 
                return;
            
            // If the node is not present in the dictionary, then we are reentering a 
            // previously removed node, so we must include it in the dict again.
            if (nodeAlreadyInQueue && !_nodeRecordDict.ContainsKey(record.Node))
            {
                _nodeRecordDict[record.Node] = record;
                return;
            }
            
            // Standard case.
            _stack.Push(record);
            _nodeRecordDict[record.Node] = record;
        }
        
        public void Remove(NodeRecord record)
        {
            _nodeRecordDict.Remove(record.Node);
        }
        
        public NodeRecord this[GraphNode node]
        {
            get => _nodeRecordDict[node];
            set => _nodeRecordDict[node] = value;
        }
        
        public NodeRecord Dequeue()
        {
            bool validNodeRecordFound = false;
            NodeRecord recoveredNodeRecord = new();
            
            do
            {
                if (_stack.Count == 0) break;
                recoveredNodeRecord = _stack.Pop();
                // Note: .NET's Queue doesn't support efficient removal by value.
                // We only remove it from the dictionary when Remove method() is used. So,
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

            if (!validNodeRecordFound) return null;
            return recoveredNodeRecord;
        }
    }
    
    public override Path FindPath(Vector2 targetPosition)
    {
        // Nodes not fully explored yet, ordered as they were found.
        NodeStack openStack = new();
        
        // Nodes already fully explored. We use a dictionary to keep track of the
        // information gathered from each node, including the connection to get there,
        // while exploring the graph.
        ClosedDict.Clear();
        
        // Get graph nodes associated with the start and target positions. 
        CurrentStartNode = Graph.GetNodeAtPosition(GlobalPosition);
        GraphNode targetNode = Graph.GetNodeAtPosition(targetPosition);
        
        // You get to the start node from nowhere (null) and at no cost (0).
        var startRecord = new NodeRecord
        {
            Node = CurrentStartNode,
            Connection = null,
            CostSoFar = 0,
        };
        openStack.Enqueue(startRecord);
        
        // Loop until we reach the target node or no more nodes are available to explore.
        NodeRecord current = NodeRecord.NodeRecordNull;
        while (openStack.Count > 0)
        {
            // Explore the pending node that was first discovered.
            current = openStack.Dequeue();
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
                if (openStack.Contains(endNode))
                {
                    endNodeRecord = openStack[endNode];
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
                    openStack.Enqueue(endNodeRecord);
                }
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
        Path foundPath = BuildPath(Graph, ClosedDict, CurrentStartNode, targetNode);
        return foundPath;
    }
}