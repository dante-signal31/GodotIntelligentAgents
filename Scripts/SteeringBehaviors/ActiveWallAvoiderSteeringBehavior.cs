using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Sensors;
using Timer = System.Timers.Timer;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

// It must be marked as Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
/// <summary>
/// Steering behavior to go to a destination avoiding walls and obstacles.
/// </summary>
public partial class ActiveWallAvoiderSteeringBehavior : 
    Node2D, 
    ISteeringBehavior, 
    ITargeter,
    IGizmos
{
    [ExportCategory("CONFIGURATION:")]
    private Node2D _target;
    /// <summary>
    /// Target to go avoiding other agents.
    /// </summary>
    [Export] public Node2D Target
    {
        get => _target;
        set
        {
            if (_target == value) return;
            _target = value;
            if (_targeter == null) return;
            _targeter.Target = value;
        }
    }
    
    /// <summary>
    /// Timeout started, in seconds, after no further collision detected, before
    /// resuming travel to target.
    /// </summary>
    [Export] public float AvoidanceTimeout { get; set; } = 0.5f;
    
    [ExportCategory("DEBUG:")]
    [Export] public bool ShowGizmos { get; set; }
    [Export] public Color GizmosColor { get; set; }
    
    private ITargeter _targeter;
    private PassiveWallAvoiderSteeringBehavior _passiveWallAvoiderSteeringBehavior;
    private ISteeringBehavior _steeringBehavior;
    private Vector2 _avoidVector;
    private Vector2 _previousAvoidVector;
    private Vector2 _toTargetVector;
    private System.Timers.Timer _avoidanceTimer;
    private bool _waitingForAvoidanceTimeout;
    private SteeringOutput _currentSteering;


    public override void _EnterTree()
    {
        SetTimer();
    }

    private void SetTimer()
    {
        _avoidanceTimer = new Timer(AvoidanceTimeout * 1000);
        _avoidanceTimer.AutoReset = false;
        _avoidanceTimer.Elapsed += OnTimerTimeout;
    }
    
    public override void _Ready()
    {
        _targeter = this.FindChild<ITargeter>();
        _steeringBehavior = (ISteeringBehavior)_targeter;
        _targeter.Target = Target;
        _passiveWallAvoiderSteeringBehavior = 
            this.FindChild<PassiveWallAvoiderSteeringBehavior>();
    }
    
    private void OnTimerTimeout(object sender, System.Timers.ElapsedEventArgs e)
    {
        _waitingForAvoidanceTimeout = false;
    }

    private void StartAvoidanceTimer()
    {
        _waitingForAvoidanceTimeout = true;
        _avoidanceTimer.Stop();
        _avoidanceTimer.Start();
    }
    
    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        if (_waitingForAvoidanceTimeout) return _currentSteering;

        SteeringOutput avoidingSteering =
            _passiveWallAvoiderSteeringBehavior.GetSteering(args);

        _avoidVector = avoidingSteering.Linear;
        if (_avoidVector == Vector2.Zero && _previousAvoidVector != Vector2.Zero)
            StartAvoidanceTimer();
        _previousAvoidVector = _avoidVector;
        
        SteeringOutput steeringToTargetVelocity = _steeringBehavior.GetSteering(args);
        _toTargetVector = steeringToTargetVelocity.Linear;
        
        _currentSteering = avoidingSteering + steeringToTargetVelocity;
        return _currentSteering;
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
        
        DrawLine(Vector2.Zero, ToLocal(GlobalPosition + _avoidVector), GizmosColor);
        DrawLine(Vector2.Zero, ToLocal(GlobalPosition + _toTargetVector), Colors.Beige);
        DrawLine(Vector2.Zero, ToLocal(GlobalPosition + _currentSteering.Linear), Colors.Blue);
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        
        ITargeter targeterBehavior = this.FindChild<ITargeter>();
        PassiveWallAvoiderSteeringBehavior passiveWallAvoiderSteeringBehavior = 
            this.FindChild<PassiveWallAvoiderSteeringBehavior>();

        List<string> warnings = new();
        
        if (targeterBehavior == null || !(targeterBehavior is ISteeringBehavior))
        {
            warnings.Add("This node needs a child of type both ISteeringBehavior and " +
                         "ITargeter to work. ");  
        }

        if (passiveWallAvoiderSteeringBehavior == null)
        {
            warnings.Add("This node needs a child of type " +
                         "PassiveWallAvoiderSteeringBehavior to work. ");
        }
        
        return warnings.ToArray();
    }


}