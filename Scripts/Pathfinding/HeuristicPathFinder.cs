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
    protected abstract class PrioritizedNodeSet: INodeCollection<T>
    {
        // Needed to keep ordered by cost the NodeRecords of the node pending to be
        // explored.
        public readonly PriorityQueue<T, float> PriorityQueue = new ();
        // Needed to keep track of the nodes still pending to be explored and to quickly
        // get their respective records.
        public readonly Dictionary<GraphNode, T> NodeRecordDict = new ();
        
        public int Count => NodeRecordDict.Count;
        public bool Contains(GraphNode node) => NodeRecordDict.ContainsKey(node);

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
        public T this[GraphNode node]
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
            bool validNodeRecordFound = false;
            T recoveredNodeRecord = new();
            
            do
            {
                if (PriorityQueue.Count == 0) break;
                recoveredNodeRecord = PriorityQueue.Dequeue();
                // Note: .NET's PriorityQueue doesn't support efficient removal by value.
                // We only remove it from the dictionary when operator -= is used. So,
                // when dequeuing, we must check if the node still exists in
                // _nodeRecordDict before processing. If it doesn't, it means that we
                // have just dequeued a node that was actually removed from the set, so
                // we skip it and dequeue the next element.
                if (NodeRecordDict.ContainsKey(recoveredNodeRecord.Node))
                {
                    validNodeRecordFound = true;
                    // Dequeue actually removes the extracted element from the queue, so
                    // we must remove it from the internal dictionary to keep coherence.
                    NodeRecordDict.Remove(recoveredNodeRecord.Node);
                }
                    
            } while (!validNodeRecordFound);

            if (!validNodeRecordFound) return null;
            return recoveredNodeRecord;
        }
    }
}