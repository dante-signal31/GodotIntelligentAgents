using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Pathfinding;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// Represents a steering behavior responsible for integrating pathfinding logic.
/// </summary>
[Tool]
public partial class PathFinderSteeringBehavior : Node2D, ISteeringBehavior
{
    [ExportCategory("CONFIGURATION:")]
    // I haven't used ITargeter interface because that interface waits for a Target field 
    // of type Node2D, but I need a Target type to connect to its PositionChanged signal.
    [Export] public Tools.Target PathTarget { get; set; }
    [Export] public MapGraph Graph { get; set; }
    
    private PathFollowingSteeringBehavior _pathFollowingSteeringBehavior;
    private IPathFinder _pathFinder;
    
    public override void _Ready()
    {
        _pathFollowingSteeringBehavior = this.FindChild<PathFollowingSteeringBehavior>();
        _pathFinder = this.FindChild<IPathFinder>();
        _pathFinder.Graph = Graph;
        PathTarget.Connect(
            Tools.Target.SignalName.PositionChanged,
            new Callable(this, MethodName.OnPathTargetPositionChanged));
    }

    private void OnPathTargetPositionChanged(Vector2 newTargetPosition)
    {
        Path newPath = _pathFinder.FindPath(newTargetPosition);
        _pathFollowingSteeringBehavior.FollowPath = newPath;
    }
    
    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        return _pathFollowingSteeringBehavior.GetSteering(args);
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new();
        
        _pathFollowingSteeringBehavior = this.FindChild<PathFollowingSteeringBehavior>();
        if (_pathFollowingSteeringBehavior == null)
        {
            warnings.Add("This node needs a child of type " +
                         "PathFollowingSteeringBehavior to work.");
        }
        
        _pathFinder = this.FindChild<IPathFinder>();
        if (_pathFinder == null)
        {
            warnings.Add("This node needs a child of type IPathFinder to work.");
        }
        
        return warnings.ToArray();
    }


}