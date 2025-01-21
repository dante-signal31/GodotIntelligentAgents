using Godot;
using System;
using System.Collections.Generic;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;

// It must be marked as Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
/// <summary>
/// <p>Node to offer an hiding steering behaviour.</p>
/// <p>Hiding makes an agent to place itself after an obstacle between him and a
/// threat.</p>
/// </summary>
public partial class HideSteeringBehavior : Node2D, ISteeringBehavior
{
    [ExportCategory("CONFIGURATION:")] 
    private MovingAgent _threat;
    /// <summary>
    /// Agent to hide from.
    /// </summary>
    [Export] public MovingAgent Threat
    {
        get => _threat;
        set
        {
            _threat = value;
            if (_hidingPointsDetector != null)  
                _hidingPointsDetector.Threat = value;
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

    private HidingPointsDetector _hidingPointsDetector;
    // TODO: Encapsulate navigation agent in its own node to allow to use different pathfinding algorithms. 
    private NavigationAgent2D _navigationAgent2D;
    private SeekSteeringBehavior _seekSteeringBehavior;
    private Courtyard _currentLevel;

    private RayCast2D _rayCaster;
    
    private bool _hidingNeeded;
    private Node2D _nextMovementTarget;

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
    
    public override void _EnterTree()
    {
        _nextMovementTarget = new Node2D();
        _rayCaster = new RayCast2D();
        _rayCaster.ExcludeParent = true;
        _rayCaster.HitFromInside = false;
        _rayCaster.CollideWithBodies = true;
        _rayCaster.CollideWithAreas = false;
        _rayCaster.Enabled = true;
        AddChild(_rayCaster);
        HidingPoint = GlobalPosition;
    }

    public override void _ExitTree()
    {
        _nextMovementTarget.QueueFree();
        _rayCaster.QueueFree();
    }

    public override void _Ready()
    {
        Node _currentRoot = GetTree().Root.FindChild<Node>();
        _currentLevel = _currentRoot.FindChild<Courtyard>();
        InitSeekSteeringBehavior();
        InitHidingPointDetector();
        InitNavigationAgent();
    }

    private void InitNavigationAgent()
    {
        _navigationAgent2D = this.FindChild<NavigationAgent2D>();
        // I want my SeekSteeringBehavior to move the agent, not the navigation agent. So
        // I set its velocity to zero.
        ///////////
        // TODO: Check this. According to the documentation, it should be set to the
        // velocity of the SeekSteeringBehavior. Seems that navigation agent does not 
        // move anything in Godot, but it need velocity to make some calcules.
        _navigationAgent2D.Velocity = Vector2.Zero;
        _navigationAgent2D.MaxSpeed = 0f;
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
        _rayCaster.TargetPosition = Threat.GlobalPosition;
        if (_rayCaster.IsColliding())
        {
            Node detectedCollider = (Node) _rayCaster.GetCollider();
            _hidingNeeded = (detectedCollider.Name == Threat.Name);
        }
        
        // Do not query when the map has never synchronized and is empty.
        Rid currentNavigationMap = _navigationAgent2D.GetNavigationMap();
        if (NavigationServer2D.MapGetIterationId(currentNavigationMap) == 0)
            return;
        // Only query when the navigation agent has not reached the target yet.
        if (!_navigationAgent2D.IsNavigationFinished())
            _nextMovementTarget.GlobalPosition = _navigationAgent2D.GetNextPathPosition();
    }
    
    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        if (_hidingNeeded)
        { // Search for the nearest hiding point.
            List<Vector2> hidingPoints = _hidingPointsDetector.HidingPoints;
            if (hidingPoints.Count > 0)
            {
                float minimumDistance = float.MaxValue;
                Vector2 nearestHidingPoint = Vector2.Zero;
                foreach (Vector2 candidatePoint in hidingPoints)
                {
                    _navigationAgent2D.TargetPosition = candidatePoint;
                    // TODO: Check that DistanceToTarget is the complete path distance and not only the distance to the target.
                    float currentDistance =_navigationAgent2D.DistanceToTarget();
                    if (currentDistance < minimumDistance)
                    {
                        minimumDistance = currentDistance;
                        nearestHidingPoint = candidatePoint;
                    }
                }
                HidingPoint = nearestHidingPoint;
            }
        }
        
        // Head to the next point in the path to the heading target. That next point
        // position is updated in _PhysicsProcess.
        return _seekSteeringBehavior.GetSteering(args);
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        _hidingPointsDetector = this.FindChild<HidingPointsDetector>();
        _seekSteeringBehavior = this.FindChild<SeekSteeringBehavior>();
        _navigationAgent2D = this.FindChild<NavigationAgent2D>();

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
            warnings.Add("This node needs a child of type NavigationAgent2D to work.");
        }
        
        return warnings.ToArray();
    }
}
