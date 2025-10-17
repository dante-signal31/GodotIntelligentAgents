using System;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

namespace GodotGameAIbyExample.Scripts.Groups;

/// <summary>
/// Represents the arguments for an event triggered when the dimensions of a formation
/// change.
/// </summary>
public class FormationDimensionsChangedArgs: EventArgs
{
    public Vector2[] MembersPositions { get; }
    public float MemberRadius { get; }
    
    public FormationDimensionsChangedArgs(
        Vector2[] membersPositions, 
        float memberRadius)
    {
        MembersPositions = membersPositions;
        MemberRadius = memberRadius;
    }
}