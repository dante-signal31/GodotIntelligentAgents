using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using Godot.Collections;
using GodotGameAIbyExample.Scripts.Extensions;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

[Tool]
public partial class RegionGraph: Node2D
{
    [ExportCategory("WIRING:")]
    /// <summary>
    /// MapGraphRegions this component is going to abstract into a graph.
    /// </summary>
    [Export] private MapGraphRegions _graphRegions;
    
    [ExportToolButton("Bake RegionGraph")]
    private Callable GenerateRegionGraphButton => Callable.From(GenerateRegionGraph);
    
    /// <summary>
    /// MapGraphRegions serialized backend.
    /// </summary>
    [Export] private RegionGraphResource _regionGraphResource = new();
    
    [ExportCategory("DEBUG:")]
    [Export] public bool ShowGizmos { get; set; }
    [Export] public Color GridColor { get; set; } = Colors.Yellow;
    [Export] public int NodeRadius { get; set; } = 10;
    [Export] public Color NodeColor { get; set; } = Colors.Orange;
    [Export] public Vector2 GizmoTextOffset { get; set; }= new(10, 10);

    /// <summary>
    /// Heuristic cost to traverse a region.
    /// </summary>
    private System.Collections.Generic.Dictionary<uint, float> _regionTraversalCosts = 
        new();

    private DijkstraPathFinder _dijkstraPathFinder;

    public override void _Ready()
    {
        _dijkstraPathFinder = this.FindChild<DijkstraPathFinder>();
        if (_graphRegions != null) _dijkstraPathFinder.Graph = _graphRegions.MapGraph;
    }

    private void GenerateRegionGraph()
    {
        GenerateRegionNodes();
        CalculateRegionTraversalPaths();
        CalculateRegionTraversalCosts();
        EstablishRegionConnections();
    }

    private void CalculateRegionTraversalCosts()
    {
        // <CrossedRegion, (TotalCost, PathsCrossingAmount)>
        System.Collections.Generic.Dictionary<uint, (float, uint)> 
            regionIdToGlobalCostAndPathCount = new();
        
        // Sum cost of every path crossing a region.
        foreach (KeyValuePair<RegionGraphResource.FromNodeToRegionPathsKey, 
                     RegionGraphResource.InterRegionPath> fromNodeToRegionPath 
                 in _regionGraphResource.FromNodeToRegionPaths)
        {
            RegionGraphResource.FromNodeToRegionPathsKey nodeToRegion = fromNodeToRegionPath.Key;
            RegionGraphResource.InterRegionPath interRegionPath = fromNodeToRegionPath.Value;
            uint crossedRegionId =
                _graphRegions.GetRegionByNodeId(nodeToRegion.FromNodeId);
            if (!regionIdToGlobalCostAndPathCount.ContainsKey(crossedRegionId))
                regionIdToGlobalCostAndPathCount[crossedRegionId] = (0, 0);
            (float totalCost, uint pathCrossingAmount) =
                regionIdToGlobalCostAndPathCount[crossedRegionId];
            totalCost += interRegionPath.Cost;
            pathCrossingAmount++;
            regionIdToGlobalCostAndPathCount[crossedRegionId] = 
                (totalCost, pathCrossingAmount);
        }

        // Calculate the average cost of every region.
        foreach (KeyValuePair<uint, (float, uint)> regionToCostAndCount in 
                 regionIdToGlobalCostAndPathCount)
        {
            uint regionId = regionToCostAndCount.Key;
            (float totalCost, uint pathCrossingAmount) = regionToCostAndCount.Value;
            _regionTraversalCosts[regionId] = totalCost / pathCrossingAmount;
        }
    }

    /// <summary>
    /// The nodes our dijkstra pathfinder must reach to end the needed calculation.
    /// </summary>
    private HashSet<PositionNode> _targetNodes = new();

    /// <summary>
    /// Ends Dijkstra calculation when every target node is reached.
    /// </summary>
    /// <returns>Returns true only when every target node ahs been reached;
    /// otherwise returns false.</returns>
    private bool EndCondition()
    {
        if (!_targetNodes.Contains(_dijkstraPathFinder.CurrentNodeRecord.Node)) 
            return false;
        _targetNodes.Remove(_dijkstraPathFinder.CurrentNodeRecord.Node);
        return _targetNodes.Count == 0;
    }

