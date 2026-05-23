using System;
using System.Collections.Generic;
using System.Timers;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Pathfinding;
using Timer = System.Timers.Timer;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// A steering behavior responsible for pathfinding on a navigation mesh.
/// This class allows an agent to follow a dynamically updated path
/// to a specified target while considering obstacles and navigation-specific constraints.
/// It integrates with Godot's `NavigationAgent2D` to calculate the path and uses
/// a path-following steering behavior to adjust the agent's movement along the path.
/// </summary>
[Tool]
public partial class MeshPathFinderSteeringBehavior: Node2D, ISteeringBehavior, IGizmos
{
    [ExportCategory("PATHFINDER CONFIGURATION:")]
    private Tools.Target _pathTarget;
    [Export] public Tools.Target PathTarget
    {
        get => _pathTarget;
        set
        {
            if (_pathTarget == value) return;
            _pathTarget = value;
            if (!_pathTarget.IsConnected(
                    Tools.Target.SignalName.PositionChanged,
                    new Callable(this, MethodName.OnPathTargetPositionChanged)))
            {
                _pathTarget.Connect(
                    Tools.Target.SignalName.PositionChanged,
                    new Callable(this, MethodName.OnPathTargetPositionChanged));
            }
            if (_meshNavigationPathFinder == null) return;
            UpdateTargetPosition(value.Position);
        }
    }
    
    [ExportCategory("AGENT AVOIDANCE CONFIGURATION:")]
    
    private bool _avoidAgents = true;
    /// <summary>
    /// If true, the agent will avoid other agents.
    /// </summary>
    [Export] public bool AvoidAgents
    {
        get => _avoidAgents;
        set
        {
            _avoidAgents = value;
            if (_meshNavigationPathFinder == null) return;
            _meshNavigationPathFinder.AvoidanceEnabled = value;
        }
    }
    
    private float _minimumDistanceBetweenAgents = 50f;
    [Export] public float MinimumDistanceBetweenAgents
    {
        get => _minimumDistanceBetweenAgents;
        set
        {
            _minimumDistanceBetweenAgents = value;
            if (_meshNavigationPathFinder == null || _currentAgent == null) return;
            _meshNavigationPathFinder.Radius = value + _currentAgent.Radius;
        }
    }

    private float _agentDetectionRange = 200f;
    /// <summary>
    /// How far from the current agent other agents will be detected.
    /// </summary>
    [Export] public float AgentDetectionRange
    {
        get => _agentDetectionRange;
        set
        {
            _agentDetectionRange = value;
            if (_meshNavigationPathFinder == null) return;
            _meshNavigationPathFinder.NeighborDistance = value;
        }
    }
    
    private float _timeHorizon = 1.0f;
    /// <summary>
    /// Maximum time to collision to calculate evasion vectors from other agents.
    /// </summary>
    [Export] public float TimeHorizon
    {
        get => _timeHorizon;
        set
        {
            _timeHorizon = value;
            if (_meshNavigationPathFinder == null) return;
            _meshNavigationPathFinder.TimeHorizonAgents = value;
        }
    }
    
    private uint _agentLayer = 1;
    /// <summary>
    /// The navigation layer this agent belongs to.
    /// </summary>
    [Export(PropertyHint.Layers2DNavigation)] public uint AgentLayer
    {
        get => _agentLayer;
        set
        {
            _agentLayer = value;
            if (_meshNavigationPathFinder == null) return;
            _meshNavigationPathFinder.AvoidanceLayers = value;
        }
    }
    
    private uint _agentDetectionLayers = 1;
    /// <summary>
    /// The navigation layer other agents will be detected on.
    /// </summary>
    [Export(PropertyHint.Layers2DNavigation)] public uint AgentDetectionLayers
    {
        get => _agentDetectionLayers;
        set
        {
            _agentDetectionLayers = value;
            if (_meshNavigationPathFinder == null) return;
            _meshNavigationPathFinder.AvoidanceMask = value;
        }
    }
    
    /// <summary>
    /// Threshold factor for determining when to use normal vector avoidance.
    /// When the dot product between avoidance and collision vectors exceeds this value 
    /// (positive or negative), the avoidance vector is replaced with a vector normal 
    /// to the collision agent's velocity to prevent chase or collision scenarios.
    /// </summary>
    [Export] public float TooAlignedFactor = 0.95f;
    
    /// <summary>
    /// Timer for too aligned situations.
    /// </summary>
    [Export] public float AvoidanceTimeout { get; set; } = 0.5f;
    
    [ExportCategory("DEBUG:")] 
    [Export] public bool ShowGizmos { get; set; } = true;
    [Export] public Color GizmosColor { get; set; } = Colors.Yellow;
    
