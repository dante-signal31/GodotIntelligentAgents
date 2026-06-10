using System;

namespace GodotGameAIbyExample.Scripts.Sensors;

/// <summary>
/// Represents a signal detected by a sensor through a sense manager.
/// </summary>
public struct DetectedSignal
{
    public RegionSenseSignal Signal;
    public DateTimeOffset DetectionTimeStamp;
}