using System;
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
public partial class ConeSensor : Node2D, ISensor
{
    public event Action<Node2D> ObjectEnteredSensor;
    public event Action<Node2D> ObjectStayedInSensor;
    public event Action<Node2D> ObjectLeftSensor;
    public event Action<float, float> ConeSensorDimensionsChanged;
    
    [ExportCategory("CONFIGURATION:")]
    
    private uint _layersToDetect;
    /// <summary>
    /// Specifies the physics layers that the sensor will monitor for objects.
    /// </summary>
    [Export(PropertyHint.Layers2DPhysics)]
    public uint LayersToDetect
    {
        get => _layersToDetect;
        set
        {
            _layersToDetect = value;
            if (_sensor == null) return;
            _sensor.DetectionLayers = value;
        }
    }
    
    [Export] public bool CheckLineOfSight = true;
    [Export(PropertyHint.Layers2DPhysics)] public uint VisualObstaclesLayersMask = 1;
    
    private float _detectionRange;
    /// <summary>
    /// Range to detect objects.
    /// </summary>
    public float DetectionRange
    {
        get => _detectionRange;
        set
        {
            _detectionRange = value;
            UpdateDetectionArea();
            ConeSensorDimensionsChanged?.Invoke(value, DetectionSemiConeAngle);
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
            _detectionSemiConeAngle = value;
            UpdateDetectionArea();
            ConeSensorDimensionsChanged?.Invoke(DetectionRange, value);
        }
    }
    
    /// <summary>
    /// This sensor forward vector.
    /// </summary>
    public Vector2 Forward => GlobalTransform.X;
    
    /// <summary>
    /// <p>List of objects currently inside this sensor range.</p>
    /// <p>Only are considered those objects included in the layer mask provided
    /// to ConeSensor.</p> 
    /// </summary>
    public HashSet<Node2D> DetectedObjects {
        get
        {
            if (CheckLineOfSight) return _objectsInSensorRangeAndVisible;
            return _objectsInSensorRange;
        } 
    }

    /// <summary>
    /// Whether there is any object inside the detection area.
    /// </summary>
    public bool AnyObjectDetected => DetectedObjects.Count > 0;
    
    private VolumetricSensor _sensor;
    private BoxRangeManager _boxRangeManager;
    private ConeRange _coneRange;
    private RayCast2D _lineOfSightRayCast;
    
    private readonly HashSet<Node2D> _objectsInSensorRange = new();
    private readonly HashSet<Node2D> _objectsInSensorRangeAndVisible = new();

    public override void _EnterTree()
    {
        if (_coneRange == null)
            _coneRange = this.FindChild<ConeRange>();
        if (_boxRangeManager == null) 
            _boxRangeManager = this.FindChild<BoxRangeManager>();
        if (_coneRange != null) InitializeConeRange();
        if (_sensor == null)
            _sensor = this.FindChild<VolumetricSensor>();
        _sensor.ObjectEnteredSensor += OnObjectEnteredArea;
        _sensor.ObjectStayedInSensor += OnObjectStayedInArea;
        _sensor.ObjectLeftSensor += OnObjectLeftArea;
    }

    public override void _Ready()
    {
        if (_sensor == null)
            _sensor = this.FindChild<VolumetricSensor>();
        if (_boxRangeManager == null)
            _boxRangeManager = this.FindChild<BoxRangeManager>();
        if (_coneRange == null)
            _coneRange = this.FindChild<ConeRange>();
        if (_coneRange != null) InitializeConeRange();
        if (_lineOfSightRayCast == null)
        {
            _lineOfSightRayCast = this.FindChild<RayCast2D>();
            _lineOfSightRayCast.Enabled = CheckLineOfSight;
            _lineOfSightRayCast.CollisionMask = LayersToDetect | 
                                                VisualObstaclesLayersMask;
        }
        
        _sensor.DetectionLayers = LayersToDetect;
    }

    public override void _ExitTree()
    {
        _sensor.ObjectEnteredSensor -= OnObjectEnteredArea;
        _sensor.ObjectStayedInSensor -= OnObjectStayedInArea;
        _sensor.ObjectLeftSensor -= OnObjectLeftArea;
    }

    private void InitializeConeRange()
    {
        if (_coneRange.IsConnected(
                ConeRange.SignalName.Updated, 
                new Callable(this, MethodName.OnConeRangeUpdated))) return;
        _coneRange.Connect(
            ConeRange.SignalName.Updated,
            new Callable(this, MethodName.OnConeRangeUpdated));
        DetectionRange = _coneRange.Range;
        DetectionSemiConeAngle = _coneRange.SemiConeDegrees;
    }

