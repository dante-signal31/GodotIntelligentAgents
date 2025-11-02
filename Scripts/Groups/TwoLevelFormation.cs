using System;
using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;

namespace GodotGameAIbyExample.Scripts.Groups;

/// <summary>
/// This class takes an IFormation object and generates ushers at every of its positions
/// and one agent following every usher.
/// </summary>
[Tool]
public partial class TwoLevelFormation : Node2D, IFormation
{
    public event EventHandler<FormationDimensionsChangedArgs> FormationDimensionsChanged;
    
    [ExportCategory("CONFIGURATION:")] 
    /// <summary>
    /// <p>Scene for the agent to instance.</p>
    /// <remarks> It must be ITargeter compliant. </remarks>
    /// </summary>
    [Export] private PackedScene _memberScene;
    
    public List<Node2D> Members { get; } = new();

    public List<Vector2> MemberPositions => _usherFormation.MemberPositions;

    public float MemberRadius => _usherFormation.MemberRadius;

    private IFormation _usherFormation;
    private bool _membersGenerated;
    private bool _membersUpdated;

    public override void _Ready()
    {
        if (Engine.IsEditorHint()) return;
        
        // Find the formation node.
        List<Node2D> nodeChildren = this.FindChildren<Node2D>();
        if (nodeChildren == null) return;
        foreach (var node in nodeChildren)
        {
            _usherFormation = node as IFormation;
            if (_usherFormation != null)
            {
                _usherFormation.FormationDimensionsChanged += 
                    OnFormationDimensionsChanged;
                break;
            } 
        }
        
        // Generate formation members.
        if (_usherFormation == null || !CanProcess()) return;
        GenerateMembers();
        // At automated tests it might happen that members are not generated because
        // agents are disabled until their specific test enables them. But at that point,
        // _Ready cannot be called again. So, in those cases, we need to generate members
        // at _Process().
        _membersGenerated = true;
    }

    private void OnFormationDimensionsChanged(
        object sender,
        FormationDimensionsChangedArgs e)
    {
        FormationDimensionsChanged?.Invoke(this, e);
    }
    
    private void GenerateMembers()
    {
        // Let _usherFormation create Ushers. We are going to focus on creating agents.
        // I've set a Target scene at _usherFormation._memberScene to let formation
        // positions be seen.
        Members.Clear();
        foreach (Vector2 positionOffset in MemberPositions)
        {
            Node2D member = _memberScene.Instantiate<Node2D>();
            member.GlobalPosition = ToGlobal(positionOffset);
            // Agent members should be out of this node hierarchy to move freely.
            GetTree().Root.CallDeferred(Node.MethodName.AddChild, member);
            Members.Add(member);
        }
    }

    private void AssignUshersToAgents()
    {
        for (int i = 0; i < Members.Count; i++)
        {
            ITargeter targeter = Members[i].FindChild<ITargeter>();
            targeter.Target = _usherFormation.Members[i];
        }
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint()) return;

        if (!_membersGenerated)
        {
            GenerateMembers();
            _membersGenerated = true;
        }
        
        if (!_membersUpdated)
        {
            // AssignUshersToAgents cannot be at Ready() because _usherFormation is
            // creating ushers at Ready() and we need to wait for them to be created. 
            AssignUshersToAgents();
            _membersUpdated = true;
        }
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new();
        
        List<Node2D> nodeChildren = this.FindChildren<Node2D>();
        IFormation formation = null;
        foreach (var node in nodeChildren)
        {
            formation = node as IFormation;
            if (formation != null) break;
        }
        if (formation == null)
        {
            warnings.Add("This node needs a child node of type " +
                         "IFormation to work properly.");  
        }


        if (_memberScene != null)
        {
            Node instance = _memberScene.Instantiate();
            bool hasITargeterNode = false;

            if (instance is ITargeter)
            {
                hasITargeterNode = true;
            }
            else
            {
                foreach (Node child in instance.GetChildren())
                {
                    if (child is ITargeter)
                    {
                        hasITargeterNode = true;
                        break;
                    }
                }
            }

            instance.QueueFree();

            if (!hasITargeterNode)
            {
                warnings.Add("The member scene must contain a node that implements " +
                             "ITargeter interface.");
            }
        }
        
        return warnings.ToArray();
    }
}