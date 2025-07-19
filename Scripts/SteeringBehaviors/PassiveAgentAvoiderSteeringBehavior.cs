using System;
using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Tools;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

// It must be marked as Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
/// <summary>
/// <p>Node to offer an agent avoider steering behaviour over a given velocity.</p>
/// <p>Represents a steering behavior where an agent avoids another agents it may
/// collision with in its path.</p>
/// <p>The difference with an obstacle avoidance algorithm is that obstacles don't move
/// while agents do.</p>
/// </summary>
public partial class PassiveAgentAvoiderSteeringBehavior: Node2D, ISteeringBehavior, IGizmos
{
    [ExportCategory("CONFIGURATION:")]
    /// <summary>
    /// Threshold factor for determining when to use normal vector avoidance.
    /// When the dot product between avoidance and collision vectors exceeds this value 
    /// (positive or negative), the avoidance vector is replaced with a vector normal 
    /// to the collision agent's velocity to prevent chase or collision scenarios.
    /// </summary>
    [Export] public float TooAlignedFactor { get; set; }= 0.95f;
    
    [ExportCategory("DEBUG:")]
    [Export] public bool ShowGizmos { get; set; }
    [Export] public Color GizmosColor { get; set; }
    
    private readonly Random _random = new();
    private PotentialCollisionDetector _potentialCollisionDetector;
    private MovingAgent _currentAgent;
    private SteeringOutput _currentSteeringOutput;
    private Color AgentColor => _currentAgent.AgentColor;
    
    public override void _EnterTree()
    {
        // Find out who our father is.
        _currentAgent = this.FindAncestor<MovingAgent>();
    }
    
    public override void _Ready()
    {
        _potentialCollisionDetector = this.FindChild<PotentialCollisionDetector>();
    }
    
    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        if (!_potentialCollisionDetector.PotentialCollisionDetected) 
            return SteeringOutput.Zero;
        
        Vector2 minimumDistanceRelativePosition;
        // If we're going to collide, or are already colliding, then we do the steering
        // based on the current position.
        if (_potentialCollisionDetector.SeparationAtPotentialCollision <= 0
            ||
            _potentialCollisionDetector.CurrentDistanceToPotentialCollisionAgent <
            _potentialCollisionDetector.CollisionDistance)
        {
            minimumDistanceRelativePosition = _potentialCollisionDetector
                .CurrentRelativePositionToPotentialCollisionAgent;
        }
        else
        {
            // If a collision is going to happen in the future, then calculate the 
            // relative position at that moment.
            minimumDistanceRelativePosition = 
                _potentialCollisionDetector.RelativePositionAtPotentialCollision;
        }

        // One issue I have with the Millington algorithm is that it multiplies
        // relativePosition with MaximumAcceleration. But I think the right thing to
        // do is to multiply the opposite of relativePosition vector, because that
        // vector goes from agent to its target, so as it is that vector would approach
        // those two agents. To make them farther away, you should take the opposite
        // vector as I'm doing here with -minimumDistanceRelativePosition.
        Vector2 avoidanceVelocity = -minimumDistanceRelativePosition.Normalized() *
                                    args.MaximumSpeed;
        
        // Here comes another change from the Millington algorithm. The problem with
        // the original algorithm is that it does not seem to take in count the edge case
        // where the two agents are going one against the other directly, in opposite 
        // directions. The rest of the method fixes that.
        
        // One way to find out if the two agents are going one against the other in 
        // opposite directions is to check the dot product between the relative position
        // and the relative velocity. If the absolute value of a dot product is near 1,
        // that means the two agents are going away or approaching, in both cases in
        // opposite directions. In the first case, it wouldn't be collision, so we
        // wouldn't be here because of the guard at the beginning of the method (the one
        // that returns a Zero if no potential collision is detected). So, if the absolute
        // value of dot product is near 1, that means that the two agents are approaching
        // in opposite directions.
        float relativeStartingPosition = 
            _potentialCollisionDetector.CurrentRelativePositionToPotentialCollisionAgent
                .Normalized()
                .Dot(
                    _potentialCollisionDetector.CurrentRelativeVelocityToPotentialCollisionAgent
                        .Normalized());
        if (Mathf.Abs(relativeStartingPosition) >= TooAlignedFactor)
        {
            // If relative velocity is too aligned with relative position, then it means
            // we can end in a direct hit, so we try an avoidance vector that is
            // perpendicular to the collision agent's velocity.
            Vector2 neededVelocity = 
                _potentialCollisionDetector.PotentialCollisionAgent.Velocity
                              .Rotated(Mathf.Pi / 2)
                              .Normalized() * args.MaximumSpeed * 
                          // Turn to one side or another randomly.
                          (_random.Next(2) * 2 - 1); 
            avoidanceVelocity = neededVelocity - _currentAgent.Velocity;
        }
        
        _currentSteeringOutput = new SteeringOutput(
            linear: avoidanceVelocity,
            angular: 0);
        
        return _currentSteeringOutput;
    }
    
    public override void _Process(double delta)
    {
        if (ShowGizmos) DrawGizmos();
    }

    private void DrawGizmos()
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_potentialCollisionDetector == null) return;
        
        if (_potentialCollisionDetector.PotentialCollisionDetected)
        {
            Vector2 currentAgentCollisionPosition =
                _potentialCollisionDetector.TimeToPotentialCollision *
                _currentAgent.Velocity +
                _currentAgent.GlobalPosition;
            Vector2 otherAgentCollisionPosition = 
                _potentialCollisionDetector.TimeToPotentialCollision *
                _potentialCollisionDetector.PotentialCollisionAgent.Velocity +
                _potentialCollisionDetector.PotentialCollisionAgent.GlobalPosition;
        
            // Draw positions for potential collision.
            DrawLine(
                Vector2.Zero, 
                ToLocal(currentAgentCollisionPosition), 
                new Color(1, 0, 0));
            DrawCircle(
                ToLocal(currentAgentCollisionPosition),
                10f,
                new Color(1, 0, 0),
                filled:false);
            DrawLine(
                ToLocal(
                    _potentialCollisionDetector.PotentialCollisionAgent.GlobalPosition), 
                ToLocal(otherAgentCollisionPosition), 
                new Color(1, 0, 0));
            DrawCircle(
                ToLocal(otherAgentCollisionPosition),
                10f,
                new Color(1, 0, 0),
                filled:false);
            // Draw current collision agent velocity.
            DrawLine(
                ToLocal(
                    _potentialCollisionDetector.PotentialCollisionAgent.GlobalPosition),
                ToLocal(
                    _potentialCollisionDetector.PotentialCollisionAgent.GlobalPosition + 
                        _potentialCollisionDetector.PotentialCollisionAgent.Velocity),
                new Color(0, 0, 1));
        }
        
        // Draw current agent velocity.
        DrawLine(
            Vector2.Zero, 
            ToLocal(GlobalPosition + _currentAgent.Velocity), 
            new Color(1, 0, 0));
    }

    public override string[] _GetConfigurationWarnings()
    {
        Tools.PotentialCollisionDetector potentialCollisionDetector =
            this.FindChild<Tools.PotentialCollisionDetector>();
        
        List<string> warnings = new();

        if (potentialCollisionDetector == null)
        {
            warnings.Add("This node needs a child of type PotentialCollisionDetector " +
                         "to work. "); 
        }
        
        return warnings.ToArray();
    }
}