    private void UpdateDetectionArea()
    {
        if (_boxRangeManager == null) return;
        _boxRangeManager.Range = DetectionRange;
        _boxRangeManager.Width = DetectionRange * 
                                Mathf.Abs(
                                    Mathf.Sin(Mathf.DegToRad(DetectionSemiConeAngle)) 
                                    * 2);
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
    private void OnConeRangeUpdated()
    {
        if (_coneRange == null) return;
        DetectionRange = _coneRange.Range;
        DetectionSemiConeAngle = _coneRange.SemiConeDegrees;
    }

    
    private bool ObjectIsVisible(Node2D otherObject)
    {
        _lineOfSightRayCast.TargetPosition = ToLocal(otherObject.GlobalPosition);
        _lineOfSightRayCast.ForceRaycastUpdate();
        Node2D detectedObject = (Node2D) _lineOfSightRayCast.GetCollider();
        if (detectedObject == null) return false;
        return detectedObject == otherObject;
    }
    
    /// <summary>
    /// Event handler to use when another object enters the detection area.
    /// </summary>
    /// <param name="otherObject">The object who enters the detection area.</param>
    private void OnObjectEnteredArea(Node2D otherObject)
    {
        // Remember that the initial detection area is a square, but our final detection
        // area is a cone whose area is a subset of the square area. So, we need to
        // check if the object is inside the cone range.
        if (!PositionIsInConeRange(otherObject.GlobalPosition)) return;
        
        _objectsInSensorRange.Add(otherObject);
        
        if (!CheckLineOfSight) ObjectEnteredSensor?.Invoke(otherObject);

        // Object can be inside the cone range but behind a cover. So, we must check
        // if there is a line-of-sight with the object.
        if (!CheckLineOfSight || !ObjectIsVisible(otherObject)) return; 
            
        _objectsInSensorRangeAndVisible.Add(otherObject);
        
        ObjectEnteredSensor?.Invoke(otherObject);
    }

    /// <summary>
    /// Event handler to use when another object stays in the detection area.
    /// </summary>
    /// <param name="otherObject">The object stays in the detection area.</param>
    private void OnObjectStayedInArea(Node2D otherObject)
    {
        // Only keep in DetectedObjects those who are in the detection area and in
        // cone range.
        if (!PositionIsInConeRange(otherObject.GlobalPosition) &&
            _objectsInSensorRange.Contains(otherObject))
        {
            _objectsInSensorRange.Remove(otherObject);
            return;
        }

        if (!PositionIsInConeRange(otherObject.GlobalPosition)) return;
        
        // Can an object appear in stay phase without having being detected
        // in enter phase? Yes, it can. If the game starts with the object already inside
        // the sensor range, then it won't be detected in the enter phase but in the stay
        // phase.
        _objectsInSensorRange.Add(otherObject);

        if (!CheckLineOfSight || !ObjectIsVisible(otherObject))
        {
            if (_objectsInSensorRangeAndVisible.Contains(otherObject)) 
                _objectsInSensorRangeAndVisible.Remove(otherObject);
            return;
        }
        
        // This is a HashSet. The type offers a built-in method to avoid duplicated
        // elements. So, we can simply add the element without checking if it is already
        // in the collection.
        _objectsInSensorRangeAndVisible.Add(otherObject);
        
        // We don't call here the ObjectStayedInSensor event because it is called from
        // PhysicsProcess().
    }
    
    /// <summary>
    /// Event handler to use when another object exits our detection area.
    /// </summary>
    /// <param name="otherObject">The object who exits our detection area.</param>
    private void OnObjectLeftArea(Node2D otherObject)
    {
        if (!_objectsInSensorRange.Contains(otherObject)) return;
        
        _objectsInSensorRange.Remove(otherObject);
        
        if (!CheckLineOfSight)
        {
            ObjectLeftSensor?.Invoke(otherObject);
            return;
        }

        if (!_objectsInSensorRangeAndVisible.Contains(otherObject)) return;
        
        _objectsInSensorRangeAndVisible.Remove(otherObject);

        ObjectLeftSensor?.Invoke(otherObject);
    }
    
    public override void _PhysicsProcess(double delta)
    {
        foreach (var detectedObject in DetectedObjects)
        {
            ObjectStayedInSensor?.Invoke(detectedObject);
        }
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