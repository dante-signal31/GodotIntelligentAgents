using Godot;
using System.Collections.Generic;
using Godot.Collections;

// It must be marked as Tool to be found by HideSteeringBehavior when it uses my custom
// extension method FindChild<T>(). Otherwise, FindChild casting to HidePointsDetector
// will fail. It seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
/// <summary>
/// <p>This nodes tracks an specific treat and tries to find hiding points from it using
/// a provided list of level obstacles positions.<p>
/// <p>Valid hiding points detected are offered through HidingPoints property.</p>
/// </summary>
public partial class HidingPointsDetector : Node2D
{
    [ExportCategory("CONFIGURATION:")]
    /// <summary>
    /// Agent to hide from.
    /// </summary>
    [Export] public Node2D Threat { get; set; }
    
    /// <summary>
    /// Obstacles positions in the level.
    /// </summary>
    [Export] public Array<Vector2> ObstaclesPositions { get; private set; } = 
        new();
    
    private uint _obstaclesLayers = 1;
    /// <summary>
    /// At which physics layers the obstacles belong to?
    /// </summary>
    [Export(PropertyHint.Layers2DPhysics)] public uint ObstaclesLayers
    {
        get => _obstaclesLayers;
        set
        {
            _obstaclesLayers = value;
            if (_rayCaster != null) _rayCaster.CollisionMask = value;
        }
    }

    private float _separationFromObstacles = 100f;
    /// <summary>
    /// How much separation our hiding point must show from obstacles?
    /// </summary>
    [Export] public float SeparationFromObstacles
    {
        get => _separationFromObstacles;
        set
        {
            _separationFromObstacles = value;
            if (_cleanAreaShapeCircle != null)
                _cleanAreaShapeCircle.Radius = value + AgentRadius;
        }
    }

    private float _agentRadius = 50f;
    /// <summary>
    /// How wide is the agent we want to hide?
    /// </summary>
    [Export] public float AgentRadius
    {
        get => _agentRadius;
        set
        {
            _agentRadius = value;
            if (_cleanAreaShapeCircle != null) 
                _cleanAreaShapeCircle.Radius = SeparationFromObstacles + value;
        }
    }

    private uint _notEmptyGroundLayers = 1;
    /// <summary>
    /// A position with any of this physic layers objects is not empty ground to be a
    /// valid hiding point.
    /// </summary>
    [Export(PropertyHint.Layers2DPhysics)] public uint NotEmptyGroundLayers
    {
        get => _notEmptyGroundLayers;
        set
        {
            _notEmptyGroundLayers = value;
            if (_cleanAreaChecker != null) _cleanAreaChecker.CollisionMask = value;
        }
    }
    
    /// <summary>
    /// Maximum scene obstacles inner distance.
    /// </summary>
    [Export] public float MaximumInnerRayDistance { get; set; } = 1000f;
    
    /// <summary>
    /// <p>Maximum distance behind the obstacle to find a valid hiding point.</p>
    /// <p>WARNING! This value must be bigger than
    /// SeparationFromObstacles + AgentRadius.</p>
    /// </summary>
    [Export] public float MaximumExternalRayDistance { get; set; } = 300f;
    
    [ExportCategory("DEBUG:")]
    /// <summary>
    /// Show calculations gizmos.
    /// </summary>
    [Export] public bool ShowCalculationGizmos { get; set; }
    [Export] public Color RayColor { get; set; } = Colors.Green;
    [Export] public Color InnerRayColor { get; set; } = Colors.Red;
    [Export] private float CollisionPointRadius { get; set; } = 5f;
    [Export] private float HidingPointRadius { get; set; } = 10f;

    public List<Vector2> HidingPoints { get; private set; } = new();
    
    private RayCast2D _rayCaster;
    private Area2D _cleanAreaChecker;
    private CollisionShape2D _cleanAreaShape;
    private CircleShape2D _cleanAreaShapeCircle;

