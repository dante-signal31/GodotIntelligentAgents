using Godot;

namespace GodotGameAIbyExample.addons.InteractiveRanges.SectorRange;

/// <summary>
/// <p>Sector cone gizmo to configure ranges for 2D games.</p>
/// <p>It's like a cone range, but this one has a minimum range.</p>
/// </summary>
[Tool]
public partial class SectorRange: Node2D
{
    /// <summary>
    /// Delegate for the Updated signal, which indicates that the state or
    /// configuration of the sector has been changed.
    /// </summary>
    [Signal] public delegate void UpdatedEventHandler();
    
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
            if (_rangeColor == value) return;
            _rangeColor = value;
            EmitSignal(SignalName.Updated);
            QueueRedraw();
        }
    }

    private float _semiConeDegrees;
    /// <summary>
    /// Half-angular width in degrees for this cone.
    /// </summary>
    [Export(PropertyHint.Range, "0, 90")] public float SemiConeDegrees
    {
        get => _semiConeDegrees;
        set
        {
            if (Mathf.IsEqualApprox(_semiConeDegrees, value)) return;
            _semiConeDegrees = value;
            EmitSignal(SignalName.Updated);
            QueueRedraw();
        }
    }
    
    private int Resolution { get; set; } = 40;

    private float _range;
    /// <summary>
    /// Length of this sector.
    /// </summary>
    [Export] public float Range
    {
        get => _range;
        set
        {
            if (Mathf.IsEqualApprox(_range, value)) return;
            _range = value;
            EmitSignal(SignalName.Updated);
            QueueRedraw();
        }
    }

    private float _minimumRange;
    /// <summary>
    /// Minimum range for this sector.
    /// </summary>
    [Export]
    public float MinimumRange
    {
        get => _minimumRange;
        set
        {
            if (Mathf.IsEqualApprox(_minimumRange, value)) return;
            _minimumRange = value;
            EmitSignal(SignalName.Updated);
            QueueRedraw();
        }
    }
    
    // <summary>
    /// <p>This node Forward vector.</p>
    /// <p>Actually looking to local screen right direction. So, X in Godot's 2D local
    /// axis.</p>
    /// </summary>
    public Vector2 Forward => GlobalTransform.X.Normalized(); 

    private Node2D _parent;
    
    private float SemiConeRadians => Mathf.DegToRad(SemiConeDegrees);

    public override void _EnterTree()
    {
        _parent = GetParent<Node2D>();
    }
    
    public override void _Draw()
    {
        if (!Engine.IsEditorHint()) return;
        
        // External arc.
        DrawArc(
            Vector2.Zero, 
            Range, 
            -SemiConeRadians, 
            SemiConeRadians, 
            Resolution,
            color: RangeColor,
            width: 1f);
        
        // Sides.
        Vector2 side = new Vector2(Range, 0);
        Vector2 minimumSide = new Vector2(MinimumRange, 0);
        DrawLine(
            minimumSide.Rotated(Mathf.DegToRad(-SemiConeDegrees)),  
            side.Rotated(Mathf.DegToRad(-SemiConeDegrees)),
            RangeColor);
        DrawLine(
            minimumSide.Rotated(Mathf.DegToRad(SemiConeDegrees)), 
            side.Rotated(Mathf.DegToRad(SemiConeDegrees)),
            RangeColor);
        
        // Internal arc.
        DrawArc(
            Vector2.Zero, 
            MinimumRange, 
            -SemiConeRadians, 
            SemiConeRadians, 
            Resolution,
            color: RangeColor,
            width: 1f);
    }
    
}