using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using GodotGameAIbyExample.Scripts.Extensions;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

[Tool]
public partial class RegionGraph: Node2D, IPositionGraph
{
    [ExportCategory("WIRING:")]
    /// <summary>
    /// MapGraphRegions this component is going to abstract into a graph.
    /// </summary>
    [Export] public MapGraphRegions GraphRegions;
    
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

    public uint GetRegionIdByPosition(Vector2 position)
    {
        IPositionNode positionNode = GraphRegions.MapGraph.GetNodeAtPosition(position);
        return GraphRegions.GetRegionByNodeId(positionNode.Id);   
    }

    public RegionNode GetRegionNodeById(uint regionId)
    {
        return _regionGraphResource.RegionIdToRegionNode[regionId];
    }

    /// <summary>
    /// Returns the shortest path from a given node to a region.
    /// </summary>
    /// <param name="startNode">Starting node.</param>
    /// <param name="regionId">Target region.</param>
    /// <returns>A path to get the nearest node of the target region.</returns>
    public Path GetShortestPathToRegion(PositionNode startNode, uint regionId)
    {
        long nodeToRegion = RegionGraphResource.GetFromNodeToRegionKey(
            startNode.Id, 
            regionId);
        InterRegionPath interRegionPath =
            _regionGraphResource.FromNodeToRegionPaths[nodeToRegion];
        Array<Vector2> pathPositions = interRegionPath.PathPositions;
        Path returnedPath = new ();
        returnedPath.LoadPathData(pathPositions);
        return returnedPath;
    }

    public override void _Ready()
    {
        _dijkstraPathFinder = this.FindChild<DijkstraPathFinder>();
        if (GraphRegions != null) _dijkstraPathFinder.Graph = GraphRegions.MapGraph;
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
        foreach (KeyValuePair<
                     long, 
                     InterRegionPath> fromNodeToRegionPath 
                 in _regionGraphResource.FromNodeToRegionPaths)
        {
            long nodeToRegionKey = fromNodeToRegionPath.Key;
            RegionGraphResource.SplitKey(
                nodeToRegionKey, 
                out uint fromNodeId, 
                out uint _);
            InterRegionPath interRegionPath = fromNodeToRegionPath.Value;
            uint crossedRegionId =
                GraphRegions.GetRegionByNodeId(fromNodeId);
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
    private HashSet<IPositionNode> _targetNodes = new();

    /// <summary>
    /// Ends Dijkstra calculation when every target node is reached.
    /// </summary>
    /// <returns>Returns true only when every target node has been reached;
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
                        PositionNode boundaryNode = (PositionNode) 
                            GraphRegions.MapGraph.GetNodeById(boundaryNodeId);
                        
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
                                PositionNode neighborBoundaryNode = (PositionNode)
                                    GraphRegions.MapGraph.GetNodeById(
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
                            
                            long nodeToRegionKey = 
                                RegionGraphResource.GetFromNodeToRegionKey(
                                    boundaryNodeId, 
                                    neighborRegionId);
                            
                            // For every neighbor region, we keep only the path to its
                            // boundary node, which is nearest to the current boundary
                            // node we are using as the starting node.
                            foreach (uint targetNodeId in neighborBoundaryNodesIds)
                            {
                                PositionNode targetNode = (PositionNode)
                                    GraphRegions.MapGraph.GetNodeById(targetNodeId);
                                if (!_regionGraphResource.FromNodeToRegionPaths.ContainsKey(
                                        nodeToRegionKey) ||
                                    _regionGraphResource.FromNodeToRegionPaths[nodeToRegionKey]
                                        .Cost > _dijkstraPathFinder.ClosedDict[targetNode]
                                        .CostSoFar)
                                {
                                    _regionGraphResource.FromNodeToRegionPaths[nodeToRegionKey] =
                                        new InterRegionPath()
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
        _regionGraphResource.PositionToRegionNode.Clear();
        
        // Traverse every region to generate their region nodes.
        foreach (KeyValuePair<uint, HashSet<uint>> regionIdToNodesIdsByRegion in 
                 GraphRegions.NodesByRegion)
        {
            uint regionId = regionIdToNodesIdsByRegion.Key;
            // Create a new region node to represent that region in the graph.
            RegionNode regionNode = new()
            {
                Id = regionId,
                Position = GraphRegions.GetRegionCenter(regionId),
            };
            // Traverse every node in the region.
            HashSet<uint> nodeIdsInRegion = regionIdToNodesIdsByRegion.Value;
            foreach (uint nodeId in nodeIdsInRegion)
            {
                PositionNode node = (PositionNode) GraphRegions.MapGraph.GetNodeById(nodeId);
                // Check if the node has connections to other regions.
                foreach (KeyValuePair<Orientation, GraphConnection> graphConnection in 
                         node.GetConnections())
                {
                    GraphConnection connection = graphConnection.Value;
                    uint otherNodeRegionId = 
                        GraphRegions.GetRegionByNodeId(connection.EndNodeId);
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
            _regionGraphResource.PositionToRegionNode[regionNode.Position] = regionNode;
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
            // Draw region number on the region center.
            DrawString(
                ThemeDB.FallbackFont, 
                ToLocal(regionNode.Position + 
                        GizmoTextOffset), 
                regionNode.Id.ToString(), 
                modulate: GridColor);
            
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
                    modulate: GridColor);
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

    /// <summary>
    /// Get the RegionNode related to a given region seed.
    /// </summary>
    /// <param name="nodeId">Region ID</param>
    /// <returns>The PositionNode where the given region seed is placed.</returns>
    public IPositionNode GetNodeById(uint nodeId)
    {
        RegionNode regionNode = _regionGraphResource.RegionIdToRegionNode[nodeId];
        return regionNode;
    }


    /// <summary>
    /// Retrieves a RegionNode located at the specified position in the graph.
    /// </summary>
    /// <param name="position">The position in the graph where the node is to be
    /// located.</param>
    /// <returns>The node located at the specified position, or the nearest node if
    /// one does not exist at the exact position. Null if nothing is found.</returns>
    public IPositionNode GetNodeAtPosition(Vector2 position)
    {
        RegionNode regionNode;
        regionNode = _regionGraphResource.PositionToRegionNode.ContainsKey(position) ? 
            _regionGraphResource.PositionToRegionNode[position]: 
            GetNodeAtNearestPosition(position);
        return regionNode;
    }

    /// <summary>
    /// Retrieves the region node located at the nearest position to the specified
    /// global position.
    /// </summary>
    /// <param name="globalPosition">The global position for which the nearest region
    /// node is determined.</param>
    /// <returns>The region node closest to the specified position.</returns>
    private RegionNode GetNodeAtNearestPosition(Vector2 globalPosition)
    {
        Vector2 nearestPosition = FindNearestPosition(globalPosition);
        RegionNode nearestNode = 
            _regionGraphResource.PositionToRegionNode[nearestPosition];
        return nearestNode;
    }

    /// <summary>
    /// Finds the nearest position in the graph to the specified target position.
    /// </summary>
    /// <param name="targetPosition">The position for which to find the nearest graph
    /// position.</param>
    /// <returns>The nearest position in the graph to the target position.</returns>
    private Vector2 FindNearestPosition(Vector2 targetPosition)
    {
        Vector2 nearestPosition = Vector2.Zero;
        float minDistance = float.MaxValue;

        foreach (Vector2 key in _regionGraphResource.PositionToRegionNode.Keys)
        {
            float distance = targetPosition.DistanceSquaredTo(key);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestPosition = key;
            }
        }
        return nearestPosition;
    }
}