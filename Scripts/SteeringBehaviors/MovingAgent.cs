using System;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;

// Needs to be a Tool to be able to averride _GetConfigurationWarnings() and find out
// if it has a child of type ISteeringBehavior.
[Tool]
public partial class MovingAgent : CharacterBody2D
{
    [ExportCategory("CONFIGURATION:")]
    [Export] private Color _agentColor = new Color(0, 1, 0);
    [Export] private float _maximumSpeed;
    /// <summary>
    /// When speed is less than this value, we consider the agent stopped.
    /// </summary>
    [Export] private float _stopSpeed;
    /// <summary>
    /// The agent maximum rotational speed in degrees.
    /// </summary>
    [Export] private float _maximumRotationalDegSpeed;
    /// <summary>
    /// Rotation will stop when the difference in degrees between the current rotation and
    /// current forward vector is less than this value.
    /// </summary>
    [Export] private float _stopRotationDegThreshold;
    [Export] private float _maximumAcceleration;
    [Export] private float _maximumDeceleration;
    // [Export] private SteeringBehavior _steeringBehavior;
    
    [ExportGroup("WIRING:")]
    [Export] private Sprite2D _bodySprite;

    /// <summary>
    /// This agent current speed
    /// </summary>
    public float CurrentSpeed { get; private set; }
    
    /// <summary>
    /// This agent maximum speed.
    /// </summary>
    public float MaximumSpeed
    {
        get => _maximumSpeed;
        set => _maximumSpeed = value;
    }

    /// <summary>
    /// Speed at which we consider agent should stop.
    /// </summary>
    public float StopSpeed
    {
        get => _stopSpeed;
        set => _stopSpeed = value;
    }
    
    /// <summary>
    /// Maximum acceleration for this agent.
    /// </summary>
    public float MaximumAcceleration
    {
        get => _maximumAcceleration;
        set => _maximumAcceleration = value;
    }
    
    /// <summary>
    /// This agent rotation in degrees.
    /// </summary>
    public float Orientation => GlobalRotationDegrees;

    /// <summary>
    /// This agent forward vector.
    /// </summary>
    public Vector2 Forward => GlobalTransform.X;
    
    private ISteeringBehavior _steeringBehavior;
    private SteeringBehaviorArgs _behaviorArgs;
    private float _maximumRotationSpeedRadNormalized;
    private float _stopRotationRadThreshold;

    private SteeringBehaviorArgs GetSteeringBehaviorArgs()
    {
        return new SteeringBehaviorArgs(
            this, 
            Velocity, 
            _maximumSpeed, 
            _stopSpeed,
            _maximumRotationalDegSpeed,
            _stopRotationDegThreshold,
            _maximumAcceleration,
            _maximumDeceleration,
            0);
    }

    public override void _EnterTree()
    {
        _maximumRotationSpeedRadNormalized =
            Mathf.DegToRad(_maximumRotationalDegSpeed) / (2 * Mathf.Pi);
        _stopRotationRadThreshold = Mathf.DegToRad(_stopRotationDegThreshold);
    }

    public override void _Ready()
    {
        _bodySprite.Modulate = _agentColor;
        _behaviorArgs = GetSteeringBehaviorArgs();
        _steeringBehavior = this.FindChild<ISteeringBehavior>();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_steeringBehavior == null || Engine.IsEditorHint()) return;
        
        // Update steering behavior args.
        _behaviorArgs.MaximumSpeed = MaximumSpeed;
        _behaviorArgs.CurrentVelocity = Velocity;
        _behaviorArgs.MaximumAcceleration = MaximumAcceleration;
        _behaviorArgs.DeltaTime = delta;
        
        // Get steering output.
        SteeringOutput steeringOutput = _steeringBehavior.GetSteering(_behaviorArgs);
        
        // Apply new steering output to our GameObject.
        Velocity = steeringOutput.Linear;
        CurrentSpeed = steeringOutput.Linear.Length();
        if (steeringOutput.Angular == 0 && Velocity != Vector2.Zero)
        {
            // If no explicit angular steering, we will just look at the direction we
            // are moving, but clamping our rotation by our rotational speed.
            float totalRotationNeeded = Forward.AngleTo(Velocity);
            if (Mathf.Abs(totalRotationNeeded) > _stopRotationRadThreshold)
            {
                float newHeading = Mathf.LerpAngle(
                    Forward.Angle(), 
                    Velocity.Angle(), 
                    _maximumRotationSpeedRadNormalized * (float)delta);
                float rotation = newHeading - Forward.Angle();
                Vector2 rotatedForward = Forward.Rotated(rotation);
                LookAt(rotatedForward + GlobalPosition);
            }
        }
        else
        {
            // In this case, our steering wants us to face and move in different
            // directions. Steering checks that no threshold is surpassed.
            GlobalRotationDegrees += steeringOutput.Angular * (float)delta;
        }
        MoveAndSlide();
    }

    public override string[] _GetConfigurationWarnings()
    {
        _steeringBehavior = this.FindChild<ISteeringBehavior>();
        
        if (_steeringBehavior == null)
        {
            return new[] {"This node needs a child of type SteeringBehavior to work."};
        }

        return Array.Empty<string>();
    }
}