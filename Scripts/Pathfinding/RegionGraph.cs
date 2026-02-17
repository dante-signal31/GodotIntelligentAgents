using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using GodotGameAIbyExample.Scripts.Extensions;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

[Tool]
public partial class RegionGraph: Node
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

    /// <summary>
    /// Heuristic cost to traverse a region.
    /// </summary>
    private System.Collections.Generic.Dictionary<uint, float> _regionTraversalCosts = 
        new();

    private DijkstraPathFinder _dijkstraPathFinder;

    public override void _Ready()
    {
        _dijkstraPathFinder = this.FindChild<DijkstraPathFinder>();
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
        foreach (KeyValuePair<RegionGraphResource.FromNodeToRegionPathsKey, 
                     RegionGraphResource.InterRegionPath> fromNodeToRegionPath 
                 in _regionGraphResource.FromNodeToRegionPaths)
        {
            RegionGraphResource.FromNodeToRegionPathsKey nodeToRegion = fromNodeToRegionPath.Key;
            RegionGraphResource.InterRegionPath interRegionPath = fromNodeToRegionPath.Value;
            
      
            
        
            
            
            _regionTraversalCosts[regionNode.RegionId] = regionNode.BoundaryNodes.Count;
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
        foreach (KeyValuePair<uint, RegionNode> regionIdToRegionNode in 
                 _regionGraphResource.RegionIdToRegionNode)
        {
            uint regionId = regionIdToRegionNode.Key;
            RegionNode regionNode = regionIdToRegionNode.Value;
            // For every boundary node get a path to the nearest boundary node of 
            // every region you can get through the neighbor region.
            foreach (KeyValuePair<uint, Array<uint>> neighborRegionToBoundaryNodes in 
                     regionNode.BoundaryNodes)
            {
                uint neighborRegionId = neighborRegionToBoundaryNodes.Key;
                // Boundary nodes towards the neighbor region are going to be our 
                // starting points for the traversing paths across the neighbor region.
                Array<uint> boundaryNodesTowardsNeighborRegion = 
                    neighborRegionToBoundaryNodes.Value;
                // Neighbor region node we want to go through.
                RegionNode neighborRegionNode = 
                    _regionGraphResource.RegionIdToRegionNode[neighborRegionId];
                // Here we are going to store the regions accessible through the neighbor
                // region.
                HashSet<uint> regionsAccessibleFromNeighbor = 
                    new(neighborRegionNode.BoundaryNodes.Keys);
                // Here we are going to store the boundary nodes of the regions accessible
                // through the neighbor region, indexed by the region.
                System.Collections.Generic.Dictionary<uint, Array<uint>>
                    regionsAccessibleFromNeighborRegionIdToBoundaryNodesId = new(); 
                // Here we are going to keep only the boundary nodes, of the regions
                // accessible through the neighbor regions, that look to the neighbor
                // region. These are the end nodes of our paths traversing the neighbor
                // region.
                HashSet<uint> boundaryNodesIdAccessibleFromNeighborRegion = new();
                // Once you know which regions you can get through the neighbor region,
                // we calculate the shortest path to every boundary node of those regions
                // from the current region boundary node. This path goes from the current
                // region boundary node to the nearest boundary node of the region you can
                // get through the neighbor region. Both ends of this path don't belong to
                // the neighbor region, but the path traverses it, so its cost is the
                // traverse cost of the neighbor region. There are many more possible
                // paths, when you take in count every possible node end, so we calculate
                // every traversal path and get the average cost to get an approximation
                // of the overall traversal cost.
                foreach (uint regionAccessibleId in regionsAccessibleFromNeighbor)
                {
                    // Get the nodes of the accessible region that are boundary to the
                    // neighbor region. These will be the ends of our traversal paths for
                    // that particular accessible region.
                    Array<uint> accessibleRegionBoundaryNodesId = 
                        _regionGraphResource.RegionIdToRegionNode[regionAccessibleId]
                        .BoundaryNodes[neighborRegionId];
                    // Add those nodes to the other boundary nodes accessible of other
                    // regions accessible through the neighbor region.
                    boundaryNodesIdAccessibleFromNeighborRegion
                        .UnionWith(accessibleRegionBoundaryNodesId);
                    // Take note of the accessible region boundary nodes, indexed by
                    // the accessible region.
                    regionsAccessibleFromNeighborRegionIdToBoundaryNodesId[regionAccessibleId] = 
                        accessibleRegionBoundaryNodesId;
                }

                // Prepare our set of end nodes for our paths.
                HashSet<PositionNode> boundaryNodesAccessibleFromNeighborRegion = new();
                foreach (uint boundaryNodeId in 
                         boundaryNodesIdAccessibleFromNeighborRegion)
                {
                    boundaryNodesAccessibleFromNeighborRegion.Add(
                        _graphRegions.MapGraph.GetNodeById(boundaryNodeId));
                }
                System.Collections.Generic.Dictionary<uint, List<PositionNode>>
                    regionsAccessibleFromNeighborRegionIdToBoundaryNodes = new();
                foreach (
                    KeyValuePair<uint, Array<uint>> regionAccessibleIdToBoundaryNodesId in 
                    regionsAccessibleFromNeighborRegionIdToBoundaryNodesId)
                {
                    uint regionAccessibleId = regionAccessibleIdToBoundaryNodesId.Key;
                    Array<uint> boundaryNodesId = regionAccessibleIdToBoundaryNodesId.Value;
                    foreach (uint boundaryNodeId in boundaryNodesId)
                    {
                        regionsAccessibleFromNeighborRegionIdToBoundaryNodes[regionAccessibleId]
                            .Add(_graphRegions.MapGraph.GetNodeById(boundaryNodeId));
                    }
                }

                // Now calculate the shortest path from every boundary node of the current
                // region towards every boundary node (that looks to the neighbor region)
                // of the regions accessible through the neighbor region.
                _regionGraphResource.FromNodeToRegionPaths.Clear();
                foreach (uint boundaryNodeId in boundaryNodesTowardsNeighborRegion)
                {
                    PositionNode boundaryNode = 
                        _graphRegions.MapGraph.GetNodeById(boundaryNodeId);
                    _targetNodes.Clear();
                    _targetNodes.UnionWith(boundaryNodesAccessibleFromNeighborRegion);
                    _dijkstraPathFinder.CalculateCosts(boundaryNode, EndCondition);
                    // Once calculation ends, we have in Dijkstra pathfinder closed list
                    // the shortest path from the boundary node to every boundary node of
                    // the regions accessible through the neighbor region. But, for
                    // every accessible region, we only need the shortest path. So we get 
                    // it.
                    foreach (
                        KeyValuePair<uint, List<PositionNode>> 
                            regionAccessibleFromNeighborRegionIdToBoundaryNodes in 
                        regionsAccessibleFromNeighborRegionIdToBoundaryNodes)
                    {
                        RegionGraphResource.FromNodeToRegionPathsKey nodeToRegion = new()
                        {
                            FromNodeId = boundaryNodeId,
                            ToRegionId =
                                regionAccessibleFromNeighborRegionIdToBoundaryNodes.Key
                        };
                        // For every accessible region, we keep only the path to the
                        // boundary node which is nearest to the current boundary node
                        // we are using as the starting node.
                        foreach (PositionNode targetNode in 
                                 regionAccessibleFromNeighborRegionIdToBoundaryNodes.Value)
                        {
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
                Array<uint> currentRegionBoundaryNodesToNeighbor = 
                    neighborRegionIdBoundaryNodes.Value;
                RegionNode neighborRegionNode = 
                    _regionGraphResource.RegionIdToRegionNode[neighborRegionId];
                Array<uint> neighborRegionBoundaryNodesToCurrent = 
                    neighborRegionNode.BoundaryNodes[regionId];
                
                RegionGraphConnection connection = new();
                connection.StartNodeId = regionNode.Id;
                connection.EndNodeId = neighborRegionNode.Id;
                connection.EnterNodeIdToExitNodeId = GetEnterExitMappings(
                    currentRegionBoundaryNodesToNeighbor, 
                    neighborRegionBoundaryNodesToCurrent);
                connection.Cost = (_regionTraversalCosts[neighborRegionId] + 
                                  _regionTraversalCosts[regionId]) / 2;
                
                regionNode.Connections[neighborRegionId] = connection;
            }
        }
    }

    private void GenerateRegionNodes()
    {
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
                        regionNode.BoundaryNodes[otherNodeRegionId].Add(nodeId);
                    }
                }
            }
            _regionGraphResource.RegionIdToRegionNode[regionId] = regionNode;
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