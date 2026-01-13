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
    private class NodeRecordStack: INodeRecordCollection<NodeRecord>
    {
        // Needed to keep ordered the NodeRecords pending to be explored. The order is
        // last-found-first-to-be-explored.
        private readonly Stack<NodeRecord> _stack = new ();
        
        // Needed to keep track of the nodes still pending to be explored and to quickly
        // get their respective records.
        private readonly Dictionary<PositionNode, NodeRecord> _nodeRecordDict = new ();
        
        public int Count => _nodeRecordDict.Count;
        public bool Contains(PositionNode node) => _nodeRecordDict.ContainsKey(node);
        
        public void Add(NodeRecord record)
        {
            // If the stack contains the node already (so it is present at the dict),
            // then do nothing.
            if (_nodeRecordDict.ContainsKey(record.Node)) return;
            
            // Standard case.
            _stack.Push(record);
            _nodeRecordDict[record.Node] = record;
        }
        
        public void Remove(NodeRecord record)
        {
            _nodeRecordDict.Remove(record.Node);
            
            
            // Rebuild the stack without the removed record
            var tempStack = new Stack<NodeRecord>();
            while (_stack.Count > 0)
            {
                var currentRecord = _stack.Pop();
                if (currentRecord.Node != record.Node)
                {
                    tempStack.Push(currentRecord);
                }
            }

            // Replace the old queue with the rebuilt one
            while (tempStack.Count > 0)
            {
                _stack.Push(tempStack.Pop());
            }
        }

        // Not used in DFS.
        public void RefreshRecord(NodeRecord record) { }

        public NodeRecord this[PositionNode node]
        {
            get => _nodeRecordDict[node];
            set => _nodeRecordDict[node] = value;
        }
        
        public NodeRecord Get()
        {
            if (_stack.Count == 0) return null;
            NodeRecord recoveredNodeRecord = _stack.Pop();
            _nodeRecordDict.Remove(recoveredNodeRecord.Node);
            return recoveredNodeRecord;
        }
    }
    
    public override Path FindPath(Vector2 targetPosition)
    {
        // Depth-first search does NOT guarantee to find the shortest path. Actually, when
        // it finds a path, it's likely to be suboptimal.
        return FindPath<NodeRecordStack>(targetPosition);
    }
}