using System;
using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    [Export] public Array<Vector2> ObstaclesPositions { get; set; } = 
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
    /// Step length to advance the inner ray. The smaller value gives more accuracy to
    /// calculate the exit point but it's slower to calculate. 
    /// </summary>
    [Export] public float InnerRayStep { get; set; } = 3f;
    
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
    private List<(Vector2, Vector2)> AfterCollisionRayEnds { get; set; } = new();

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

    public override void _Ready()
    {
        // TODO: Maybe it's more correct to make them children of the HidingPointsDetector node? 
        GetTree().Root.CallDeferred(MethodName.AddChild, _rayCaster);
        GetTree().Root.CallDeferred(MethodName.AddChild, _cleanAreaChecker);
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
        UpdateCleanHidingPoints();
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
        foreach (Vector2 obstaclePosition in ObstaclesPositions)
        {
            _rayCaster.TargetPosition = _rayCaster.ToLocal(obstaclePosition);
            // If you not force a raycast update, the raycast target position won't
            // be updated until next physics frame.
            _rayCaster.ForceRaycastUpdate();
            if (_rayCaster.IsColliding())
            {
                RayCollisionPoints.Add(_rayCaster.GetCollisionPoint());
            }
        }
    }

    private void UpdateCleanHidingPoints()
    {
        foreach (Vector2 rayCollisionPoint in RayCollisionPoints)
        {
            Vector2 _rayDirection =
                (rayCollisionPoint - Threat.GlobalPosition).Normalized();
            float innerAdvance = 0f;
            while (innerAdvance < MaximumInnerRayDistance)
            {
                innerAdvance += MinimumCleanRadius;
                Vector2 candidateHidingPoint = rayCollisionPoint +
                                               _rayDirection * innerAdvance;
                if (IsCleanHidingPoint(candidateHidingPoint))
                {
                    // Candidate point zone is clean. We can place and agent there.
                    AfterCollisionRayEnds.Add((rayCollisionPoint, candidateHidingPoint));
                    break;
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
        
        // Draw a Line from Threat to Obstacles position.
        foreach (Vector2 obstacle in ObstaclesPositions)
        {
            
            DrawLine(
                ToLocal(Threat.GlobalPosition), 
                ToLocal(obstacle), 
                RayColor);
        }
        
        // Draw a circle at each ray collision point with the obstacle.
        foreach (Vector2 rayCollisionPoint in RayCollisionPoints)
        {
            DrawCircle(
                ToLocal(rayCollisionPoint),
                CollisionPointRadius,
                RayColor,
                filled: false);
        }
        
        // Draw a line from collision point to hiding point.
        foreach ((Vector2 collisionPoint, Vector2 hidingPoint) in 
                 AfterCollisionRayEnds)
        {
            DrawLine(
                ToLocal(collisionPoint),
                ToLocal(hidingPoint),
                RayColor);
            DrawCircle(
                ToLocal(hidingPoint),
                HidingPointRadius,
                InnerRayColor,
                filled: false);
            DrawCircle(ToLocal(hidingPoint),
                MinimumCleanRadius,
                RayColor,
                filled: false);
        }
    }
}
