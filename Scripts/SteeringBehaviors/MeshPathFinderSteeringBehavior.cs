using System.Collections.Generic;
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
    [ExportCategory("CONFIGURATION:")]
    private Tools.Target _pathTarget;
    [Export] public Tools.Target PathTarget
    {
        get => _pathTarget;
        set
        {
            if (_pathTarget == value) return;
            _pathTarget = value;
            _pathTarget.Connect(
                Tools.Target.SignalName.PositionChanged,
                new Callable(this, MethodName.OnPathTargetPositionChanged));
            if (_meshNavigationPathFinder == null) return;
            UpdateTargetPosition(value.Position);
        }
    }
    
    [Export] public float AgentRadius { get; set; } = 50.0f;
    
    [ExportCategory("DEBUG:")] 
    [Export] public bool ShowGizmos { get; set; } = true;
    [Export] public Color GizmosColor { get; set; } = Colors.Yellow;
    
    private Path _currentPath = new();

    public Path CurrentPath
    {
        get => _currentPath;
        private set
        {
            _currentPath = value;
            _pathFollowingSteeringBehavior.FollowPath = value;
        }
    }
    
    private PathFollowingSteeringBehavior _pathFollowingSteeringBehavior;
    private MeshNavigationAgent2D _meshNavigationPathFinder;
    private bool _showGizmos;
    private Color _gizmosColor;

    public override void _Ready()
    {
        _pathFollowingSteeringBehavior = this.FindChild<PathFollowingSteeringBehavior>();
        
        // Configure the mesh pathfinder.
        _meshNavigationPathFinder = this.FindChild<MeshNavigationAgent2D>();
        _meshNavigationPathFinder.Radius = AgentRadius;
        _meshNavigationPathFinder.Connect(
            NavigationAgent2D.SignalName.PathChanged,
            new Callable(this, MethodName.OnPathUpdated));
        
        // Show path gizmo if we are debugging.
        CurrentPath.Name = $"{Name} - Path";
        CurrentPath.ShowGizmos = ShowGizmos;
        CurrentPath.GizmosColor = GizmosColor;
        GetTree().Root.CallDeferred(MethodName.AddChild, CurrentPath);

        // Make the agent head to the target if we already have one.
        if (PathTarget == null) return;
        UpdateTargetPosition(PathTarget.Position);
    }
    
    /// <summary>
    /// <p>Callback for when the target position changes.</p>
    /// <p>This method updates the mesh navigation server's target position and generates
    /// a synthetic call to the server to force it to recalculate the path to the new
    /// target.</p>'
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

    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        if (!_meshNavigationPathFinder.IsReady) return SteeringOutput.Zero;
        if (CurrentPath.PathLength == 0)
        {
            // Just in case, force a query to the mesh navigation server to generate
            // a new path.
            _meshNavigationPathFinder?.GetNextPathPosition();
            return SteeringOutput.Zero;
        } 
        return _pathFollowingSteeringBehavior.GetSteering(args);
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