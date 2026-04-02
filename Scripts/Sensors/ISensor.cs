using System;
using System.Collections.Generic;
using Godot;

namespace GodotGameAIbyExample.Scripts.Sensors;

/// <summary>
/// Public API every sensor must implement whatever its specific cover area. 
/// </summary>
public interface ISensor
{
    /// <summary>
    /// Event when the sensor starts to detect an object.
    /// </summary>
    public event Action<Node2D> ObjectEnteredSensor;
    
    /// <summary>
    /// Event when the sensor stays inside an object.
    /// </summary>
    public event Action<Node2D> ObjectStayedInSensor;
    
    /// <summary>
    /// Event when an object ends to detect an object.
    /// </summary>
    public event Action<Node2D> ObjectLeftSensor;
    
    /// <summary>
    /// <p>List of objects currently inside this sensor range.</p>
    /// <p>Only are considered those objects included in the layermask provided
    /// to ConeSensor.</p> 
    /// </summary>
    public HashSet<Node2D> DetectedObjects { get; }

    /// <summary>
    /// Whether there is any object detected by the sensor.
    /// </summary>
    public bool AnyObjectDetected { get; }
}