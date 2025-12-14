using Godot;

namespace GodotGameAIbyExample.Scripts.Tools;

/// <summary>
/// The Target class represents a 2D marker in the Godot game engine, providing
/// functionality for handling user input and notifying changes to its position
/// through signals.
/// </summary>
/// <remarks>
/// This class derives from Marker2D and is intended to be used as a selectable and 
/// movable target point in a 2D game scene. It emits a signal whenever its position
/// is changed.
/// </remarks>
[Tool]
public partial class Target : Marker2D
{
    [Signal] public delegate void PositionChangedEventHandler(Vector2 newPosition);
    
    public const string PointSelectedAction = "PointSelected";
    
    [ExportCategory("CONFIGURATION:")]
    [Export] public Color MarkerColor { get; set; } = new Color(1, 0, 0);

    private Vector2 _currentPosition;

    public override void _Ready()
    {
        base._Ready();
        Modulate = MarkerColor;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);
        if (@event.IsActionPressed(PointSelectedAction))
        {
            Vector2 newPosition = GetGlobalMousePosition();
            if (_currentPosition != newPosition) 
                EmitSignal(SignalName.PositionChanged, newPosition);
            GlobalPosition = newPosition;
            GetViewport().SetInputAsHandled();
        }
    }
}