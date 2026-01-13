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
        
        // Needed to keep track of the nodes already discovered.
        private readonly Dictionary<PositionNode, NodeRecord> _nodeRecordDict = new ();
        
        public int Count => _nodeRecordDict.Count;
        public bool Contains(PositionNode node) => _nodeRecordDict.ContainsKey(node);
        
        public void Add(NodeRecord record)
        {
            // If the node was already discovered before, then do nothing. If you enqueue
            // the discovered nodes again, you would end up with loops.
            if (_nodeRecordDict.ContainsKey(record.Node)) return;
            
            // Standard case.
            _queue.Enqueue(record);
            _nodeRecordDict[record.Node] = record;
        }
        
        // Not used in BFS.
        public void Remove(NodeRecord record) { }

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