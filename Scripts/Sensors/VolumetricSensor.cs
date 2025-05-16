using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;

namespace GodotGameAIbyExample.Scripts.Sensors;

/// <summary>
/// This component emits events when objects are detected by the volumetric sensor it is
/// attached to. 
/// </summary>
[Tool]
public partial class VolumetricSensor : Node2D
{

    [Signal] private delegate void ObjectEnteredAreaEventHandler(Node2D detectedObject);
    
    [Signal] private delegate void ObjectStayedInAreaEventHandler(Node2D detectedObject);
    
    [Signal] private delegate void ObjectLeftAreaEventHandler(Node2D detectedObject);

    [ExportCategory("CONFIGURATION:")]
    /// <summary>
    /// Specifies the physics layers that the will monitor for detections.
    /// </summary>
    [Export(PropertyHint.Layers2DPhysics)] public uint DetectionLayers { get; set; }
    
    /// <summary>
    /// Current set of objects that is inside the detection area.
    /// </summary>
    public HashSet<Node2D> DetectedObjects { get; } = new();
    
    /// <summary>
    /// Whether there is any object inside the detection area.
    /// </summary>
    public bool AnyObjectDetected => DetectedObjects.Count > 0;
    
    private Area2D _area;
    private CollisionShape2D _collisionShape;
    
    public override void _Ready()
    {
        _area = this.FindChild<Area2D>();
        if (_area == null) return;
        // Look for collision shape. It may be under VolumetricSensor or under Area2D.
        // If it's under VolumetricSensor, we need to reparent it to Area2D to run the
        // game. But if we are in the editor, we need to reparent it to VolumetricSensor
        // in order to be able to link collision shape with BoxManager.
        _collisionShape = this.FindChild<CollisionShape2D>();
        if (_collisionShape == null)
        {
            _collisionShape = this.FindChild<CollisionShape2D>(recursive: true);
            if (_collisionShape == null) return;
        }
        
        if (Engine.IsEditorHint())
        {
            _collisionShape.Reparent(this);
        }
        else
        {
            _collisionShape.Reparent(_area);
        }
        
        _area.BodyEntered += OnObjectEntered;
        _area.BodyExited += OnObjectExited;

        UpdateDetectedObjectsSet();
    }

    
    public override void _ExitTree()
    {
        _collisionShape.Reparent(this);
    }

    /// <summary>
    /// <p>Update DetectedObjects set with any object inside the detection area.</p>
    /// <p>It's especially useful to detect objects just created inside the detection
    /// area, so they don't trigger any entered event.</p>
    /// </summary>
    private void UpdateDetectedObjectsSet()
    {
        if (_area == null || _collisionShape == null) return;
        foreach (Node2D body in _area.GetOverlappingBodies())
        {
            if (DetectedObjects.Contains(body)) continue;
            DetectedObjects.Add(body);
            EmitSignal(SignalName.ObjectEnteredArea, body);
        }
    }
    
    private void OnObjectEntered(Node2D body)
    {
        if (DetectedObjects.Contains(body)) return;
        DetectedObjects.Add(body);
        EmitSignal(SignalName.ObjectEnteredArea, body);
    }

    private void OnObjectExited(Node2D body)
    {
        if (!DetectedObjects.Contains(body)) return;
        DetectedObjects.Remove(body);
        EmitSignal(SignalName.ObjectLeftArea, body);
    }

    public override void _PhysicsProcess(double delta)
    {
        foreach (var detectedObject in DetectedObjects)
        {
            EmitSignal(SignalName.ObjectStayedInArea, detectedObject);
        }
    }

    public override string[] _GetConfigurationWarnings()
    {
        Area2D area = this.FindChild<Area2D>();
        CollisionShape2D collisionShape = this.FindChild<CollisionShape2D>();

        List<string> warnings = new();

        if (area== null)
        {
            warnings.Add("This node needs a child Area2D node to work. ");
        }

        if (collisionShape == null)
        {
            warnings.Add("This node needs a child CollisionShape2D node to work");
        }

        return warnings.ToArray();
    }
}