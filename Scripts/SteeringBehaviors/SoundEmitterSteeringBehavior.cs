using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Sensors;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// A steering behavior that dynamically manages the emission of sound signals based on
/// velocity, using an underlying steering behavior to determine movement logic.
/// </summary>
/// <remarks>
/// The behavior implements logic to activate or deactivate a sound signal emitter based
/// on the linear velocity of the agent. When the velocity exceeds a configurable
/// threshold, sound emission is activated; otherwise, it is deactivated. This behavior
/// is useful for AI agents where sound emission should reflect their activity level or
/// speed.
/// </remarks>
[Tool]
public partial class SoundEmitterSteeringBehavior: Node2D, ISteeringBehavior
{
    [ExportCategory("CONFIGURATION:")]
    /// <summary>
    /// Speed under which no sound is emitted.
    /// </summary>
    [Export] public float SoundSpeedThreshold = 30f;
    
    private ISteeringBehavior _currentSteeringBehavior;
    private RegionSenseSoundSignalEmitter _soundEmitter;

    private void GetChildrenReferences()
    {
        _currentSteeringBehavior = this.FindChild<ISteeringBehavior>();
        _soundEmitter = this.FindChild<RegionSenseSoundSignalEmitter>();
    }

    public override void _Ready()
    {
        GetChildrenReferences();
    }

    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        SteeringOutput steering = _currentSteeringBehavior.GetSteering(args);

        // Make sure we only emit sound when we are moving at a speed above the threshold.
        if (steering.Linear.Length() < SoundSpeedThreshold &&
            _soundEmitter.IsEmissionActive)
        {
            _soundEmitter.StopEmission();
        }
        else if (steering.Linear.Length() >= SoundSpeedThreshold &&
                 !_soundEmitter.IsEmissionActive)
        {
            _soundEmitter.StartEmission();
        }

        return steering;
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new();
        
        GetChildrenReferences();
        
        if (_currentSteeringBehavior == null)
        {
            warnings.Add("This node needs a child that implements ISteeringBehavior " +
                         "to work.");
        }

        if (_soundEmitter == null)
        {
            warnings.Add("This node needs a child of type RegionSenseSoundSignalEmitter " +
                         "to work.");
        }
        
        return warnings.ToArray();
    }
}