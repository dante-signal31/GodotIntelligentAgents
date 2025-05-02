using Godot;

namespace GodotGameAIbyExample.addons.InteractiveRanges.CircularRange;

/// <summary>
/// Circular gizmo to configure ranges for 2D games.
/// </summary>
[Tool]
public partial class CircularRange : Node2D
{
    [Signal] public delegate void UpdatedEventHandler();
    
    [ExportCategory("CONFIGURATION:")]
    /// <summary>
    /// Gizmo color.
    /// </summary>
    [Export] public Color RangeColor { get; set; } = new Color(1, 0, 0);
    
    private float _radius;
    /// <summary>
    /// Range from the center.
    /// </summary>
    public float Radius
    {
        get=> _radius;
        set
        {
            if (Mathf.IsEqualApprox(_radius, value)) return;
            _radius = value;
            EmitSignal(SignalName.Updated);
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (!Engine.IsEditorHint()) return;
        
        DrawCircle(Vector2.Zero, Radius, RangeColor, filled: false);
    }
}