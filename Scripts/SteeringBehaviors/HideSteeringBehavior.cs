using Godot;
using System.Collections.Generic;
using System.Timers;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Pathfinding;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;

// It must be marked as Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
/// <summary>
/// <p>Node to offer a hiding steering behaviour.</p>
/// <p>Hiding makes an agent to place itself after an obstacle between him and a
/// threat.</p>
/// </summary>
public partial class HideSteeringBehavior : Node2D, ISteeringBehavior
{
    [ExportCategory("CONFIGURATION:")] 
    private GodotGameAIbyExample.Scripts.SteeringBehaviors.MovingAgent _threat;
    /// <summary>
    /// Agent to hide from.
    /// </summary>
    [Export] public GodotGameAIbyExample.Scripts.SteeringBehaviors.MovingAgent Threat
    {
        get => _threat;
        set
        {
            _threat = value;
            if (_hidingPointsDetector != null)  
                _hidingPointsDetector.Threat = value;
            if (_rayCast2D != null)
                _rayCast2D.CollisionMask = Threat.CollisionLayer | ObstaclesLayers;
        }
    }
    
    /// <summary>
    /// Distance at which we give our goal as reached and we stop our agent.
    /// </summary>
    [Export] public float ArrivalDistance { get; set; } = .1f;
    
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
            if (_hidingPointsDetector != null) 
                _hidingPointsDetector.ObstaclesLayers = value;
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
            if (_hidingPointsDetector != null)
                _hidingPointsDetector.SeparationFromObstacles = value;
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
            if (_hidingPointsDetector != null)
                _hidingPointsDetector.AgentRadius = value;
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
            if (_hidingPointsDetector != null)
                _hidingPointsDetector.NotEmptyGroundLayers = value;
        }
    }
    /// <summary>
    /// Minimum time in seconds between hiding point path recalculations.
    /// </summary>
    [Export] private float pathRecalculationTime = 0.5f;

    [ExportCategory("DEBUG:")]
    /// <summary>
    /// Show gizmos.
    /// </summary>
    [Export] public bool ShowGizmos { get; set; }
    [Export] public Color RayColor { get; set; } = Colors.Green;

    private Vector2 _hidingPoint;
    /// <summary>
    /// Current hiding point position selected by this behavior.
    /// </summary>
    public Vector2 HidingPoint
    {
        get => _hidingPoint;
        private set
        {
            _hidingPoint = value;
            if (_navigationAgent2D != null)
                _navigationAgent2D.TargetPosition = value;
        }
    }

    /// <summary>
    /// Whether this agent is currently visible by its threat.
    /// </summary>
    public bool VisibleByThreat => _threatCanSeeUs;
    
    private HidingPointsDetector _hidingPointsDetector;
    private INavigationAgent _navigationAgent2D;
    private SeekSteeringBehavior _seekSteeringBehavior;
    private Courtyard _currentLevel;
    private RayCast2D _rayCast2D;
    private Vector2 _previousThreatPosition = Vector2.Zero;
    
    private bool ThreatHasJustMoved => Threat.GlobalPosition != _previousThreatPosition;
    
    private bool _threatCanSeeUs;
    private bool _hidingPointRecheckNeeded;
    private bool _hidingPointReached;
    private Node2D _nextMovementTarget;
    private System.Timers.Timer _pathRecalculationTimer;
    private bool _pathRecalculationCooldownActive;
    
    public override void _EnterTree()
    {
        if (_nextMovementTarget == null) _nextMovementTarget = new Node2D();
        HidingPoint = GlobalPosition;
        _pathRecalculationTimer = new System.Timers.Timer(pathRecalculationTime * 1000);
        _pathRecalculationTimer.AutoReset = false;
        _pathRecalculationTimer.Elapsed += OnRecalculationPathTimerTimeout;
    }
    
    private void OnRecalculationPathTimerTimeout(object sender, ElapsedEventArgs e)
    {
        _pathRecalculationCooldownActive = false;
    }

    private void StartPathRecalculationTimer()
    {
        _pathRecalculationTimer.Stop();
        _pathRecalculationTimer.Start();
        _pathRecalculationCooldownActive = true;
    }

    public override void _ExitTree()
    {
        _nextMovementTarget?.QueueFree();
    }

    public override void _Ready()
    {
        Node2D _currentRoot = GetTree().Root.FindChild<Node2D>();
        if (_currentRoot == null) return;
        _currentLevel = _currentRoot.FindChild<Courtyard>();
        // Next guard is needed to not receiving warnings when this node is opened in its
        // own scene.
        if (_currentLevel == null) return;
        InitRayCast2D();
        InitSeekSteeringBehavior();
        InitHidingPointDetector();
        InitNavigationAgent();
    }

    private void InitRayCast2D()
    {
        _rayCast2D = this.FindChild<RayCast2D>();
        if (_rayCast2D == null) return;
        if (Threat != null) 
            _rayCast2D.CollisionMask = Threat.CollisionLayer | ObstaclesLayers;
        // Make HitFromInside false to not detect our own agent.
        _rayCast2D.HitFromInside = false;
        _rayCast2D.CollideWithBodies = true;
        _rayCast2D.CollideWithAreas = false;
        _rayCast2D.Enabled = true;
    }

    private void InitNavigationAgent()
    {
        _navigationAgent2D = this.FindChild<INavigationAgent>();
        _navigationAgent2D.TargetPosition = _hidingPoint;
        _navigationAgent2D.Radius = AgentRadius;
    }

    private void InitHidingPointDetector()
    {
        _hidingPointsDetector = this.FindChild<HidingPointsDetector>();
        _hidingPointsDetector.Threat = Threat;
        _hidingPointsDetector.ObstaclesPositions = _currentLevel.ObstaclePositions;
        _hidingPointsDetector.ObstaclesLayers = ObstaclesLayers;
        _hidingPointsDetector.SeparationFromObstacles = SeparationFromObstacles;
        _hidingPointsDetector.AgentRadius = AgentRadius;
        _hidingPointsDetector.NotEmptyGroundLayers = NotEmptyGroundLayers;
    }

    private void InitSeekSteeringBehavior()
    {
        _seekSteeringBehavior = this.FindChild<SeekSteeringBehavior>();
        _seekSteeringBehavior.Target = _nextMovementTarget;
        _seekSteeringBehavior.ArrivalDistance = ArrivalDistance;
    }

    public override void _PhysicsProcess(double delta)
    { 
        if (Threat == null || _rayCast2D == null) return;
        
        // Check if there is a line of sight with the threat.
        _rayCast2D.TargetPosition = ToLocal(Threat.GlobalPosition);
        _rayCast2D.ForceRaycastUpdate();
        if (_rayCast2D.IsColliding())
        {
            Node detectedCollider = (Node) _rayCast2D.GetCollider();
            _threatCanSeeUs = (detectedCollider.Name == Threat.Name);
        }
        else
        {
            _threatCanSeeUs = true;
        }
        
        // Starting threat position counts as ThreatHasJustMoved because
        // _previousThreatPosition is init as Vector2.Zero.
        if (ThreatHasJustMoved && !_pathRecalculationCooldownActive)
        {
            _hidingPointRecheckNeeded = true;
            // A path recalculation cooldown is needed, or the path will be recalculated
            // repeatedly while the threat moves without giving a useful hiding path until
            // the threat stops for the first time.
            StartPathRecalculationTimer();
        }
        _previousThreatPosition = Threat.GlobalPosition;
        
        // Do not query when the map has never synchronized and is empty.
        if (!_navigationAgent2D.IsReady) return;
        // Only query when the navigation agent has not reached the target yet.
        if (_navigationAgent2D.IsNavigationFinished())
        {
            _hidingPointReached = true;
        }
        else
        {
            _nextMovementTarget.GlobalPosition = _navigationAgent2D.GetNextPathPosition();
        }
    }
    
    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        // Look for a new hiding point if the threat can see us and if it is threat first 
        // position (only once) or has just moved.
        if (_threatCanSeeUs && _hidingPointRecheckNeeded && 
            !_pathRecalculationCooldownActive ||
            // This second condition is needed for blender steeringBehaviors, where 
            // another steering behavior can move this agent without HideSteeringBehavior
            // intervention.
            _threatCanSeeUs && _hidingPointReached) 
        { // Search for the nearest hiding point.
            List<Vector2> hidingPoints = _hidingPointsDetector.HidingPoints;
            if (hidingPoints.Count > 0)
            {
                float minimumDistance = float.MaxValue;
                Vector2 nearestHidingPoint = Vector2.Zero;
                foreach (Vector2 candidatePoint in hidingPoints)
                {
                    _navigationAgent2D.TargetPosition = candidatePoint;
                    _hidingPointReached = false;
                    float currentDistance =_navigationAgent2D.DistanceToTarget();
                    if (currentDistance < minimumDistance)
                    {
                        minimumDistance = currentDistance;
                        nearestHidingPoint = candidatePoint;
                    }
                }
                HidingPoint = nearestHidingPoint;
                _hidingPointRecheckNeeded = false;
            }
        }

        if (!_hidingPointReached)
        {
            // Head to the next step in the path to the hiding point. That next point
            // position is updated in _PhysicsProcess.
            return _seekSteeringBehavior.GetSteering(args);
        }

        // If we don't need to hide, then return zero.
        return SteeringOutput.Zero;
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        _hidingPointsDetector = this.FindChild<HidingPointsDetector>();
        _seekSteeringBehavior = this.FindChild<SeekSteeringBehavior>();
        _navigationAgent2D = this.FindChild<INavigationAgent>();
        _rayCast2D = this.FindChild<RayCast2D>();

        List<string> warnings = new();
        
        if (_hidingPointsDetector == null)
        {
            warnings.Add("This node needs a child of type HidingPointsDetector to work.");
        }
        if (_seekSteeringBehavior == null)
        {
            warnings.Add("This node needs a child of type SeekSteeringBehavior to work.");
        }
        if (_navigationAgent2D == null)
        {
            warnings.Add("This node needs a child that complies with the " +
                         "INavigationAgent interface to work.");
        }
        if (_rayCast2D == null)
        {
            warnings.Add("This node needs a child of type RayCast2D to work.");
        }
        
        return warnings.ToArray();
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
        if (!ShowGizmos ||
            !Engine.IsEditorHint()) return;
        
        // Draw detection raycast.
        DrawLine(
            ToLocal(_rayCast2D.GlobalPosition), 
            _rayCast2D.TargetPosition, 
            RayColor);
    }
}
