using Godot;
using System;
using System.Collections.Generic;
using Godot.Collections;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;

// It must be marked as Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
/// <summary>
/// <p> Node to offer a group align steering behaviour. </p>
/// <p> Group align steering behaviour makes the agent look at the same direction than
/// the average orientation of a group of target nodes. </p>
/// </summary>
public partial class GroupAlignSteeringBehavior : Node2D, ISteeringBehavior
{
    [ExportCategory("CONFIGURATION:")]
    /// <summary>
    /// List of agents to align with averaging their orientations.
    /// </summary>
    [Export] public Array<Node2D> Targets { get; set; }
    
    private float _decelerationRadius = 30f;
    /// <summary>
    /// Rotation to start to slow down (degress).
    /// </summary>
    [Export] public float DecelerationRadius 
    { 
        get => _decelerationRadius;
        set
        {
            _decelerationRadius = value;
            if (_alignSteeringBehavior != null)
                _alignSteeringBehavior.DecelerationRadius = value;
        }
    }
    
    private Curve _decelerationCurve;
    /// <summary>
    /// Deceleration curve.
    /// </summary>
    [Export] public Curve DecelerationCurve
    {
        get => _decelerationCurve;
        set
        {
            _decelerationCurve = value;
            if (_alignSteeringBehavior != null)
                _alignSteeringBehavior.DecelerationCurve = value;
        }
    }
    
    private float _accelerationRadius = 30f;
    /// <summary>
    /// At this rotation start angle will be at full speed (degress).
    /// </summary>
    [Export] public float AccelerationRadius
    {
        get => _accelerationRadius;
        set
        {
            _accelerationRadius = value;
            if (_alignSteeringBehavior != null)
                _alignSteeringBehavior.AccelerationRadius = value;
        }
    }
    
    private Curve _accelerationCurve;

    /// <summary>
    /// Acceleration curve.
    /// </summary>
    [Export] public Curve AccelerationCurve
    {
        get => _accelerationCurve;
        set
        {
            _accelerationCurve = value;
            if (_alignSteeringBehavior != null)
                _alignSteeringBehavior.AccelerationCurve = value;
        }
    }
    
    [ExportCategory("DEBUG:")]
    /// <summary>
    /// Make orientation gizmos visible.
    /// </summary>
    [Export] private bool OrientationGizmosVisible { get; set; }
    /// <summary>
    /// Color for other's orientation gizmos.
    /// </summary>
    [Export] private Color OtherOrientationGizmosColor { get; set; }
    /// <summary>
    /// Length for other gizmos markers.
    /// </summary>
    [Export] private float OtherOrientationGizmosLength { get; set; }
    /// <summary>
    /// Agent's own orientation gizmos color.
    /// </summary>
    [Export] private Color OwnOrientationGizmoColor { get; set; }
    /// <summary>
    /// Agent's own orientation gizmos length.
    /// </summary>
    [Export] private float OwnOrientationGizmoLength { get; set; }
    
    /// <summary>
    /// <p>Average orientation, in degrees, counting every agent's targets.</p>
    /// </summary>
    public float AverageOrientation{ get; private set; }

    private Node2D _orientationMarker;
    private AlignSteeringBehavior _alignSteeringBehavior;

    public override void _EnterTree()
    {
        // _orientationMarker will be the target of AlignSteeringBehavior.
        _orientationMarker = new Node2D();
        _orientationMarker.RotationDegrees = RotationDegrees;
    }

    public override void _ExitTree()
    {
        _orientationMarker.QueueFree();
    }

    public override void _Ready()
    {
        _alignSteeringBehavior = this.FindChild<AlignSteeringBehavior>();
        _alignSteeringBehavior.Target = _orientationMarker;
        _alignSteeringBehavior.AccelerationRadius = AccelerationRadius;
        _alignSteeringBehavior.DecelerationRadius = DecelerationRadius;
        _alignSteeringBehavior.AccelerationCurve = AccelerationCurve;
        _alignSteeringBehavior.DecelerationCurve = DecelerationCurve;
    }

    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        if (Targets == null || Targets.Count == 0 || _alignSteeringBehavior == null) 
            return new SteeringOutput(Vector2.Zero, 0);

        // Let's average heading counting every agent's targets. You'd better get an
        // average vector from heading vectors than average their angle rotation values.
        // This way you can be sure that resulting average is in the inner angle between
        // every target vector pair.
        Vector2 headingSum = new();
        
        foreach (Node2D target in Targets)
        {
            // Remember that, for our agents, forward direction point rightwards, i.e. X
            // axis. So, their respective GlobalTtransform.x vectors are actually their
            // heading vectors.
            headingSum += target.GlobalTransform.X;
        }
        Vector2 averageHeading = headingSum / Targets.Count;
        
        // Store resulting orientation.
        AverageOrientation = Mathf.RadToDeg(averageHeading.Angle());
        
        // Rotate our marker to point at the average heading.
        _orientationMarker.GlobalRotationDegrees = AverageOrientation;
        
        return _alignSteeringBehavior.GetSteering(args);
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        _alignSteeringBehavior = this.FindChild<AlignSteeringBehavior>();

        List<string> warnings = new();
        
        if (_alignSteeringBehavior == null)
        {
            warnings.Add("This node needs a child of type AlignSteeringBehavior to work.");
        }
        return warnings.ToArray();
    }
    
    public override void _Process(double delta)
    {
        if (OrientationGizmosVisible) DrawGizmos();
    }

    private void DrawGizmos()
    {
        QueueRedraw();
    }
    
    public override void _Draw()
    {
        if (!OrientationGizmosVisible || Targets == null || Engine.IsEditorHint()) return;

        // Draw other orientation markers.
        foreach (Node2D target in Targets)
        {
            Vector2 targetOrientationDirection = new Vector2(
                Mathf.Cos(Mathf.DegToRad(target.RotationDegrees)),
                Mathf.Sin(Mathf.DegToRad(target.RotationDegrees))
            ) * OtherOrientationGizmosLength;
            DrawLine(
                Vector2.Zero, 
                ToLocal(GlobalPosition + targetOrientationDirection), 
                OtherOrientationGizmosColor);
        }
        
        // Draw resulting average orientation.
        Vector2 ownOrientationDirection = new Vector2(
            Mathf.Cos(Mathf.DegToRad(AverageOrientation)),
            Mathf.Sin(Mathf.DegToRad(AverageOrientation))
        ) * OwnOrientationGizmoLength;
        DrawLine(
            Vector2.Zero, 
            ToLocal(GlobalPosition + ownOrientationDirection), 
            OwnOrientationGizmoColor);
    }
}
