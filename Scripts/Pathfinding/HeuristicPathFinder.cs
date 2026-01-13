using System.Collections.Generic;
using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Classes implementing inheriting this class are responsible for navigating a graph
/// structure to find a path to a target position.
/// <remarks>
/// The heuristic pathfinders are informed searchers that use heuristics to
/// estimate which graph branches are more promising to get the goal so they can be
/// explored first.
/// </remarks> 
/// </summary>
[Tool]
public abstract partial class HeuristicPathFinder<T>: PathFinder<T>
    where T: NodeRecord, new()
{
    /// <summary>
    /// A collection of node records with priority-based access for use in pathfinding
    /// algorithms like Dijkstra or AStar.
    /// </summary>
    /// <remarks>
    /// This class maintains a set of node records along with their associated costs for
    /// traversing a graph. It provides functionality to add and remove nodes, check for
    /// node existence, and retrieve the node with the lowest cost value.
    /// </remarks>
    protected abstract class PrioritizedNodeRecordSet: INodeRecordCollection<T>
    {
        // Needed to keep ordered by cost the NodeRecords of the node pending to be
        // explored.
        protected readonly PriorityQueue<T, float> PriorityQueue = new ();
        // Needed to keep track of the nodes still pending to be explored and to quickly
        // get their respective records.
        protected readonly Dictionary<PositionNode, T> NodeRecordDict = new ();
        
        public int Count => NodeRecordDict.Count;
        
        public void Clear()
        {
            PriorityQueue.Clear();
            NodeRecordDict.Clear();
        }
        
        public bool Contains(PositionNode node) => NodeRecordDict.ContainsKey(node);
        

        public abstract void Add(T record);
        
        public void Remove(T record)
        {
            NodeRecordDict.Remove(record.Node);
        }

        /// <summary>
        /// Indexer providing access to a node's corresponding record in the prioritized
        /// node set.
        /// </summary>
        /// <param name="node">The graph node for which the corresponding record is
        /// requested or set.</param>
        /// <returns>
        /// The <typeparamref name="T"/> instance associated with the specified graph
        /// node.
        /// </returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when attempting to retrieve a record for a node that does not exist
        /// in the collection.
        /// </exception>
        public T this[PositionNode node]
        {
            get => NodeRecordDict[node];
            set => NodeRecordDict[node] = value;
        }

        /// <summary>
        /// Extracts and removes the node record with the lowest cost value
        /// from the prioritized set. 
        /// </summary>
        /// <returns>
        /// The node record with the lowest cost value or a null if there are
        /// no valid records available in the set.
        /// </returns>
        public T Get()
        {
            while (PriorityQueue.Count > 0)
            {
                 T recoveredNodeRecord = PriorityQueue.Dequeue();
                // Note: .NET's PriorityQueue doesn't support efficient removal by value.
                // So we do "lazy removal". That means that we can have duplicates in the 
                // queue. So, we use _nodeRecordDict to find valid queue records.
                // When dequeuing, we must check if the node still exists in
                // _nodeRecordDict before processing. If it doesn't, it means that we
                // have just dequeued a node that was actually lazily removed from the
                // set, so we skip it and dequeue the next element.
                if (NodeRecordDict.TryGetValue(
                        recoveredNodeRecord.Node, out var current) &&
                    current == recoveredNodeRecord)
                {
                    // Dequeue actually removes the extracted element from the queue, so
                    // we must remove it from the internal dictionary to keep coherence.
                    NodeRecordDict.Remove(recoveredNodeRecord.Node);
                    return recoveredNodeRecord;
                }
            }
            return null;
        }

        /// <summary>
        /// <p> Regenerate queue order.</p>
        /// <remarks> Queue reorders when you include a new element, but not when you
        /// update an existing one priority. So, use this method whenever you change
        /// any of the existing record priorities. </remarks>
        /// </summary>
        public abstract void RefreshRecord(T record);
    }
}