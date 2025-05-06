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
    /// Target to go avoiding other agents.
    /// </summary>
    [Export] public Node2D Target { get; set; }
    
    /// <summary>
    /// Timeout started after no further collision detected, before resuming travel to
    /// target.
    /// </summary>
    [Export] public float AvoidanceTimeout { get; set; } = 1.0f;
    
    [ExportCategory("DEBUG:")]
    [Export] private bool ShowGizmos { get; set; }
    
    private ITargeter _targeter;
    private ISteeringBehavior _steeringBehavior;
    private PotentialCollisionDetector _potentialCollisionDetector;
    private Timer _avoidanceTimer;
    private bool _waitingForAvoidanceTimeout;
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
        _targeter = this.FindChild<ITargeter>();
        _steeringBehavior = (ISteeringBehavior)_targeter;
        _targeter.Target = Target;
        _potentialCollisionDetector = this.FindChild<PotentialCollisionDetector>();
        _avoidanceTimer = this.FindChild<Timer>();
        _avoidanceTimer.Timeout += OnAvoidanceTimeout;
    }

    private void OnAvoidanceTimeout()
    {
        _waitingForAvoidanceTimeout = false;
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
        if (Target == null) 
            return;

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
                ToLocal(_potentialCollisionDetector.PotentialCollisionAgent.GlobalPosition), 
                ToLocal(otherAgentCollisionPosition), 
                new Color(1, 0, 0));
            DrawCircle(
                ToLocal(otherAgentCollisionPosition),
                10f,
                new Color(1, 0, 0),
                filled:false);
            // Draw current collision agent velocity.
            DrawLine(
                ToLocal(_potentialCollisionDetector.PotentialCollisionAgent.GlobalPosition),
                ToLocal(_potentialCollisionDetector.PotentialCollisionAgent.GlobalPosition + 
                        _potentialCollisionDetector.PotentialCollisionAgent.Velocity),
                new Color(0, 0, 1));
        }
        
        // Draw current agent velocity.
        DrawLine(
            Vector2.Zero, 
            ToLocal(GlobalPosition + _currentAgent.Velocity), 
            new Color(1, 0, 0));
    }

    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        SteeringOutput steeringToTargetVelocity = _steeringBehavior.GetSteering(args);
        if (!_potentialCollisionDetector.PotentialCollisionDetected &&
            !_waitingForAvoidanceTimeout)
            return steeringToTargetVelocity;
        if (!_potentialCollisionDetector.PotentialCollisionDetected)
            return _currentSteeringOutput;

        Vector2 minimumDistanceRelativePosition;
        // If we're going to collide, or are already colliding, then we do the steering
        // based on current position.
        if (_potentialCollisionDetector.MinimumSeparationAtPotentialCollision <= 0
            ||
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
        // those two agents. To make them farther away you should take the opposite
        // vector as I'm doing here with -minimumDistanceRelativePosition.
        Vector2 avoidanceVelocity = -minimumDistanceRelativePosition.Normalized() *
                              args.MaximumSpeed;

        Vector2 newVelocity = steeringToTargetVelocity.Linear + avoidanceVelocity;
        
        // This is another change from Millington algorithm.
        // It's harder to evade collision agent If we end going along the same direction. 
        // So, we want to use a resulting vector pointing in the opposite direction than
        // the velocity of the collision agent. This way we will avoid it passing it
        // across its tail.
        int sign = 
            _potentialCollisionDetector.PotentialCollisionAgent.Velocity
                .Dot(newVelocity) > 0
                ? -1
                : 1;
        
        _currentSteeringOutput = new SteeringOutput(
            sign * (newVelocity), 
            steeringToTargetVelocity.Angular);
        StartAvoidanceTimer();
        return _currentSteeringOutput;
    }

    private void StartAvoidanceTimer()
    {
        _avoidanceTimer.Start();
        _waitingForAvoidanceTimeout = true;
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