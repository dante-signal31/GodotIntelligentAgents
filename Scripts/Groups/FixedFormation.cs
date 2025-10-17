using System;
using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;

namespace GodotGameAIbyExample.Scripts.Groups;

/// <summary>
/// <p>Represents a formation in which all members are positioned according to a
/// predefined pattern without dynamic changes. The class manages the members of the
/// formation and ensures they follow a specified formation pattern.</p>
/// <remarks>The problem with this approach is that formation members don't move by
/// themselves. They are just children of an usher, which is who actually moves. So,
/// as members don't use their MoveAndSlide() method, they don't check for
/// collisions and formation needs a global collider.</remarks>
/// </summary>
[Tool]
public partial class FixedFormation : Node2D
{
    // TODO: Implement IFormation interface.
    
    [ExportCategory("CONFIGURATION:")] 
    [Export] private PackedScene _memberScene;
    
    public List<Node2D> Members { get; private set; } = new();
    
    private FormationPattern _formationPattern;
    
    public override void _Ready()
    {
        if (Engine.IsEditorHint()) return;
        
        _formationPattern = this.FindChild<FormationPattern>();
        GenerateMembers();
    }

    private void GenerateMembers()
    {
        foreach (Vector2 positionOffset in _formationPattern.Positions.Offsets)
        {
            Node2D member = _memberScene.Instantiate<Node2D>();
            member.Position = positionOffset;
            // For my tests I'm instantiating MovingAgents. The problem with then is that
            // they try to move, while they are expected here to be static. That is making
            // my formation movements look weird.
            // So, I'm stopping their movement disabling their process mode.
            // In a more realistic implementation, I would instantiate a specific scene
            // that would not move but would keep its process mode activated to be able
            // to animate or make whatever they are supposed to do.
            member.ProcessMode = ProcessModeEnum.Disabled;
            AddChild(member);
            Members.Add(member);
        }
    }

    public override string[] _GetConfigurationWarnings()
    {
        FormationPattern formationPattern = this.FindChild<FormationPattern>();
        
        List<string> warnings = new();
        
        if (formationPattern == null)
        {
            warnings.Add("This node needs a child node of type " +
                         "FormationPattern to work properly.");  
        }
        
        return warnings.ToArray();
    }
}