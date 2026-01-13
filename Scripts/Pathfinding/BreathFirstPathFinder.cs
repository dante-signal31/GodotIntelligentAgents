using System.Collections.Generic;
using System.Linq;
using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Implements the Breadth-First Search (BFS) algorithm for pathfinding.
/// </summary>
[Tool]
public partial class BreathFirstPathFinder: NotInformedPathFinder
{
    private class NodeRecordQueue: INodeRecordCollection<NodeRecord>
    {
        // Needed to keep ordered the NodeRecords pending to be explored. The order is
        // first-found-first-to-be-explored.
        private readonly Queue<NodeRecord> _queue = new ();
        
        // Needed to keep track of the nodes still pending to be explored and to quickly
        // get their respective records.
        private readonly Dictionary<PositionNode, NodeRecord> _nodeRecordDict = new ();
        
        public int Count => _nodeRecordDict.Count;
        public bool Contains(PositionNode node) => _nodeRecordDict.ContainsKey(node);
        
        public void Add(NodeRecord record)
        {
            // If the queue contains the node already (so it is present at the dict),
            // then do nothing.
            if (_nodeRecordDict.ContainsKey(record.Node)) return;
            
            // Standard case.
            _queue.Enqueue(record);
            _nodeRecordDict[record.Node] = record;
        }
        
        public void Remove(NodeRecord record)
        {
            _nodeRecordDict.Remove(record.Node);
            
            // Rebuild the queue without the removed record
            var tempQueue = new Queue<NodeRecord>();
            while (_queue.Count > 0)
            {
                var currentRecord = _queue.Dequeue();
                if (currentRecord.Node != record.Node)
                {
                    tempQueue.Enqueue(currentRecord);
                }
            }
            
            // Replace the old queue with the rebuilt one
            while (tempQueue.Count > 0)
            {
                _queue.Enqueue(tempQueue.Dequeue());
            }
        }

        // Not used in BFS.
        public void RefreshRecord(NodeRecord record) { }

        public NodeRecord this[PositionNode node]
        {
            get => _nodeRecordDict[node];
            set => _nodeRecordDict[node] = value;
        }
        
        public NodeRecord Get()
        {
            if (_queue.Count == 0) return null;
            NodeRecord recoveredNodeRecord = _queue.Dequeue();
            _nodeRecordDict.Remove(recoveredNodeRecord.Node);
            return recoveredNodeRecord;
        }
    }
    
    public override Path FindPath(Vector2 targetPosition)
    {
        // Actually, a Breath-First pathfinder only guarantees to find the shortest path
        // if every connection has the same cost. With scenes with variable costs, the
        // algorithm will ignore that some connections are cheaper than others. I.e., a 
        // Breth-First pathfinder will always find the path with fewer nodes, not the
        // cost-cheaper one.
        return FindPath<NodeRecordQueue>(targetPosition);
    }
}