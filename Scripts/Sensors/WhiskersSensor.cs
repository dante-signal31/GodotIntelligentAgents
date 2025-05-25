using System;
using System.Collections;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace GodotGameAIbyExample.Scripts.Sensors;

/// <summary>
/// <p>An array of ray sensors placed over a circular sector.</p>
///
/// <p>The sector is placed around the local -Y axis (UP as you look 2D screen), so the
/// forward direction for this sensor is the local -Y direction.</p>
///
/// <p> Remember that in Godot, +Y goes downwards as you see the screen. So when in
/// comments I say "UP-screen" I really mean the natural UP as you see the screen i.e.,
/// Godot's -Y axis. So, in this script, the Forward vector I defined in the last
/// paragraph and "UP-screen" direction are equivalent.</p> 
///
/// <p>Be aware that when this script detects that you are in editor mode, it will just
/// populate a list of placements shown with gizmos, but it won't instance any sensor
/// until this prefab is placed in the scene.</p>
/// </summary>
[Tool]
public partial class WhiskersSensor : Node2D
{
    /// <summary>
    /// <p>Event to trigger when an object is detected by this sensor.</p>
    /// <p>The event is emitted with the detected object Node2D as a parameter.</p>
    /// </summary>
    [Signal] public delegate void ObjectDetectedEventHandler(Node2D detectedObject);
    
    /// <summary>
    /// <p>Event to trigger when no object is detected by this sensor.</p>
    /// <p>The event is emitted with no parameters.</p>
    /// </summary>
    [Signal] public delegate void NoObjectDetectedEventHandler();
    
    /// <summary>
    /// A class wrapping a list of ray sensors to make it easier to search for them.
    /// </summary>
    private class RaySensorList : IEnumerable<RaySensor>
    {
        
        // It should have 2N + 3 sensors.
        // Think in this array of sensors as looking to UP-screen direction,
        // Inside this list:
        //  * Top left sensor is always at index 0.
        //  * Center sensor is always at the middle index.
        //  * Top right sensor is always at the end index.
        private List<RaySensor> _raySensors;
        
        /// <summary>
        /// Current amount of sensors in this list.
        /// </summary>
        public int Count { get; private set; }
        
        /// <summary>
        /// Get the center sensor.
        /// </summary>
        public RaySensor CenterSensor => _raySensors[Count / 2];
        
        /// <summary>
        /// Get the leftmost sensor (assuming whiskers locally looks to UP-screen
        /// direction).
        /// </summary>
        public RaySensor LeftMostSensor => _raySensors[0];
        
        /// <summary>
        /// Get the rightmost sensor (assuming whiskers locally looks to UP direction).
        /// </summary>
        public RaySensor RightMostSensor => _raySensors[Count - 1];
        
        // <summary>
        /// Get the sensor at the given index counting from the leftmost sensor to center.
        /// </summary>
        /// <param name="index">0 index is the leftmost sensor</param>
        /// <returns></returns>
        public RaySensor GetSensorFromLeft(int index) => _raySensors[index];
        
        // <summary>
        ///  Get the sensor at the given index counting from the rightmost sensor to
        /// center.
        /// </summary>
        /// <param name="index">0 index is the rightmost sensor</param>
        /// <returns></returns>
        public RaySensor GetSensorFromRight(int index) => _raySensors[Count - 1 - index];
        
        public RaySensorList(List<RaySensor> raySensors)
        {
            _raySensors = raySensors;
            Count = _raySensors.Count;
        }
        
        /// <summary>
        /// Remove every sensor in the list.
        ///
        /// This method leaves the list empty.
        /// </summary>
        public void Clear()
        {
            foreach (RaySensor sensor in _raySensors)
            {
                if (sensor != null) sensor.QueueFree();
            }
            _raySensors?.Clear();
            Count = 0;
        }
        
