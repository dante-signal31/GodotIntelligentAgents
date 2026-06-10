namespace GodotGameAIbyExample.Scripts.Sensors;

/// <summary>
/// A modality is a representation of a kind of signal that can be sent using a region
/// sense manager. This representation defines the limits of this kind of signals and
/// how it is attenuated by distance.
/// </summary>
public abstract class RegionSenseModality
{
    /// <summary>
    /// Maximum range of the modality.
    /// </summary>
    public float MaximumRange { get; }
    
    /// <summary>
    /// Attenuation factor by unit of distance for this modality.
    /// </summary>
    public float Attenuation { get; }
    
    /// <summary>
    /// How long it will take (in seconds) for the signal to travel one unit of distance.
    /// </summary>
    /// <remarks>
    /// Using inverse transmission speed is more useful than uninverted because this way
    /// we can represent almost infinite speeds just with an inverse transmission speed of
    /// zero.
    /// </remarks>
    public float InverseTransmissionSpeed { get; }
    
    protected RegionSenseModality(
        float maximumRange, 
        float attenuation, 
        float inverseTransmissionSpeed)
    {
        MaximumRange = maximumRange;
        Attenuation = attenuation;
        InverseTransmissionSpeed = inverseTransmissionSpeed;
    }

    /// <summary>
    /// Extra checks to be performed before the signal is sent to the agent.
    /// </summary>
    /// <remarks>
    /// If the modality does not override this method, it will always return true.
    /// </remarks>
    /// <param name="signal">Signal to be sent.</param>
    /// <param name="sensor">Sensor candidate to receive the signal.</param>
    /// <returns>True if the sensor can receive the signal. Otherwise, false.</returns>
    public abstract bool ExtraChecks(RegionSenseSignal signal, IRegionSenseSensor sensor);
}