using Godot;

namespace GodotGameAIbyExample.Scripts.Tools;

/// <summary>
/// Struct to represent ray ends for every sensor in the editor local space.
/// </summary>
public struct RayEnds
{
    public Vector2 Start;
    public Vector2 End;

    public RayEnds()
    {
        Start = Vector2.Zero;
        End = Vector2.Zero;
    }
        
    public RayEnds(Vector2 start, Vector2 end)
    {
        Start = start;
        End = end;
    }
}