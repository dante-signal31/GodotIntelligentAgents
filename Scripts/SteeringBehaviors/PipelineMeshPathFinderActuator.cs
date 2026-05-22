using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Pathfinding;
using GodotGameAIbyExample.Scripts.Tools;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// Actuator that uses a MeshPathFinderSteeringBehavior to get a path to a goal and a
/// steering output to move the agent along the calculated path.
/// </summary>
[Tool]
public partial class PipelineMeshPathFinderActuator: Node2D, IPipelineActuator
{
    private MeshPathFinderSteeringBehavior _meshPathFinderSteeringBehavior;
    private Vector2 _currentGoalPosition;
    
    private void LoadChildrenReferences()
    {
        _meshPathFinderSteeringBehavior = 
            this.FindChild<MeshPathFinderSteeringBehavior>();
    }
    
    public override void _Ready()
    {
        LoadChildrenReferences();
        _meshPathFinderSteeringBehavior.AvoidAgents = false;
    }
    
    public Path GetPath(PipelineGoal goal, SteeringBehaviorArgs args)
    {
        if (_currentGoalPosition != goal.Position)
        {
            _meshPathFinderSteeringBehavior.UpdateTargetPosition(goal.Position);
            _currentGoalPosition = goal.Position;
        }
        
        return _meshPathFinderSteeringBehavior.CurrentPath;
    }

    public SteeringOutput GetOutput(PipelineGoal goal, SteeringBehaviorArgs args)
    {
        if (_currentGoalPosition != goal.Position)
        {
            _meshPathFinderSteeringBehavior.UpdateTargetPosition(goal.Position);
            _currentGoalPosition = goal.Position;
        }
        
        SteeringOutput steeringOutput = 
            _meshPathFinderSteeringBehavior.GetSteering(args);
        SteeringOutput output = new(
            linear: goal.HasSpeed? 
                steeringOutput.Linear.Normalized() * goal.Speed: 
                steeringOutput.Linear.Normalized() * args.MaximumSpeed,
            angular: goal.HasRotation? goal.Rotation: 0);
        return output;
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        LoadChildrenReferences();

        List<string> warnings = new();
        
        if (_meshPathFinderSteeringBehavior == null)
        {
            warnings.Add("This node needs a child of type " +
                         "MeshPathFinderSteeringBehavior to work.");
        }
        
        return warnings.ToArray();
    }
    
    
}