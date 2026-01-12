using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Implements the A* (A-Star) pathfinding algorithm, which is used to calculate
/// the shortest path between a start and target position in a graph. It calculates the
/// least-cost path from a starting position to a target position by exploring nodes
/// systematically based on their estimated cost to get the goal from them.
/// </summary>
[Tool]
public partial class AStarPathFinder: HeuristicPathFinder<AStarNodeRecord>
{
    /// <summary>
    /// Represents a prioritized set of nodes for the A* pathfinding algorithm.
    /// This class manages open nodes, ordering them based on their total estimated cost
    /// to reach the target, enabling efficient retrieval of the next node to explore.
    /// </summary>
    protected class AStarPrioritizedNodeRecordSet: PrioritizedNodeRecordSet
    {
        public override void Add(AStarNodeRecord record)
        {
            if (Contains(record.Node))
            {
                RefreshRecord(record);
            }
            else
            {
                PriorityQueue.Enqueue(record, record.TotalEstimatedCostToTarget);
                NodeRecordDict[record.Node] = record;
            }
        }

        public override void RefreshRecord(AStarNodeRecord nodeRecord)
        {
            // Rebuild the PriorityQueue.
            var tempSet = new HashSet<AStarNodeRecord> { nodeRecord };
            while (PriorityQueue.Count > 0)
            {
                var item = PriorityQueue.Dequeue();
                if (item.Node == nodeRecord.Node) continue;
                tempSet.Add(item);
            }
            PriorityQueue.Clear();
            foreach (AStarNodeRecord record in tempSet)
            {
                PriorityQueue.Enqueue(record, record.TotalEstimatedCostToTarget);
            }
        }
    }

    private IAStarHeuristic _heuristic;

    public override void _Ready()
    {
        base._Ready();
        if (Engine.IsEditorHint()) return;
        _heuristic = this.FindChild<IAStarHeuristic>();
    }
    