        public IEnumerator<RaySensor> GetEnumerator()
        {
            return _raySensors.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// Class to represent ray ends for every sensor in the editor local space.
    /// </summary>
    public partial class RayEnds: Resource
    {
        public Vector2 Start;
        public Vector2 End;
    }
    
    [ExportCategory("CONFIGURATION:")]
    private uint _sensorsLayersMask;
    /// <summary>
    /// Layers to be detected by this sensor
    /// </summary>
    [Export(PropertyHint.Layers2DPhysics)] public uint SensorsLayersMask
    {
        get => _sensorsLayersMask;
        set
        {
            _sensorsLayersMask = value;
            foreach (RaySensor raySensor in _sensors)
            {
                raySensor.DetectionLayers = _sensorsLayersMask;
            }
        }
    }
    
    private uint _sensorResolution = 1;
    /// <summary>
    /// Number of rays for this sensor: (sensorResolution * 2) + 3"
    /// </summary>
    [Export(PropertyHint.Range, "0, 10, 1")] public uint SensorResolution
    {
        get => _sensorResolution;
        set
        {
            _sensorResolution = value;
            _onValidatingUpdatePending = true;
        }
    }

    private float _semiConeDegrees = 45.0f;
    /// <summary>
    /// Angular width in degrees for this sensor.
    /// </summary>
    [Export(PropertyHint.Range, "0.0, 180.0, 1.0")] public float SemiConeDegrees
    {
        get => _semiConeDegrees;
        set
        {
            _semiConeDegrees = value;
            _onValidatingUpdatePending = true;
        }
    }

    private float _range = 1.0f;
    /// <summary>
    /// Maximum range for these rays.
    /// </summary>
    [Export] public float Range
    {
        get => _range;
        set
        {
            _range = value;
            _onValidatingUpdatePending = true;
        }
    }

    private float _minimumRange = 0.2f;
    /// <summary>
    /// Minimum range for these rays. Useful to make rays start not at the agent's center.
    /// </summary>
    [Export] public float MinimumRange
    {
        get => _minimumRange;
        set
        {
            _minimumRange = value;
            _onValidatingUpdatePending = true;
        }
    }

    /// <summary>
    /// <p>Range proportion for whiskers at left side.</p>
    /// <ul>0.0 = leftmost sensor.</ul>
    /// <ul>1.0 = center sensor.</ul>
    /// </summary>
    [Export] private Curve _leftRangeSemiCone;
    
    /// <summary>
    /// <p>Range proportion for whiskers at right side.</p>
    /// <ul>0.0 = center sensor</ul>
    /// <ul>1.0 = right sensor.</ul>
    /// </summary>
    [Export] private Curve _rightRangeSemiCone;
    
    [Export] private Array<RayEnds> _rayEnds;

    [ExportCategory("DEBUG:")]
    /// <summary>
    /// Whether to show gizmos for sensors.
    /// </summary>
    [Export] private bool ShowGizmos { get; set; }= true;
    
    /// <summary>
    /// Color for this script gizmos.
    /// </summary>
    [Export] private Color GizmoColor { get; set; }= new Color(1, 0, 0);
    
    
    /// <summary>
    /// Number of rays for this sensor with the given resolution.
    /// </summary>
    public uint SensorAmount => (_sensorResolution * 2) + 3;
    
    /// <summary>
    /// Whether this sensor detects any collider.
    /// </summary>
    public bool IsAnyObjectDetected
    {
        get
        {
            if (_sensors == null) return false;
            foreach (RaySensor sensor in _sensors)
            {
                if (sensor.IsObjectDetected) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Set of detected objects.
    /// </summary>
    public HashSet<Node2D> DetectedObjects
    {
        get
        {
            HashSet<Node2D> detectedObjects = new();
            UpdateRayCastHits();
            foreach (RayCastHit hit in _rayCastHits)
            {
                detectedObjects.Add(hit.DetectedObject);
            }
            return detectedObjects;
        }
    }

    /// <summary>
    /// Set of detected hits.
    /// </summary>
    public HashSet<RayCastHit> DetectedHits
    {
        get
        {
            UpdateRayCastHits();
            return _rayCastHits;
        }
    }

    /// <summary>
    /// <p>This node Forward vector.</p>
    /// <p>Actually looking to local screen up direction. So, -Y in Godot's 2D local
    /// axis.</p>
    /// </summary>
    public Vector2 Forward => -GlobalTransform.Y.Normalized();
    
    /// <summary>
    /// Whether this index is the one of the center sensor.
    /// </summary>
    /// <param name="index">Sensor index</param>
    /// <returns>True if the center sensor has this index.</returns>
    public bool IsCenterSensor(int index)=> index == SensorAmount / 2;
    
    
    private RaySensorList _sensors;
    private bool _onValidatingUpdatePending;
    private HashSet<RayCastHit> _rayCastHits = new();

    private void UpdateRayCastHits()
    {
        _rayCastHits.Clear();
        int index = -1;
        foreach (RaySensor sensor in _sensors)
        {
            index++;
            if (!sensor.IsObjectDetected) continue;
            RayCastHit currentHit = sensor.DetectedHit;
            _rayCastHits.Add(currentHit);
        }
    }
    
    /// <summary>
    /// <p>Refresh positions for sensor ends.</p>
    /// <p>These positions are local to the current agent</p>
    /// </summary>
    /// <returns>New list for sensor ends local positions.</returns>
    private void UpdateRayEnds()
    {
        Array<RayEnds> rayEnds = new();

        float totalPlacementAngle = SemiConeDegrees * 2;
        float placementAngleInterval = totalPlacementAngle / (SensorAmount - 1);

        for (int i = 0; i < SensorAmount; i++)
        {
            float currentAngle = SemiConeDegrees - (placementAngleInterval * i);
            Vector2 placementVector = Forward.Rotated(currentAngle);
            Vector2 placementVectorStart = placementVector * MinimumRange;
            Vector2 placementVectorEnd =
                placementVector * (MinimumRange + GetSensorLength(i));
            
            RayEnds rayEnd = new RayEnds{
                Start = placementVectorStart,
                End = placementVectorEnd,
            };
            
            rayEnds.Add(rayEnd);
        }
        
        _rayEnds = rayEnds;
    }

    /// <summary>
    /// Calculates and returns the length of a sensor based on the sensor index provided.
    ///
    /// It uses index to use the proper proportion curve for left and right side.
    /// </summary>
    /// <param name="sensorIndex">Index of this sensor</param>
    /// <returns>This sensor length from the minimum range.</returns>
    private float GetSensorLength(int sensorIndex)
    {
        uint middleSensorIndex = SensorAmount / 2;

        if (sensorIndex < middleSensorIndex)
        {
            return GetLeftSensorLength(sensorIndex, middleSensorIndex);
        }
        return GetRightSensorLength(sensorIndex, middleSensorIndex);
    }

    /// <summary>
    /// Calculate the length of the right sensor based on the sensor index using
    /// a right range semicone curve.
    /// </summary>
    /// <param name="sensorIndex">Index of this sensor.</param>
    /// <param name="middleSensorIndex">Middle sensor index.</param>
    /// <returns>This sensor length from minimum range.</returns>
    private float GetRightSensorLength(int sensorIndex, uint middleSensorIndex)
    {
        float curvePoint = Mathf.InverseLerp(
            middleSensorIndex, 
            SensorAmount - 1, 
            sensorIndex);
        float curvePointRange = _rightRangeSemiCone.Sample(curvePoint) * 
                                (Range - MinimumRange);
        return curvePointRange;
    }

    /// <summary>
    /// Calculate the length of the left sensor based on the sensor index using
    /// a left range semicone curve.
    /// </summary>
    /// <param name="sensorIndex">Index of this sensor.</param>
    /// <param name="middleSensorIndex">Middle sensor index.</param>
    /// <returns>This sensor length from minimum range.</returns>
    private float GetLeftSensorLength(int sensorIndex, uint middleSensorIndex)
    {
        float curvePoint = Mathf.InverseLerp(0, middleSensorIndex, sensorIndex);
        float curvePointRange = _leftRangeSemiCone.Sample(curvePoint) * 
                                (Range - MinimumRange);
        return curvePointRange;
    }

    /// <summary>
    /// Create a new list of sensors and place them.
    /// </summary>
    private void SetupSensors()
    {
        PopulateSensors();
        PlaceSensors();
        SetSensorsLayerMask();
    }


    /// <summary>
    /// <p>Sets the detection layers mask for all sensors to the configured layer mask
    /// value.</p>
    /// <p>Updates the layer mask of each RaySensor in the sensor list to match the
    /// specified SensorsLayersMask.</p>
    /// </summary>
    private void SetSensorsLayerMask()
    {
        foreach (RaySensor raySensor in _sensors)
        {
            raySensor.DetectionLayers = SensorsLayersMask;
        }
    }

    /// <summary>
    /// Place sensors in the correct positions for current resolution and current
    /// range sector.
    /// </summary>
    private void PlaceSensors()
    {
        int i = 0;
        foreach (RayEnds rayEnd in _rayEnds)
        {
            RaySensor raySensor = _sensors.GetSensorFromLeft(i);
            raySensor.GlobalPosition = ToGlobal(rayEnd.Start);
            raySensor.EndPosition = ToGlobal(rayEnd.End);
            i++;
        }
    }

    /// <summary>
    /// Create a new list of sensors.
    /// </summary>
    private void PopulateSensors()
    {
        _sensors?.Clear();
        List<RaySensor> raySensors = new();
        for (int i = 0; i < SensorAmount; i++)
        {
            RaySensor raySensor = new RaySensor();
            AddChild(raySensor);
            raySensors.Add(raySensor);
        }
        _sensors = new RaySensorList(raySensors);
    }

    public override void _Ready()
    {
        
        if (Engine.IsEditorHint())
        { // If in editor then only place gizmos.
            UpdateRayEnds();
        }
        else
        { // If not in editor then create real sensors.
            SetupSensors();
            SubscribeToSensorsEvents();
        }
    }

    /// <summary>
    /// Subscribe to sensors events.
    /// </summary>
    private void SubscribeToSensorsEvents()
    {
        if (_sensors == null) return;
        foreach (RaySensor raySensor in _sensors)
        {
            raySensor.Connect(
                RaySensor.SignalName.ObjectDetected,
                new Callable(this, MethodName.OnObjectDetected));
            raySensor.Connect(
                RaySensor.SignalName.NoObjectDetected,
                new Callable(this, MethodName.OnNoObjectDetected));
        }
    }

    /// <summary>
    /// Handles the detection of an object by emitting the ObjectDetected signal.
    /// </summary>
    /// <param name="detectedObject">The object that was detected by the sensor.</param>
    private void OnObjectDetected(Node2D detectedObject)
    {
        EmitSignal(SignalName.ObjectDetected, detectedObject);  
    }

    /// <summary>
    /// Handles the event when no objects are detected by the sensor system.
    /// </summary>
    private void OnNoObjectDetected()
    {
        if (DetectedObjects.Count == 0) EmitSignal(SignalName.NoObjectDetected);
    }

    public override void _ValidateProperty(Dictionary property)
    {
        // I want to serialize _rayEnds but not show it in the inspector.
        if (property["name"].AsString() == "_rayEnds")
        {
            property["usage"] = (int)PropertyUsageFlags.NoEditor;
        }
    }
    
    public override void _Process(double delta)
    {
        if (ShowGizmos) DrawGizmos();
    }

    private void DrawGizmos()
    {
        QueueRedraw();
    }
    
    public override void _Draw()
    {
        if (!ShowGizmos || !Engine.IsEditorHint()) return;

        foreach (RayEnds rayEnd in _rayEnds)
        {
            DrawLine(
                ToLocal(rayEnd.Start), 
                ToLocal(rayEnd.End), 
                GizmoColor);
            DrawCircle(ToLocal(rayEnd.Start), 10.0f, GizmoColor);
            DrawCircle(ToLocal(rayEnd.End), 10.0f, GizmoColor);
        }
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        SectorRange sectorRange= this.FindChild<SectorRange>();
        
        List<string> warnings = new();
        
        if (!sectorRange == null)
        {
            warnings.Add("This node needs a child node of type " +
                         "SectorRange to work properly.");  
        }
        
        return warnings.ToArray();
    }
    
}