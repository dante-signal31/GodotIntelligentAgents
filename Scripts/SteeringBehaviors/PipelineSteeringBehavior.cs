using System.Collections.Generic;
using System.Timers;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using Timer = System.Timers.Timer;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// Represents a pipeline-based steering behavior that combines multiple
/// elements to compute a SteeringOutput. The behavior involves targeting,
/// goal decomposition, constraint enforcement, and goal execution through an actuator.
/// </summary>
[Tool]
public partial class PipelineSteeringBehavior: Node2D, ISteeringBehavior
{
    [ExportCategory("CONFIGURATION:")]
    [Export] public float AvoidanceTimeout { get; set; } = 0.5f;
    
    private PipelineTargeterAggregator _targeterAggregator;
    private PipelineDecomposerAggregator _decomposerAggregator;
    private PipelineConstraintsAggregator _constraintsAggregator;
    private IPipelineActuator _actuator;
    private ISteeringBehavior _deadlockSteeringBehavior;
    private Timer _cooldownTimer;
    private bool _waitingForCooldownTimeout;
    private SteeringOutput _currentSteering;

    private void UpdateChildrenReferences()
    {
        _targeterAggregator = this.FindChild<PipelineTargeterAggregator>();
        _decomposerAggregator = this.FindChild<PipelineDecomposerAggregator>();
        _constraintsAggregator = this.FindChild<PipelineConstraintsAggregator>();
        _deadlockSteeringBehavior = this.FindChild<ISteeringBehavior>();
        _actuator = this.FindChild<IPipelineActuator>();
    }
    
    public override void _Ready()
    {
        UpdateChildrenReferences();
        
        // Set up timer.
        _cooldownTimer = new Timer(AvoidanceTimeout * 1000);
        _cooldownTimer.AutoReset = false;
        _cooldownTimer.Elapsed += OnCooldownTimeout;
    }
    
    private void OnCooldownTimeout(object sender, ElapsedEventArgs elapsedEventArgs)
    {
        _waitingForCooldownTimeout = false;
    }
    
    private void StartCooldownTimer()
    {
        _cooldownTimer.Stop();
        _cooldownTimer.Start();
        _waitingForCooldownTimeout = true;
    }

    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        if (_waitingForCooldownTimeout) return _currentSteering;
        
        // First, we ask the targeters to get a high-level goal.
        PipelineGoal targeterGoals = _targeterAggregator.GetAggregatedGoal(args);
        
        // Divide and win. We ask the decomposer to decompose the high-level goal into
        // smaller, simpler to get goals.
        PipelineGoal partialGoal = _decomposerAggregator.DecomposeGoal(
            targeterGoals, 
            args);
        
        // Check if the subgoal violates any constraint. If that's the case, the
        // constraints will try to calculate a new subgoal, the similar as possible to
        // the original one, but that does not violate the constraint. If no new
        // subgoal could be found that complies with every constraint, then we return
        // a null goal.
        PipelineGoal constrainedGoal = _constraintsAggregator.ApplyConstraints(
            partialGoal, 
            args,
            _actuator);
        
        if (constrainedGoal == null)
            // If we get here, then it means no meaningful goal was found.
            // So, we fall back to a deadlock-steering behavior.
            return _deadlockSteeringBehavior.GetSteering(args);
        
        // If a subgoal could be calculated, then we ask the actuator to get the best
        // steering output to achieve the goal.
        _currentSteering = _actuator.GetOutput(constrainedGoal, args);
        StartCooldownTimer();
        return _currentSteering;
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        UpdateChildrenReferences();
        
        List<string> warnings = new();
        
        if (_targeterAggregator == null)
        {
            warnings.Add("This node needs a child of type " +
                         "PipelineTargeterAggregator to work.");
        }
        if (_decomposerAggregator == null)
        {
            warnings.Add("This node needs a child of type PipelineDecomposerAggregator to " +
                         "work.");
        }
        if (_constraintsAggregator == null)
        {
            warnings.Add("This node needs a child of type PipelineConstraintsAggregator to " +
                         "work.");
        }

        if (_deadlockSteeringBehavior == null)
        {
            warnings.Add("This node needs a child of type ISteeringBehavior to work.");
        }

        if (_actuator == null)
        {
            warnings.Add("This node needs a child of type IPipelineActuator to work.");
        }
        
        return warnings.ToArray();
    }
}