namespace GodotGameAIbyExample.Scripts.Sensors;

/// <summary>
/// Class that describes the sound modality that a RegionSenseSoundSignalEmitter emits.
/// </summary>
public class SoundModality: RegionSenseModality
{
    public SoundModality(
        float maximumRange, float attenuation, float inverseTransmissionSpeed): 
        base(maximumRange, attenuation, inverseTransmissionSpeed)
    { }

    public override bool ExtraChecks(RegionSenseSignal signal, IRegionSenseSensor sensor)
    {
        // We don't need to do any extra checks for sound modality.
        return true;
    }
}