using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using Godot.Collections;
using GodotGameAIbyExample.Scripts.Extensions;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

// It must be marked as Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
/// <summary>
/// <p>Node to offer an offset Follow steering behaviour.</p>
/// <p>Represents a steering behavior where an agent follows a target with an offset.
/// The agent anticipates the target's position based on its current position, velocity,
/// and a look-ahead time.</p>
/// </summary>
// TODO: Rename this class to OffsetFollowSteeringBehavior according to standard.
public partial class OffsetFollowBehavior: Node2D, ISteeringBehavior
{
    private const string OffsetFromTargetMarkerName = "OffsetFromTargetMarker";

    [ExportCategory("CONFIGURATION:")]
    /// <summary>
    /// Target to follow.
    /// </summary>
    [Export] public MovingAgent Target { get; set; }
    
    [ExportCategory("DEBUG:")]
    [Export] private bool ShowGizmos { get; set; }

    private ArriveSteeringBehaviorNLA _followSteeringBehavior;
    private Node2D _offsetFromTargetMarker;
    private Vector2 _offsetFromTarget;
    private MovingAgent _currentAgent;
    
    private Color AgentColor => _currentAgent.AgentColor;

    public override void _EnterTree()
    {
        // Find out who our father is.
        _currentAgent = this.FindAncestor<MovingAgent>();
    }

    public override void _Ready()
    {
        _followSteeringBehavior = this.FindChild<ArriveSteeringBehaviorNLA>();;
        Array<Node> node2DChildren = FindChildren(
            $"{OffsetFromTargetMarkerName}*", 
            "Node2D", 
            false, 
            true);
        if (node2DChildren.Count == 0) return;
        _offsetFromTargetMarker = (Node2D) node2DChildren[0];
        _followSteeringBehavior.Target = _offsetFromTargetMarker;
        UpdateOffsetFromTarget();
    }

    /// <summary>
    /// <p>Updates the offset position from the target in the local coordinate space.</p>
    /// </summary>
    public void UpdateOffsetFromTarget()
    {
        _offsetFromTarget = Target.ToLocal(_offsetFromTargetMarker.GlobalPosition);
    }

    public override void _Process(double delta)
    {
        if (ShowGizmos) DrawGizmos();
    }

    private void DrawGizmos()
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Target == null || _offsetFromTargetMarker == null || Engine.IsEditorHint()) 
            return;
        DrawLine(
            Vector2.Zero, 
            ToLocal(_offsetFromTargetMarker.GlobalPosition), 
            AgentColor);
        DrawCircle(
            ToLocal(_offsetFromTargetMarker.GlobalPosition),
            30f,
            AgentColor,
            filled:false);
        DrawLine(
            ToLocal(Target.GlobalPosition), 
            ToLocal(_offsetFromTargetMarker.GlobalPosition), 
            AgentColor);
    }

    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        // Buckland uses a look-ahead algorithm to place marker. In my tests I didn't
        // like it because in movement the follower approached nearer than offset and
        // when target stopped the follower retreated in an oddly way. So I discarded
        // the look-ahead algorithm. Nevertheless I let it commented if you want to
        // assess it.
        //
        // The look-ahead time is proportional to the distance between the target and
        // the followed; and is inversely proportional to the sum of the agent's
        // velocities.
        // float lookAheadTime = _offsetFromTarget.Length() / 
        //                       (args.MaximumSpeed + Target.CurrentSpeed);
        // Place the marker where we think the target will be at the look-ahead
        // time.
        // _offsetFromTargetMarker.GlobalPosition = Target.ToGlobal(_offsetFromTarget) + 
        //                                          (Target.Velocity *
        //                                           lookAheadTime);
        
        // In editor, offset marker can be moved freely to define the offset from
        // target. But at execution time, offset marker should follow the target to
        // be used as target by child steering behavior. Luckily, GetSteering()
        // is not called from editor.
        _offsetFromTargetMarker.GlobalPosition = Target.ToGlobal(_offsetFromTarget);
        
        // Let the child steering behavior get to the new marker position.
        return _followSteeringBehavior.GetSteering(args);
    }

    public override string[] _GetConfigurationWarnings()
    {
        ArriveSteeringBehaviorNLA followSteeringBehavior = 
            this.FindChild<ArriveSteeringBehaviorNLA>();
        Array<Node> node2DChildren = FindChildren($"{OffsetFromTargetMarkerName}", 
            type:"Node2D", 
            recursive:false, 
            owned:true);
        
        List<string> warnings = new();
        
        if (followSteeringBehavior == null)
        {
          warnings.Add("This node needs a child of type ArriveSteeringBehaviorNLA " +
                       "to work.");  
        }
        
        if (node2DChildren.Count != 1)
            warnings.Add("This node needs exactly one child of type Node2D named " +
                         "OffsetFromTargetMarker to work.");
        
        return warnings.ToArray();
    }
}