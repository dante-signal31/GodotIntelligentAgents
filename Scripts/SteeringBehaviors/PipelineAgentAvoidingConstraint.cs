using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Pathfinding;
using GodotGameAIbyExample.Scripts.Tools;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// Constraint that prevents an agent from colliding with other agents
/// while moving towards a goal along a path.
/// </summary>
[Tool]
public partial class PipelineAgentAvoidingConstraint: Node2D, IPipelineConstraint
{
    private PotentialCollisionDetector _collisionDetector;
    private VOAgentAvoiderSteeringBehavior _voAvoider;
    
    private void LoadChildrenReferences()
    {
        _voAvoider = this.FindChild<VOAgentAvoiderSteeringBehavior>();
        // I'll use the collision detector from VOAgentAvoider. So I do a recursive
        // search.
        _collisionDetector = this.FindChild<PotentialCollisionDetector>(recursive:true);
    }
    
    public override void _Ready()
    {
        LoadChildrenReferences();
    }
    
    public bool IsViolated(Path pathToGoal, PipelineGoal goal, SteeringBehaviorArgs args)
    {
        // Check whether the velocity vector to get next path point is going to make
        // us collide with another agent.
        Vector2 velocityToFirstPoint = GetVelocityToFirstPathPoint(pathToGoal, goal);
        return _collisionDetector.IsCollidingVelocity(
            velocityToFirstPoint,
            args.CurrentAgent.Radius, 
            out _);
    }

    private Vector2 GetVelocityToFirstPathPoint(Path pathToGoal, PipelineGoal goal)
    {
        if (pathToGoal.TargetPositions.Count == 0) return Vector2.Zero;
        Vector2 directionToNextPoint = 
            (pathToGoal.TargetPositions[0] - GlobalPosition).Normalized();
        Vector2 velocityToNextPoint = directionToNextPoint * goal.Speed;
        return velocityToNextPoint;
    }

    public PipelineGoal SuggestGoal(
        Path pathToGoal, 
        PipelineGoal goal, 
        SteeringBehaviorArgs args)
    {
        Vector2 velocityToFirstPoint = GetVelocityToFirstPathPoint(pathToGoal, goal);
        
        if (!_collisionDetector.IsCollidingVelocity(
                velocityToFirstPoint, 
                args.CurrentAgent.Radius, 
                out _)) return goal;
        
        // If we are going to collide with an agent, get the velocity vector most similar
        // to the one to get us to the first path point. Then convert that velocity vector 
        // into a synthetic goal.
        Vector2 bestCandidateVelocity =
            _voAvoider.GetBestCandidateVelocity(velocityToFirstPoint);
        PipelineGoal avoidGoal = new()
        {
            Position = GlobalPosition + bestCandidateVelocity.Normalized() * goal.Speed,
            Speed = goal.Speed,
            Rotation = goal.Rotation,
        };
        
        return avoidGoal;
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        LoadChildrenReferences();

        List<string> warnings = new();
        
        if (_collisionDetector == null)
        {
            warnings.Add("This node needs a child of type PotentialCollisionDetector " +
                         "to work.");
        }
        if (_voAvoider== null)
        {
            warnings.Add("This node needs a child of type " +
                         "VOAgentAvoiderSteeringBehavior to work.");
        }
        
        return warnings.ToArray();
    }
}