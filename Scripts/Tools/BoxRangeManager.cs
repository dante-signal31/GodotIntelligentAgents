using Godot;

namespace GodotGameAIbyExample.Scripts.Tools;

// It must be marked as Tool to be found when used my custom extension
// method FindChild<T>(). Otherwise, FindChild casting will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
/// <summary>
/// <p>This node allows resizing a box BoxCollisionShape2D in realtime.</p>
///
/// <p>WARNING! when using this component, the Area2D position that hosts CollisionShape
/// should be set only by this script. So let the collider component offset the
/// property alone and do not edit it through the inspector.</p>
/// </summary>
public partial class BoxRangeManager : Node2D
{
    private readonly float _defaultRange = 10.0f;
    private readonly float _defaultWidth = 10.0f;
    
    /// <summary>
    /// <p>Possible grow directions for the box collider:</p>
    /// <list type="bullet">
    /// <item>
    /// <b>Symmetric</b>: grow in every direction. If you change the range, then UP and
    /// DOWN grow. If you change width, then LEFT and RIGHT grow.
    /// </item>
    /// <item>
    /// <b>Up</b>: grow in the UP direction if you change range. If you change width,
    /// then LEFT and RIGHT grow.
    /// </item>
    /// <item>
    /// <b>Down</b>: grow in the DOWN direction if you change range. If you change width,
    /// then LEFT and RIGHT grow.
    /// </item>
    /// <item>
    /// <b>Left</b>: grow in the LEFT direction if you change width. If you change the
    /// range, then UP and DOWN grow.
    /// </item>
    /// <item>
    /// <b>Right</b>: grow in the RIGHT direction if you change width. If you change
    /// the range, then UP and DOWN grow.
    /// </item>
    /// </list>
    /// </summary>
    private enum GrowDirection
    {   
        Symmetric,
        Up,
        Down,
        Left,
        Right  
    }

    [ExportCategory("CONFIGURATION:")] 
    private Vector2 _initialOffset;
    ///<summary>
    /// Offset of this sensor at its (1,1) dimensions.
    /// </summary>
    [Export] private Vector2 InitialOffset
    {
        get => _initialOffset;
        set
        {
            _initialOffset = value;
            RefreshBoxSize();
        }
    }

    private float _range = 10.0f;
    ///<summary>
    /// Length for this sensor. It moves UP and DOWN of the box.
    /// </summary>
    [Export] public float Range
    {
        get => _range;
        set
        {
            _range = value;
            SetBoxSize(Width, value);
        }
    }
    
    private float _width = 10.0f;
    ///<summary>
    /// Width for this sensor. It moves LEFT and RIGHT of the box.
    /// </summary>
    [Export] public float Width
    {
        get => _width;
        set
        {
            _width = value;
            SetBoxSize(value, Range);
        }
    }
    
    ///<summary>
    /// Grow direction for this sensor when width or range is change.
    /// </summary>
    [Export] private GrowDirection _growDirection;
    
    /// <summary>
    /// Reset this component to the initial offset (0,0) and width and range 10.
    /// </summary>
    [ExportToolButton("Reset")] public Callable ResetButton => 
        new Callable(this, MethodName.ResetBoxManager);


    [ExportGroup("WIRING:")] 
    [Export] public CollisionShape2D boxCollisionShape { get; set; }
    
    
    private GrowDirection _currentGrowDirection;
    private const float OffsetBias = 0.5f;

    // public override void _Ready()
    // {
    //     ResetButton = new Callable(this, MethodName.ResetBoxManager);
    // }

    /// <summary>
    /// Get offset vector needed to keep the box collider in the same position as before
    /// after changing the size.
    /// </summary>
    /// <returns>New offset vector.</returns>
    private Vector2 GetGrowOffsetVector()
    {
        switch (_growDirection)
        {
            case GrowDirection.Symmetric:
                return new Vector2(0, 0);
            case GrowDirection.Up:
                return new Vector2(0, -OffsetBias);
            case GrowDirection.Down:
                return new Vector2(0, OffsetBias);
            case GrowDirection.Left:
                return new Vector2(-OffsetBias, 0);
            case GrowDirection.Right:
                return new Vector2(OffsetBias, 0);
            default:
                return new Vector2(0, 0);
        }
    }
    
    /// <summary>
    /// Get the vector needed to grow the box collider to the new size.
    /// </summary>
    /// <param name="currentSize">Current box dimensions.</param>
    /// <param name="newSize">New box dimensions.</param>
    /// <returns>Vector with dimensions changes.</returns>
    private Vector2 GetGrowVector(Vector2 currentSize, Vector2 newSize)
    {
        return newSize - currentSize;
    }
    
    /// <summary>
    /// Update the box with its new dimensions.
    /// </summary>
    private void RefreshBoxSize()
    {
        SetBoxSize(Width, Range);
    }

    /// <summary>
    /// Sets the size of the box collider and adjusts its offset accordingly.
    /// </summary>
    /// <param name="newWidth">The new width of the box collider.</param>
    /// <param name="newRange">The new range (height) of the box collider.</param>
    private void SetBoxSize(float newWidth, float newRange)
    {
        if (boxCollisionShape == null) return;
        RectangleShape2D currentRectangleShape = boxCollisionShape.Shape as 
            RectangleShape2D;
        if (currentRectangleShape == null) return;
        
        boxCollisionShape.Position = Vector2.Zero;
        currentRectangleShape.Size = new Vector2(_defaultWidth, _defaultRange);
        Vector2 newSize = new Vector2(newWidth, newRange);
        Vector2 growOffsetVector = GetGrowOffsetVector();
        Vector2 growVector = GetGrowVector(currentRectangleShape.Size, newSize);
        currentRectangleShape.Size = newSize;
        boxCollisionShape.Position = InitialOffset + growVector * growOffsetVector;
    }
    
    public void ResetBoxManager()
    {
        InitialOffset = new Vector2(0, 0);
        Range = _defaultRange;
        Width = _defaultWidth;
        _growDirection = GrowDirection.Symmetric;
        ResetBoxCollider();
        RefreshBoxSize();
    }
    
    /// <summary>
    /// Resets the box collider to its default settings, including offset and size.
    /// Updates the box dimensions to align with the specified width and range values.
    /// </summary>
    public void ResetBoxCollider()
    {
        if (boxCollisionShape == null) return;
        boxCollisionShape.Position = Vector2.Zero;
        
        RectangleShape2D currentRectangleShape = boxCollisionShape.Shape as 
            RectangleShape2D;
        if (currentRectangleShape == null) return;
        currentRectangleShape.Size = Vector2.One;
        
        RefreshBoxSize();
    }

}