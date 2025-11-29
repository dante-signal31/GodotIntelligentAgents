using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Pathfinding;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// A steering behavior that enables an entity to follow a predefined path by navigating
/// between sequential position nodes. 
/// </summary>
[Tool]
public partial class PathFollowingSteeringBehavior: Node2D, ISteeringBehavior
{
    [ExportCategory("CONFIGURATION:")] 
    private Path _foollowPath;

    [Export]
    public Path FollowPath
    {
        get => _foollowPath;
        set
        {
            _foollowPath = value;
            _pathStarted = false;
        }
    }
    // TODO: I'm repeating this param in the underlying steering behavior. I must refactor this into ITargeter interface.
    [Export] private int _arrivalDistance = 10;
    
    private ISteeringBehavior _currentSteeringBehavior;
    private ITargeter _targeter;
    private Node2D _target;
    private bool _pathStarted;

    public override void _Ready()
    {
        _currentSteeringBehavior = this.FindChild<ISteeringBehavior>();
        if (_currentSteeringBehavior == null) return;
        _targeter = (ITargeter) _currentSteeringBehavior;
        _target = new Node2D();
        GetTree().Root.CallDeferred(MethodName.AddChild, _target);
        _targeter.Target = _target;
    }

    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        if (!_pathStarted)
        {
            _target.GlobalPosition = FollowPath.CurrentTargetPosition;
            _pathStarted = true;
        }
        
        float distanceToTarget = 
            GlobalPosition.DistanceTo(FollowPath.CurrentTargetPosition);
        if (distanceToTarget < _arrivalDistance)
        {
            _target.GlobalPosition = FollowPath.GetNextPositionTarget();
        }
        
        return _currentSteeringBehavior.GetSteering(args);
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new();
        
        _currentSteeringBehavior = this.FindChild<ISteeringBehavior>();
        if (_currentSteeringBehavior == null)
        {
            warnings.Add("This node needs a child of type ISteeringBehavior to work.");
        }
        else
        {
            _targeter = _currentSteeringBehavior as ITargeter;
        }
        
        if (_targeter == null)
        {
            warnings.Add("This node needs that child node of type ISteeringBehavior " +
                         "implements ITargeter too to work.");
        }
        
        return warnings.ToArray();
    }
    
}