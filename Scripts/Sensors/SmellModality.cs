namespace GodotGameAIbyExample.Scripts.Sensors;

/// <summary>
/// Represents the smell modality, a specific type of region sense modality.
/// This modality models signals related to smells, defining their behavior when
/// transmitted in the region sense system.
/// </summary>
public class SmellModality: RegionSenseModality
{
    public SmellModality(
        float maximumRange, float attenuation, float inverseTransmissionSpeed): 
        base(maximumRange, attenuation, inverseTransmissionSpeed)
    { }

    public override bool ExtraChecks(RegionSenseSignal signal, IRegionSenseSensor sensor)
    {
        // We don't need to do any extra checks for smell modality.
        return true;
    }
}