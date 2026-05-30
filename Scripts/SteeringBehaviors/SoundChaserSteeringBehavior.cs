using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Sensors;
using GodotGameAIbyExample.Scripts.Tools;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// Represents a steering behavior responsible for directing an agent toward
/// the source of the strongest sound signal detected within its sensing range.
/// The behavior utilizes a sound sensor to identify signals and dynamically
/// updates a pathfinding target for navigation.
/// </summary>
[Tool]
public partial class SoundChaserSteeringBehavior: Node2D, ISteeringBehavior
{
    [ExportCategory("CONFIGURATION:")]
    /// <summary>
    /// Distance at which we give our goal as reached and we stop our agent.
    /// </summary>
    [Export] public float ArrivalDistance = 100f;
    
    private RegionSenseSoundSensor _soundSensor;
    private MeshPathFinderSteeringBehavior _meshPathFinderSteeringBehavior;
    private Target _target;
    private Vector2 _currentTargetPosition;
    
    private void GetChildrenReferences()
    {
        _soundSensor = this.FindChild<RegionSenseSoundSensor>();
        _meshPathFinderSteeringBehavior = this.FindChild<MeshPathFinderSteeringBehavior>();
    }

    public override void _Ready()
    {
        GetChildrenReferences();
        if (Engine.IsEditorHint()) return;
        Target target = new Target();
        target.Name = $"{Name} - Target";
        target.Visible = false;
        target.FollowMouse = false;
        target.GlobalPosition = Vector2.Inf;
        GetTree().Root.CallDeferred(Node.MethodName.AddChild, target);
        _target = target;
    }

    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        if (!_soundSensor.AnyObjectDetected) return SteeringOutput.Zero;
        
        // Chase the strongest signal.
        RegionSenseSignal strongestSignal = _soundSensor.DetectedSignals.Peek().Signal;

        if (_currentTargetPosition.DistanceTo(strongestSignal.Source.GlobalPosition) >
            ArrivalDistance)
        {
            _target.GlobalPosition = strongestSignal.Source.GlobalPosition;
            _meshPathFinderSteeringBehavior.PathTarget = _target;
            _currentTargetPosition = _target.GlobalPosition;
        }
        
        return _meshPathFinderSteeringBehavior.GetSteering(args);
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new();
        
        GetChildrenReferences();
        
        if (_soundSensor == null)
        {
            warnings.Add("This node needs a child of type RegionSenseSoundSensor " +
                         "to work.");
        }
        
        if (_meshPathFinderSteeringBehavior == null)
        {
            warnings.Add("This node needs a child of type " +
                         "MeshPathFinderSteeringBehavior " +
                         "to work.");
        }
        
        return warnings.ToArray();
    }
}