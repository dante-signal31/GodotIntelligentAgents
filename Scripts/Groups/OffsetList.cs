using Godot;
using Godot.Collections;

namespace GodotGameAIbyExample.Scripts.Groups;

/// <summary>
/// <p>A class that encapsulates a collection of offsets represented as a list of Vector2
/// values.</p>
/// <p>Every offset represents a relative point position.</p>
/// </summary>
// Tool attribute is needed to use this resource in fields exported from tools nodes.
[Tool]
// GlobalClass attribute is needed to find this resource in the general resources list.
[GlobalClass]
public partial class OffsetList : Resource
{
    [ExportCategory("CONFIGURATION:")]
    [Export] public Array<Vector2> Offsets = new();
}