    private void CalculateRegionTraversalPaths()
    {
        // Region traversal cost is a heuristic that estimates the cost to completely
        // go through a region. So, the connection between two regions is estimated by
        // added the half of the region traversal cost of its region ends.
        _regionGraphResource.FromNodeToRegionPaths.Clear();
        foreach (KeyValuePair<uint, RegionNode> regionIdToRegionNode in 
                 _regionGraphResource.RegionIdToRegionNode)
        {
            uint regionId = regionIdToRegionNode.Key;
            RegionNode regionNode = regionIdToRegionNode.Value;
            HashSet<uint> neighborRegionIds = new(regionNode.BoundaryNodes.Keys);
            
            // For every boundary node get a path to the nearest boundary node of 
            // every neighbor region.
            foreach (KeyValuePair<uint, Array<uint>> neighborRegionToBoundaryNodes in 
                     regionNode.BoundaryNodes)
            {
                // Boundary nodes are going to be our starting points for the paths.
                Array<uint> boundaryNodesTowardsNeighborRegion = 
                    neighborRegionToBoundaryNodes.Value;

                // Now calculate the shortest path from every boundary node of the current
                // region towards the nearest boundary node of every neighbor region.
                foreach (uint boundaryNodeId in boundaryNodesTowardsNeighborRegion)
                {
                    try
                    {
                        // Start node.
                        PositionNode boundaryNode =
                            _graphRegions.MapGraph.GetNodeById(boundaryNodeId);
                        
                        // Gather possible targets. They are the boundary nodes in
                        // the neighbor regions.
                        _targetNodes.Clear();
                        foreach (uint neighborRegionId in neighborRegionIds)
                        {
                            // Get nodes from the neighbor region that are boundary to
                            // the current region.
                            Array<uint> neighborBoundaryNodesIds = _regionGraphResource
                                .RegionIdToRegionNode[neighborRegionId]
                                .BoundaryNodes[regionId];
                            foreach (uint neighborBoundaryNodeId in neighborBoundaryNodesIds)
                            {
                                PositionNode neighborBoundaryNode =
                                    _graphRegions.MapGraph.GetNodeById(
                                        neighborBoundaryNodeId);
                                _targetNodes.Add(neighborBoundaryNode);
                            }
                        }

                        // Map from the boundary node to every target node.
                        _dijkstraPathFinder.CalculateCosts(boundaryNode, EndCondition);
                        
                        // Select the best paths to every neighbor region from the current
                        // boundary node.
                        foreach (uint neighborRegionId in neighborRegionIds)
                        {
                            // Get nodes from the neighbor that are boundary to the current
                            // region.
                            Array<uint> neighborBoundaryNodesIds = _regionGraphResource
                                .RegionIdToRegionNode[neighborRegionId]
                                .BoundaryNodes[regionId];
                            
                            RegionGraphResource.FromNodeToRegionPathsKey nodeToRegion = new()
                            {
                                FromNodeId = boundaryNodeId,
                                ToRegionId = neighborRegionId
                            };
                            
                            // For every neighbor region, we keep only the path to its
                            // boundary node which is nearest to the current boundary node
                            // we are using as the starting node.
                            foreach (uint targetNodeId in neighborBoundaryNodesIds)
                            {
                                PositionNode targetNode =
                                    _graphRegions.MapGraph.GetNodeById(targetNodeId);
                                if (!_regionGraphResource.FromNodeToRegionPaths.ContainsKey(
                                        nodeToRegion) ||
                                    _regionGraphResource.FromNodeToRegionPaths[nodeToRegion]
                                        .Cost > _dijkstraPathFinder.ClosedDict[targetNode]
                                        .CostSoFar)
                                {
                                    _regionGraphResource.FromNodeToRegionPaths[nodeToRegion] =
                                        new RegionGraphResource.InterRegionPath()
                                        {
                                            Cost = _dijkstraPathFinder.ClosedDict[targetNode]
                                                .CostSoFar,
                                            PathPositions = _dijkstraPathFinder
                                                .BuildPath(_dijkstraPathFinder.ClosedDict,
                                                    boundaryNode, targetNode).TargetPositions
                                        };
                                }
                            }
                        }
                    }
                    catch (KeyNotFoundException e)
                    {
                        Console.Error.WriteLine($"Boundary node not found " +
                                                $"({boundaryNodeId}) when calculating " +
                                                $"paths for region {regionId}.");
                    }
                }
            }
        }
    }

