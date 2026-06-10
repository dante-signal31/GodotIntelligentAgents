using Godot;

namespace GodotGameAIbyExample.Scripts.Sensors;

/// <summary>
/// An example of a sensor that can detect smell signal like modality.
/// </summary>
[Tool]
public partial class RegionSenseSmellSensor: RegionSenseSensor<SmellModality>
{ }