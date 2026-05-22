using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// Pipeline targeter to set the rotation channel as the rotation of another agent.
/// </summary>
[Tool]
public partial class PipelineAlignRotationTargeter: Node, IPipelineTargeter
{
    private AlignSteeringBehavior _alignSteeringBehavior;

    private void LoadChildrenReferences()
    {
        _alignSteeringBehavior = this.FindChild<AlignSteeringBehavior>();
    }
    
    public override void _Ready()
    {
        LoadChildrenReferences();
    }
    
    public PipelineGoal GetGoal(SteeringBehaviorArgs args)
    {
        SteeringOutput alignSteeringOutput = _alignSteeringBehavior.GetSteering(args);
        PipelineGoal alignGoal = new()
        {
            Rotation = alignSteeringOutput.Angular,
        };
        return alignGoal;
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        LoadChildrenReferences();
        
        List<string> warnings = new();
        
        if (_alignSteeringBehavior == null)
        {
            warnings.Add("This node needs any child of type " +
                         "AlignSteeringBehavior to work.");
        }
        
        return warnings.ToArray();
    }
}