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
/// <p>Node to offer an agent avoider steering behaviour.</p>
/// <p>Represents a steering behavior where an agent avoids another agents it may
/// collision with in its path.</p>
/// </summary>
public partial class AgentAvoiderSteeringBehavior : Node2D, ISteeringBehavior, ITargeter
{
    [ExportCategory("CONFIGURATION:")]
    /// <summary>
    /// Target to follow.
    /// </summary>
    [Export] public Node2D Target { get; set; }
    
    [ExportCategory("DEBUG:")]
    [Export] private bool ShowGizmos { get; set; }
    
    private ITargeter _targeter;
    private ISteeringBehavior _steeringBehavior;
    private PotentialCollisionDetector _potentialCollisionDetector;
    private MovingAgent _currentAgent;
    private Vector2 _newVelocity;
    
    private Color AgentColor => _currentAgent.AgentColor;

    public override void _EnterTree()
    {
        // Find out who our father is.
        _currentAgent = this.FindAncestor<MovingAgent>();
    }

    public override void _Ready()
    {
        _targeter = this.FindChild<ITargeter>();
        _steeringBehavior = (ISteeringBehavior)_targeter;
        _targeter.Target = Target;
        
        _potentialCollisionDetector = this.FindChild<PotentialCollisionDetector>();
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
        if (Target == null || 
            !_potentialCollisionDetector.PotentialCollisionDetected || 
            Engine.IsEditorHint()) 
            return;

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
            AgentColor);
        DrawCircle(
            ToLocal(currentAgentCollisionPosition),
            10f,
            AgentColor,
            filled:false);
        DrawLine(
            ToLocal(_potentialCollisionDetector.PotentialCollisionAgent.GlobalPosition), 
            ToLocal(otherAgentCollisionPosition), 
            AgentColor);
        DrawCircle(
            ToLocal(otherAgentCollisionPosition),
            10f,
            AgentColor,
            filled:false);
        
        // Draw new evasion velocity.
        DrawLine(
            Vector2.Zero, 
            _newVelocity, 
            AgentColor);
    }

    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        if (!_potentialCollisionDetector.PotentialCollisionDetected)
            return _steeringBehavior.GetSteering(args);

        Vector2 minimumDistanceRelativePosition;
        // If we're going to collide, or are already colliding, then we do the steering
        // based on current position.
        if (_potentialCollisionDetector.MinimumSeparationAtPotentialCollision <= 0 ||
            _potentialCollisionDetector.CurrentDistanceToPotentialCollisionAgent <
            _potentialCollisionDetector.CollisionDistance)
        {
            minimumDistanceRelativePosition = _potentialCollisionDetector
                .CurrentRelativePositionToPotentialCollisionAgent;
        }
        else
        {
            // If collision is going to happen in the future then calculate the 
            // relative position in that moment.
            minimumDistanceRelativePosition = _potentialCollisionDetector
                                      .CurrentRelativePositionToPotentialCollisionAgent +
                                  _potentialCollisionDetector
                                      .CurrentRelativeVelocityToPotentialCollisionAgent *
                                  _potentialCollisionDetector
                                      .TimeToPotentialCollision;
        }

        // Another issue I have with Millington algorithm is that it multiplies
        // relativePosition with MaximumAcceleration. But I think the right thing to
        // do is multiply the opposite of relativePosition vector, because that
        // vector goes from agent to its target, so as it is that vector would approach
        // those two agents. To meke them farther away you should take the opposite
        // vector as I'm doing here with -minimumDistanceRelativePosition.
        _newVelocity = -minimumDistanceRelativePosition.Normalized() *
                              args.MaximumAcceleration;
        
        return new SteeringOutput(_newVelocity, 0);
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        ITargeter targeterBehavior = 
            this.FindChild<ITargeter>();

        Tools.PotentialCollisionDetector potentialCollisionDetector =
            this.FindChild<Tools.PotentialCollisionDetector>();
        
        List<string> warnings = new();
        
        if (targeterBehavior == null || !(targeterBehavior is ISteeringBehavior))
        {
            warnings.Add("This node needs a child of type both ISteeringBehavior and " +
                         "ITargeter to work. ");  
        }

        if (potentialCollisionDetector == null)
        {
            warnings.Add("This node needs a child of type PotentialCollisionDetector " +
                         "to work. "); 
        }
        
        return warnings.ToArray();
    }
}