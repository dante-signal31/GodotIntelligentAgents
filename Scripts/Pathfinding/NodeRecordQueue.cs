using System.Collections.Generic;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Represents a queue-based collection for managing <see cref="NodeRecord"/> objects,
/// specifically designed for use in pathfinding algorithms. This class maintains a
/// dictionary for quick access to records by their associated <see cref="PositionNode"/>
/// and ensures that duplicate nodes are not added to the collection.
/// </summary>
public class NodeRecordQueue: INodeRecordCollection<NodeRecord>
{
    // Needed to keep ordered the NodeRecords pending to be explored. The order is
    // first-found-first-to-be-explored.
    private readonly Queue<NodeRecord> _queue = new();

    // Needed to keep track of the nodes already discovered.
    private readonly Dictionary<IPositionNode, NodeRecord> _nodeRecordDict = new();

    public int Count => _nodeRecordDict.Count;

    public void Clear()
    {
        _queue.Clear();
        _nodeRecordDict.Clear();
    }

    public bool Contains(IPositionNode node) => _nodeRecordDict.ContainsKey(node);

    public void Add(NodeRecord record)
    {
        // If the node was already discovered before, then do nothing. If you enqueue
        // the discovered nodes again, you would end up with loops.
        if (_nodeRecordDict.ContainsKey(record.Node)) return;

        // Standard case.
        _queue.Enqueue(record);
        _nodeRecordDict[record.Node] = record;
    }

    public NodeRecord this[IPositionNode node]
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