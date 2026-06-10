using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Godot;
using Timer = System.Timers.Timer;

namespace GodotGameAIbyExample.Scripts.Sensors;

/// <summary>
/// Manages the registration of sensors and the distribution of signals
/// to registered sensors within a defined region.
/// </summary>
public partial class RegionSenseManager: Node2D
{
    [ExportCategory("REGION SENSE MANAGER CONFIGURATION:")]
    private float _sendingPeriod = 0.1f;
    /// <summary>
    /// Time in seconds between signal emissions.
    /// </summary>
    [Export] public float SendingPeriod
    {
        get => _sendingPeriod;
        set
        {
            _sendingPeriod = value;
            if (_senderTimer == null) return;
            _senderTimer.Interval = value * 1000;
        }
    }
    
    /// <summary>
    /// Number of pixels for the unit of distance.
    /// </summary>
    [Export] public float DistanceUnit = 100f;
    
    /// <summary>
    /// Structure used to store information about a signal to be delivered to a
    /// specific sensor at a specific time.
    /// </summary>
    private struct SignalNotification
    {
        public double Time;
        public IRegionSenseSensor Sensor;
        public RegionSenseSignal Signal;
    }

    protected readonly HashSet<IRegionSenseSensor> _registeredSensors = new();
    private readonly PriorityQueue<SignalNotification, double> _signalQueue = new();
    private Timer _senderTimer;

    public override void _Ready()
    {
        InitializeSenderTimer();
    }

    private void InitializeSenderTimer()
    {
        _senderTimer = new Timer(SendingPeriod * 1000);
        _senderTimer.AutoReset = true;
        _senderTimer.Elapsed += OnTimerElapsed;
        _senderTimer.Start();
    }

    private void OnTimerElapsed(object sender, ElapsedEventArgs e)
    {
        SendSignals();
    }

    /// <summary>
    /// Register a sensor to receive signals from this RegionSenseManager.
    /// </summary>
    /// <param name="sensor">Sensor interested in receiving signals.</param>
    public virtual void RegisterSensor(IRegionSenseSensor sensor)
    {
        _registeredSensors.Add(sensor);
    }
    
    /// <summary>
    /// Unregister a sensor from receiving signals from this RegionSenseManager.
    /// </summary>
    /// <param name="sensor">Sensor no longer interested in receiving signals.</param>
    public virtual void UnregisterSensor(IRegionSenseSensor sensor)
    {
        _registeredSensors.Remove(sensor);
    }

    /// <summary>
    /// Called by signal sources to send a signal to the sensors.
    /// </summary>
    /// <param name="signal">Signal to be sent.</param>
    public virtual void RegisterSignal(RegionSenseSignal signal)
    {
        NotifySensors(signal, _registeredSensors.ToArray());
    }

    protected virtual void NotifySensors(
        RegionSenseSignal signal, 
        IRegionSenseSensor[] sensorArray)
    {
        foreach (IRegionSenseSensor sensor in sensorArray)
        {
            // Is this sensor interested in this signal modality?
            if (!sensor.SensesModality(signal.Modality)) continue;
            
            // Is this sensor near enough?
            float distance = signal.Source.GlobalPosition.DistanceTo(sensor.GlobalPosition);
            if (distance > signal.Modality.MaximumRange) continue;
            
            // Is the signal powerful enough to be perceived by the sensor?
            if (!SignalPowerfulEnoughForSensor(signal, sensor)) continue;

            // Now, let's perform the specific checks for this modality.
            if (!signal.Modality.ExtraChecks(signal, sensor)) continue;
            
            // OK, if we got here, then the signal should be delivered to this sensor,
            // but when? We must calculate the time it takes the signal to reach
            // the sensor. 
            float timeToSensor = distance/DistanceUnit * signal.Modality.InverseTransmissionSpeed;
            double deliveryTime = 
                (DateTimeOffset.UtcNow - DateTimeOffset.UnixEpoch).TotalSeconds + 
                timeToSensor;
            
            // Now create a signal notification and add it to the queue to be sent.
            SignalNotification notification = new()
            {
                Time = deliveryTime,
                Sensor = sensor,
                Signal = signal,
            };
            _signalQueue.Enqueue(notification, deliveryTime);
        }
    }

    /// <summary>
    /// Determines whether a signal is powerful enough to be perceived by a sensor,
    /// considering the distance, strength, and modality-specific attenuation.
    /// </summary>
    /// <param name="signal">The signal being evaluated for perception by the sensor.</param>
    /// <param name="sensor">The sensor evaluating the signal.</param>
    /// <returns>True if the signal's power at the sensor's location is above the
    /// sensor's modality threshold; otherwise, false.</returns>
    protected virtual bool SignalPowerfulEnoughForSensor(
        RegionSenseSignal signal,
        IRegionSenseSensor sensor)
    {
        float distance = signal.Source.GlobalPosition.DistanceTo(sensor.GlobalPosition);
        
        float receivedPower = signal.Strength *
                              MathF.Pow(signal.Modality.Attenuation, 
                                  distance/DistanceUnit);

        return receivedPower >= sensor.ModalityThreshold(signal.Modality);
    }

    /// <summary>
    /// Send every signal in the signal queue that is due to be sent.
    /// </summary>
    private void SendSignals()
    {
        double currentTime = 
            (DateTimeOffset.UtcNow - DateTimeOffset.UnixEpoch).TotalSeconds;
        
        while (_signalQueue.Count > 0 && _signalQueue.Peek().Time <= currentTime)
        {
            SignalNotification notification = _signalQueue.Dequeue();
            notification.Sensor.NotifySignal(notification.Signal);
        }
    }
}