using System.Collections.Generic;
using System.Linq;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;
using GodotGameAIbyExample.Scripts.Tools;

namespace GodotGameAIbyExample.Scripts.Groups;

/// <summary>
/// The FormationObstacleManager class handles formation movement in the presence of
/// large obstacles, addressing issues where formation agents become stuck attempting
/// to follow ushers trapped within obstacles. It employs logic to redirect agents to
/// the main formation target or ushers, circumventing obstacles
/// and enabling smoother and more realistic navigation.
/// </summary>
public partial class FormationObstacleManager : Node2D
{
    [ExportCategory("CONFIGURATION:")]
    /// <summary>
    /// Our agents radius.
    /// </summary>
    [Export] public float ObstacleDetectionRadius { get; set; } = 50.0f;

    /// <summary>
    /// Layer where our obstacles are.
    /// </summary>
    [Export(PropertyHint.Layers2DPhysics)] public uint ObstaclesLayers { get; set; } = 1;
    
    /// <summary>
    /// Time to let pass between checks for obstacles.
    /// </summary>
    [Export] public float DetectionCooldown { get; set; } = 0.5f;
    
    [ExportCategory("WIRING:")] 
    [Export] private Node2D _iTargeterBehavior;
    [Export] private Node2D _iFormation;
    [Export] private Node2D _iFormationUshers;

    private ITargeter _targeter;
    private IFormation _formation;
    private IFormation _formationUshers;
    private CleanAreaChecker _areaChecker;
    private System.Timers.Timer _detectionCooldownTimer;
    private bool _waitingForDetectionCooldownTimeout;
    HashSet<int> _formationPositionsInsideObstacles = new(); 

    public override void _EnterTree()
    {
        _areaChecker = new CleanAreaChecker(
            ObstacleDetectionRadius, 
            ObstaclesLayers, 
            this);
        SetTimer();
    }

    public override void _ExitTree()
    {
        _areaChecker.Dispose();
        _detectionCooldownTimer.Elapsed -= OnTimerTimeout;
    }

    public override void _Ready()
    {
        _targeter = (ITargeter) _iTargeterBehavior;
        _formation = (IFormation) _iFormation;
        _formationUshers = (IFormation) _iFormationUshers;
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

    public HashSet<int> GetFormationPositionsInsideObstacles()
    {
        HashSet<int> formationPositionsInsideObstacles = new();
        for (int i = 0; i < _formationUshers.MemberPositions.Count; i++)
        {
            if (!_areaChecker.IsCleanArea(ToGlobal(_formationUshers.MemberPositions[i])))
            {
                formationPositionsInsideObstacles.Add(i);
            }
        }
        return formationPositionsInsideObstacles;
    }

    
    public void RedirectAgentsToFormationTarget(HashSet<int> agentsToRedirect)
    {
        foreach (int agentIndex in agentsToRedirect)
        {
            ITargeter agentTargeter = 
                _formation.Members[agentIndex].FindChild<ITargeter>();
            if (agentTargeter != null)
            {
                agentTargeter.Target = _targeter.Target;
            }
        }
    }

    public void RedirectAgentsToUshers(HashSet<int> agentsToRedirect)
    {
        foreach (int agentIndex in agentsToRedirect)
        {
            ITargeter agentTargeter = 
                _formation.Members[agentIndex].FindChild<ITargeter>();
            if (agentTargeter != null)
            {
                agentTargeter.Target = _formationUshers.Members[agentIndex];
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // We are using a shapecast to check for obstacles, and that is a heavy load. So,
        // we'd better not do it every frame. 
        if (_waitingForDetectionCooldownTimeout) return;
        
        // To avoid members trying to follow ushers while they are inside obstacles.
        // Those members are redirected to the main formation target. As soon as their 
        // respective ushers are outside the obstacle, they are redirected to ushers
        // again.
        HashSet<int> positionsInsideObstacles = GetFormationPositionsInsideObstacles();
        
        HashSet<int> positionsJustEnteredObstacles = 
            positionsInsideObstacles
                .Except(_formationPositionsInsideObstacles)
                .ToHashSet();
        HashSet<int> positionJustLeftObstacles =
            _formationPositionsInsideObstacles
                .Except(positionsInsideObstacles)
                .ToHashSet();
        
        _formationPositionsInsideObstacles = positionsInsideObstacles;
        
        if (positionsJustEnteredObstacles.Count > 0)
        {
            RedirectAgentsToFormationTarget(positionsJustEnteredObstacles);
        }
        
        if (positionJustLeftObstacles.Count > 0)
        {
            RedirectAgentsToUshers(positionJustLeftObstacles);
        }
        
        StartDetectionCooldownTimer();
    }
}