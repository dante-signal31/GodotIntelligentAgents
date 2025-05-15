using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.addons.InteractiveRanges.ConeRange;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Tools;

namespace GodotGameAIbyExample.Scripts.Sensors;

/// <summary>
/// Component to implement a cone of vision sensor.
/// </summary>
[Tool]
public partial class ConeSensor : Node2D
{
    /// <summary>
    /// Delegate for handling events when an object enters the cone of vision.
    /// </summary>
    [Signal] private delegate void ObjectEnteredConeEventHandler(Node2D detectedObject);


    /// <summary>
    /// Delegate for handling events when an object exits the cone of vision.
    /// </summary>
    [Signal] private delegate void ObjectLeftConeEventHandler(Node2D lostObject);

    /// <summary>
    /// Delegate for handling changes in the cone sensor's dimensions,
    /// such as range and angle.
    /// </summary>
    [Signal] private delegate void ConeSensorDimensionsChangedEventHandler(
        float newRange, float newDegrees);

    [ExportCategory("CONFIGURATION:")]
    private float _detectionRange = 10.0f;
    /// <summary>
    /// Range to detect objects.
    /// </summary>
    [Export] private float DetectionRange
    {
        get => _detectionRange;
        set
        {
            _detectionRange = value;
            UpdateDetectionArea();
            EmitSignal(
                SignalName.ConeSensorDimensionsChanged, 
                value, 
                DetectionSemiConeAngle);
        }
    } 
    
    private float _detectionSemiConeAngle = 45.0f;
    /// <summary>
    /// Semicone angle for detection (in degrees).
    /// </summary>
    [Export(PropertyHint.Range, "0,90,0.1")] public float DetectionSemiConeAngle
    {
        get => _detectionSemiConeAngle;
        set
        {
            _detectionSemiConeAngle = value;
            UpdateDetectionArea();
            EmitSignal(
                SignalName.ConeSensorDimensionsChanged,
                DetectionRange,
                value);
        }
    }
    
    /// <summary>
    /// Specifies the physics layers that the sensor will monitor for objects.
    /// </summary>
    [Export(PropertyHint.Layers2DPhysics)] private int LayersToDetect { get; set; } = 1;
    
    /// <summary>
    /// This sensor forward vector.
    /// </summary>
    public Vector2 Forward => GlobalTransform.X;
    
    /// <summary>
    /// <p>List of objects currently inside this sensor range.</p>
    /// <p>Only are considered those objects included in the layermask provided
    /// to ConeSensor.</p> 
    /// </summary>
    public HashSet<Node2D> DetectedObjects { get; private set; } = new();
    
    private VolumetricSensor _sensor;
    private BoxRangeManager _boxRangeManager;
    private ConeRange _coneRange;
    
    public override void _Ready()
    {
        _sensor = this.FindChild<VolumetricSensor>();
        _boxRangeManager = this.FindChild<BoxRangeManager>();
        _coneRange = this.FindChild<ConeRange>();
        
        _sensor.DetectionLayers = LayersToDetect;
    }
    
    private void UpdateDetectionArea()
    {
        if (_boxRangeManager == null) return;
        _boxRangeManager.Range = DetectionRange;
        _boxRangeManager.Width = DetectionRange * 
                                Mathf.RadToDeg(Mathf.Sin(DetectionSemiConeAngle)) * 2;
    }

    /// <summary>
    /// Whether a global position is inside the cone range of the agent.
    /// </summary>
    /// <param name="position">Global position to check.</param>
    /// <returns>True if the position is inside the cone.</returns>
    private bool PositionIsInConeRange(Vector2 position)
    {
        float distance = GlobalPosition.DistanceTo(position);
        float heading = Mathf.RadToDeg(Forward.AngleTo(position - GlobalPosition));
        return distance <= DetectionRange && heading <= DetectionSemiConeAngle;
    }

    /// <summary>
    /// <p>Event handler launched when the cone range gizmo is updated.</p>
    /// <p>This way DetectionRange and DetectionSemiconeAngle are updated.</p>
    /// </summary>
    /// <param name="range">How far we will detect other agents.</param>
    /// <param name="semiConeDegrees">How many degrees from forward we will admit
    /// detecting an agent.</param>
    public void OnConeRangeUpdated()
    {
        DetectionRange = _coneRange.Range;
        DetectionSemiConeAngle = _coneRange.SemiConeDegrees;
    }
    
    /// <summary>
    /// Event handler to use when another object enters our cone area.
    /// </summary>
    /// <param name="otherObject">The object who enters our cone area.</param>
    public void OnObjectEnteredCone(Node2D otherObject)
    {
        if (!PositionIsInConeRange(otherObject.GlobalPosition)) return;
        
        DetectedObjects.Add(otherObject);
        
        EmitSignal(SignalName.ObjectEnteredCone, otherObject);
    }
    
    /// <summary>
    /// Event handler to use when another object exits our detection area.
    /// </summary>
    /// <param name="otherObject">The object who exits our detection area.</param>
    public void OnObjectLeftCone(Node2D otherObject)
    {
        if (!DetectedObjects.Contains(otherObject)) return;
        
        DetectedObjects.Remove(otherObject);

        EmitSignal(SignalName.ObjectLeftCone, otherObject);
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        VolumetricSensor sensor = this.FindChild<VolumetricSensor>();
        BoxRangeManager coneSensorDimensionsChanged = this.FindChild<BoxRangeManager>();
        ConeRange coneRange = this.FindChild<ConeRange>();

        List<string> warnings = new();

        if (sensor == null)
        {
            warnings.Add("This node needs a child VolumetricSensor node to work. ");
        }

        if (coneSensorDimensionsChanged == null)
        {
            warnings.Add("This node needs a child BoxRangeManager node to work");
        }
        
        if (coneRange == null)
        {
            warnings.Add("This node needs a child ConeRange node to work");
        }

        return warnings.ToArray();
    }
}