    private Path _currentPath = new();
    /// <summary>
    /// Path currently followed by the agent
    /// </summary>
    public Path CurrentPath
    {
        get => _currentPath;
        private set
        {
            _currentPath = value;
            if (_pathFollowingSteeringBehavior == null) return;
            _pathFollowingSteeringBehavior.FollowPath = value;
        }
    }
    
    private MovingAgent _currentAgent;
    private PathFollowingSteeringBehavior _pathFollowingSteeringBehavior;
    private MeshNavigationAgent2D _meshNavigationPathFinder;
    private Vector2 _currentAvoidVector;
    private readonly Random _random = new();
    private SteeringOutput _currentSteering;
    private Timer _avoidanceTimer;
    private bool _waitingForAvoidanceTimeout;

    public override void _Ready()
    {
        _currentAgent = this.FindAncestor<MovingAgent>();
        _pathFollowingSteeringBehavior = this.FindChild<PathFollowingSteeringBehavior>();

        // Configure the mesh pathfinder.
        _meshNavigationPathFinder = this.FindChild<MeshNavigationAgent2D>();
        _meshNavigationPathFinder.Radius = _currentAgent.Radius +
                                           MinimumDistanceBetweenAgents;
        _meshNavigationPathFinder.Connect(
            NavigationAgent2D.SignalName.PathChanged,
            new Callable(this, MethodName.OnPathUpdated));

        // Show path gizmo if we are debugging.
        if (CurrentPath == null) CurrentPath = new Path();
        CurrentPath.Name = $"{Name} - Path";
        CurrentPath.ShowGizmos = ShowGizmos;
        CurrentPath.GizmosColor = GizmosColor;
        GetTree().Root.CallDeferred(MethodName.AddChild, CurrentPath);

        // Configure agent avoidance.
        _meshNavigationPathFinder.Connect(
            NavigationAgent2D.SignalName.VelocityComputed,
            new Callable(this, MethodName.OnAvoidVectorComputed));
        _meshNavigationPathFinder.MaxSpeed = _currentAgent.MaximumSpeed;
        AvoidAgents = _avoidAgents;
        AgentDetectionRange = _agentDetectionRange;
        TimeHorizon = _timeHorizon;
        AgentLayer = _agentLayer;
        AgentDetectionLayers = _agentDetectionLayers;

        // Make the agent head to the target if we already have one.
        if (PathTarget == null) return;
        UpdateTargetPosition(PathTarget.Position);
        
        // Set up timer.
        _avoidanceTimer = new Timer(AvoidanceTimeout * 1000);
        _avoidanceTimer.AutoReset = false;
        _avoidanceTimer.Elapsed += OnAvoidanceTimeout;
    }
    
    private void OnAvoidanceTimeout(object sender, ElapsedEventArgs elapsedEventArgs)
    {
        _waitingForAvoidanceTimeout = false;
    }
    
    private void StartAvoidanceTimer()
    {
        _avoidanceTimer.Stop();
        _avoidanceTimer.Start();
        _waitingForAvoidanceTimeout = true;
    }

    /// <summary>
    /// <p>Callback for when the target position changes.</p>
    /// <p>This method updates the mesh navigation server's target position and generates
    /// a synthetic call to the server to force it to recalculate the path to the new
    /// target.</p>
    /// </summary>
    /// <param name="newTargetPosition">New target position.</param>
    private void OnPathTargetPositionChanged(Vector2 newTargetPosition)
    {
        if (Engine.IsEditorHint() || !CanProcess()) return;
        UpdateTargetPosition(newTargetPosition);
    }

    /// <summary>
    /// This method updates the mesh navigation server's target position and generates
    /// a synthetic call to the server to force it to recalculate the path to the new
    /// target.
    /// </summary>
    /// <param name="newTargetPosition"></param>
    private void UpdateTargetPosition(Vector2 newTargetPosition)
    {
        _meshNavigationPathFinder.TargetPosition = newTargetPosition;
        // Oddly, you must call GetNextPathPosition() to make navigation server
        // recalculate the path to the new target position.
        _meshNavigationPathFinder.GetNextPathPosition();
    }
    
    /// <summary>
    /// Callback for when the mesh navigation server updates its path to the new target
    /// position.
    /// </summary>
    private void OnPathUpdated()
    {
        CurrentPath = new Path(_meshNavigationPathFinder.PathToTarget);
    }

    /// <summary>
    /// Callback for when the mesh navigation server calculates the best vector to avoid
    /// collision with other agents while moving towards the target.
    /// </summary>
    /// <param name="avoidVector">Calculated avoid vector.</param>
    private void OnAvoidVectorComputed(Vector2 avoidVector)
    {
        _currentAvoidVector = avoidVector;
    }
    
    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        // Calculate best path to target.
        if (_meshNavigationPathFinder == null || !_meshNavigationPathFinder.IsReady)
            return SteeringOutput.Zero;

