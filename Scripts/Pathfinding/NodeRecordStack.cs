using System.Collections.Generic;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Represents a stack-based collection of node records used in pathfinding algorithms.
/// This stack ensures that each node is only stored once to avoid loops during traversal.
/// </summary>
public class NodeRecordStack: INodeRecordCollection<NodeRecord>
{
    // Needed to keep ordered the NodeRecords pending to be explored. The order is
    // last-found-first-to-be-explored.
    private readonly Stack<NodeRecord> _stack = new ();
        
    // Needed to keep track of the nodes already discovered.
    private readonly Dictionary<IPositionNode, NodeRecord> _nodeRecordDict = new ();
        
    public int Count => _nodeRecordDict.Count;
        
    public void Clear()
    {
        _stack.Clear();
        _nodeRecordDict.Clear();
    }
        
    public bool Contains(IPositionNode node) => _nodeRecordDict.ContainsKey(node);
        
    public void Add(NodeRecord record)
    {
        // If the node was already discovered before, then do nothing. If you stack
        // the discovered nodes again, you would end up with loops.
        if (_nodeRecordDict.ContainsKey(record.Node)) return;
            
        // Standard case.
        _stack.Push(record);
        _nodeRecordDict[record.Node] = record;
    }

    public NodeRecord this[IPositionNode node]
    {
        get => _nodeRecordDict[node];
        set => _nodeRecordDict[node] = value;
    }
        
    public NodeRecord Get()
    {
        if (_stack.Count == 0) return null;
        NodeRecord recoveredNodeRecord = _stack.Pop();
        return recoveredNodeRecord;
    }
}