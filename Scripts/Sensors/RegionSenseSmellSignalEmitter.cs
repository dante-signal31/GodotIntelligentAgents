using Godot;

namespace GodotGameAIbyExample.Scripts.Sensors;

/// <summary>
/// Represents a signal emitter that operates within a defined region and uses the
/// smell sense modality.
/// </summary>
[Tool]
public partial class RegionSenseSmellSignalEmitter: 
    RegionSenseSignalEmitter<SmellModality>
{
    protected override SmellModality GenerateModality()
    {
        return new SmellModality(
            ModalityMaximumRange, 
            ModalityAttenuation, 
            ModalityInverseTransmissionSpeed);
    }
}