using System;
using System.Collections.Generic;
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
/// <p> This steering behavior takes its children WeightBlendedSteeringBehavior nodes
/// and gets the steering of the first node that returns a non zero steering. </p>
/// </summary>
public partial class PriorityWeightBlendedSteeringBehavior : Node2D, ISteeringBehavior, IGizmos
{

    [ExportCategory("DEBUG:")]
    /// <summary>
    /// Show gizmos.
    /// </summary>
    [Export] public bool ShowGizmos { get; set; }

    /// <summary>
    /// Colors for this object's gizmos.
    /// </summary>
    [Export] public Color GizmosColor { get; set; }

    private List<WeightBlendedSteeringBehavior> _weightBlendedSteeringBehaviors;
    private SteeringOutput _currentSteering;
    
    public override void _Ready()
    {
        _weightBlendedSteeringBehaviors =
            this.FindChildren<WeightBlendedSteeringBehavior>(recursive: false);
    }

    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        _currentSteering = SteeringOutput.Zero;
        
        foreach (WeightBlendedSteeringBehavior weightBlendedSteeringBehavior in
                 _weightBlendedSteeringBehaviors)
        {
            SteeringOutput steeringOutput = 
                weightBlendedSteeringBehavior.GetSteering(args);
            if (steeringOutput.Equals(SteeringOutput.Zero)) continue;
            _currentSteering = steeringOutput;
            break;
        }
        
        return _currentSteering;
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
        if (!ShowGizmos) return;
        
        if (_currentSteering == null) return;
        
        // Draw total steering.
        DrawLine(
            Vector2.Zero, 
            ToLocal(GlobalPosition + _currentSteering.Linear), 
            GizmosColor);
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        WeightBlendedSteeringBehavior weightBlendedSteeringBehavior = 
            this.FindChild<WeightBlendedSteeringBehavior>();
        
        List<string> warnings = new();
        
        if (weightBlendedSteeringBehavior == null)
        {
            warnings.Add("This node needs at least one child of type " +
                         "WeightBlendedSteeringBehavior ITargeter to work. ");  
        }
        
        return warnings.ToArray();
    }
    

}


