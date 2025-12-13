using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Pathfinding;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

public partial class PathFinderSteeringBehavior: Node2D, ISteeringBehavior
{
    [ExportCategory("CONFIGURATION:")]
    [Export] public Target PathTarget { get; set; }
    [Export] public MapGraph Graph { get; set; }
    
    private PathFollowingSteeringBehavior _pathFollowingSteeringBehavior;
    private IPathFinder _pathFinder;
    
    public override void _Ready()
    {
        _pathFollowingSteeringBehavior = this.FindChild<PathFollowingSteeringBehavior>();
        _pathFinder = this.FindChild<IPathFinder>();
        PathTarget.Connect(
            Target.SignalName.PositionChanged,
            new Callable(this, MethodName.OnPathTargetPositionChanged));
    }

    private void OnPathTargetPositionChanged(Vector2 newTargetPosition)
    {
        Path newPath = _pathFinder.FindPath(Graph, newTargetPosition);
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