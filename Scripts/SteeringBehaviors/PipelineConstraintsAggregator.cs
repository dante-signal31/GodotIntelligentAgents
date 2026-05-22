using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Pathfinding;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// Represents an aggregator responsible for managing and applying
/// pipeline constraint logic during pathfinding and steering behaviors. It collects
/// child nodes implementing <c>IPipelineConstraint</c> and applies their constraints
/// to determine the validity of a given pipeline goal and the associated movement path.
/// </summary>
[Tool]
public partial class PipelineConstraintsAggregator: Node
{
    [ExportCategory("CONFIGURATION:")] 
    // If we want to give every constraint the chance to find a violation and propose an
    // alternative, then we must set this value at least to the same value as the number
    // of constraints.
    [Export] public int MaximumTries = 2;
    
    private IPipelineConstraint[] _pipelineConstraints;

    /// <summary>
    /// Loads all child nodes implementing the IPipelineConstraint interface.
    /// </summary>
    /// <remarks>
    /// Be aware that order matters. Constraints will be evaluated in the order they are
    /// in the tree. So, they will be evaluated from top to bottom.
    /// </remarks>
    private void LoadPipelineConstraints()
    {
        _pipelineConstraints = this.FindChildren<IPipelineConstraint>()?.ToArray();
    }

    public override void _Ready()
    {
        LoadPipelineConstraints();
    }

    public PipelineGoal ApplyConstraints(
        PipelineGoal goal, 
        SteeringBehaviorArgs args,
        IPipelineActuator actuator)
    {
        // goal is passed by reference, so I allocate a new one to not overwrite the
        // original goal.
        PipelineGoal validGoal = goal.GetGoalCopy();
        
        for (int i = 0; i < MaximumTries; i++)
        {
            bool violationFound = false;
            // Get the path the actuator would choose to get the goal.
            Path path = actuator.GetPath(validGoal, args);

            // Check if any constraint is violated by the path or the goal.
            foreach (IPipelineConstraint constraint in _pipelineConstraints)
            {
                if (constraint.IsViolated(path, validGoal, args))
                {
                    violationFound = true;
                    
                    // If so, try to find a new goal that does not violate the constraint.
                    validGoal = constraint.SuggestGoal(path, validGoal, args);
                    
                    // Make a new round to check if suggested goal complies with the
                    // constraints.
                    break;
                }
            }
            if (!violationFound) break;
        }
        
        return validGoal;
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        LoadPipelineConstraints();
        
        List<string> warnings = new();
        
        if (_pipelineConstraints == null || _pipelineConstraints.Length == 0)
        {
            warnings.Add("This node needs any child of type " +
                         "IPipelineConstraint to work.");
        }
        
        return warnings.ToArray();
    }
}