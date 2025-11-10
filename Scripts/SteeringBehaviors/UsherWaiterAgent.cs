using Godot;
using GodotGameAIbyExample.Scripts.Extensions;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

public partial class UsherWaiterAgent: MovingAgent
{
    [ExportCategory("USHER WAITER CONFIGURATION:")]
    // <summary>
    /// Maximum distance in pixels that the following agent can lag behind
    /// usher.
    /// </summary>
    [Export] public int MaximumLaggingBehindDistance { get; set; } = 500;
    
    // Following agent.
    public MovingAgent FollowingAgent {get; set;}
    
    // Steering behavior to move the formation.
    private ITargeter _targeter;
    
    private float _originalMaximumSpeed;
    
    /// <summary>
    /// Distance between the members' average positions and formation usher.
    /// </summary>
    private float LaggingBehindDistance => GlobalPosition.DistanceTo(
        FollowingAgent.GlobalPosition);
    
    /// <summary>
    /// Whether usher is going away from the following agent. 
    /// </summary>
    private bool GoingAwayFromAveragePosition =>
        ToLocal(_targeter.Target.GlobalPosition).Dot(
            ToLocal(FollowingAgent.GlobalPosition)) < 0;
    
    public override void _Ready()
    {
        base._Ready();
        _targeter = this.FindChild<ITargeter>();
        _originalMaximumSpeed = MaximumSpeed;
    }
    
    public override void _PhysicsProcess(double delta)
    {
        if (Engine.IsEditorHint() || 
            FollowingAgent == null || 
            _targeter == null || 
            _targeter.Target == null) return;
        
        if (GoingAwayFromAveragePosition)
        {
            // If we are leaving behind the following agent. We want to slow down so that
            // the following agent has time to catch the usher.
            MaximumSpeed = _originalMaximumSpeed * 
                           (1 - Mathf.Min(
                               LaggingBehindDistance, 
                               MaximumLaggingBehindDistance) / MaximumLaggingBehindDistance);
        }
        else
        {
            // We are going towards the following agent, so we can go at full speed
            // because we are meeting with it.
            MaximumSpeed = _originalMaximumSpeed;       
        }

        base._PhysicsProcess(delta);
    }
}