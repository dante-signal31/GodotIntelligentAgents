using Godot;
using Godot.Collections;
using GodotGameAIbyExample.Scripts.Groups;


namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Represents a path consisting of a sequence of position nodes that entities can
/// traverse. The path can be configured to loop back to the beginning when the end
/// is reached.
/// </summary>
[Tool]
public partial class Path: GroupPattern
{
    [Signal] public delegate void PathUpdatedEventHandler();

    [ExportCategory("PATH CONFIGURATION:")]
    private bool _loop;
    /// <summary>
    /// Indicates whether the path should loop back to the beginning once the end is
    /// reached.
    /// </summary>
    [Export] public bool Loop
    {
        get => _loop;
        set
        {
            if (_loop == value) return;
            _loop = value;
            EmitSignal(SignalName.PathUpdated);
        }
    }

    /// <summary>
    /// Target positions of the path.
    /// </summary>
    public Array<Vector2> TargetPositions => Positions.Offsets;
    
    /// <summary>
    /// How many positions this path has.
    /// </summary>
    public int PathLength => Positions.Offsets.Count;

    /// <summary>
    /// Current index of the position we are going to.
    /// </summary>
    public int CurrentTargetPositionIndex { get; private set; }
    
    /// <summary>
    /// Position at the current position index.
    /// </summary>
    public Vector2 CurrentTargetPosition => Positions.Offsets[CurrentTargetPositionIndex];

    public void LoadPathData(Array<Vector2> positions)
    {
        Positions.Offsets = positions;
        // New path, then reset index.
        CurrentTargetPositionIndex = 0;
        EmitSignal(SignalName.PathUpdated);
    }
    
    /// <summary>
    /// <p>Get the next position target in Path.</p>
    /// </summary>
    /// <returns>
    /// <p>Next position node if we are not at the end.</p>
    /// <p>If we are at the end and Loop is false, then the last target position is
    /// returned; whereas if the loop is true, then the index is reset to 0 and the
    /// first target position is returned.</p></returns>
    public Vector2 GetNextPositionTarget()
    {
        if (CurrentTargetPositionIndex == Positions.Offsets.Count - 1)
        {
            if (Loop) CurrentTargetPositionIndex = 0;
        }
        else
        {
            CurrentTargetPositionIndex++;
        }
        return Positions.Offsets[CurrentTargetPositionIndex];
    }


    /// <summary>
    /// Empty path constructor.
    /// </summary>
    public Path()
    {
        Positions ??= new OffsetList();
    }

    /// <summary>
    /// Constructor for a path with a predefined set of positions.
    /// </summary>
    /// <param name="pathPositions">Initial path positions.</param>
    public Path(Vector2[] pathPositions) : this()
    {
        Array<Vector2> positions = new(pathPositions);
        LoadPathData(positions);
    }
    
    public override void _Ready()
    {
        base._Ready();
        PositionGizmoRadius = 10;
        GizmoTextOffset = new(10, 10);
        Positions ??= new OffsetList();
    }

    public override void _Draw()
    {
        if (!ShowGizmos) return;
        
        if (Positions.Offsets.Count < 1) return;

        Vector2 previousPosition = Vector2.Zero;
        // Draw path positions
        for (int i=0; i < Positions.Offsets.Count; i++)
        {
            Vector2 gizmoBorder = new Vector2(PositionGizmoRadius, PositionGizmoRadius);
            Vector2 textPosition = Positions.Offsets[i] - gizmoBorder - GizmoTextOffset;
            DrawString(ThemeDB.FallbackFont, textPosition, $"{Name}-{i}");
            DrawCircle(
                Positions.Offsets[i], 
                PositionGizmoRadius, 
                GizmosColor, 
                filled: false);
            if (i >= 1)
            {
                // Draw edges between positions.
                DrawLine(previousPosition, Positions.Offsets[i], GizmosColor);
            }
            previousPosition = Positions.Offsets[i];
        }
    }
}