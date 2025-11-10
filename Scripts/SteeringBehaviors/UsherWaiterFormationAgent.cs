using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Groups;
using GodotGameAIbyExample.Scripts.Extensions;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// <p>This agent is the invisible leader of a formation. It decides where to move the
/// formation and its members follow him.</p>
/// <p>This agent is different from the UsherFormationAgent in that it slows down when
/// the members are lagging behind the ushers.</p>
/// </summary>
[Tool]
public partial class UsherWaiterFormationAgent: UsherFormationAgent
{
    [ExportCategory("USHER WAITER FORMATION CONFIGURATION:")]
    /// <summary>
    /// Maximum distance in pixels that the members average position can lag behind
    /// ushers formation.
    /// </summary>
    [Export] public int MaximumLaggingBehindDistance { get; set; } = 500;
    
    // Formation members.
    public IFormation Formation {get; private set;}
    
    // Steering behavior to move the formation.
    private ITargeter _targeter;
    
    private float _originalMaximumSpeed;
    // Our formation origin is not centered at the average position, so we
    // must compensate for that difference.
    private float _originalAveragePositionDistance;
    
    private Vector2 FormationAveragePosition
    {
        get
        {
            Vector2 averagePosition = Vector2.Zero;
            if (Formation == null || Formation.Members.Count == 0) 
                return averagePosition;
            foreach (Node2D member in Formation.Members)
            {
                averagePosition += member.GlobalPosition;
            }
            return averagePosition / Formation.Members.Count;
        }
    }

    /// <summary>
    /// Distance between the members' average positions and formation usher.
    /// </summary>
    private float LaggingBehindDistance => GlobalPosition.DistanceTo(
        FormationAveragePosition) - _originalAveragePositionDistance;
    
    /// <summary>
    /// Whether usher is going away from the formation members. 
    /// </summary>
    private bool GoingAwayFromAveragePosition =>
        ToLocal(_targeter.Target.GlobalPosition).Dot(
            ToLocal(FormationAveragePosition)) < 0;
    
    public override void _Ready()
    {
        base._Ready();
        Formation = this.FindChild<IFormation>();
        _targeter = this.FindChild<ITargeter>();
        _originalAveragePositionDistance = 
            GlobalPosition.DistanceTo(FormationAveragePosition);
        _originalMaximumSpeed = MaximumSpeed;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Engine.IsEditorHint() || Formation == null) return;
        
        if (GoingAwayFromAveragePosition)
        {
            // If we are leaving behind the average position, then that means some members
            // are lagging behind. We want to slow down so that members have time to catch
            // the formation.
            MaximumSpeed = _originalMaximumSpeed * 
                           (1 - Mathf.Min(
                                LaggingBehindDistance, 
                                MaximumLaggingBehindDistance) / MaximumLaggingBehindDistance);
        }
        else
        {
            // We are going towards the average position, so we can go at full speed
            // because we are meeting with those members that are lagging behind.
            MaximumSpeed = _originalMaximumSpeed;       
        }

        base._PhysicsProcess(delta);
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new();
        
        List<Node2D> nodeChildren = this.FindChildren<Node2D>();
        IFormation formation = null;
        foreach (var node in nodeChildren)
        {
            formation = node as IFormation;
            if (formation != null) break;
        }
        if (formation == null)
        {
            warnings.Add("This node needs a child node of type " +
                         "IFormation to work properly.");  
        }
        
        ITargeter targeter = null;
        foreach (var node in nodeChildren)
        {
            targeter = node as ITargeter;
            if (targeter != null) break;
        }

        if (targeter == null)
        {
            warnings.Add("This node needs a child node of type " +
                         "ITargeter to work properly."); 
        }
        
        return warnings.ToArray();
    }
}