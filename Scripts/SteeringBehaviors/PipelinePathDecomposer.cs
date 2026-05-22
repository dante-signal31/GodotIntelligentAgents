using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Pathfinding;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// The <c>PipelinePathDecomposer</c> class is responsible for decomposing a high-level
/// position steering goal into smaller, incremental positional goals along a computed path.
/// </summary>
[Tool]
public partial class PipelinePathDecomposer: Node, IPipelineDecomposer
{
    [ExportCategory("CONFIGURATION:")] 
    [Export] private float _minimumChunkLength = 300f;
    
    private MeshPathFinderSteeringBehavior _meshPathFinderSteeringBehavior;
    private Vector2 _currentGoalPosition;
    private Path _currentPath;
    private SteeringBehaviorArgs _currentArgs;
    
    private void LoadChildrenReferences()
    {
        _meshPathFinderSteeringBehavior = this.FindChild<MeshPathFinderSteeringBehavior>();
    }
    
    public override void _Ready()
    {
        LoadChildrenReferences();
        if (_meshPathFinderSteeringBehavior == null) return;
        // We only want to use the mesh pathfinder to find the path, not to avoid other
        // agents.
        _meshPathFinderSteeringBehavior.AvoidAgents = false;
    }

    public PipelineGoal Decompose(PipelineGoal goal, SteeringBehaviorArgs args)
    {
        _currentArgs = args;

        if (!_meshPathFinderSteeringBehavior.PathFindingReady) return goal;
        
        if (_currentGoalPosition != goal.Position)
        {
            _meshPathFinderSteeringBehavior.UpdateTargetPosition(goal.Position);
            _currentGoalPosition = goal.Position;
        }
        
        _currentPath = _meshPathFinderSteeringBehavior.CurrentPath;

        if (_currentPath == null) return goal;
        
        // If the path is too short, then we don't need to decompose it.
        Path partialPath = new();
        foreach (Vector2 pathTargetPosition in _currentPath.TargetPositions)
        {
            partialPath.TargetPositions.Add(pathTargetPosition);
            if (partialPath.PathLength >= _minimumChunkLength) break;
        }
        if (partialPath.TargetPositions.Count <= 1) return goal;
        
        // Goal is passed by reference, so we are updating directly the passed in
        // parameter. It doesn't matter because decomposers are run sequentially.
        goal.Position = partialPath.TargetPositions[^1];
        return goal;
        // PipelineGoal decomposedGoal = new()
        // {
        //     Position = partialPath.TargetPositions[^1],
        //     Rotation = goal.Rotation,
        //     Speed = goal.Speed
        // };
        // return decomposedGoal;
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