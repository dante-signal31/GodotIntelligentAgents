using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Pathfinding;

namespace GodotGameAIbyExample.Scripts.Sensors;

/// <summary>
/// The FEMSenseManager class is responsible for managing sensors and propagating signals
/// across a graph-based structure. While RegionSenseManager deals with signals that
/// disappear once delivered, FEMSenseManager can deal with smell-like signals that
/// linger in a place dissipating over time until its disappearance.
/// </summary>
public partial class FEMSenseManager: RegionSenseManager
{
    [ExportCategory("FEM SENSE MANAGER CONFIGURATION:")] 
    /// <summary>
    /// How many seconds between each dissipation update.
    /// </summary>
    [Export] public float DissipationUpdatePeriod = 1.0f;
    /// <summary>
    /// Under this intensity, a node will be considered as low that does not
    /// disseminate anymore.
    /// </summary>
    [Export] public float MinimumDisseminationIntensity = 5.0f;
    
    
    [ExportCategory("DEBUG:")] 
    [Export] public bool ShowGizmos;
    [Export] public Color DissipationColor = Colors.Red;
    [Export(PropertyHint.Range, "0.0,1.0,0.1")] public float GizmoAlpha = 0.5f;
    [Export] public uint FrameRedrawCounter { get; set; } = 40;
    
    [ExportCategory("WIRING:")]
    [Export] private SmellMapGraph _mapGraph;
    
    Dictionary<uint, float> _dissipationIntensities = new();
    private readonly Dictionary<uint, float> _nodeIntensities = new();
    private readonly Dictionary<uint, List<IRegionSenseSensor>> _registeredNodeSensors = 
        new();
    private readonly Queue<PositionNode> _openNodes = new();
    private readonly HashSet<uint> _closedNodes = new();
    private uint _frameCounter;
    
    private Timer _dissipationUpdateTimer;

    private void GetChildrenReferences()
    {
        _dissipationUpdateTimer = this.FindChild<Timer>();
    }

    public override void _Ready()
    {
        base._Ready();
        GetChildrenReferences();
        ConfigureDissipationTimer();
    }

    private void ConfigureDissipationTimer()
    {
        _dissipationUpdateTimer.WaitTime = DissipationUpdatePeriod;
        _dissipationUpdateTimer.OneShot = false;
        _dissipationUpdateTimer.Autostart = true;
        
        if (_dissipationUpdateTimer.IsConnected(
                Timer.SignalName.Timeout,
                new Callable(this, MethodName.OnDissipationUpdate))) return;
        _dissipationUpdateTimer.Connect(
            Timer.SignalName.Timeout, 
            new Callable(this, MethodName.OnDissipationUpdate));
        _dissipationUpdateTimer.Start();
    }

    private void OnDissipationUpdate()
    {
        _dissipationIntensities.Clear();
        // Apply dissipation in any node that already has a smell intensity.
        foreach (KeyValuePair<uint, float> nodeIntensity in _nodeIntensities)
        {
            Vector2 nodePosition = _mapGraph.GetNodeById(nodeIntensity.Key).Position;
            float nodeDissipation = _mapGraph.GetPositionCustomFloatData(
                nodePosition, 
                "SmellDissipation");
            float newIntensity = Mathf.Max(
                0, 
                nodeIntensity.Value * Mathf.Pow(nodeDissipation, DissipationUpdatePeriod));
            _dissipationIntensities[nodeIntensity.Key] = newIntensity;
        }
    }

    public override void RegisterSensor(IRegionSenseSensor sensor)
    {
        PositionNode node = _mapGraph.GetNodeAtNearestPosition(sensor.GlobalPosition);
        if (!_registeredNodeSensors.ContainsKey(node.Id))
            _registeredNodeSensors.Add(node.Id, new List<IRegionSenseSensor>());
        _registeredNodeSensors[node.Id].Add(sensor);
    }
    
    public override void UnregisterSensor(IRegionSenseSensor sensor)
    {
        PositionNode node = _mapGraph.GetNodeAtNearestPosition(sensor.GlobalPosition);
        _registeredNodeSensors[node.Id].Remove(sensor);
    }
    
    /// <summary>
    /// Called by signal sources to send a signal to the sensors.
    /// </summary>
    /// <remarks>
    /// FEMSenseManager uses a map-graph-based approach to determine whether to relay or
    /// not a signal to sensors.
    /// </remarks>
    /// <param name="signal">Signal to be sent.</param>
    public override void RegisterSignal(RegionSenseSignal signal)
    {
        UpdateNodeIntensitiesWithSignal(signal);
        foreach (KeyValuePair<uint, List<IRegionSenseSensor>> nodeSensors in
                 _registeredNodeSensors)
        { 
            NotifySensors(signal, nodeSensors.Value.ToArray());
        }
        
    }