    /// <summary>
    /// Minimum radius free from obstacles and other obstacles to be a valid hiding point.
    /// </summary>
    private float MinimumCleanRadius => SeparationFromObstacles + AgentRadius;
    
    private List<Vector2> RayCollisionPoints { get; set; } = new();
    private List<(Vector2, Vector2)> InnerRayCollisionEnds { get; set; } = new();
    private List<(Vector2, Vector2)> ExternalRayCollisionEnds { get; set; } = new();

    public override void _EnterTree()
    {
        InitRayCaster();
        InitCleanAreaChecker();
    }

    private void InitCleanAreaChecker()
    {
        _cleanAreaChecker = new Area2D();
        _cleanAreaChecker.CollisionMask = NotEmptyGroundLayers;
        _cleanAreaShapeCircle = new CircleShape2D();
        _cleanAreaShapeCircle.Radius = SeparationFromObstacles + AgentRadius;
        _cleanAreaShape = new CollisionShape2D();
        _cleanAreaShape.Shape = _cleanAreaShapeCircle;
        _cleanAreaChecker.AddChild(_cleanAreaShape);
    }

    private void InitRayCaster()
    {
        _rayCaster = new RayCast2D();
        _rayCaster.CollisionMask = ObstaclesLayers;
        _rayCaster.HitFromInside = true;
        _rayCaster.CollideWithBodies = true;
        _rayCaster.CollideWithAreas = false;
        _rayCaster.Enabled = true;
    }

    public override void _ExitTree()
    {
        _rayCaster.QueueFree();
        _cleanAreaChecker.QueueFree();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_rayCaster == null) return;
        
