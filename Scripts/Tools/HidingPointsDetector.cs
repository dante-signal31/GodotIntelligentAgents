using System;
using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Schema;
using Godot.Collections;
using GodotGameAIbyExample.Scripts.Extensions;

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
    // TODO: Should this be exported really?
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
    [Export] public float MaximumAdvanceAfterCollision { get; set; } = 1000f;
    
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
    // [Export] public float MaximumExternalRayDistance { get; set; } = 300f;
    
    [ExportCategory("DEBUG:")]
    /// <summary>
    /// Show calculations gizmos.
    /// </summary>
    // TODO: Some of this fields can be made private.
    [Export] public bool ShowCalculationGizmos { get; set; }
    [Export] public Color RayColor { get; set; } = Colors.Green;
    [Export] public Color CleanRadiusColor { get; set; } = Colors.Red;
    [Export] private float CollisionPointRadius { get; set; } = 5f;
    [Export] private float HidingPointRadius { get; set; } = 10f;

    public List<Vector2> HidingPoints { get; private set; } = new();
    
    private RayCast2D _rayCaster;
    private ShapeCast2D _cleanAreaChecker;
    // TODO: _cleanAreaShape should not be global. It can be local to InitCleanAreaChecker()
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
        InitCleanAreaChecker();
    }

    private void InitCleanAreaChecker()
    {
        _cleanAreaShapeCircle = new CircleShape2D();
        _cleanAreaShapeCircle.Radius = MinimumCleanRadius;
        _cleanAreaChecker = new ShapeCast2D();
        _cleanAreaChecker.CollisionMask = NotEmptyGroundLayers;
        _cleanAreaChecker.Shape = _cleanAreaShapeCircle;
        _cleanAreaChecker.CollideWithBodies = true;
        _cleanAreaChecker.TargetPosition = Vector2.Zero;
        _cleanAreaChecker.ExcludeParent = true;
        _cleanAreaChecker.Enabled = true;
    }

    private void InitRayCaster()
    {
        _rayCaster = this.FindChild<RayCast2D>();
        _rayCaster.CollisionMask = ObstaclesLayers;
        _rayCaster.HitFromInside = true;
        _rayCaster.CollideWithBodies = true;
        _rayCaster.CollideWithAreas = false;
        _rayCaster.Enabled = true;
    }

    public override void _Ready()
    {
        CallDeferred(MethodName.AddChild, _cleanAreaChecker);
        InitRayCaster();
    }

    public override void _ExitTree()
    {
        _cleanAreaChecker.QueueFree();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_rayCaster == null || Threat == null) return;
        
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

    /// <summary>
    /// <p>Update the list of points after the obstacles that are clear enough to allow
    /// the agent to hide there.</p>
    /// <p>It uses a ShapeCast2D to continue sight right, after collision with obstacle,
    /// until it finds a valid hiding point free from the obstacle.</p>
    /// </summary>
    private void UpdateCleanHidingPoints()
    {
        AfterCollisionRayEnds.Clear();
        HidingPoints.Clear();
        foreach (Vector2 rayCollisionPoint in RayCollisionPoints)
        {
            Vector2 rayDirection =
                (rayCollisionPoint - Threat.GlobalPosition).Normalized();
            float innerAdvance = 0f;
            while (innerAdvance < MaximumAdvanceAfterCollision)
            {
                innerAdvance += InnerRayStep;
                Vector2 candidateHidingPoint = rayCollisionPoint +
                                               rayDirection * innerAdvance;
                if (IsCleanHidingPoint(candidateHidingPoint))
                {
                    // Candidate point zone is clean. We can place and agent there.
                    HidingPoints.Add(candidateHidingPoint);
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
        // This call is rather expensive. An improvement would be finding a way to not
        // call ForceShapecastUpdate several times in every physics frame.
        _cleanAreaChecker.ForceShapecastUpdate();
        return (!_cleanAreaChecker.IsColliding());
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        _rayCaster = this.FindChild<RayCast2D>();

        List<string> warnings = new();
        
        if (_rayCaster == null)
        {
            warnings.Add("This node needs a child of type RayCast2D to work.");
        }
        
        return warnings.ToArray();
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
        
        foreach ((Vector2 collisionPoint, Vector2 hidingPoint) in 
                 AfterCollisionRayEnds)
        {
            // Draw a line from collision point to hiding point.
            DrawLine(
                ToLocal(collisionPoint),
                ToLocal(hidingPoint),
                RayColor);
            // Mark hiding point position.
            DrawCircle(
                ToLocal(hidingPoint),
                HidingPointRadius,
                CleanRadiusColor,
                filled: false);
            // Draw a circle at hiding point with the clean radius.
            DrawCircle(ToLocal(hidingPoint),
                MinimumCleanRadius,
                RayColor,
                filled: false);
        }
    }
}