    /// <summary>
    /// Disseminate the signal through the graph and add it to the remaining intensities
    /// of previous signals not yet dissipated.
    /// </summary>
    /// <param name="signal">Signal just received.</param>
    private void UpdateNodeIntensitiesWithSignal(RegionSenseSignal signal)
    {
        Dictionary<uint, float> disseminationIntensities = new();
        
        // Nodes not fully explored yet, ordered as they are found to traverse the graph
        // using a breath-first order.
        _openNodes.Clear();
        
        // Nodes already fully explored.
        _closedNodes.Clear();
        
        // Start from the source signal node.
        PositionNode sourceNode = 
            _mapGraph.GetNodeAtNearestPosition(signal.Source.GlobalPosition);
        disseminationIntensities[sourceNode.Id] = signal.Strength;
        _openNodes.Enqueue(sourceNode);
        // Add the node to be explored in the closed list once you have added it to the
        // open list. This way you can avoid exploring the same node multiple times. 
        _closedNodes.Add(sourceNode.Id);
        
        // Breath-first exploration of the graph.
        while (_openNodes.Count > 0)
        {
            PositionNode current = _openNodes.Dequeue();
            
            foreach (GraphConnection graphConnection in current.Connections.Values)
            {
                // Where does that connection lead us?
                PositionNode endNode = _mapGraph.GetPositionNodeById(graphConnection.EndNodeId);
                // If that connection leads to an already explored node, skip it.
                if (_closedNodes.Contains(endNode.Id)) continue;
                
                // Otherwise, calculate the signal attenuation from the current node
                // through this connection.
                float strengthThroughConnection =
                    // In a smell graph, the connection cost is the connection
                    // attenuation. We could have used de ModalityAttenuation, like we did 
                    // in RegionSenseManager. In this case, I use the graph not only as
                    // a way to take in count obstacles in dissemination, but also as a
                    // way to use customizable attenuation and dissipation values per
                    // tile.
                    disseminationIntensities[current.Id] * graphConnection.Cost;
                
                // If a node has such a low signal intensity, skip it.
                if (strengthThroughConnection < MinimumDisseminationIntensity) continue;
                
                // As we are using the same attenuation factor for all connections, and
                // the exploration is a breath-first order, we can safely assume that
                // the first time you meet this end node, it will receive the signal
                // with the highest possible intensity. 
                disseminationIntensities[endNode.Id] = strengthThroughConnection;
                
                // Include the discovered node in the open set to explore it further
                // later.
                _openNodes.Enqueue(endNode);
                // Add the node to be explored in the closed list once you have added it
                // to the open list. This way you can avoid exploring the same node
                // multiple times.
                _closedNodes.Add(endNode.Id);
            }
        }
        
        // Node intensity is a sum of dissipation (the remaining intensity from previous
        // iterations) and dissemination (the added intensity from the new signal). Now
        // that dissemination intensities are calculated, we must add the dissipation 
        // intensities.
        _nodeIntensities.Clear();
        // First, dissipation intensities.
        foreach (KeyValuePair<uint, float> dissipationIntensity in _dissipationIntensities)
        {
            _nodeIntensities[dissipationIntensity.Key] = dissipationIntensity.Value;
        }
        // Next, dissemination intensities.
        foreach (KeyValuePair<uint, float> disseminationIntensity in disseminationIntensities)
        {
            if (_nodeIntensities.ContainsKey(disseminationIntensity.Key))
            {
                _nodeIntensities[disseminationIntensity.Key] += disseminationIntensity.Value;
            }
            else
            {
                _nodeIntensities[disseminationIntensity.Key] = disseminationIntensity.Value;
            }
        }
    }

    protected override bool SignalPowerfulEnoughForSensor(
        RegionSenseSignal signal, 
        IRegionSenseSensor sensor)
    {
        uint sensorNodeId = _mapGraph.GetNodeAtNearestPosition(sensor.GlobalPosition).Id;
        if (!_nodeIntensities.TryGetValue(sensorNodeId, out var receivedPower)) 
            return false;
        return receivedPower >= sensor.ModalityThreshold(signal.Modality);
    }

    public override string[] _GetConfigurationWarnings()
    {
        GetChildrenReferences();

        List<string> warnings = new();

        if (_dissipationUpdateTimer== null)
        {
            warnings.Add("This node need a child Timer node to work. ");
        }

        return warnings.ToArray();
    }
    
    public override void _Process(double delta)
    {
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
        
        Vector2 cellSize = _mapGraph.CellSize;
        
        // Get the maximum intensity.
        float maxIntensity = 0f;
        foreach (KeyValuePair<uint, float> nodeIntensity in _nodeIntensities)
        {
            if (nodeIntensity.Value > maxIntensity)
            {
                maxIntensity = nodeIntensity.Value;
            }
        }

        foreach (KeyValuePair<uint, float> nodeIntensity in _nodeIntensities)
        {
            IPositionNode node = _mapGraph.GetNodeById(nodeIntensity.Key);
            Vector2 position = node.Position;
            float intensity = nodeIntensity.Value;
            
            // Don't draw any cell whose intensity is under minimum.
            if (intensity < MinimumDisseminationIntensity) continue;
            
            // Debug color saturation is proportional to normalized intensity.
            float normalizedIntensity = intensity / maxIntensity;
            float hue = DissipationColor.H;
            float saturation = normalizedIntensity;
            float value = 1.0f;
            Color intensityColor = Color.FromHsv(hue, saturation, value);
            intensityColor.A = currentAlpha;
            
            // Draw a rectangle with the intensity color covering the node cell.
            Vector2 halfSize = cellSize / 2;
            Rect2 rect = new Rect2(position - halfSize, cellSize);
            DrawRect(rect, intensityColor, filled: true);
        }
    }
}