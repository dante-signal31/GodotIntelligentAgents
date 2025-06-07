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
    [Signal] public delegate void ObjectEnteredConeEventHandler(Node2D detectedObject);


    /// <summary>
    /// Delegate for handling events when an object exits the cone of vision.
    /// </summary>
    [Signal] public delegate void ObjectLeftConeEventHandler(Node2D lostObject);

    /// <summary>
    /// Delegate for handling changes in the cone sensor's dimensions,
    /// such as range and angle.
    /// </summary>
    [Signal] public delegate void ConeSensorDimensionsChangedEventHandler(
        float newRange, float newDegrees);
    
    [ExportCategory("CONFIGURATION:")]
    /// <summary>
    /// Specifies the physics layers that the sensor will monitor for objects.
    /// </summary>
    [Export(PropertyHint.Layers2DPhysics)] public uint LayersToDetect { get; set; } = 1;
    
    private float _detectionRange;
    /// <summary>
    /// Range to detect objects.
    /// </summary>
    public float DetectionRange
    {
        get => _detectionRange;
        set
        {
            // // Guard needed to avoid infinite calls between this component and _coneRange
            // // when changing the range.
            // if (Mathf.IsEqualApprox(_detectionRange, value)) return;
            
            _detectionRange = value;
            UpdateDetectionArea();
            EmitSignal(
                SignalName.ConeSensorDimensionsChanged, 
                value, 
                DetectionSemiConeAngle);
            
            // if (Mathf.IsEqualApprox(_coneRange.Range, value)) return;
            // _coneRange.Range = value;
        }
    } 
    
    private float _detectionSemiConeAngle;
    /// <summary>
    /// Semicone angle for detection (in degrees).
    /// </summary>
    public float DetectionSemiConeAngle
    {
        get => _detectionSemiConeAngle;
        set
        {
            // Guard needed to avoid infinite calls between this component and _coneRange
            // when changing the angle.
            // if (Mathf.IsEqualApprox(_detectionSemiConeAngle, value)) return;
            
            _detectionSemiConeAngle = value;
            UpdateDetectionArea();
            EmitSignal(
                SignalName.ConeSensorDimensionsChanged,
                DetectionRange,
                value);
            
            // if (Mathf.IsEqualApprox(_coneRange.SemiConeDegrees, value)) return;
            // _coneRange.SemiConeDegrees = value;
        }
    }
    
    /// <summary>
    /// This sensor forward vector.
    /// </summary>
    public Vector2 Forward => GlobalTransform.X;
    
    /// <summary>
    /// <p>List of objects currently inside this sensor range.</p>
    /// <p>Only are considered those objects included in the layermask provided
    /// to ConeSensor.</p> 
    /// </summary>
    public HashSet<Node2D> DetectedObjects { get; } = new();

    /// <summary>
    /// Whether there is any object inside the detection area.
    /// </summary>
    public bool AnyObjectDetected => DetectedObjects.Count > 0;
    
    private VolumetricSensor _sensor;
    private BoxRangeManager _boxRangeManager;
    private ConeRange _coneRange;
    
    public override void _Ready()
    {
        _sensor = this.FindChild<VolumetricSensor>();
        _boxRangeManager = this.FindChild<BoxRangeManager>();
        _coneRange = this.FindChild<ConeRange>();
        
        _sensor.DetectionLayers = LayersToDetect;

        if (_coneRange == null ) return;
        _coneRange.Connect(
            ConeRange.SignalName.Updated,
            Callable.From(OnConeRangeUpdated));
        DetectionRange = _coneRange.Range;
        DetectionSemiConeAngle = _coneRange.SemiConeDegrees;
    }
    
    private void UpdateDetectionArea()
    {
        if (_boxRangeManager == null) return;
        _boxRangeManager.Range = DetectionRange;
        _boxRangeManager.Width = DetectionRange * 
                                Mathf.Sin(Mathf.DegToRad(DetectionSemiConeAngle)) * 2;
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
        if (_coneRange == null) return;
        DetectionRange = _coneRange.Range;
        DetectionSemiConeAngle = _coneRange.SemiConeDegrees;
    }
    
    /// <summary>
    /// Event handler to use when another object enters the detection area.
    /// </summary>
    /// <param name="otherObject">The object who enters the detection area.</param>
    public void OnObjectEnteredArea(Node2D otherObject)
    {
        if (!PositionIsInConeRange(otherObject.GlobalPosition)) return;
        
        DetectedObjects.Add(otherObject);
        
        EmitSignal(SignalName.ObjectEnteredCone, otherObject);
    }

    /// <summary>
    /// Event handler to use when another object stays in the detection area.
    /// </summary>
    /// <param name="otherObject">The object stays in the detection area.</param>
    public void OnObjectStayedInArea(Node2D otherObject)
    {
        if (!PositionIsInConeRange(otherObject.GlobalPosition) &&
            DetectedObjects.Contains(otherObject))
        {
            DetectedObjects.Remove(otherObject);
            return;
        }

        if (!PositionIsInConeRange(otherObject.GlobalPosition)) return;
        
        if (!DetectedObjects.Contains(otherObject)) DetectedObjects.Add(otherObject);
    }
    
    /// <summary>
    /// Event handler to use when another object exits our detection area.
    /// </summary>
    /// <param name="otherObject">The object who exits our detection area.</param>
    public void OnObjectLeftArea(Node2D otherObject)
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