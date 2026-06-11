using Godot;
using Timer = Godot.Timer;

namespace GodotGameAIbyExample.Scripts.Sensors;

/// <summary>
/// An example of a class that can emit a sound signal like modality.
/// </summary>
[Tool]
public abstract partial class RegionSenseSignalEmitter<T>:
    Node2D where T: RegionSenseModality
{
    [ExportCategory("CONFIGURATION:")] 
    /// <summary>
    /// Scene path to the RegionSenseManager node that will manage this node's signals.
    /// </summary>
    [Export] public string SenseManagerName = "";
    
    private float _modalityMaximumRange = 500f;
    /// <summary>
    /// Maximum range of the modality.
    /// </summary>
    [Export] public float ModalityMaximumRange
    {
        get => _modalityMaximumRange;
        set
        {
            _modalityMaximumRange = value;
            _currentModality = GenerateModality();
        }
    }

    private float _modalityAttenuation = 0.9f;
    /// <summary>
    /// Attenuation factor by unit of distance for this modality.
    /// </summary>
    [Export] public float ModalityAttenuation
    {
        get => _modalityAttenuation;
        set
        {
            _modalityAttenuation = value;
            _currentModality = GenerateModality();
        }
    }

    private float _modalityInverseTransmissionSpeed = 0.2f;
    /// <summary>
    /// How long it will take (in seconds) for the signal to travel one unit of distance.
    /// </summary>
    /// <remarks>
    /// Using inverse transmission speed is more useful than uninverted because this way
    /// we can represent almost infinite speeds just with an inverse transmission speed of
    /// zero.
    /// </remarks>
    [Export] public float ModalityInverseTransmissionSpeed
    {
        get => _modalityInverseTransmissionSpeed;
        set
        {
            _modalityInverseTransmissionSpeed = value;
            _currentModality = GenerateModality();
        }
    }

    private float _emissionPeriod = 0.5f;

    /// <summary>
    /// Time in seconds between emissions of the signal.
    /// </summary>
    [Export] public float EmissionPeriod
    {
        get => _emissionPeriod;
        set
        {
            _emissionPeriod = value;
            InitializeEmissionTimer();
        }
    }
    
    /// <summary>
    /// Whether to automatically start the emission of the signal.
    /// </summary>
    /// <remarks>
    /// If you set this value to false, you will have to call StartEmission() manually.
    /// </remarks>
    [Export] public bool AutoStartEmission = true;
    
    /// <summary>
    /// Signal strength.
    /// </summary>
    [Export] public float SignalStrength = 100.0f;
    
    [ExportCategory("DEBUG:")] 
    [Export] public bool ShowGizmos = true;
    [Export] public Color MaximumRangeColor = Colors.Red;
    
    /// <summary>
    /// Indicates whether the signal is currently being emitted.
    /// </summary>
    public bool IsEmissionActive => !_emissionTimer.IsStopped();

    protected T _currentModality;
    private Timer _emissionTimer;
    private float _maximumAttenuatedRange;
    private RegionSenseManager _regionSenseManager;

    /// <summary>
    /// Generates a new modality based on the current configuration.
    /// </summary>
    protected abstract T GenerateModality();
    
    /// <summary>
    /// Starts the emission of the signal.
    /// </summary>
    public void StartEmission()
    {
        _emissionTimer.Start();
    }
    
    /// <summary>
    /// Stops the emission of the signal.
    /// </summary>
    public void StopEmission()
    {
        _emissionTimer.Stop();
    }

    public override void _Ready()
    {
        _currentModality = GenerateModality();
        InitializeEmissionTimer();
        _regionSenseManager = (RegionSenseManager) GetTree()
            .CurrentScene
            .FindChild(SenseManagerName, recursive: true, owned: false);
    }

    private void InitializeEmissionTimer()
    {
        // I initially used a C# timer, but when registering a signal, the
        // RegionSenseManager complained about that one of the operations (a call to the 
        // GlobalPosition of a node) had been called out of godot's main thread. The
        // solution was to use a Godot timer instead. This way, RegionSenseManager's
        // RegisterSignal() is called from Godot's main thread.
        if (_emissionTimer == null) _emissionTimer = new Timer();
        _emissionTimer.WaitTime = EmissionPeriod;
        _emissionTimer.OneShot = false;
        _emissionTimer.Autostart = AutoStartEmission;
        
        if (_emissionTimer.IsConnected(
                Timer.SignalName.Timeout,
                new Callable(this, MethodName.OnEmissionElapsed))) return;
        _emissionTimer.Connect(
            Timer.SignalName.Timeout, 
            new Callable(this, MethodName.OnEmissionElapsed));
        
        // CallDeferred(Node.MethodName.AddChild, _emissionTimer);
        AddChild(_emissionTimer);
    }

    private void OnEmissionElapsed()
    {
        RegionSenseSignal signal = new()
        {
            Modality = _currentModality,
            EmissionPosition = GlobalPosition,
            Source = this,
            Strength = SignalStrength
        };
        _regionSenseManager.RegisterSignal(signal);
    }
    
    public override void _Process(double delta)
    {
        if (ShowGizmos) DrawGizmos();
    }

    private void DrawGizmos()
    {
        QueueRedraw();
    }
    
    public override void _Draw()
    {
        if (!ShowGizmos) return;
        
        // Draw maximum range circle.
        DrawCircle(Vector2.Zero, ModalityMaximumRange, MaximumRangeColor, filled: false);
    }
}