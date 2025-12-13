using Godot;

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
