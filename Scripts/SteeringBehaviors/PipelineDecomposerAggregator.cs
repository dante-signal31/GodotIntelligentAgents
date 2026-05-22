using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// Pass the targeter goal sequentially through a series of decomposers. Every decomposer
/// will be evaluated in turn. The subgoal generated from a decomposer will be used as
/// input for the next decomposer. This way, the given gih level goal can be decomposed
/// into a smaller, and easily achievable, subgoal.
/// </summary>
/// <remarks>
/// The <c>PipelineDecomposerAggregator</c> works with child nodes that implement
/// the <c>IPipelineDecomposer</c> interface.
/// </remarks>
[Tool]
public partial class PipelineDecomposerAggregator: Node
{
    private IPipelineDecomposer[] _pipelineDecomposers;

    /// <summary>
    /// Loads all child nodes implementing the IPipelineDecomposer interface.
    /// </summary>
    /// <remarks>
    /// Be aware that order matters. Decomposers will be evaluated in the order they are
    /// in the tree. So, they will be evaluated from top to bottom.
    /// </remarks>
    private void LoadPipelineDecomposers()
    {
        _pipelineDecomposers = this.FindChildren<IPipelineDecomposer>()?.ToArray();
    }

    public override void _Ready()
    {
        LoadPipelineDecomposers();
    }
    
    public PipelineGoal DecomposeGoal(PipelineGoal goal, SteeringBehaviorArgs args)
    {
        // goal is passed by reference, so I allocate a new one to not overwrite the
        // global goal.
        PipelineGoal partialGoal = goal.GetGoalCopy();
        
        foreach (IPipelineDecomposer decomposer in _pipelineDecomposers)
        {
            partialGoal = decomposer.Decompose(partialGoal, args);
        }
        
        return partialGoal;
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        LoadPipelineDecomposers();
        
        List<string> warnings = new();
        
        if (_pipelineDecomposers == null || _pipelineDecomposers.Length == 0)
        {
            warnings.Add("This node needs any child of type " +
                         "IPipelineDecomposer to work.");
        }
        
        return warnings.ToArray();
    }
}