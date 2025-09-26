using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// This agent is the invisible leader of a formation. It decides where to move the
/// formation and its members follow him.
/// </summary>
[Tool]
public partial class UsherFormationAgent: MovingAgent, IGizmos
{
    private enum MovementNeeded
    {
        Stop,
        TurnLeft,
        GoStraight,
        TurnRight
    }
    
    [ExportCategory("USHER FORMATION CONFIGURATION:")] 
    [Export] public bool RealisticTurns = false;

    [Export(PropertyHint.Range,"0.0,1.0,0.01")] 
    public float DeviationToleranceBeforeTurn = 0.1f;

    [Export] private float _agentRadius = 50;
    
    [ExportCategory("DEBUG:")] 
    [Export] public bool ShowGizmos { get; set; }

    [Export] public Color GizmosColor { get; set; }
    
    private Node _agentParent;
    
    private UsherHingeAgent _rightHinge;
    private Vector2 _rightHingeRelativePosition;
    private UsherHingeAgent _leftHinge;
    private Vector2 _leftHingeRelativePosition;
    private UsherHingeAgent _engagedHinge;

    private Node2D _hingeTarget;
    private bool _showGizmos;
    private Color _gizmosColor;

    /// <summary>
    /// Whether the agent is currently executing a left turn.
    /// </summary>
    /// <remarks>
    /// This property evaluates to true when the hinge agent engaged for steering
    /// matches the left hinge of the formation. 
    /// </remarks>
    private bool IsTurningLeft => _engagedHinge == _leftHinge;
    
    /// <summary>
    /// Whether the agent is currently executing a right turn.
    /// </summary>
    /// <remarks>
    /// This property evaluates to true when the hinge agent engaged for steering
    /// matches the right hinge of the formation. 
    /// </remarks>
    private bool IsTurningRight => _engagedHinge == _rightHinge;


    /// <summary>
    /// Indicates whether the agent is currently executing any type of turn.
    /// </summary>
    /// <remarks>
    /// This property evaluates to true if the agent's engaged hinge is steering
    /// either to the left or to the right. 
    /// </remarks>
    private bool IsTurning => IsTurningLeft || IsTurningRight;

    public override void _EnterTree()
    {
        base._EnterTree();
        
        if (Engine.IsEditorHint() || !IsTurning) return;
        
        _hingeTarget = new Node2D();
        _hingeTarget.Name = "HingeTarget";
        CallDeferred(MethodName.AddChild, _hingeTarget);
        _hingeTarget.CallDeferred(Node2D.MethodName.SetOwner, this);
    }

    public override void _Ready()
    {
        base._Ready();
        
        // You only want to get parent once, at the beginning. So, you cannot call it
        // from _EnterTree because it is called every time we reparent this node.
        _agentParent = GetParent();
        
        List<UsherHingeAgent> usherHingeAgents = this.FindChildren<UsherHingeAgent>();
        if (usherHingeAgents == null) return;
        _rightHinge = usherHingeAgents.Find(hinge => hinge.Name == "RightHinge");
        _leftHinge = usherHingeAgents.Find(hinge => hinge.Name == "LeftHinge");
        _rightHingeRelativePosition =  _rightHinge.GlobalPosition - GlobalPosition;
        _leftHingeRelativePosition = _leftHinge.GlobalPosition - GlobalPosition;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        
        if (Engine.IsEditorHint() || !IsTurning) return;
        
        _hingeTarget?.QueueFree();
        _hingeTarget = null;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Engine.IsEditorHint()) return;
        
