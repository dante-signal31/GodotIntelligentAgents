using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Implements a depth-first search (DFS) pathfinding algorithm for navigating a
/// graph structure.
/// </summary>
[Tool]
public partial class DepthFirstPathFinder: NotInformedPathFinder
{
    private class NodeStack: INodeCollection<NodeRecord>
    {
        // Needed to keep ordered the NodeRecords pending to be explored. The order is
        // last-found-first-to-be-explored.
        private readonly Stack<NodeRecord> _stack = new ();
        
        // Needed to keep track of the nodes still pending to be explored and to quickly
        // get their respective records.
        private readonly Dictionary<GraphNode, NodeRecord> _nodeRecordDict = new ();
        
        public int Count => _nodeRecordDict.Count;
        public bool Contains(GraphNode node) => _nodeRecordDict.ContainsKey(node);
        
        public void Add(NodeRecord record)
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
        
        public NodeRecord Get()
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
        return FindPath<NodeStack>(targetPosition);
    }
}