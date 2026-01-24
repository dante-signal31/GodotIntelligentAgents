using System;
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
    [Export] private MapGraph _mapGraph;
    
    /// <summary>
    /// MapGraphRegions serialized backend.
    /// </summary>
    [Export] private MapGraphRegionsResource _graphRegionsResource = new();
    
    [ExportToolButton("Bake Regions")]
    private Callable GenerateRegionsButton => Callable.From(GenerateRegions);
    
    [ExportCategory("DEBUG:")] 
    
    [Export] public bool ShowGizmos;

    [Export(PropertyHint.Range, "0.0,1.0,0.1")] public float GizmoAlpha = 0.5f;
    
    private uint _seedsCount;
    
    private readonly NodeRegionsRecordSet _nodeRegionsOpenSet = new();

    /// <summary>
    /// A dictionary that maps region IDs to their corresponding influence values.
    /// </summary>
    private readonly System.Collections.Generic.Dictionary<uint, float> _regionsInfluence 
        = new();
    
    /// <summary>
    /// A dictionary that maps nodes to their corresponding region records.
    /// </summary>
    private readonly System.Collections.Generic.Dictionary<PositionNode, RegionNodeRecord> 
        _exploredNodes = new();

    /// <summary>
    /// Colors to show the regions in debugging mode.
    /// </summary>
    private readonly System.Collections.Generic.Dictionary<uint, Color> _regionColors 
        = new();
    
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
                PositionNode endNode = _mapGraph.GetNodeById(graphConnection.EndNodeId);

                // Was that node already explored?
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
    }

    private void InitCollections()
    {
        _nodeRegionsOpenSet.Clear();
        _regionsInfluence.Clear();
        _exploredNodes.Clear();
        for (uint i = 0; i < Seeds.Count; i++)
        {
            RegionSeed regionSeed = Seeds[(int)i];
            _regionsInfluence[i] = regionSeed.Influence;
            PositionNode seedNode = _mapGraph.GetNodeAtPosition(regionSeed.Position);
            RegionNodeRecord nodeRecord = new RegionNodeRecord()
            {
                Node = seedNode,
                Connection = null,
                CostSoFar = DefaultCost / regionSeed.Influence,
                RegionId = i
            };
            _nodeRegionsOpenSet.Add(nodeRecord);
        }
    }
    
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
        QueueRedraw();
    }
    
    public override void _Draw()
    {
        float currentAlpha = ShowGizmos? GizmoAlpha : 0;
        
        Vector2 cellSize = _mapGraph.CellSize;

        foreach (RegionNodeRecord regionNodeRecord in _exploredNodes.Values)
        {
            Vector2 position = regionNodeRecord.Node.Position;
            uint regionId = regionNodeRecord.RegionId;
            Color regionColor = _regionColors[regionId];
            regionColor.A = currentAlpha;
            Vector2 halfSize = cellSize / 2;
            Rect2 rect = new Rect2(position - halfSize, cellSize);
            DrawRect(rect, regionColor, filled: true);
        }
    }
    
    private void ClearRegionsGizmos()
    {
        Vector2 cellSize = _mapGraph.CellSize;

        foreach (RegionNodeRecord regionNodeRecord in _exploredNodes.Values)
        {
            Vector2 position = regionNodeRecord.Node.Position;
            Color regionColor = Colors.Transparent;
            Vector2 halfSize = cellSize / 2;
            Rect2 rect = new Rect2(position - halfSize, cellSize);
            DrawRect(rect, regionColor, filled: true);
        }
    }
}