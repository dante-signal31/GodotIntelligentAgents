using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Pathfinding;

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

    public override void _Ready()
    {
        _currentAgent = this.FindAncestor<MovingAgent>();
        _pathFollowingSteeringBehavior = this.FindChild<PathFollowingSteeringBehavior>();
        
        // Configure the mesh pathfinder.
        _meshNavigationPathFinder = this.FindChild<MeshNavigationAgent2D>();
        _meshNavigationPathFinder.Radius = _currentAgent.Radius;
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
        
        // Actually, we are using the avoidance vector calculated after the last
        // GetSteering call. But just one frame lag does not seem a big deal.
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