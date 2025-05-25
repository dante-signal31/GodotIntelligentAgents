using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;

namespace GodotGameAIbyExample.Scripts.Sensors;

public partial class RaySensor : Node2D
{
    private const string StartPointName = "StartPoint";
    private const string EndPointName = "EndPoint";
    
    /// <summary>
    /// <p>Signal to emit when the sensor detects a node.</p>
    /// <p>It's emitted with the detected object as parameter.</p>
    /// </summary>
    [Signal] private delegate void ObjectDetectedEventHandler(Node2D detectedObject);
    
    /// <summary>
    /// <p>Signal to emit when the sensor doesn't detect any node.</p>
    /// </summary>
    [Signal] private delegate void NoObjectDetectedEventHandler();
    
    [ExportCategory("CONFIGURATION:")]
    
    private uint _detectionLayers;
    /// <summary>
    /// Layers to be detected by this sensor.
    /// </summary>
    [Export(PropertyHint.Layers2DPhysics)] public uint DetectionLayers
    {
        get => _detectionLayers;
        set
        {
            _detectionLayers = value;
            UpdateRayData();
        }
    }
    
    private bool _ignoreCollidersOverlappingStartPoint;

    /// <summary>
    /// Whether to ignore colliders overlapping start point.
    /// </summary>
    [Export] private bool IgnoreColliderOverlappingStartPoint
    {
        get => _ignoreCollidersOverlappingStartPoint;
        set
        {
            _ignoreCollidersOverlappingStartPoint = value;
            UpdateRayData();
        }
    }
    
    [ExportCategory("WIRING:")]
    /// <summary>
    /// Whether to show debugging gizmos for this sensor.
    /// </summary>
    [Export] private bool ShowGizmos { get; set; }
    
    /// <summary>
    /// Gizmo color for this sensor.
    /// </summary>
    [Export] private Color GizmoColor { get; set; } = new Color(1, 0, 0);
    
    /// <summary>
    /// Color to show when the sensor detects an object.
    /// </summary>
    [Export] private Color GizmoDetectedColor { get; set; } = new Color(0, 1, 0);
    
    /// <summary>
    /// Radius for the gizmos that mark the ray ends.
    /// </summary>
    [Export] private float GizmoRadius { get; set; } = 5.0f;

    /// <summary>
    /// <p>Whether this sensor has detected any object.</p>
    /// <p>It's null if no object is detected.</p>
    /// </summary>
    public bool IsObjectDetected => DetectedObject != null;
    
    private Node2D _detectedObject;

    /// <summary>
    /// Current object detected by this sensor.
    /// </summary>
    public Node2D DetectedObject
    {
        get => _detectedObject;
        private set
        {
            if (_detectedObject == value) return;
            _detectedObject = value;
            if (value == null)
            {
                EmitSignal(SignalName.NoObjectDetected);
            }
            else
            {
                EmitSignal(SignalName.ObjectDetected, value);
            }
        }
    }
    
    /// <summary>
    /// Current hit detected by this sensor.
    /// </summary>
    public RayCastHit DetectedHit { get; private set; }

    /// <summary>
    /// Raycast start global position.
    /// </summary>
    public Vector2 StartPosition
    {
        get => _startPoint.GlobalPosition;
        set
        {
            _startPoint.GlobalPosition = value;
            UpdateRayData();
        }
    }

    /// <summary>
    /// Raycast end global position.
    /// </summary>
    public Vector2 EndPosition
    {
        get => _endPoint.GlobalPosition;
        set
        {
            _endPoint.GlobalPosition = value;
            UpdateRayData();
        }
    }
    
    private Marker2D _startPoint;
    private Marker2D _endPoint;
    private RayCast2D _rayCast = new();
    private Vector2 _rayDirection;
    private float _rayDistance;
    
    private void UpdateRayData()
    {
        _rayCast.Position = _startPoint.Position;
        _rayCast.TargetPosition = _endPoint.Position;
        _rayCast.CollisionMask = DetectionLayers;
        _rayCast.ExcludeParent = IgnoreColliderOverlappingStartPoint;
    }
    
    private void UpdateDetectionHit()
    {
        if (!_rayCast.IsColliding()) return;
        
        Vector2 collisionPoint = _rayCast.GetCollisionPoint();
        float collisionDistance = GlobalPosition.DistanceTo(collisionPoint);
        float rayLength = _rayCast.TargetPosition.Length();
        RayCastHit currentHit = new RayCastHit{
            Position = collisionPoint,
            Normal = _rayCast.GetCollisionNormal(),
            DetectedObject = (Node2D) _rayCast.GetCollider(),
            Distance = collisionDistance,
            Fraction = collisionDistance / rayLength,
        };
        
        DetectedHit = currentHit;
    }

    public override void _Ready()
    {
        _startPoint = (Marker2D) FindChild(StartPointName);
        _endPoint = (Marker2D) FindChild(EndPointName);
        UpdateRayData();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_rayCast.IsColliding() && DetectedObject == _rayCast.GetCollider()) return;

        if (!_rayCast.IsColliding() && DetectedObject == null) return;
        
        if (_rayCast.IsColliding())
        {
            Node2D collider = (Node2D) _rayCast.GetCollider();
            DetectedObject = collider;
            UpdateDetectionHit();
            EmitSignal(SignalName.ObjectDetected, collider);
        }
        else
        {
            DetectedObject = null;
            EmitSignal(SignalName.NoObjectDetected);
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
        
        DrawLine(
            ToLocal(_rayCast.GlobalPosition), 
            ToLocal(ToGlobal(_rayCast.TargetPosition)), 
            GizmoColor);
        DrawCircle(ToLocal(_rayCast.GlobalPosition), 10.0f, GizmoColor);
        DrawCircle(
            ToLocal(ToGlobal(_rayCast.TargetPosition)), 
            10.0f, 
            GizmoColor);
        
        if (!IsObjectDetected) return;

        DrawLine(
            ToLocal(_rayCast.GlobalPosition),
            ToLocal(DetectedHit.Position),
            GizmoDetectedColor);
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        Node startNode = FindChild(StartPointName);
        Node endNode = FindChild(EndPointName);
        
        List<string> warnings = new();
        
        if (!(startNode is Marker2D))
        {
            warnings.Add("This node needs a child node called StartPoint of type " +
                         "Marker2D.");  
        }
        
        if (!(endNode is Marker2D))
        {
            warnings.Add("This node needs a child node called EndPoint of type " +
                         "Marker2D.");  
        }
        
        return warnings.ToArray();
    }
    
    
    
}