        if (!RealisticTurns)
        {
            base._PhysicsProcess(delta);
        }
        else
        {
            UpdateSteeringBehaviorArgs(delta);
            // Get steering output.
            SteeringOutput steeringOutput = SteeringBehavior.GetSteering(_behaviorArgs);
            switch (GetMovementNeeded(steeringOutput.Linear))
            {
                case MovementNeeded.Stop:
                    Velocity = Vector2.Zero;
                    break;
                case MovementNeeded.TurnLeft:
                    TurnLeft(steeringOutput.Linear);
                    break;
                case MovementNeeded.GoStraight:
                    GoStraight(steeringOutput.Linear);
                    MoveAndSlide();
                    break;
                case MovementNeeded.TurnRight:
                    TurnRight(steeringOutput.Linear);
                    break;
            }

            // MoveAndSlide();
        }
    }

    /// <summary>
    /// Determines the type of movement adjustment needed based on the provided direction.
    /// </summary>
    /// <param name="direction">The direction vector representing the desired movement of
    /// the agent.</param>
    /// <returns>A <see cref="MovementNeeded"/> value indicating whether the agent should
    /// turn left, go straight, or turn right.</returns>
    private MovementNeeded GetMovementNeeded(Vector2 direction)
    {
        if (direction.Length() < StopSpeed) return MovementNeeded.Stop;
        if (Forward.Dot(direction.Normalized()) > 
            1 - DeviationToleranceBeforeTurn) return MovementNeeded.GoStraight;
        if (direction.Cross(Forward) > 0) return MovementNeeded.TurnLeft;
        return MovementNeeded.TurnRight;
    }

    /// <summary>
    /// Adjusts the agent's velocity to move in a straight direction based on the provided
    /// vector.
    /// </summary>
    /// <param name="direction">The direction vector representing the straight movement to
    /// be applied to the agent's velocity.</param>
    private void GoStraight(Vector2 direction)
    {
        if (IsTurning)
        {
            _engagedHinge.StopRotation();
            DisengageHinge();
        }
        Velocity = direction;
    }


    /// <summary>
    /// Rotate the agent clockwise based on the specified direction vector, using
    /// right hinge as axis.
    /// </summary>
    /// <param name="direction">The vector indicating the direction in which the agent
    /// should orient to perform a left turn.</param>
    private void TurnRight(Vector2 direction)
    {
        Turn(direction, MovementNeeded.TurnRight);
    }

    /// <summary>
    /// Rotate the agent counterclockwise based on the specified direction vector, using
    /// left hinge as axis.
    /// </summary>
    /// <param name="direction">The vector indicating the direction in which the agent
    /// should orient to perform a left turn.</param>
    private void TurnLeft(Vector2 direction)
    {
        Turn(direction, MovementNeeded.TurnLeft);
    }

    private void Turn(Vector2 direction, MovementNeeded movementNeeded)
    {
        bool justStartedTurning = 
            (movementNeeded == MovementNeeded.TurnRight && !IsTurningRight) ||
            (movementNeeded == MovementNeeded.TurnLeft && !IsTurningLeft);
        
        if (justStartedTurning)
        { // We just started turning from the other direction or from straight movement.
            if (IsTurning)
            { // If we were already turning to the other direction, stop it.
                _engagedHinge.StopRotation();
                DisengageHinge();
            }
            // Setup hinge to turn to the new direction.
            EngageHinge(movementNeeded == MovementNeeded.TurnRight ? 
                _rightHinge : 
                _leftHinge);
            _engagedHinge.TargetToLookAt = _hingeTarget;
        }
        
        // If we make the hinge loot to the formation target, the rotation will end
        // before the formation center looks at the target directly. So, we must make
        // the hinge look to a target parallel to the one of the formation center.
        _hingeTarget.GlobalPosition = 
            _engagedHinge.GlobalPosition + direction.Normalized() * _agentRadius * 2;
    }

    private void EngageHinge(UsherHingeAgent hinge)
    {
        // Make formation dependent on the hinge. This way, the formation will
        // turn when the hinge turns.
        hinge.Owner = _agentParent;
        hinge.Reparent(_agentParent);
        _engagedHinge = hinge;
        Reparent(hinge);
    }
    
    private void DisengageHinge()
    {
        // When not turning, the hinge must be reparented to the agent to follow it
        // when moving.
        Reparent(_agentParent);
        _engagedHinge.Reparent(this);
        _engagedHinge.Owner = this;
        _engagedHinge.SetForward(Forward);
        _engagedHinge = null;
    }
    
    public override void _Process(double delta)
    {
        DrawGizmos();
    }

    private void DrawGizmos()
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!ShowGizmos) return;

        // Draw agentRadius size.
        DrawCircle(Vector2.Zero, _agentRadius, GizmosColor, filled: false);
        
        // Draw a line to target.
        DrawLine(
            Vector2.Zero, 
            ToLocal( ((ITargeter)SteeringBehavior).Target.GlobalPosition ), 
            GizmosColor);
        DrawCircle(
            ToLocal( ((ITargeter)SteeringBehavior).Target.GlobalPosition ), 
            30f, 
            GizmosColor, 
            filled:false);
        
        // // Draw a line from hinge to hinge target.
        // if (_hingeTarget == null || _engagedHinge == null) return;
        // DrawLine(
        //     ToLocal(_engagedHinge.GlobalPosition), 
        //     ToLocal(_hingeTarget.GlobalPosition), 
        //     GizmosColor);
        // DrawCircle(
        //     ToLocal(_engagedHinge.GlobalPosition), 
        //     20f, 
        //     GizmosColor, 
        //     filled:false);
        
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings());

        List<UsherHingeAgent> usherHingeAgents = this.FindChildren<UsherHingeAgent>();
        
        if (usherHingeAgents == null || usherHingeAgents.Count < 2)
        {
            warnings.Add("This node needs at least 2 UsherHingeAgents children nodes " +
                         "to work.");
        }
        
        if (usherHingeAgents == null) return warnings.ToArray();
        
        bool hasRightHinge = usherHingeAgents.Exists(
            hinge => hinge.Name == "RightHinge");
        if (!hasRightHinge)
        {
            warnings.Add("This node needs a UsherHingeAgent child node named " +
                         "'RightHinge' to work.");
        }
        
        bool hasLeftHinge = usherHingeAgents.Exists(
            hinge => hinge.Name == "LeftHinge");
        if (!hasLeftHinge)
        {
            warnings.Add("This node needs a UsherHingeAgent child node named " +
                         "'LeftHinge' to work.");
        }
        
        return warnings.ToArray();
    }


}