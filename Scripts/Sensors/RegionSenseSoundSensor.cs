using System;
using System.Collections.Generic;
using System.Timers;
using Godot;
using Timer = System.Timers.Timer;

namespace GodotGameAIbyExample.Scripts.Sensors;

/// <summary>
/// Represents a sound-based region sense sensor that detects and processes signals
/// within a specified region. This class is specialized for detecting sound modality
/// signals and provides functionality to filter, buffer, and track detected objects.
/// </summary>
[Tool]
public partial class RegionSenseSoundSensor : Node2D, IRegionSenseSensor, ISensor
{
    public event Action<Node2D> ObjectEnteredSensor;
    public event Action<Node2D> ObjectStayedInSensor;
    public event Action<Node2D> ObjectLeftSensor;

    public struct DetectedSignal
    {
        public RegionSenseSignal Signal;
        public DateTimeOffset DetectionTimeStamp;
    }
    
    [ExportCategory("CONFIGURATION:")]
    /// <summary>
    /// How many received signals to keep in memory.
    /// </summary>
    [Export] public int DetectionBufferSize = 100;
    
    /// <summary>
    /// Minimum strength threshold for a signal to be considered detected.
    /// </summary>
    [Export] public float MinimumStrengthDetectionThreshold = 40f;
    
    /// <summary>
    /// How long to keep a signal in memory (in seconds).
    /// </summary>
    [Export] public float DetectionExpirationTime = 1.0f;
    
    /// <summary>
    /// How often (in seconds) to clean old signals from the detection buffer.
    /// </summary>
    [Export] public float CleaningPeriod = 0.3f;

    public HashSet<Node2D> DetectedObjects
    {
        get
        {
            HashSet<Node2D> detectedObjects = new();
            
            foreach (var detectedSignal in _detectionBuffer)
            {
                detectedObjects.Add(detectedSignal.Signal.Source);
            }

            return detectedObjects;
        }
    } 

    public bool AnyObjectDetected => DetectedObjects.Count > 0;
    
    /// <summary>
    /// Priority queue of detected signals, sorted by signal strength. The strongest
    /// signal, the first.
    /// </summary>
    public PriorityQueue<DetectedSignal, float> DetectedSignals {
        get
        {
            PriorityQueue<DetectedSignal, float> detectedSignals = new();
            
            foreach (var detectedSignal in _detectionBuffer)
            {
                detectedSignals.Enqueue(detectedSignal, detectedSignal.Signal.Strength);
            }
            
            return detectedSignals;
        }
    }
    
    private readonly Queue<DetectedSignal> _detectionBuffer = new();
    private Timer _cleaningTimer;
    private RegionSenseManager _regionSenseManager;

    public override void _Ready()
    {
        InitializeCleaningTimer();
        _regionSenseManager = 
            GetNode<RegionSenseManager>("/root/RegionSenseManager");
        _regionSenseManager.RegisterSensor(this);
    }

    private void InitializeCleaningTimer()
    {
        _cleaningTimer = new Timer(CleaningPeriod * 1000);
        _cleaningTimer.AutoReset = true;
        _cleaningTimer.Elapsed += OnCleaningTimerElapsed;
        _cleaningTimer.Start();
    }

    private void OnCleaningTimerElapsed(object sender, ElapsedEventArgs e)
    {
        CleanDetectionBuffer();
    }

    /// <summary>
    /// Cleans the detection buffer by removing expired signals and updating the
    /// detected objects set. If objects are no longer detected due to signal expiration,
    /// the appropriate event is triggered.
    /// </summary>
    private void CleanDetectionBuffer()
    {
        // Remove expired signals from the front of the queue
        while (_detectionBuffer.Count > 0 && 
               (DateTimeOffset.UtcNow - _detectionBuffer.Peek().DetectionTimeStamp).TotalSeconds > DetectionExpirationTime)
        {
            DetectedSignal removedSignal = _detectionBuffer.Dequeue();
            
            // Check if signal source is still in the set of detected objects, because a
            // newer signal from that source is still in the _detectionBuffer.
            if (DetectedObjects.Contains(removedSignal.Signal.Source)) continue;
            
            // Signal source is not in the set of detected objects, so it must have left.
            ObjectLeftSensor?.Invoke(removedSignal.Signal.Source);
        }
    }

    /// <summary>
    /// Adds a detected signal to the internal detection buffer, ensuring that the buffer
    /// size remains bounded.
    /// </summary>
    /// <param name="signal">The signal to be added to the detection buffer.</param>
    private void AddDetectedSignal(RegionSenseSignal signal)
    {
        // Keep the buffer size bounded.
        if (_detectionBuffer.Count >= DetectionBufferSize)
            _detectionBuffer.Dequeue();
        
        // Check if signal source is new.
        if (!DetectedObjects.Contains(signal.Source))
            ObjectEnteredSensor?.Invoke(signal.Source);
        
        // Add the signal to the buffer.
        _detectionBuffer.Enqueue(
            new DetectedSignal
            {
                Signal = signal, 
                DetectionTimeStamp = DateTimeOffset.Now
            });
    }

    public float ModalityThreshold(RegionSenseModality modality)
    {
        if (modality is RegionSenseSoundSignalEmitter.SoundModality)
        {
            return MinimumStrengthDetectionThreshold;
        }
        // I'm only interested in sound modality. Any other one is ignored. One way
        // to do this is through SensesModality(), another one is setting an infinite
        // strength threshold in those modalities we are not interested in.
        return float.PositiveInfinity;
    }

    public bool SensesModality(RegionSenseModality modality)
    {
        return modality is RegionSenseSoundSignalEmitter.SoundModality;
    }

    public void NotifySignal(RegionSenseSignal signal)
    {
        AddDetectedSignal(signal);
    }
    
    public override void _PhysicsProcess(double delta)
    {
        foreach (Node2D detectedObject in DetectedObjects)
        {
            ObjectStayedInSensor?.Invoke(detectedObject);
        }
    }
}