        UpdateRayCollisionPoints();
        UpdateInnerRayCollisionPoints();
        UpdateHidingPoints();
    }
    
    /// <summary>
    /// <p>Update the list of sight collision points between the threat agent and the
    /// obstacles in the level.</p>
    /// <p>This collision points is the raycast collision point between the threat
    /// agent position and the obstacle position.</p>
    /// </summary>
    private void UpdateRayCollisionPoints()
    {
        RayCollisionPoints.Clear();
        _rayCaster.GlobalPosition = Threat.GlobalPosition;
        foreach (Vector2 obstaclesPosition in ObstaclesPositions)
        {
            _rayCaster.TargetPosition = _rayCaster.ToLocal(obstaclesPosition);
            if (_rayCaster.IsColliding())
            {
                RayCollisionPoints.Add(_rayCaster.GetCollisionPoint());
            }
        }
    }
    
    /// <summary>
    /// <p>Update the list of exit points of the rays between the threat agent positions
    /// and the obstacles positions in the level, once those rays are continued further
    /// from the obstacles center position to exit the obstacles colliders.</p>
    /// </summary>
    private void UpdateInnerRayCollisionPoints()
    {
        InnerRayCollisionEnds.Clear();
        foreach (Vector2 rayCollisionPoint in RayCollisionPoints)
        {
            _rayCaster.GlobalPosition = rayCollisionPoint;
            Vector2 _rayDirection =
                (rayCollisionPoint - Threat.GlobalPosition).Normalized();
            _rayCaster.TargetPosition = _rayDirection * MaximumInnerRayDistance;
            if (_rayCaster.IsColliding())
            {
                Vector2 innerRayCollisionPoint = _rayCaster.GetCollisionPoint();
                InnerRayCollisionEnds.Add((rayCollisionPoint, innerRayCollisionPoint));
            }
        }
    }

    /// <summary>
    /// <p>Once the ray exits the obstacle collider through the exit point, we must
    /// extend the ray to the nearest empty ground position. That place is going to be
    /// a valid hiding point.</p>
    /// <p>Problem comes from not convex obstacles, obstacles with holes or obstacles
    /// with other obstacles near to them. In those cases, the hiding point can be
    /// inside an obstacle again.</p>
    /// <p>To avoid those cases, we must check if the hiding point is inside an obstacle
    /// casting a volume over the check point. If that cast returns nothing then the
    /// point is valid for hiding. But if the cast returns something, then the hiding
    /// point is not valid and we must check another point further in the ray
    /// direction.</p>
    /// <p>I could have used this method from the very beginning since we found the
    /// first ray collision points, but it is resource intensive, so I preferred to use
    /// an internal raycast. Hopefully, most cases won't need to use the volume casting
    /// to find a valid point after first obstacle.</p> 
    /// </summary>
    private void UpdateHidingPoints()
    {
        HidingPoints.Clear();
        ExternalRayCollisionEnds.Clear();
        foreach ((Vector2 rayCollisionPoint, Vector2 innerRayCollisionPoint) in
                 InnerRayCollisionEnds)
        {
            Vector2 rayDirection =
                (innerRayCollisionPoint - rayCollisionPoint).Normalized();
            Vector2 candidateHidingPoint = innerRayCollisionPoint +
                                           rayDirection *
                                           MinimumCleanRadius;
            _cleanAreaChecker.GlobalPosition = candidateHidingPoint;
            while (candidateHidingPoint.Length() < MaximumExternalRayDistance)
            {
                if (IsCleanHidingPoint(candidateHidingPoint))
                {
                    // Candidate point zone is clean. We can place and agent there.
                    HidingPoints.Add(candidateHidingPoint);
                    ExternalRayCollisionEnds.Add(
                        (innerRayCollisionPoint, candidateHidingPoint));
                    break;
                }
                else
                {
                    // Candidate point zone is not clean. We must extend the ray.
                    candidateHidingPoint = candidateHidingPoint.Normalized() *
                                           (candidateHidingPoint.Length() +
                                            MinimumCleanRadius);
                }
            }
        }
    }

    /// <summary>
    /// Whether this point is free from obstacles to be a valid hiding point.
    /// </summary>
    /// <param name="hidingPoint">Position to check</param>
    /// <returns>True if position is free from obstacles, false otherwise</returns>
    private bool IsCleanHidingPoint(Vector2 hidingPoint)
    {
        _cleanAreaChecker.GlobalPosition = hidingPoint;
        return (_cleanAreaChecker.GetOverlappingBodies().Count == 0);
    }
    
    public override void _Process(double delta)
    {
        if (ShowCalculationGizmos) DrawGizmos();
    }

    private void DrawGizmos()
    {
        QueueRedraw();
    }
    
    public override void _Draw()
    {
        if (!ShowCalculationGizmos ||
            Engine.IsEditorHint()) return;
        foreach (Vector2 obstacle in ObstaclesPositions)
        {
            
            DrawLine(
                ToLocal(Threat.GlobalPosition), 
                ToLocal(obstacle), 
                RayColor);
        }
        
        foreach (Vector2 rayCollisionPoint in RayCollisionPoints)
        {
            DrawCircle(
                ToLocal(rayCollisionPoint),
                CollisionPointRadius,
                RayColor,
                filled: false);
        }

        foreach ((Vector2 rayCollisionPoint, Vector2 innerRayCollisionPoint) in 
                 InnerRayCollisionEnds)
        {
            DrawLine(
                ToLocal(rayCollisionPoint),
                ToLocal(innerRayCollisionPoint),
                InnerRayColor);
            DrawCircle(
                ToLocal(innerRayCollisionPoint),
                CollisionPointRadius,
                InnerRayColor,
                filled: false);
        }

        foreach ((Vector2 innerRayCollisionPoint, Vector2 hidingPoint) in 
                 ExternalRayCollisionEnds)
        {
            DrawLine(
                ToLocal(innerRayCollisionPoint),
                ToLocal(hidingPoint),
                RayColor);
            DrawCircle(
                ToLocal(hidingPoint),
                HidingPointRadius,
                RayColor);
        }
    }
}
