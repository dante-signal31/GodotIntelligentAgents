using Godot;
using GodotGameAIbyExample.Scripts.Tools;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// Pipeline targeter to set the position channel as a manual position set with a Target
/// node.
/// </summary>
[Tool]
public partial class PipelineManualPositionTargeter: Node, IPipelineTargeter
{
    [ExportCategory("CONFIGURATION:")] 
    [Export] public Node2D Target;
    
    public PipelineGoal GetGoal(SteeringBehaviorArgs _)
    {
        if (Target == null) return null;
        PipelineGoal goal = new()
        {
            Position = Target.GlobalPosition,
        };
        return goal;
    }
}