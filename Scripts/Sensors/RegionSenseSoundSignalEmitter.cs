using Godot;

namespace GodotGameAIbyExample.Scripts.Sensors;

/// <summary>
/// An example of a class that can emit a sound signal like modality.
/// </summary>
[Tool]
public partial class RegionSenseSoundSignalEmitter: RegionSenseSignalEmitter<SoundModality>
{
    protected override SoundModality GenerateModality()
    {
        return new SoundModality(
            ModalityMaximumRange, 
            ModalityAttenuation, 
            ModalityInverseTransmissionSpeed);
    }
}