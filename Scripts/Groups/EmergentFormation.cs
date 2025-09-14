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
    [Export] private float _cleanAreaRadius = 55.0f;
    
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
    private bool _waitingForNewAttemptTimeout;
    private MovingAgent _ownAgent;
    
    private Node2D _partner;

    /// <summary>
    /// Specifies the partnered agent to synchronize movements or behaviors within the
    /// formation.
    /// </summary>
    private Node2D Partner
    {
        get => _partner;
        set
        {
            _partner = value;
            _followSteeringBehavior.Target = (MovingAgent) value;
        }
    }
    
    private Vector2 _partnerOffset;

    /// <summary>
    /// Specifies the offset position of the partnered agent to synchronize movements or
    /// behaviors within the formation.
    /// </summary>
    private Vector2 PartnerOffset
    {
        get => _partnerOffset;
        set
        {
            _partnerOffset = value;
            if (_followSteeringBehavior == null) return;
            _followSteeringBehavior.OffsetFromTarget = value;
        }
    }

    public override void _EnterTree()
    {
        SetTimer();
        _ownAgent = (MovingAgent) GetParent();
    }

    private void SetTimer()
    {
        _newAttemptTimer = new System.Timers.Timer(_newAttemptDelay * 1000);
        _newAttemptTimer.AutoReset = false;
        _newAttemptTimer.Elapsed += OnTimerTimeout;
    }
    
    private void OnTimerTimeout(object sender, System.Timers.ElapsedEventArgs e)
    {
        _waitingForNewAttemptTimeout = false;
    }

    private void StartNewAttemptTimer()
    {
        _waitingForNewAttemptTimeout = true;
        _newAttemptTimer.Stop();
        _newAttemptTimer.Start();
    }
    
    public override void _Ready()
    {
        Array<Node> groupNodes = GetTree().GetNodesInGroup(_formationGroupName);
        _groupMembers = new Queue<Node2D>(groupNodes.Cast<Node2D>().ToArray());
        _followSteeringBehavior = GetParent().FindChild<OffsetFollowBehavior>();
        _cleanAreaChecker = new CleanAreaChecker(
            _cleanAreaRadius, 
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
    
    public override void _PhysicsProcess(double delta)
    {
        if (Engine.IsEditorHint()) return;
        
        if (_partner != null)
        {
            // If we have a suitable formation partner, then check we can still use the
            // selected offset position.
            Vector2 offsetGlobalPosition = Partner.ToGlobal(PartnerOffset);
            // If we are within the offset area, then there is no need to check anything.
            if (GlobalPosition.DistanceTo(offsetGlobalPosition) <= 2 * _cleanAreaRadius ||
                // If we are outside the clean area, then we need to check if we can still
                // use the offset position.
                _cleanAreaChecker.IsCleanArea(offsetGlobalPosition))
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
        //
        // Vanilla algorithm has a weak point. There is a small chance that three members 
        // (or more) partner to follow each other, forming a ring apart from the main
        // formation. So, the main formation can go away while these other members chase
        // each other in a loop. This problem is inherent to a simple behavior where a
        // formation member just follows another member.
        //
        // A more complex behavior could be used to avoid this problem, running graph
        // searches to check that following Partner references up in the 
        // graph you eventually reach the formation leader. If a formation leader cannot
        // be reached, then a member can assume that it is in a loop and can run a search
        // for a new partner to break the loop and join the main formation.
        foreach (Node2D member in _groupMembers)
        {
            // Don't try to partner with our own agent.
            if (member == _ownAgent) continue;
            
            // Don't try to partner with agents that are using us as partners.
            EmergentFormation memberEmergentFormation = 
                member.FindChild<EmergentFormation>();
            if (memberEmergentFormation != null &&
                memberEmergentFormation.Partner == _ownAgent)
                continue;
            
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