using System.Collections.Generic;
using Godot;
using Godot.Collections;
using GodotGameAIbyExample.Scripts.Extensions;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

[Tool]
public partial class RegionPathFinder: Node2D, IPathFinder
{
    [ExportCategory("CONFIGURATION:")]
    [Export] public RegionGraph RegionGraph;
    
    [ExportCategory("WIRING:")]
    [Export] private DijkstraPathFinder _firstMilePathFinder;
    [Export] private Node2D _regionLevelPathFinderNode2D;
    [Export] private Node2D _lastMilePathFinderNode2D;
    
    private MapGraph MapGraph => RegionGraph.GraphRegions.MapGraph;
    private IPathFinder _regionLevelPathFinder;
    private IPathFinder _lastMilePathFinder;

    public IPositionGraph Graph
    {
        get=> RegionGraph;
        set
        {
            RegionGraph = (RegionGraph) value;
        }
    }
    
    public override void _Ready()
    {
        // At "first mile" we use Dijkstra to get to the nearest boundary node of the
        // next region.
        if (_firstMilePathFinder != null) _firstMilePathFinder.Graph = MapGraph;
        
        // At region level you can use both Dijkstra and A*.
        if (_regionLevelPathFinderNode2D != null)
        {
            _regionLevelPathFinder = (IPathFinder) _regionLevelPathFinderNode2D;
            _regionLevelPathFinder.Graph = RegionGraph;
        }
        
        // At region level you can use both Dijkstra and A*.
        if (_lastMilePathFinderNode2D != null)
        {
            _lastMilePathFinder = (IPathFinder) _lastMilePathFinderNode2D;
            _lastMilePathFinder.Graph = MapGraph;
        }
    }

    /// <summary>
    /// Get a path from the current position to the target position, traversing every
    /// intermediate region.
    /// </summary>
    /// <param name="targetPosition">End position.</param>
    /// <param name="fromPosition">Starting position.</param>
    /// <returns>A path to get to the targetPosition from fromPosition</returns>
    public Path FindPath(Vector2 targetPosition, Vector2 fromPosition=default)
    {
        Path totalPath = new();
        
        PositionNode targetNode = 
            (PositionNode) MapGraph.GetNodeAtPosition(targetPosition);
        uint targetRegionId = RegionGraph.GraphRegions.GetRegionByNodeId(targetNode.Id);
        RegionNode targetRegion = RegionGraph.GetRegionNodeById(targetRegionId);
        
        PositionNode initialNode = fromPosition==default?
            (PositionNode) MapGraph.GetNodeAtPosition(GlobalPosition):
            (PositionNode) MapGraph.GetNodeAtPosition(fromPosition);
        uint initialRegionId = RegionGraph.GraphRegions.GetRegionByNodeId(initialNode.Id);
        RegionNode initialRegion = RegionGraph.GetRegionNodeById(initialRegionId);
        
        // Get the path in regionGraph space.
        Path regionPath = _regionLevelPathFinder.FindPath(
            targetRegion.Position, 
            initialRegion.Position);
        
        // Now get the sequence of regions to traverse.
        uint[] regionIdsSequence = new uint[regionPath.PathLength+1];
        regionIdsSequence[0] = initialRegionId;
        uint index = 1;
        foreach (Vector2 regionPathPosition in regionPath.TargetPositions)
        {
            regionIdsSequence[index] = 
                RegionGraph.GraphRegions.GetRegionByPosition(regionPathPosition);
            index++;
        }
        
        // "First mile". Get the path to the nearest boundary node of the next region.
        uint currentRegionIndex = 0;
        uint currentRegionId = regionIdsSequence[currentRegionIndex];
        uint nextRegionId = regionIdsSequence[currentRegionIndex + 1];
        RegionNode nextRegion = RegionGraph.GetRegionNodeById(nextRegionId);
        Array<uint> nextRegionBoundaryNodes = nextRegion.BoundaryNodes[initialRegionId];
        _firstMilePathFinder.CalculateCosts(
            initialNode, 
            // End condition: exit when you find the first boundary node of the next
            // region.
            () => nextRegionBoundaryNodes.Contains(
                _firstMilePathFinder.CurrentNodeRecord.Node.Id));
        PositionNode nearestNextRegionBoundaryNode = null;
        foreach (uint nextRegionBoundaryNode in nextRegionBoundaryNodes)
        {
            PositionNode candidateNextRegionBoundaryNode = 
                (PositionNode) MapGraph.GetNodeById(nextRegionBoundaryNode);
            // There should be only une candidate node at the closed list, the nearest
            // one.
            if (_firstMilePathFinder.ClosedDict.ContainsKey(
                    candidateNextRegionBoundaryNode))
            {
                nearestNextRegionBoundaryNode = candidateNextRegionBoundaryNode;
                break;
            }
        }
        Path pathToNextRegion = _firstMilePathFinder.BuildPath(
            _firstMilePathFinder.ClosedDict, 
            initialNode, 
            nearestNextRegionBoundaryNode);
        totalPath.AppendPath(pathToNextRegion);
        
        // Once you are in intermediate regions, you can use static routing embed in 
        // regionGraph to get to the final region.
        currentRegionId = nextRegionId;
        currentRegionIndex++;
        while (currentRegionId != targetRegionId)
        {
            // The last end position is now our starting position.
            PositionNode startNode = nearestNextRegionBoundaryNode;
            // Prepare to travel to the next region.
            nextRegionId = regionIdsSequence[currentRegionIndex + 1];
            Path traversalPath = RegionGraph.GetShortestPathToRegion(
                startNode, 
                nextRegionId);
            Vector2 endPosition = traversalPath.TargetPositions[^1];
            nearestNextRegionBoundaryNode = 
                (PositionNode) MapGraph.GetNodeAtPosition(endPosition);
            // Append the path to traverse this region to the global path.
            totalPath.AppendPath(traversalPath);
            // Go to the next region.
            currentRegionId = nextRegionId;
            currentRegionIndex++;
        }
        
        // "Last-mile": we reached the target region, so we use A* to go from the
        // boundary node to the target one.
        //
        // Again, the last end position is now our starting position.
        PositionNode finalBoundaryNode = nearestNextRegionBoundaryNode;
        Path pathToTarget = _lastMilePathFinder.FindPath(
            targetNode.Position,
            finalBoundaryNode.Position);
        totalPath.AppendPath(pathToTarget);
        return totalPath;
    }
}