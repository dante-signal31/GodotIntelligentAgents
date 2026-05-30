using Godot;

namespace GodotGameAIbyExample.Scripts.Sensors;

/// <summary>
/// <p> Interface for region sense sensors. </p>
/// <p> A sensor is a component capable of receiving a signal. </p>
/// </summary>
public interface IRegionSenseSensor
{
    /// <summary>
    /// Global position of the sensor.
    /// </summary>
    public Vector2 GlobalPosition { get; }
    
    /// <summary>
    /// Receiving threshold for the given signal modality.
    /// </summary>
    /// <param name="modality">Signal modality.</param>
    /// <returns>Power threshold of this sensor for that modality. </returns>
    public float ModalityThreshold(RegionSenseModality modality);
    
    /// <summary>
    /// Whether this sensor is interested in the given signal modality.
    /// </summary>
    /// <param name="modality">Signal modality.</param>
    /// <returns>True is this sensor wants to receive signals of this modality. Otherwise,
    /// false.</returns>
    public bool SensesModality(RegionSenseModality modality);
    
    /// <summary>
    /// Notify this sensor that it has received a signal.
    /// </summary>
    /// <param name="signal">The signal this sensor has received.</param>
    public void NotifySignal(RegionSenseSignal signal);
}