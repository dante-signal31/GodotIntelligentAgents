using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// This class divides a MapGraph in regions, based on defined seeds.
/// </summary>
[Tool]
public partial class MapGraphRegions: Node2D
{
    /// <summary>
    /// This collection manages nodes to be explored in priority order
    /// based on their accumulated path cost, ensuring that the lowest-cost nodes
    /// are processed first.
    /// </summary>
    protected class NodeRegionsRecordSet: 
        HeuristicPathFinder<RegionNodeRecord>.PrioritizedNodeRecordSet
    {
        public override void Add(RegionNodeRecord record)
        {
            PriorityQueue.Enqueue(record, record.CostSoFar);
            NodeRecordDict[record.Node] = record;
        }
    }
    
    [ExportCategory("CONFIGURATION:")]
    /// <summary>
    /// Take connection cost into account when calculating the regions.
    /// </summary>
    [Export] public bool CostAware;
    [Export] public float DefaultCost = 100;
    
    /// <summary>
    /// List of seeds to be used to generate regions.
    /// </summary>
    [Export] public Array<RegionSeed> Seeds = new();
    
    [ExportCategory("WIRING:")]
    /// <summary>
    /// MapGraph this component is going to divide into regions.
    /// </summary>
    [Export] public MapGraph MapGraph { get; private set; }
    
    /// <summary>
    /// MapGraphRegions serialized backend.
    /// </summary>
    [Export] private MapGraphRegionsResource _graphRegionsResource = new();
    
    [ExportToolButton("Bake Regions")]
    private Callable GenerateRegionsButton => Callable.From(GenerateRegions);
    
    [ExportCategory("DEBUG:")] 
    [Export] public bool ShowGizmos;
    [Export] public Color GizmosColor = Colors.GhostWhite;
    [Export] private float _seedRadius = 10;
    [Export(PropertyHint.Range, "0.0,1.0,0.1")] public float GizmoAlpha = 0.5f;
    [Export] public uint FrameRedrawCounter { get; set; } = 20;
    
    private uint _seedsCount;
    private readonly NodeRegionsRecordSet _nodeRegionsOpenSet = new();
    private uint _frameCounter;
    private readonly System.Collections.Generic.Dictionary<PositionNode, RegionNodeRecord>
        _exploredNodes = new();

    /// <summary>
    /// A dictionary that maps region IDs to their corresponding influence values.
    /// </summary>
    private readonly System.Collections.Generic.Dictionary<uint, float> _regionsInfluence 
        = new();

    /// <summary>
    /// Colors to show the regions in debugging mode.
    /// </summary>
    private readonly System.Collections.Generic.Dictionary<uint, Color> _regionColors 
        = new();

    /// <summary>
    /// IDs of regions present in the map graph.
    /// </summary>
    public HashSet<uint> Regions { get; } = new();
    
    /// <summary>
    /// IDs of nodes present in each region.
    /// </summary>
    public readonly System.Collections.Generic.Dictionary<uint, HashSet<uint>> NodesByRegion 
        = new();

    public uint GetRegionByPosition(Vector2 position)
    {
        uint nearestNodeId = MapGraph.GetNodeAtPosition(position).Id;
        return _graphRegionsResource.NodesIdToRegionsId[nearestNodeId];
    }
    
    public uint GetRegionByNodeId(uint nodeId) => 
        _graphRegionsResource.NodesIdToRegionsId[nodeId];

    public Vector2 GetRegionCenter(uint regionId)
    {
        return Seeds[(int)regionId].Position;
    }

    public override void _Ready()
    {
        UpdateRegionsColors();
        UpdateRegionsArray();
        UpdateNodesByRegion();
    }

    /// <summary>
    /// Generates and assigns regions within the map graph. Each region is defined by its
    /// influence and cost parameters.
    /// </summary>
    public void GenerateRegions()
    {
        InitCollections();
        
        while (_nodeRegionsOpenSet.Count > 0)
        {
            RegionNodeRecord current = _nodeRegionsOpenSet.Get();
            if (current == null) break;

            foreach (GraphConnection graphConnection in current.Node.Connections.Values)
            {
                float connectionCost = current.CostSoFar + (CostAware?
                    // The more region influence, the lower the spreading cost.
                    graphConnection.Cost / _regionsInfluence[current.RegionId] : 
                    DefaultCost / _regionsInfluence[current.RegionId]);
                
                // Where does that connection lead us?
                PositionNode endNode = MapGraph.GetNodeById(graphConnection.EndNodeId);
                
                // Is that node already explored?
                if (_exploredNodes.TryGetValue(
                        endNode, 
                        out RegionNodeRecord endNodeRecord))
                {
                    // If the node was already explored, but with a lower cost, skip it.
                    if (connectionCost >= endNodeRecord.CostSoFar) continue;
                    // Otherwise, update the record with the lower cost, the
                    // connection and the new cost.
                    endNodeRecord.RegionId = current.RegionId;
                    endNodeRecord.Connection = graphConnection;
                    endNodeRecord.CostSoFar = connectionCost;
                    _nodeRegionsOpenSet.Add(endNodeRecord);
                }
                else
                {
                    // If the node was not explored yet, add it to the open set.
                    RegionNodeRecord newRecord = new()
                    {
                        Node = endNode,
                        Connection = graphConnection,
                        CostSoFar = connectionCost,
                        RegionId = current.RegionId
                    };
                    _nodeRegionsOpenSet.Add(newRecord);
                    _exploredNodes[endNode] = newRecord;
                }
            }
        }
        UpdateRegionsResource(_exploredNodes);
        UpdateRegionsArray();
        UpdateNodesByRegion();
    }