    private void EstablishRegionConnections()
    {
        // Once that region nodes are created, establish connections between them.
        foreach (KeyValuePair<uint, RegionNode> regionIdToRegionNode in 
                 _regionGraphResource.RegionIdToRegionNode)
        {
            uint regionId = regionIdToRegionNode.Key;
            RegionNode regionNode = regionIdToRegionNode.Value;
            // Create a connection with every region this one has boundary nodes with.
            foreach (KeyValuePair<uint, Array<uint>> neighborRegionIdBoundaryNodes in 
                     regionNode.BoundaryNodes)
            {
                uint neighborRegionId = neighborRegionIdBoundaryNodes.Key;
                RegionNode neighborRegionNode = 
                    _regionGraphResource.RegionIdToRegionNode[neighborRegionId];
                GraphConnection connection = new();
                connection.StartNodeId = regionNode.Id;
                connection.EndNodeId = neighborRegionNode.Id;
                connection.Cost = (_regionTraversalCosts[neighborRegionId] + 
                                  _regionTraversalCosts[regionId]) / 2;
                
                regionNode.Connections[neighborRegionId] = connection;
            }
        }
    }

    private void GenerateRegionNodes()
    {
        _regionGraphResource.RegionIdToRegionNode.Clear();
        
        // Traverse every region to generate their region nodes.
        foreach (KeyValuePair<uint, HashSet<uint>> regionIdToNodesIdsByRegion in 
                 _graphRegions.NodesByRegion)
        {
            uint regionId = regionIdToNodesIdsByRegion.Key;
            // Create a new region node to represent that region in the graph.
            RegionNode regionNode = new()
            {
                RegionId = regionId,
                Position = _graphRegions.GetRegionCenter(regionId),
            };
            // Traverse every node in the region.
            HashSet<uint> nodeIdsInRegion = regionIdToNodesIdsByRegion.Value;
            foreach (uint nodeId in nodeIdsInRegion)
            {
                PositionNode node = _graphRegions.MapGraph.GetNodeById(nodeId);
                // Check if the node has connections to other regions.
                foreach (KeyValuePair<Orientation, GraphConnection> graphConnection in 
                         node.GetConnections())
                {
                    GraphConnection connection = graphConnection.Value;
                    uint otherNodeRegionId = 
                        _graphRegions.GetRegionByNodeId(connection.EndNodeId);
                    // If the node is connected to another region, add it to the region's
                    // boundary nodes.
                    if (otherNodeRegionId != regionId)
                    {
                        if (!regionNode.BoundaryNodes.ContainsKey(otherNodeRegionId))
                            regionNode.BoundaryNodes[otherNodeRegionId] = new();
                        regionNode.BoundaryNodes[otherNodeRegionId].Add(nodeId);
                    }
                }
            }
            
            _regionGraphResource.RegionIdToRegionNode[regionId] = regionNode;
        }
    }
    
    public override void _Process(double delta)
    {
        DrawGizmos();
    }

    private void DrawGizmos()
    {
        QueueRedraw();
    }
    
    public override void _Draw()
    {
        if (!ShowGizmos) return;

        // Draw region center positions.
        foreach (KeyValuePair<uint, RegionNode> regionIdToRegionNode in 
                 _regionGraphResource.RegionIdToRegionNode)
        {
            RegionNode regionNode = regionIdToRegionNode.Value;
            DrawCircle(regionNode.Position, NodeRadius, NodeColor);
            
            // Draw connections between regions.
            foreach (KeyValuePair<uint, GraphConnection> regionNodeConnection in 
                     regionNode.Connections)
            {
                GraphConnection connection = regionNodeConnection.Value;
                RegionNode endRegionNode = 
                    _regionGraphResource.RegionIdToRegionNode[connection.EndNodeId];
                DrawLine(regionNode.Position, endRegionNode.Position, GridColor);
                // Draw connection cost.
                DrawString(
                    ThemeDB.FallbackFont, 
                    ToLocal(regionNode.Position + 
                            (endRegionNode.Position - regionNode.Position) / 2 + 
                    GizmoTextOffset), 
                    connection.Cost.ToString("G"), 
                    modulate: NodeColor);
            }
        }
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new();
        
        _dijkstraPathFinder = this.FindChild<DijkstraPathFinder>();
        if (_dijkstraPathFinder == null)
        {
            warnings.Add("This node needs a child of type DijkstraPathFinder to work.");
        }
        
        return warnings.ToArray();
    }
}