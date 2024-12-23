using Godot;

public partial class Target : Marker2D
{
    public const string PointSelectedAction = "PointSelected";
    
    [ExportCategory("CONFIGURATION:")]
    [Export] public Color MarkerColor { get; set; } = new Color(1, 0, 0);

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
            GlobalPosition = GetGlobalMousePosition();
            GetViewport().SetInputAsHandled();
        }
    }
}
