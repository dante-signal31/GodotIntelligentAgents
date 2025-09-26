using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;

// It must be marked as Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

[Tool]
/// <summary>
/// <p> Align steering behaviour makes the agent look at the same direction than
/// a target GameObject. </p>
/// </summary>
public partial class FaceSteeringBehavior : Node, ISteeringBehavior, ITargeter
{
    [ExportCategory("CONFIGURATION:")]
    /// <summary>
    /// Target to face to.
    /// </summary>
    [Export] public Node2D Target { get; set; }
    
    private AlignSteeringBehavior _alignSteeringBehavior;
    private Vector2 _targetPosition;
    private Node2D _marker;

    public override void _Ready()
    {
        if (Target != null) _targetPosition = Target.GlobalPosition;
        _alignSteeringBehavior = this.FindChild<AlignSteeringBehavior>();
        // We use an align steering behavior to make the agent update its rotation. But
        // align behavior copies another Node2D rotation, so we need a dummy
        // Node2D to rotate it in the direction to look at. That dummy Node2D
        // will be passed to align steering behavior, to give it something to copy.
        _marker = new Node2D();
        _marker.Name = "FaceAlignMarker";
        // Marker should be a child of the root node to avoid to be sure it's independent
        // of our movement.
        GetTree().Root.CallDeferred(Window.MethodName.AddChild, _marker);
        _marker.CallDeferred(Node2D.MethodName.SetOwner, GetTree().Root);
        // Make the align steering behavior to copy the dummy Node2D rotation.
        _alignSteeringBehavior.Target = _marker;
    }

    // public override void _ExitTree()
    // {
    //     _marker?.QueueFree();
    // }

    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        if (Target == null) return new SteeringOutput(Vector2.Zero, 0);
        
        _targetPosition = Target.GlobalPosition;
        Vector2 currentPosition = args.Position;
        
        Vector2 direction = _targetPosition - currentPosition;
        
        // Rotate the dummy Node2D in the direction we want to look at. Remember
        // that dummy Node2D is the align steering behavior target since _Ready().
        _marker.LookAt(_marker.GlobalPosition + direction);
        return _alignSteeringBehavior.GetSteering(args);
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        _alignSteeringBehavior = this.FindChild<AlignSteeringBehavior>();

        List<string> warnings = new();
        
        if (_alignSteeringBehavior == null)
        {
            warnings.Add("This node needs a child of type AlignSteeringBehavior to work.");
        }
        
        return warnings.ToArray();
    }
}