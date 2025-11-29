using System.Collections.Generic;
using Godot;
using Godot.Collections;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;

namespace GodotGameAIbyExample.Scripts.Groups;

/// <summary>
/// The class is used to define and manage defines the positions of the group members
/// in relation with group origin. 
/// </summary>
[Tool]
public partial class GroupPattern : Node2D, IGizmos
{
    [ExportCategory("GROUP PATTERN CONFIGURATION:")]
    [Export] public OffsetList Positions { get; set; }

    [ExportCategory("DEBUG:")] 
    [Export] public bool ShowGizmos { get; set; } = true;

    [Export] public Color GizmosColor { get; set; } = Colors.GreenYellow;
    
    private int _originGizmoRadius = 10;
    protected int PositionGizmoRadius = 50;
    protected Vector2 GizmoTextOffset = new(10, 10);
    private int _previousPositionsSize;
    private bool _recreatingPositionsNodes;
    private Array<Vector2> _previousPositions = new();
    
    /// <summary>
    /// List of position nodes associated with this group.
    /// </summary>
    private readonly List<GroupMemberPosition> _positionNodes = new();
    
    public override void _Ready()
    {
        // Start on a blank canvas. New position nodes will be created with those
        // stored inside the scene when this was closed.
        RemoveOldPositionsNodes();
    }
    
    public override void _Process(double delta)
    {
        CheckForChangesInPositionsList();
        CleanPositionNodesHierarchy();
        DrawGizmos();
    }

    /// <summary>
    /// Checks for changes in the Positions list by comparing the current offsets
    /// with the previously stored offsets. If any modification in the number or
    /// values of positions is detected, the method triggers the necessary actions
    /// to respond to these changes and updates the stored offsets to reflect the
    /// current state of the Positions list.
    /// </summary>
    private void CheckForChangesInPositionsList()
    {
        if (Positions == null || !Engine.IsEditorHint()) return;
        
        // If any position has been added or removed from the list, we need to recreate
        // the position nodes.
        if (_previousPositions.Count != Positions.Offsets.Count)
        {
            _OnPositionListChanged();
            _previousPositions = Positions.Offsets;
        }
        else
        {
            for (int i = 0; i < Positions.Offsets.Count; i++)
            {
                // If any position in the list has been modified, then apply that
                // modification to its respective node.
                //
                // When you change the position value in the list, the gizmo will move,
                // but the position node will stay the same in the viewport until you
                // move the mouse into the viewport or click on the modified node. Don't
                // worry; the node position has been updated, although the viewport
                // doesn't. Actually, this is a bug in Godot, as you can read here:
                // https://stackoverflow.com/questions/78590785/node2d-position-marker-not-updating-in-editor
                // https://github.com/godotengine/godot/issues/92894
                if (_previousPositions[i] != Positions.Offsets[i])
                {
                    _positionNodes[i].Position = Positions.Offsets[i];
                    _previousPositions[i] = Positions.Offsets[i];
                    break;
                }
            }
        }
    }

    /// <summary>
    /// User should not create or remove GroupMemberPositions nodes manually. So,
    /// this method validates the hierarchy of position nodes associated with the
    /// formation pattern. If the number of position nodes does not match the number of
    /// offsets, the related nodes are recreated to ensure consistency.
    /// </summary>
    private void CleanPositionNodesHierarchy()
    {
        if (Positions == null || !Engine.IsEditorHint()) return;
        
        List<GroupMemberPosition> currentNodePositions = 
            this.FindChildren<GroupMemberPosition>();
        if ((currentNodePositions == null ||
            currentNodePositions.Count != Positions.Offsets.Count) && 
            !_recreatingPositionsNodes)
            RecreatePositionNodes();
    }

