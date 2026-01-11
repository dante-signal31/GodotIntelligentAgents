using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;

namespace GodotGameAIbyExample.Scripts.Groups;

/// <summary>
/// Manages the tactical movement of agents within a formation. Makes formation members
/// take cover when a hiding point becomes available near their path.
/// </summary>
[Tool]
public partial class FormationTacticalMovementManager: Node2D, IGizmos
{
    [ExportCategory("CONFIGURATION:")]
    // MaximumDistanceFromUshers should be lower than UsherWaiterFormation
    // MaximumLaggingBehindDistance, otherwise agents could stay behind cover so long
    // that formation could get stuck with no further advance.
    [Export] public float MaximumDistanceFromUshers { get; set; } = 300.0f;
    
    /// <summary>
    /// Time to let pass between checks for obstacles.
    /// </summary>
    [Export] public float DetectionCooldown { get; set; } = 0.5f;
    
    [ExportCategory("WIRING:")] 
    [Export] private Node2D _iTargeterBehavior;
    [Export] private Node2D _iFormationMembers;
    [Export] private Node2D _iFormationUshers;
    [Export] private GroupPattern _groupPattern;
    
    [ExportCategory("DEBUG:")]
    [Export] public bool ShowGizmos { get; set; }
    [Export] public Color GizmosColor { get; set; }
    
    private ITargeter _targeter;
    private IFormation _formationMembers;
    private IFormation _formationUshers;
    // Make sure to configure the HidingPointsDetector node with a SeparationFromObstacles
    // value higher than the range of the WhiskerSensors of the agents in the formation.
    // Otherwise, agents sensor will touch covers when approaching hiding points, which
    // will trigger an avoidance movement.
    private HidingPointsDetector _hidingPointsDetector;
    private System.Timers.Timer _detectionCooldownTimer;
    private bool _waitingForDetectionCooldownTimeout;
    private Vector2[] _availableHidingPoints;
    private Node2D[] _formationNodesHidingPointTargets;
    private bool _formationNodesHidingPointTargetsInitialized;

    private bool _currentTargetChanged;
    private Node2D _currentTarget;
    private bool _showGizmos;
    private Color _gizmosColor;

    private Node2D CurrentTarget
    {
        get => _currentTarget;
        set
        {
            if (_currentTarget == value) return;
            _currentTarget = value;
            _hidingPointsDetector.Threat = value;
            _currentTargetChanged = true;
        }
    }
    
    public override void _EnterTree()
    {
        SetTimer();
    }

    public override void _ExitTree()
    {
        _detectionCooldownTimer.Elapsed -= OnTimerTimeout;
    }
    
    private void SetTimer()
    {
        _detectionCooldownTimer = 
            new System.Timers.Timer(DetectionCooldown * 1000);
        _detectionCooldownTimer.AutoReset = false;
        _detectionCooldownTimer.Elapsed += OnTimerTimeout;
    }

    private void StartDetectionCooldownTimer()
    {
        _waitingForDetectionCooldownTimeout = true;
        _detectionCooldownTimer.Stop();
        _detectionCooldownTimer.Start();
    }
    
    private void StopDetectionCooldownTimer()
    {
        _waitingForDetectionCooldownTimeout = false;
        _detectionCooldownTimer.Stop();
    }
    
    private void OnTimerTimeout(object sender, System.Timers.ElapsedEventArgs e)
    {
        _waitingForDetectionCooldownTimeout = false;
    }

    /// <summary>
    /// Redirects a specific agent to a designated hiding point. Updates the agent's
    /// target position and assigns the provided hiding point as its new target for
    /// tactical movement.
    /// </summary>
    /// <param name="agentIndex">
    /// The index of the agent within the formation that is being redirected to the
    /// hiding point.
    /// </param>
    /// <param name="hidingPoint">
    /// The global position of the hiding point to which the agent will be redirected.
    /// </param>
    private void RedirectAgentToHidingPoint(int agentIndex, Vector2 hidingPoint)
    {
        ITargeter agentTargeter = 
            _formationMembers.Members[agentIndex].FindChild<ITargeter>();
        _formationNodesHidingPointTargets[agentIndex].GlobalPosition = hidingPoint;
        agentTargeter.Target = _formationNodesHidingPointTargets[agentIndex];
    }

    /// <summary>
    /// Redirects the specified agents to target the formation ushers. Each agent is
    /// assigned to the corresponding usher in the formation based on their index. This
    /// method ensures agents without suitable hiding points are redirected towards the
    /// ushers.
    /// </summary>
    /// <param name="agentsToRedirect">
    /// A set of agent indices representing the agents to be redirected to the formation
    /// ushers.
    /// </param>
    private void RedirectAgentsToUshers(HashSet<int> agentsToRedirect)
    {
        foreach (int agentIndex in agentsToRedirect)
        {
            ITargeter agentTargeter = 
                _formationMembers.Members[agentIndex].FindChild<ITargeter>();
            if (agentTargeter != null)
            {
                agentTargeter.Target = _formationUshers.Members[agentIndex];
            }
        }
    }
    
