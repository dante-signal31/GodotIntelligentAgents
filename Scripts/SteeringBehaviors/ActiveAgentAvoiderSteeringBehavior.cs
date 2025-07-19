using System.Collections.Generic;
using System.Timers;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using Timer = System.Timers.Timer;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

// It must be marked as Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
/// <summary>
/// <p>Node to offer an active agent avoiding steering behaviour.</p>
/// <p>Represents a steering behavior where an agent goes to a target avoiding another
/// agents it may collision with in its path.</p>
/// <p>The difference with an obstacle avoidance algorithm is that obstacles don't move
/// while agents do.</p>
/// </summary>
public partial class ActiveAgentAvoiderSteeringBehavior: 
    Node2D, 
    ISteeringBehavior, 
    IGizmos
{
    [ExportCategory("CONFIGURATION:")]
    /// <summary>
    /// Timeout started after no further collision detected, before resuming travel to
    /// target.
    /// </summary>
    [Export] public float AvoidanceTimeout { get; set; } = 1.0f;
    
    [ExportCategory("DEBUG:")]
    public bool ShowGizmos { get; set; }
    public Color GizmosColor { get; set;}

    private ITargeter _targeter;
    private ISteeringBehavior _steeringBehavior;
    private PassiveAgentAvoiderSteeringBehavior _passiveAvoiderBehavior; 
    private System.Timers.Timer _avoidanceTimer;
    private bool _waitingForAvoidanceTimeout;
    private SteeringOutput _currentSteeringOutput;
    
    
    public override void _Ready()
    {
        _targeter = this.FindChild<ITargeter>();
        _steeringBehavior = (ISteeringBehavior)_targeter;
        _passiveAvoiderBehavior = this.FindChild<PassiveAgentAvoiderSteeringBehavior>();
        _avoidanceTimer = new Timer(AvoidanceTimeout * 1000);
        _avoidanceTimer.Elapsed += OnAvoidanceTimeout;
    }
    
    private void OnAvoidanceTimeout(object sender, ElapsedEventArgs elapsedEventArgs)
    {
        _waitingForAvoidanceTimeout = false;
    }
    
    private void StartAvoidanceTimer()
    {
        _avoidanceTimer.Stop();
        _avoidanceTimer.Start();
        _waitingForAvoidanceTimeout = true;
    }
    
    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        SteeringOutput steeringToTargetVelocity = _steeringBehavior.GetSteering(args);
        SteeringOutput avoidingSteeringVelocity = 
            _passiveAvoiderBehavior.GetSteering(args);
        
        // Nothing to avoid, but we are waiting for avoidance timeout, so let's
        // continue our current velocity.
        if (avoidingSteeringVelocity.Equals(SteeringOutput.Zero) &&
            _waitingForAvoidanceTimeout)
            return _currentSteeringOutput;
        
        // Nothing to avoid and waiting nothing, so let's just go to our target.
        if (avoidingSteeringVelocity.Equals(SteeringOutput.Zero)) 
            return steeringToTargetVelocity;
        
        // If we get here, then there's an agent to avoid. Add avoiding vector to our
        // velocity to avoid collision. 
        Vector2 newVelocity = steeringToTargetVelocity.Linear + 
                              avoidingSteeringVelocity.Linear;
        _currentSteeringOutput = new SteeringOutput(
            newVelocity, 
            steeringToTargetVelocity.Angular);
        
        // We need a cooldown or we can get stuck in a cycle where our agent changes 
        // its heading to avoid collision but, in the next frame, as its heading is
        // different, then algorithm can conclude there is no longer a collision risk
        // so avoiding vector is discarded... and, in the next frame, agent is looking
        // in the same direction as originally and collision risk returns restarting
        // the cycle.
        StartAvoidanceTimer();
        
        return _currentSteeringOutput;
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        ITargeter targeterBehavior = 
            this.FindChild<ITargeter>();

        PassiveAgentAvoiderSteeringBehavior passiveAvoiderBehavior =
            this.FindChild<PassiveAgentAvoiderSteeringBehavior>();
        
        List<string> warnings = new();
        
        if (targeterBehavior == null || !(targeterBehavior is ISteeringBehavior))
        {
            warnings.Add("This node needs a child of type both ISteeringBehavior and " +
                         "ITargeter to work. ");  
        }

        if (passiveAvoiderBehavior== null)
        {
            warnings.Add("This node needs a child of type " +
                         "PassiveAgentAvoiderSteeringBehavior to work. "); 
        }
        
        return warnings.ToArray();
    }
    
}