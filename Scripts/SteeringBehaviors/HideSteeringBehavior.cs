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
    [Export] public MovingAgent Threat { get; set; }
    
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

    private RayCast2D _rayCaster;
    
    private Vector2 _hidingPoint;
    private bool _hidingNeeded;
    private Node2D _nextMovementTarget;
    
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
    }

    public override void _ExitTree()
    {
        _nextMovementTarget.QueueFree();
        _rayCaster.QueueFree();
    }

    public override void _Ready()
    {
        _seekSteeringBehavior = this.FindChild<SeekSteeringBehavior>();
        _seekSteeringBehavior.Target = _nextMovementTarget;
        _seekSteeringBehavior.ArrivalDistance = ArrivalDistance;
        _hidingPointsDetector = this.FindChild<HidingPointsDetector>();
        _hidingPointsDetector.ObstaclesLayers = ObstaclesLayers;
        _hidingPointsDetector.SeparationFromObstacles = SeparationFromObstacles;
        _hidingPointsDetector.AgentRadius = AgentRadius;
        _hidingPointsDetector.NotEmptyGroundLayers = NotEmptyGroundLayers;
        _navigationAgent2D = this.FindChild<NavigationAgent2D>();
        _navigationAgent2D.Velocity = Vector2.Zero;
        _navigationAgent2D.TargetPosition = _hidingPoint;
        _navigationAgent2D.Radius = AgentRadius;
    }

    public override void _PhysicsProcess(double delta)
    {
        _nextMovementTarget.GlobalPosition = _navigationAgent2D.GetNextPathPosition();
    }

    // TODO: Detect we have line of sight to the threat and start hiding.
    
    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        throw new NotImplementedException();
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
