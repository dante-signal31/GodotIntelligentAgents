using Godot;

namespace GodotGameAIbyExample.addons.InteractiveRanges.ConeRange;

/// <summary>
/// Cone gizmo to configure ranges for 2D games.
/// </summary>
[Tool]
public partial class ConeRange : Node2D
{
    [ExportCategory("CONFIGURATION:")] 
    private Color _rangeColor = new Color(1, 0, 0);
    /// <summary>
    /// Gizmo color.
    /// </summary>
    [Export] public Color RangeColor
    {
        get => _rangeColor;
        set
        {
            _rangeColor = value;
            QueueRedraw();
        }
    }

    private float _semiConeDegrees;
    /// <summary>
    /// Half angular width in degrees for this cone.
    /// </summary>
    [Export(PropertyHint.Range, "0, 90")] public float SemiConeDegrees
    {
        get => _semiConeDegrees;
        set
        {
            _semiConeDegrees = value;
            QueueRedraw();
        }
    }

    private int _resolution;
    /// <summary>
    /// <p>Resolution for this gizmo.</p>
    /// <p>The higher, the smoother the arc will be.</p>
    /// </summary>
    [Export] public int Resolution
    {
        get => _resolution;
        set
        {
            _resolution = value;
            QueueRedraw();
        }
    }

    private float _range;
    /// <summary>
    /// Length of this cone.
    /// </summary>
    [Export] public float Range
    {
        get => _range;
        set
        {
            _range = value;
            QueueRedraw();
        }
    }

    private Node2D _parent;
    
    private float SemiConeRadians => Mathf.DegToRad(SemiConeDegrees);

    public override void _EnterTree()
    {
        _parent = GetParent<Node2D>();
    }

    public override void _Draw()
    {
        if (!Engine.IsEditorHint()) return;
        
        DrawArc(
            Vector2.Zero, 
            Range, 
            -SemiConeRadians, 
            SemiConeRadians, 
            Resolution,
            color: RangeColor,
            width: 3f);
        Vector2 side = new Vector2(Range, 0);
        DrawLine(
            Vector2.Zero, 
            side.Rotated(Mathf.DegToRad(-SemiConeDegrees)),
            RangeColor);
        DrawLine(Vector2.Zero, 
            side.Rotated(Mathf.DegToRad(SemiConeDegrees)),
            RangeColor);
    }
}