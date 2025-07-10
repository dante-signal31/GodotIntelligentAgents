using System;
using System.Collections.Generic;
using System.Numerics;
using System.Timers;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using Vector2 = Godot.Vector2;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

// It must be marked as Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
/// <summary>
/// Steering behavior to avoid walls and obstacles using usher algorithm to smooth
/// movements.
/// </summary>
public partial class SmoothedWallAvoiderSteeringBehavior : Node2D, ISteeringBehavior
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
            if (_chaseToUsherTargeter == null) return;
            _chaseToUsherTargeter.Target = value;
        }
    }

    /// <summary>
    /// Usher scene to spawn.
    /// </summary>
    [Export] private PackedScene _usherScene;

    /// <summary>
    /// How far should usher start ahead of current agent.
    /// </summary>
    [Export] private float _usherAdvantage;

    /// <summary>
    /// Distance to usher to consider it reached.
    /// </summary>
    [Export] private float _reachingDistanceToUsher = 2.0f;

    /// <summary>
    /// Time to wait after reaching usher to start chasing it again.
    /// </summary>
    [Export] private float _secondsToWaitAfterReachingUsher;

    [ExportCategory("DEBUG:")]
    
    /// <summary>
    /// Show gizmos.
    /// </summary>
    [Export] public bool ShowGizmos { get; private set; }
    
    /// <summary>
    /// Colors for this object's gizmos.
    /// </summary>
    [Export] public Color GizmoColor { get; private set; } = Colors.White;
    
    private ITargeter _chaseToUsherTargeter;
    private ISteeringBehavior _chaseToUsherSteeringBehavior;
    private MovingAgent _currentAgent;
    private MovingAgent _usherAgent;
    private ITargeter _usherTargeter;
    private System.Timers.Timer _advantageTimer;
    private bool _givingAdvantageToUsher;
    private bool _usherReached;
    private SteeringOutput _currentSteering;
    
    
    public override void _EnterTree()
    {
        // Find out who our father is.
        _currentAgent = this.FindAncestor<MovingAgent>();
        SetTimer();
    }

    private void SetTimer()
    {
        _advantageTimer = new System.Timers.Timer(
            _secondsToWaitAfterReachingUsher * 1000);
        _advantageTimer.AutoReset = false;
        _advantageTimer.Elapsed += OnTimerTimeout;
    }

    private void OnTimerTimeout(object sender, ElapsedEventArgs e)
    {
        _givingAdvantageToUsher = false;
        //GD.Print($"Advantage timer ended at {DateTime.Now:HH:mm:ss}.");
    }
    
    private void StartTimer()
    {
        _givingAdvantageToUsher = true;
        _advantageTimer.Start();
        //GD.Print($"Advantage timer started at {DateTime.Now:HH:mm:ss}.");
    }
    
    public override void _Ready()
    {
        if (Engine.IsEditorHint() || 
            _usherScene == null || 
            _currentAgent.ProcessMode == ProcessModeEnum.Disabled) return;
        
        // Create usher to follow.
        _usherAgent = (MovingAgent) _usherScene.Instantiate();
        GetTree().Root.CallDeferred(Window.MethodName.AddChild, _usherAgent);
        
        // Place usher ahead of our agent.
        _usherAgent.Position = _currentAgent.Position + 
                               _usherAdvantage * _currentAgent.Forward;
        
        // Configure usher.
        _usherAgent.MaximumSpeed = _currentAgent.MaximumSpeed;
        _usherAgent.StopSpeed = _currentAgent.StopSpeed;
        _usherAgent.MaximumAcceleration = _currentAgent.MaximumAcceleration;
        _usherAgent.MaximumDeceleration = _currentAgent.MaximumDeceleration;
        _usherAgent.MaximumRotationalDegSpeed = _currentAgent.MaximumRotationalDegSpeed;
        
        // Give usher a place to go to.
        _usherTargeter = (ITargeter)_usherAgent.SteeringBehavior;
        _usherTargeter.Target = _target;
        
        // Prepare to follow the usher.
        _chaseToUsherTargeter = this.FindChild<ITargeter>();
        _chaseToUsherTargeter.Target = _usherAgent;
        _chaseToUsherSteeringBehavior = (ISteeringBehavior) _chaseToUsherTargeter;
        
        _currentSteering = new SteeringOutput();
    }
    
    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        if (_chaseToUsherSteeringBehavior == null) return new SteeringOutput();
        
        float distanceToUsher =
            _currentAgent.GlobalPosition.DistanceTo(_usherAgent.GlobalPosition);
        
        // To smooth our own movements, usher should have some advantage. So, if the usher
        // stops, and we reach it, then we wait to let it advance again and get advantage.
        if (distanceToUsher < _reachingDistanceToUsher)
        {
            // If we are too near to usher, stay still.
            _usherReached = true;
            _currentSteering = new SteeringOutput(
                linear: Vector2.Zero,
                angular: _currentSteering.Angular);
        } 
        else if (_usherReached && distanceToUsher > _reachingDistanceToUsher)
        {
            // Usher is going away from us. Wait for a time to give him some advantage.
            _usherReached = false;
            _currentSteering = new SteeringOutput(
                linear: Vector2.Zero,
                angular: _currentSteering.Angular);
            StartTimer();
        }
        else if (_givingAdvantageToUsher)
        {
            // If we are waiting to give advantage to usher, stay still.
            _currentSteering = new SteeringOutput(
                linear: Vector2.Zero,
                angular: _currentSteering.Angular);
        }
        else
        {
            // If we are not giving advantage to usher, follow usher.
            _currentSteering = _chaseToUsherSteeringBehavior.GetSteering(args);
        }
        
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

        if (Engine.IsEditorHint())
        {
            if (_currentAgent == null) return;
            
            DrawLine(
                Vector2.Zero, 
                _currentAgent.Forward * _usherAdvantage, 
                GizmoColor);
            DrawCircle(
                _currentAgent.Forward * _usherAdvantage, 
                10.0f, 
                GizmoColor);
        }
        else
        {
            if (_usherAgent == null) return;
            
            DrawLine(
                Vector2.Zero, 
                ToLocal(_usherAgent.GlobalPosition), 
                GizmoColor);
            DrawCircle(
                ToLocal(_usherAgent.GlobalPosition), 
                10.0f, 
                GizmoColor);
        }
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        ITargeter chaseToUsherBehavior = this.FindChild<ITargeter>();
        
        List<string> warnings = new();
        
        if (chaseToUsherBehavior == null || !(chaseToUsherBehavior is ISteeringBehavior))
        {
            warnings.Add("This node needs a child of type both ISteeringBehavior and " +
                         "ITargeter to work. ");  
        }
        
        return warnings.ToArray();
    }
}