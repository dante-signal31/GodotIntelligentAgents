using System.Collections.Generic;
using Godot;
using Godot.Collections;
using System.Linq;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;
using GodotGameAIbyExample.Scripts.Tools;

namespace GodotGameAIbyExample.Scripts.Groups;

[Tool]
public partial class EmergentFormation : Node2D
{
    [ExportCategory("CONFIGURATION:")]
    
    /// <summary>
    /// Group name to use for formation.
    /// </summary>
    [Export] private string _formationGroupName = "EmergentFormation";
    
    /// <summary>
    /// Layers with obstacles for this agent placement.
    /// </summary>
    [Export(PropertyHint.Layers2DPhysics)] private uint _notCleanLayers = 11;
    
    /// <summary>
    /// Radius to check for clean areas.
    /// </summary>
    [Export] public float CleanAreaRadius { get; private set; }= 55.0f;
    
    /// <summary>
    /// Possible offsets to try with a selected formation partner.
    /// </summary>
    [Export] private OffsetList _offsets = 
        ResourceLoader.Load<OffsetList>(
            "res://Resources/OffsetList_EmergentFormation.tres");
    
    /// <summary>
    /// Maximum number of attempts to find a valid formation partner.
    /// </summary>
    [Export] private int _maxFormationAttempts = 30;

    /// <summary>
    /// Time delay before attempting to find a formation partner again.
    /// </summary>
    [Export] private float _newAttemptDelay = 1.0f;
    
    /// <summary>
    /// Time before checking again if we are in a loop.
    /// </summary>
    [Export] private float _loopDetectionCooldown = 1.0f;
    
    /// <summary>
    /// Indicates whether we have a suitable formation partner.
    /// </summary>
    public bool NoSuitableFormationPartner => _partner == null;
    
    /// <summary>
    /// Indicates whether all attempts to search for a suitable formation partner
    /// have been exhausted.
    /// </summary>
    public bool SearchForSuitablePartnerGivenUp => 
        _formationAttempts >= _maxFormationAttempts;

    private Queue<Node2D> _groupMembers = new();
    private OffsetFollowBehavior _followSteeringBehavior;
    private CleanAreaChecker _cleanAreaChecker;
    private int _formationAttempts;
    private System.Timers.Timer _newAttemptTimer;
    private System.Timers.Timer _loopDetectionCooldownTimer;
    private bool _waitingForNewAttemptTimeout;
    private bool _waitingForLoopDetectionCooldownTimeout;
    private bool _loopDetected;
    private MovingAgent _ownAgent;
    private EmergentFormation _partnerEmergentFormation;
    
    private Node2D _partner;
    private HashSet<Node2D> _loopMembers = new();

    /// <summary>
    /// Specifies the partnered agent to synchronize movements or behaviors within the
    /// formation.
    /// </summary>
    public Node2D Partner
    {
        get => _partner;
        private set
        {
            _partner = value;
            _followSteeringBehavior.Target = (MovingAgent) value;
            _partnerEmergentFormation = value?.FindChild<EmergentFormation>();
        }
    }
    
    private Vector2 _partnerOffset;

    /// <summary>
    /// Specifies the offset position of the partnered agent to synchronize movements or
    /// behaviors within the formation.
    /// </summary>
    public  Vector2 PartnerOffset
    {
        get => _partnerOffset;
        private set
        {
            _partnerOffset = value;
            if (_followSteeringBehavior == null) return;
            _followSteeringBehavior.OffsetFromTarget = value;
        }
    }
    
    /// <summary>
    /// Whether the current partner is the leader of the formation.
    /// </summary>
    private bool PartnerIsLeader => _partnerEmergentFormation == null;

    public override void _EnterTree()
    {
        SetNewAttemptTimer();
        SetLoopDetectionCooldownTimer();
        _ownAgent = (MovingAgent) GetParent();
    }

    private void SetNewAttemptTimer()
    {
        _newAttemptTimer = new System.Timers.Timer(_newAttemptDelay * 1000);
        _newAttemptTimer.AutoReset = false;
        _newAttemptTimer.Elapsed += OnNewAttemptTimerTimeout;
    }
    
    private void SetLoopDetectionCooldownTimer()
    {
        _loopDetectionCooldownTimer = new System.Timers.Timer(_loopDetectionCooldown * 1000);
        _loopDetectionCooldownTimer.AutoReset = false;
        _loopDetectionCooldownTimer.Elapsed += OnLoopDetectionCooldownTimerTimeout;
    }
    
    private void OnNewAttemptTimerTimeout(object sender, System.Timers.ElapsedEventArgs e)
    {
        _waitingForNewAttemptTimeout = false;
    }
    
    private void OnLoopDetectionCooldownTimerTimeout(
        object sender, 
        System.Timers.ElapsedEventArgs e)
    {
        _waitingForLoopDetectionCooldownTimeout = false;
    }

    private void StartNewAttemptTimer()
    {
        _waitingForNewAttemptTimeout = true;
        _newAttemptTimer.Stop();
        _newAttemptTimer.Start();
    }
    
    private void StartLoopDetectionCooldownTimer()
    {
        _waitingForLoopDetectionCooldownTimeout = true;
        _loopDetectionCooldownTimer.Stop();
        _loopDetectionCooldownTimer.Start();
    }
    
    public override void _Ready()
    {
        Array<Node> groupNodes = GetTree().GetNodesInGroup(_formationGroupName);
        _groupMembers = new Queue<Node2D>(groupNodes.Cast<Node2D>().ToArray());
        _followSteeringBehavior = GetParent().FindChild<OffsetFollowBehavior>();
        _cleanAreaChecker = new CleanAreaChecker(
            CleanAreaRadius, 
            _notCleanLayers, 
            this);
    }