        // Sometimes, the agent gets its target, but a nearby agent makes it move from
        // the target. The problem is that, as path finding and avoidance are separate
        // systems, the pathfinder thinks that the agent stays at the target, so it does
        // not return a new vector to reach it. In those cases we must force pathfinder
        // to recalculate the path.
        if (_meshNavigationPathFinder.IsNavigationFinished() &&
            GlobalPosition.DistanceTo(_meshNavigationPathFinder.TargetPosition) >
            _meshNavigationPathFinder.TargetDesiredDistance)
        {
            OnPathTargetPositionChanged(_meshNavigationPathFinder.TargetPosition);
        }
        
        // Godot needs this call every physics frame to keep navigation map updated.
        _meshNavigationPathFinder?.GetNextPathPosition();
        
        // No path? then no move.
        if (CurrentPath.PathLength == 0) return SteeringOutput.Zero;
        
        SteeringOutput pathSteering = _pathFollowingSteeringBehavior.GetSteering(args);
        
        // If we don't want to avoid other agents, then we end here.
        if (!AvoidAgents) return pathSteering;
        
        // If we want to avoid other agents also, then we need to calculate the avoidance
        // vector nearest to our current path. 
        //
        // Tell our mesh navigation server which is our vector to target. This lets
        // it calculate the avoidance vector nearest to our current path.
        _meshNavigationPathFinder.SetVelocity(pathSteering.Linear);
            
        // Now, get the avoidance vector. Actually, we are using the avoidance vector
        // calculated after the last GetSteering call. But just one frame lag does not
        // seem a big deal. But we must check the avoidance vector for the classic
        // edge case for avoidance systems.
        //
        // THE EDGE CASE:
        // The builtin avoidance system in Godot suffers the same edge problem
        // as Millington's or ANN. The problem is that it does not seem to take in
        // count the edge case where the two agents are going one against the other
        // directly, in opposite directions. The rest of the method fixes that.
        //
        // One way to find out if the two agents are going one against the other in 
        // opposite directions is to check the dot product between the evasion vector
        // and the current velocity. If the absolute value of a dot product is near 1,
        // that means the two agents are going away or approaching, in both cases in
        // the same "line". In the first case, it wouldn't be a collision, but we want
        // an avoidance movement, not a chase.In the second case, that means that the
        // two agents are approaching in opposite directions.
        if (_currentAvoidVector != Vector2.Zero && !_waitingForAvoidanceTimeout)
        {
            // Remember that in other algorithms we calculated evasion vector and
            // afterward we assed it to the velocity to get the final vector. But in 
            // this case MeshNavigationAgent2D already calculates the final vector. So,
            // to get the evasion vector, we must subtract the current velocity from
            // the calculated vector.
            Vector2 evasionVector = (_currentAvoidVector - pathSteering.Linear);
            float alignmentFactor = Mathf.Abs(
                evasionVector.Normalized().Dot(
                    args.CurrentAgent.Velocity.Normalized())); 
            if (Mathf.Abs(alignmentFactor) >= TooAlignedFactor && evasionVector.Length() > 50f)
            {
                // If relative velocity is too aligned with evasionVector, then it means
                // we can end in a direct hit, so we try an evasion vector that is
                // perpendicular to the current agent's velocity.
                Vector2 avoidVectorNormalized = args.CurrentAgent.Velocity
                                                    .Rotated(Mathf.Pi / 2)
                                                    .Normalized() * 
                                                // Turn to one side or another randomly.
                                                (_random.Next(2) * 2 - 1); 
                _currentAvoidVector = avoidVectorNormalized * args.MaximumSpeed;
                StartAvoidanceTimer();
            }
        }
        
        SteeringOutput avoidSteering = new SteeringOutput(
            _currentAvoidVector,
            pathSteering.Angular
        );
        return avoidSteering;
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        _pathFollowingSteeringBehavior = this.FindChild<PathFollowingSteeringBehavior>();
        _meshNavigationPathFinder = this.FindChild<MeshNavigationAgent2D>();

        List<string> warnings = new();
        
        if (_pathFollowingSteeringBehavior == null)
        {
            warnings.Add("This node needs a child of type " +
                         "PathFollowingSteeringBehavior to work.");
        }
        if (_meshNavigationPathFinder == null)
        {
            warnings.Add("This node needs a child of type MeshNavigationAgent2D to " +
                         "work.");
        }
        
        return warnings.ToArray();
    }
}