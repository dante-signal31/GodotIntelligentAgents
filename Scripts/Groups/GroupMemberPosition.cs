using Godot;

namespace GodotGameAIbyExample.Scripts.Groups;

/// <summary>
/// This is an editor tool to place member visually. It is not instanced at runtime.
/// </summary>
[Tool]
public partial class GroupMemberPosition : Node2D
{
    [Signal] public delegate void PositionChangedEventHandler(
        int index, 
        Vector2 newPosition);

    private Vector2 _previousPosition;
    
    /// <summary>
    /// Index of this position in the formation pattern array.
    /// </summary>
    public int Index { get; private set; }

    public void Init(int index)
    {
        Index = index;
    }
    
    public override void _Process(double delta)
    {
        if (Position != _previousPosition)
        {
            EmitSignal(SignalName.PositionChanged, Index, Position);
            _previousPosition = Position;
        }
    }
}