    /// <summary>
    /// Resets the number of formation attempts made for finding a suitable partner
    /// in a group formation context.
    /// </summary>
    /// <remarks>
    /// This method is typically used to retry the partner search process from the
    /// beginning, ensuring that the number of attempts is set back to zero. It is
    /// useful in situations where a previously failed attempt at finding a suitable
    /// formation partner needs to be entirely reattempted.
    /// </remarks>
    public void RetryFormationPartnerSearch()
    {
        _formationAttempts = 0;
    }

    /// <summary>
    /// <p>Vanilla algorithm has a weak point. There is a small chance that three members 
    /// (or more) partner to follow each other, forming a loop apart from the main
    /// formation. So, the main formation can go away while these other members chase
    /// each other endlessly. This problem is inherent to a simple behavior where a
    /// formation member just follows another member.</p>
    /// 
    /// <p>A more complex behavior could be used to avoid this problem, running graph
    /// searches to check that following Partner references up in the 
    /// graph until you eventually reach the formation leader. If a formation leader
    /// cannot be reached, then a member can assume that it is in a loop and can run
    /// a new search for a new partner to break the loop and join the main formation.</p>
    ///
    /// <p>This method runs a recursive check to see if we are in a loop.</p>
    /// <param name="currentLoopDetectionCalls">Current number of steps in the
    /// recursive callstack.</param>
    /// <returns>True if we are in a loop detached from the leader.
    /// False instead.</returns>
    /// </summary>
    public bool WeAreInALoop(ref HashSet<Node2D> loopMembers, 
        int currentLoopDetectionCalls = 0)
    {
        // Recursive calls are very expensive. So we should avoid use them in every frame.
        // Instead, we should use a cooldown timer to check if we are in a loop only every
        // _loopDetectionCooldown seconds.
        // So, if we are in a cooldown, then we return the cached result.
        if (_waitingForLoopDetectionCooldownTimeout) return _loopDetected;
        
        // If we don't have a partner, then we are not in a loop.
        if (Partner == null) return false;
        
        // If our current partner is the leader, then we are not in a loop.
        if (PartnerIsLeader) return false;
        
        // If we have visited before this member, then we are in a loop.
        if (loopMembers.Contains(_ownAgent)) return true;
        
        // Maximum recursion deep would be If we were in a loop composed of all group
        // members, except the leader (extremely odd situation, but theoretically
        // possible). This would end in endless recursion calls, so we break from it.
        if (currentLoopDetectionCalls > _groupMembers.Count - 1) return true;
        
        // If our partner is not the leader, and we have not reached maximum recursion
        // depth, then we continue checking with from our current partner.
        loopMembers.Add(_ownAgent);
        _loopDetected = 
            _partnerEmergentFormation.WeAreInALoop(
                ref loopMembers, 
                ++currentLoopDetectionCalls);
        
        // After our check, start a cooldown timer to wait until the new check.
        StartLoopDetectionCooldownTimer();
        
        return _loopDetected;
    }
    
    public override void _PhysicsProcess(double delta)
    {
        if (Engine.IsEditorHint()) return;
        
        if (_partner != null)
        {
            _loopMembers.Clear();
            
            // If we have a suitable formation partner, then check we can still use the
            // selected offset position.
            Vector2 offsetGlobalPosition = Partner.ToGlobal(PartnerOffset);
            
            // If we are within the offset area, then there is no need to check anything.
            if ((GlobalPosition.DistanceTo(offsetGlobalPosition) <= 2 * CleanAreaRadius ||
                // If we are outside the clean area, then we need to check if we can still
                // use the offset position.
                _cleanAreaChecker.IsCleanArea(offsetGlobalPosition)) &&
                // If we were in a loop, then we should look for a new partner.
                !WeAreInALoop(ref _loopMembers))
                return;
            
            Partner = null;
            PartnerOffset = Vector2.Zero;
        }
        
        // If we have tried too many times to find a new partner, then we must give up
        // and wait to try again.
        if ((_formationAttempts >= _maxFormationAttempts)
            && !_waitingForNewAttemptTimeout)
        {
            _formationAttempts = 0;
            StartNewAttemptTimer();
        }
        
        if (_waitingForNewAttemptTimeout) return;
        
        // If we get here, it means we have no suitable formation partner. So, we must
        // find one.
        foreach (Node2D member in _groupMembers)
        {
            // Don't try to partner with our own agent.
            if (member == _ownAgent) continue;
            
            // Don't try to partner with agents that are already in our loop.
            if (_loopMembers.Contains(member)) continue;
            
            // Don't try to partner with agents that are using us as partners.
            EmergentFormation memberEmergentFormation = 
                member.FindChild<EmergentFormation>();
            if (memberEmergentFormation != null &&
                memberEmergentFormation.Partner == _ownAgent)
                continue;
            
            // Don't try to partner with agents that are already in our loop.
            if (_loopMembers.Contains(member)) continue;
            
            foreach (Vector2 offset in _offsets.Offsets)
            {
                Vector2 offsetGlobalPosition = member.ToGlobal(offset);
                if (_cleanAreaChecker.IsCleanArea(offsetGlobalPosition))
                {
                    Partner = member;
                    PartnerOffset = offset;
                    _formationAttempts = 0;
                    return;
                }
            }
        }

        _formationAttempts++;
    }

    public override string[] _GetConfigurationWarnings()
    {
        OffsetFollowBehavior followSteeringBehavior = 
            GetParent().FindChild<OffsetFollowBehavior>();
        
        List<string> warnings = new();
        
        if (followSteeringBehavior == null)
        {
            warnings.Add("This node needs a sibling of type OffsetFollowBehavior " +
                         "to work.");  
        }
        
        return warnings.ToArray();
    }
}