    public override Path FindPath(Vector2 targetPosition)
    {
        // Nodes not fully explored yet, ordered by the estimated cost to get the target
        // through them.
        AStarPrioritizedNodeRecordSet openRecordSet = new ();
        // Nodes already fully explored. We use a dictionary to keep track of the
        // information gathered from each node, including the connection to get there,
        // while exploring the graph.
        ClosedDict.Clear();
        
        // Get graph nodes associated with the start and target positions. 
        CurrentStartNode = Graph.GetNodeAtPosition(GlobalPosition);
        PositionNode targetNode = Graph.GetNodeAtPosition(targetPosition);
        
        // You get to the start node from nowhere (null) and at no cost (0).
        AStarNodeRecord starNodeRecord = new (){
            Node = CurrentStartNode,
            Connection = null,
            CostSoFar = 0,
            // For A* to guarantee the shortest path, the heuristic must never
            // overestimate the actual cost (it must be "admissible"). E.g., the heuristic
            // to the target should not be higher than the actual cost to get there.
            //
            // For instance, If you are using Euclidean Distance as a heuristic while your
            // connection costs (graphConnection.Cost) are significantly lower (e.g.,
            // smaller than the pixel distance), the heuristic becomes too "aggressive."
            // Consequence: The algorithm relies so heavily on the direct distance to
            // the target (h) that it ignores terrain costs (g), because the h-value
            // outweighs the accumulated cost.
            //
            // Solution: Ensure graph connection costs are on the same scale as the
            // used heuristic (Euclidean distance in the example).
            TotalEstimatedCostToTarget = _heuristic.EstimateCostToTarget(
                CurrentStartNode.Position,
                targetPosition)
        };
        openRecordSet.Add(starNodeRecord); 
        
        // Loop until we reach the target node or no more nodes are available to explore.
        AStarNodeRecord current = AStarNodeRecord.AStarNodeRecordNull;
        while (openRecordSet.Count > 0)
        {
            // Explore prioritizing the node with the lowest total estimated cost to get
            // the target.
            current = openRecordSet.Get();

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
                // Calculate the cost to reach the end node from the current node.
                float endNodeCost = current.CostSoFar + graphConnection.Cost;

                AStarNodeRecord endNodeRecord;
                // In Dijkstra If that connection leaded to a node fully explored, we
                // skipped it because no better path could be found to any closed node.
                // Whereas in A* you can have closed a node under a wrong estimation.
                // That's why, in A*, you must check if you've just found a better path to
                // an already closed node.
                if (ClosedDict.ContainsKey(endNode))
                {
                    endNodeRecord = ClosedDict[endNode];

                    // No better path to this node, so skip it.
                    if (endNodeRecord.CostSoFar <= endNodeCost) continue;

                    // Otherwise, we must asses this node again to check if it's more 
                    // promising than the previous time. So, we must remove it from 
                    // the ClosedDict and add it back to the OpenSet.
                    ClosedDict.Remove(endNode);

                    // We could call the heuristic again, but it will return the same
                    // value as the last time. What has changed is the CostSoFar part,
                    // so we remove the old CostSoFar from the total to add the new value.
                    float estimatedCostToTarget =
                        endNodeRecord.TotalEstimatedCostToTarget -
                        endNodeRecord.CostSoFar;
                    endNodeRecord.CostSoFar = endNodeCost;
                    endNodeRecord.TotalEstimatedCostToTarget =
                        estimatedCostToTarget + endNodeCost;
                    endNodeRecord.Connection = graphConnection;

                    // Add the node to the openSet to assess it fully again.
                    openRecordSet.Add(endNodeRecord);
                }
                // OK, we've just found a node that is still being assessed in the open
                // list.
                else if (openRecordSet.Contains(endNode))
                {
                    endNodeRecord = openRecordSet[endNode];
                    // If the end node is already in the open set, but with a lower cost,
                    // it means that we are NOT found a better path to get to it. So skip
                    // it.
                    if (endNodeRecord.CostSoFar <= endNodeCost) continue;
                    // Otherwise, update the record with the lower cost and the connection
                    // to get there with that lower cost.
                    //
                    // We could call the heuristic again, but it will return the same
                    // value as the last time. What has changed is the CostSoFar part,
                    // so we remove the old CostSoFar from the total to add the new value.
                    float estimatedCostToTarget =
                        endNodeRecord.TotalEstimatedCostToTarget -
                        endNodeRecord.CostSoFar;
                    endNodeRecord.TotalEstimatedCostToTarget = estimatedCostToTarget +
                        endNodeCost;
                    endNodeRecord.CostSoFar = endNodeCost;
                    endNodeRecord.Connection = graphConnection;
                    openRecordSet.RefreshRecord(endNodeRecord);
                }
                else
                {
                    // If the open set does not contain that node, it means we have
                    // discovered a new node. So include it in the open set to explore it 
                    // further later.
                    endNodeRecord = new AStarNodeRecord
                    {
                        Node = endNode,
                        Connection = graphConnection,
                        CostSoFar = endNodeCost,
                        TotalEstimatedCostToTarget =
                            endNodeCost + _heuristic.EstimateCostToTarget(
                                endNode.Position, 
                                targetPosition)
                    };
                    openRecordSet.Add(endNodeRecord);
                }
            }

            // As we've finished looking at the connections of the current node, mark it
            // as fully explored, including it in the closed list.
            ClosedDict[current.Node] = current;
        }

        // If we get here and the current record does not point to the targetNode, then
        // we've fully explored the graph without finding a valid path to get the target.
        if (current?.Node == null || current.Node != targetNode) return null;
    
        // As we've got the target node, analyze the closedDict to follow back connections
        // from the target node to start node to build the path.
        Path foundPath = BuildPath(ClosedDict, CurrentStartNode, targetNode);
        return foundPath;
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new();
        
        _heuristic= this.FindChild<IAStarHeuristic>();
        if (_heuristic== null)
        {
            warnings.Add("This node needs a child of type IAStartHeuristic to work.");
        }
        
        return warnings.ToArray();
    }
}