    /// <summary>
    /// <p>Creates new position nodes based on the offsets defined in the `Positions`
    /// property.</p>
    /// <p>Each position node is initialized, added as a child node, and connected to
    /// listen for position changes. The created nodes are named sequentially, and their
    /// positions are set to match the corresponding offsets.</p>
    /// </summary>
    private void CreateNewPositionNodes()
    {
        _positionNodes.Clear();
        _previousPositions.Clear();
        for (int i=0; i < Positions.Offsets.Count; i++)
        {
            GroupMemberPosition memberPosition = new();
            memberPosition.Init(i);
            memberPosition.Name = $"GroupPosition{i}";
            AddChild(memberPosition);
            memberPosition.SetOwner(GetTree().EditedSceneRoot);
            memberPosition.Position = Positions.Offsets[i];
            SetEditableInstance(memberPosition, true);
            memberPosition.Connect(
                GroupMemberPosition.SignalName.PositionChanged, 
                new Callable(this, MethodName._OnPositionChanged));
            _positionNodes.Add(memberPosition);
            _previousPositions.Add(Positions.Offsets[i]);
        }
    }

    /// <summary>
    /// Removes all existing position nodes of type `GroupMemberPosition` that are
    /// children of the current `GroupPattern` node.
    /// </summary>
    private void RemoveOldPositionsNodes()
    {
        List<GroupMemberPosition> oldPositions = 
            this.FindChildren<GroupMemberPosition>();
        
        if (oldPositions == null) return;
        
        foreach (GroupMemberPosition oldPosition in oldPositions)
        {
            RemoveChild(oldPosition);
            oldPosition.QueueFree();
        }
    }

    /// <summary>
    /// Updates the offset position at the specified index within the `Positions`
    /// property.</summary>
    /// <param name="index">The index of the position offset to update within the
    /// `Positions` property.</param>
    /// <param name="position">The new offset position to assign at the specified
    /// index.</param>
    private void _OnPositionChanged(int index, Vector2 position)
    {
        Positions.Offsets[index] = position;
    }

    /// <summary>
    /// Handles changes to the list of position offsets within the `Positions` property.
    /// If the size of the offset list changes, this method recreates all position nodes
    /// to reflect the new list. Otherwise, it updates the positions of existing nodes
    /// to match the updated offsets. This ensures position nodes stay in sync with
    /// current offsets.
    /// </summary>
    private void _OnPositionListChanged()
    {
        // If any position is added or removed, we need to recreate the position nodes.
        if (_previousPositionsSize != Positions.Offsets.Count)
        {
            RecreatePositionNodes();
            return;
        }
        
        // Otherwise, just update the positions.
        List<GroupMemberPosition> currentPositions = 
            this.FindChildren<GroupMemberPosition>();
        
        if (currentPositions == null || Positions == null) return;
        
        for (int i = 0; i < Positions.Offsets.Count; i++)
        {
            currentPositions[i].Position = Positions.Offsets[i];
        }
    }

    /// <summary>
    /// Recreates the hierarchy of position nodes associated with the group
    /// pattern.
    /// </summary>
    private void RecreatePositionNodes()
    {
        _recreatingPositionsNodes = true;
        RemoveOldPositionsNodes();
        CreateNewPositionNodes();
        _previousPositionsSize = Positions.Offsets.Count;
        _recreatingPositionsNodes = false;
    }

    private void DrawGizmos()
    {
        QueueRedraw();
    }
    
    public override void _Draw()
    {
        if (!ShowGizmos) return;
        
        // Mark the group pattern origin.
        DrawCircle(Vector2.Zero, _originGizmoRadius, GizmosColor, filled: true);
        
        if (Positions == null) return;
        
        // Draw group pattern positions.
        for (int i=0; i < Positions.Offsets.Count; i++)
        {
            Vector2 gizmoBorder = new Vector2(PositionGizmoRadius, PositionGizmoRadius);
            Vector2 textPosition = Positions.Offsets[i] - gizmoBorder - GizmoTextOffset;
            DrawString(ThemeDB.FallbackFont, textPosition, $"{i}");
            DrawCircle(
                Positions.Offsets[i], 
                PositionGizmoRadius, 
                GizmosColor, 
                filled: false);
        }
    }
}