    /// <summary>
    /// Updates the mapping of nodes to their respective region IDs in the graph regions
    /// resource. This way, the generated regions are kept serialized across executions.
    /// </summary>
    private void UpdateRegionsResource(
        System.Collections.Generic.Dictionary<PositionNode, RegionNodeRecord> 
            exploredNodes)
    {
        _graphRegionsResource.NodesIdToRegionsId.Clear();
        foreach (KeyValuePair<PositionNode, RegionNodeRecord> exploredNode in
                 exploredNodes)
        {
            _graphRegionsResource.NodesIdToRegionsId[exploredNode.Key.Id] =
                exploredNode.Value.RegionId;
        }
    }

    /// <summary>
    /// Updates the set of region IDs by synchronizing the current collection of regions
    /// with the mappings stored in the associated MapGraphRegionsResource object.
    /// This ensures that the Regions property reflects the latest region assignments.
    /// </summary>
    private void UpdateRegionsArray()
    {
        Regions.Clear();
        Regions.UnionWith(_graphRegionsResource.NodesIdToRegionsId.Values);
    }

    /// <summary>
    /// Updates the mapping between region IDs and the sets of node IDs that belong to
    /// those regions.
    /// </summary>
    private void UpdateNodesByRegion()
    {
        foreach (uint regionId in Regions)
        {
            HashSet<uint> nodesInRegion = new();
            foreach (KeyValuePair<uint, uint> nodeIdTopRegionId in 
                     _graphRegionsResource.NodesIdToRegionsId)
            {
                if (nodeIdTopRegionId.Value == regionId) 
                    nodesInRegion.Add(nodeIdTopRegionId.Key);
            }
            NodesByRegion[regionId] = nodesInRegion;
        }
    }

    /// <summary>
    /// Initializes and clears the collections used for region generation in the map
    /// graph.
    /// </summary>
    private void InitCollections()
    {
        _nodeRegionsOpenSet.Clear();
        _regionsInfluence.Clear();
        _exploredNodes.Clear();
        for (uint i = 0; i < Seeds.Count; i++)
        {
            RegionSeed regionSeed = Seeds[(int)i];
            _regionsInfluence[i] = regionSeed.Influence;
            PositionNode seedNode = MapGraph.GetNodeAtPosition(regionSeed.Position);
            RegionNodeRecord nodeRecord = new RegionNodeRecord()
            {
                Node = seedNode,
                Connection = null,
                CostSoFar = 0,
                RegionId = i
            };
            _nodeRegionsOpenSet.Add(nodeRecord);
            // Take seed nodes as already explored.
            _exploredNodes[seedNode] = nodeRecord;
        }
    }

    /// <summary>
    /// Updates the colors of the defined regions in the map based on the
    /// <see cref="RegionSeed.GizmoColor"/> property of each seed.
    /// </summary>
    private void UpdateRegionsColors()
    {
        _regionColors.Clear();
        for (uint i = 0; i < Seeds.Count; i++)
        {
            RegionSeed regionSeed = Seeds[(int)i];
            _regionColors[i] = regionSeed.GizmoColor;
        }
    }

    public override void _Process(double delta)
    {
        // Any update to the seeds array elements?
        if (Seeds.Count != _seedsCount)
        {
            UpdateRegionsColors();
            _seedsCount = (uint)Seeds.Count;
        }
        
        DrawGizmos();
    }

    private void DrawGizmos()
    {
        if ((++_frameCounter) < FrameRedrawCounter) return;
        _frameCounter = 0;
        QueueRedraw();
    }
    
    public override void _Draw()
    {
        if (!ShowGizmos) return;
        
        float currentAlpha = ShowGizmos? GizmoAlpha : 0;
        
        Vector2 cellSize = MapGraph.CellSize;

        foreach (KeyValuePair<uint, uint> nodeIdToRegionId in 
                 _graphRegionsResource.NodesIdToRegionsId)
        {
            PositionNode node = MapGraph.GetNodeById(nodeIdToRegionId.Key);
            Vector2 position = node.Position;
            uint regionId = nodeIdToRegionId.Value;
            Color regionColor = _regionColors[regionId];
            regionColor.A = currentAlpha;
            Vector2 halfSize = cellSize / 2;
            Rect2 rect = new Rect2(position - halfSize, cellSize);
            DrawRect(rect, regionColor, filled: true);
        }

        foreach (RegionSeed seed in Seeds)
        {
            DrawCircle(seed.Position, _seedRadius, GizmosColor);
        }
    }
}