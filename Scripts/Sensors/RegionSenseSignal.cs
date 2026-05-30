using Godot;

namespace GodotGameAIbyExample.Scripts.Sensors;

/// <summary>
/// A signal is a message that indicated that something has occurred in the game level.
/// E.g., a sound has been emitted.
/// </summary>
public struct RegionSenseSignal
{
    /// <summary>
    /// Signal strength.
    /// </summary>
    public float Strength;
    
    /// <summary>
    /// Position of the signal source.
    /// </summary>
    public Node2D Source;
    
    /// <summary>
    /// Unique identifier of the modality this signal is based on.
    /// </summary>
    public RegionSenseModality Modality;
}