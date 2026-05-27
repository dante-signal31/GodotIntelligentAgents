using System;
using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;

namespace GodotGameAIbyExample.Scripts.Sensors;

/// <summary>
/// TouchSensor is a sensor component that detects objects
/// entering, staying, or leaving its defined touch area.
/// </summary>
[Tool]
public partial class TouchSensor: Node2D, ISensor
{
    public event Action<Node2D> ObjectEnteredSensor;
    public event Action<Node2D> ObjectStayedInSensor;
    public event Action<Node2D> ObjectLeftSensor;
    
    [ExportCategory("CONFIGURATION:")]
    [Export(PropertyHint.Layers2DPhysics)] private uint _detectionLayers;

    public HashSet<Node2D> DetectedObjects { get; } = new();

    public bool AnyObjectDetected => DetectedObjects.Count > 0;
    
    private Area2D _touchArea;
    private MovingAgent _currentAgent;

    private void GetChildrenReferences()
    {
        _touchArea = this.FindChild<Area2D>();
    }

    private void SubscribeToArea2DSignals()
    {
        if (_touchArea == null) return;
        _touchArea.Connect(Area2D.SignalName.BodyEntered,
            new Callable(this, MethodName.OnBodyEntered));
        _touchArea.Connect(Area2D.SignalName.BodyExited,
            new Callable(this, MethodName.OnBodyExited));
    }
    
    private void OnBodyEntered(Node2D body)
    {
        // Ignore our own agent.
        if (body.Name == _currentAgent.Name) return;
        
        if (!DetectedObjects.Add(body)) return;
        ObjectEnteredSensor?.Invoke(body);
    }
    
    private void OnBodyExited(Node2D body)
    {
        // Ignore our own agent.
        if (body.Name == _currentAgent.Name) return;
        
        if (!DetectedObjects.Remove(body)) return;
        ObjectLeftSensor?.Invoke(body);
    }

    public override void _Ready()
    {
        GetChildrenReferences();
        SubscribeToArea2DSignals();
        _currentAgent = GetParent<MovingAgent>();
    }

    public override void _PhysicsProcess(double delta)
    {
        foreach (Node2D detectedObject in DetectedObjects)
        {
            ObjectStayedInSensor?.Invoke(detectedObject);
        }
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new();
        
        GetChildrenReferences();
        
        if (_touchArea == null)
        {
            warnings.Add("This node needs a child of type Area2D to work.");
        }
        
        return warnings.ToArray();
    }
}