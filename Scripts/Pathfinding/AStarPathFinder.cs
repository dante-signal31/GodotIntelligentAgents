using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

[Tool]
public partial class AStarPathFinder: PathFinder<AStarNodeRecord>, IPathFinder
{
    private static readonly AStarNodeRecord NodeRecordNull = new AStarNodeRecord
    {
        Node = null,
        Connection = null,
        CostSoFar = 0,
        TotalEstimatedCostToTarget = float.MaxValue
    };
    
    protected class AStarPrioritizedNodeSet: PrioritizedNodeSet
    {
        public static AStarPrioritizedNodeSet operator +(
            AStarPrioritizedNodeSet set, 
            AStarNodeRecord record)
        {
            set.PriorityQueue.Enqueue(record, record.TotalEstimatedCostToTarget);
            set.NodeRecordDict[record.Node] = record;
            return set;
        }
    }

    private IAStarHeuristic _heuristic;

    public override void _Ready()
    {
        base._Ready();
        if (Engine.IsEditorHint()) return;
        _heuristic = this.FindChild<IAStarHeuristic>();
    }

    // /// <summary>
    // /// Heuristic to get an estimated cost to get to the target from a given position.
    // /// </summary>
    // /// <param name="startPosition">Start position.</param>
    // /// <param name="targetPosition">Current position to get to.</param>
    // /// <returns>An estimated cost.</returns>
    // private float EstimateCostToTarget(Vector2 startPosition, Vector2 targetPosition)
    // {
    //     // TODO: Extract heuristics to their own external nodes.
    //     // We could use DistanceTo() but DistanceSquaredTo() is way faster.
    //     // Actually we don't need an accurate distance, but just a magnitude to compare
    //     // estimated costs from other nodes.
    //     //return GlobalPosition.DistanceSquaredTo(targetPosition);
    //     return startPosition.DistanceTo(targetPosition);
    // }
    
    public override Path FindPath(Vector2 targetPosition)
    {
        // Nodes not fully explored yet, ordered by the cost to get them from the
        // start node.
        AStarPrioritizedNodeSet openSet = new ();
        // Nodes already fully explored. We use a dictionary to keep track of the
        // information gathered from each node, including the connection to get there,
        // while exploring the graph.
        ClosedDict.Clear();
        
        // Get graph nodes associated with the start and target positions. 
        CurrentStartNode = Graph.GetNodeAtPosition(GlobalPosition);
        GraphNode targetNode = Graph.GetNodeAtPosition(targetPosition);
        
        // You get to the start node from nowhere (null) and at no cost (0).
        openSet += new AStarNodeRecord{
            Node = CurrentStartNode,
            Connection = null,
            CostSoFar = 0,
            TotalEstimatedCostToTarget = _heuristic.EstimateCostToTarget(
                CurrentStartNode.Position,
                targetPosition)
        };
        
        // Loop until we reach the target node or no more nodes are available to explore.
        AStarNodeRecord current = NodeRecordNull;
        while (openSet.Count > 0)
        {
            // Explore prioritizing the node with the lowest total estimated cost to get
            // the target.
            current = openSet.ExtractLowestCostNodeRecord();

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

                    // Add the node to the openSet to assess it fully again.
                    openSet += endNodeRecord;
                }
                // OK, we've just found a node that is still being assessed in the open
                // list.
                else if (openSet.Contains(endNode))
                {
                    endNodeRecord = openSet[endNode];
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
                            _heuristic.EstimateCostToTarget(
                                endNode.Position, 
                                targetPosition)
                    };
                    openSet += endNodeRecord;
                }
            }

            // As we've finished looking at the connections of the current node, mark it
            // as fully explored, including it in the closed list.
            ClosedDict[current.Node] = current;
        }

        // If we get here and the current record does not point to the targetNode, then
        // we've fully explored the graph without finding a valid path to get the target.
        if (current.Node != targetNode) return null;
    
        // As we've got the target node, analyze the closedDict to follow back connections
        // from the target node to start node to build the path.
        Path foundPath = BuildPath(Graph, ClosedDict, CurrentStartNode, targetNode);
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