    public override void _Ready()
    {
        _targeter = (ITargeter) _iTargeterBehavior;
        _formationMembers = (IFormation) _iFormationMembers;
        _formationUshers = (IFormation) _iFormationUshers;
        Node2D _currentRoot = GetTree().Root.FindChild<Node2D>();
        if (_currentRoot == null) return;
        Levels.Courtyard _currentLevel = _currentRoot.FindChild<Levels.Courtyard>();
        _hidingPointsDetector = this.FindChild<HidingPointsDetector>();
        _hidingPointsDetector.Threat = _targeter.Target;
        _hidingPointsDetector.ObstaclesPositions = _currentLevel.ObstaclePositions;
        _currentTarget = _targeter.Target;
    }


    /// <summary>
    /// Assigns suitable hiding points to agents within the defined maximum distance
    /// from the positions of the ushers in the formation. For each agent, the nearest
    /// hiding point to the current target is selected if available. If a suitable hiding
    /// point is found, the agent is redirected to it.
    /// </summary>
    /// <returns>
    /// A set of agent indices that have been successfully assigned suitable hiding
    /// points.
    /// </returns>
    private HashSet<int> AssignSuitableHidingPointsToAgents()
    {
        int agentIndex = 0;
        HashSet<int> agentsWithSuitableHidingPoints = new();
        foreach (Vector2 usherLocalPosition in _formationUshers.MemberPositions)
        {
            // Find the hiding points inside the maximum distance from the usher's
            // position.
            Vector2 usherGlobalPosition = ToGlobal(usherLocalPosition);
            Vector2[] nearbyHidingPoints = _availableHidingPoints
                .Where(hidingPoint => 
                    hidingPoint.DistanceTo(usherGlobalPosition) <= 
                    MaximumDistanceFromUshers)
                .ToArray();

            // Find the nearest hiding point to the current usher's position.
            Vector2 nearestHidingPoint = Vector2.Zero;
            if (nearbyHidingPoints.Length > 0)
            {
                nearestHidingPoint = nearbyHidingPoints
                    .OrderBy(hidingPoint => 
                        hidingPoint.DistanceTo(usherGlobalPosition))
                    .First();
            }
            
            // Assign the hiding point if any.
            if (nearestHidingPoint != Vector2.Zero)
            {
                RedirectAgentToHidingPoint(agentIndex, nearestHidingPoint);
                agentsWithSuitableHidingPoints.Add(agentIndex);
            }
            
            agentIndex++;
        }
        return agentsWithSuitableHidingPoints;
    }

    private void InitNodesHidingPointTargets()
    {
        _formationNodesHidingPointTargets = new Node2D[_formationMembers.MemberPositions.Count];
        for (int i = 0; i < _formationNodesHidingPointTargets.Length; i++)
        {
            _formationNodesHidingPointTargets[i] = new Node2D();
        }
    }
    
    public override void _PhysicsProcess(double delta)
    {
        if (Engine.IsEditorHint()) return;
        
        if (_targeter.Target != CurrentTarget) CurrentTarget = _targeter.Target;

        if (!_formationNodesHidingPointTargetsInitialized)
        {
            // This cannot be done in _Ready() because _formationMembers.MemberPosition
            // is not yet ready.
            InitNodesHidingPointTargets();
            _formationNodesHidingPointTargetsInitialized = true;            
        }
        
        // Our calculations are a heavy load. So, we'd better not do it every frame. 
        if (_waitingForDetectionCooldownTimeout && !_currentTargetChanged) return;
        _currentTargetChanged = false;
        
        _availableHidingPoints = _hidingPointsDetector.HidingPoints.ToArray();
        HashSet<int> agentsWithHidingPoints = AssignSuitableHidingPointsToAgents();
        HashSet<int> agentsWithNoHidingPoints = 
            Enumerable.Range(0, _formationMembers.Members.Count)
                .Except(agentsWithHidingPoints)
                .ToHashSet();
        RedirectAgentsToUshers(agentsWithNoHidingPoints);
        
        StartDetectionCooldownTimer();
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
        if (_groupPattern == null) return;
        
        // Draw range to find hiding positions for every 
        foreach (Vector2 usherLocalPosition in _groupPattern.Positions.Offsets)
        {
            DrawCircle(
                usherLocalPosition,
                MaximumDistanceFromUshers,
                GizmosColor,
                filled: false);
        }
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new();

        HidingPointsDetector hidingPointsDetector = this.FindChild<HidingPointsDetector>();
        if (hidingPointsDetector == null)
        {
            warnings.Add("This node needs a child node of type " +
                         "HidingPointDetector to work properly.");  
        }
        
        return warnings.ToArray();
    }


}