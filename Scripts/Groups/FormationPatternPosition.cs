using Godot;

namespace GodotGameAIbyExample.Scripts.Groups;

public partial class FormationPatternPosition : Node2D
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