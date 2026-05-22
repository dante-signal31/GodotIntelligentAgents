using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Pathfinding;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// The <c>PipelineAccelerationDecomposer</c> class is responsible for decomposing
/// a high-level velocity steering goal into smaller, incremental velocity changes along
/// a predefined acceleration profile.
/// </summary>
[Tool]
public partial class PipelineAccelerationDecomposer: Node, IPipelineDecomposer
{
    private ArriveSteeringBehaviorNLA _arriveSteeringBehaviorNla;
    private Vector2 _currentGoalPosition;
    private Path _currentPath;
    private Node2D _positionTargetMarker;
    
    private void LoadChildrenReferences()
    {
        _arriveSteeringBehaviorNla = this.FindChild<ArriveSteeringBehaviorNLA>();
    }
    
    
    public override void _Ready()
    {
        LoadChildrenReferences();
        if (_arriveSteeringBehaviorNla == null) return;

        _positionTargetMarker = new Node2D();
        GetTree().Root.CallDeferred(Node.MethodName.AddChild, _positionTargetMarker);
        _arriveSteeringBehaviorNla.Target = _positionTargetMarker;
    }

    public PipelineGoal Decompose(PipelineGoal goal, SteeringBehaviorArgs args)
    {
        if (_currentGoalPosition != goal.Position)
        {
            _positionTargetMarker.GlobalPosition = goal.Position;
            _currentGoalPosition = goal.Position;
        }
        
        // We need to call GetSteering() to get an updated speed.
        SteeringOutput steeringOutput = _arriveSteeringBehaviorNla.GetSteering(args);
        goal.Speed = steeringOutput.Linear.Length();
        
        return goal;
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        LoadChildrenReferences();
        
        List<string> warnings = new();
        
        if (_arriveSteeringBehaviorNla == null)
        {
            warnings.Add("This node needs a child of type " +
                         "ArriveSteeringBehaviorNLA to work.");
        }
        
        return warnings.ToArray();
    }
}