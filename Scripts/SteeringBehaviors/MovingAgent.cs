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
    private Color _agentColor = new Color(0, 1, 0);
    [Export] public Color AgentColor
    {
        get => _agentColor;
        set
        {
            if (_agentColor == value) return;
            _agentColor = value;
            _bodySprite.Modulate = _agentColor;
        }
    } 
    /// <summary>
    /// This agent maximum speed.
    /// </summary>
    [Export] public float MaximumSpeed { get; set; }
    /// <summary>
    /// When speed is less than this value, we consider the agent stopped.
    /// </summary>
    [Export] public float StopSpeed { get; set; }
    /// <summary>
    /// The agent maximum rotational speed in degrees.
    /// </summary>
    [Export] public float MaximumRotationalDegSpeed { get; set; }
    /// <summary>
    /// Rotation will stop when the difference in degrees between the current rotation and
    /// current forward vector is less than this value.
    /// </summary>
    [Export] public float StopRotationDegThreshold { get; set; }
    /// <summary>
    /// Maximum acceleration for this agent.
    /// </summary>
    [Export] public float MaximumAcceleration { get; set; }
    /// <summary>
    /// Maximum deceleration for this agent.
    /// </summary>
    [Export] public float MaximumDeceleration { get; set; }
    // [Export] private SteeringBehavior _steeringBehavior;
    
    [ExportGroup("WIRING:")]
    [Export] private Sprite2D _bodySprite;

    /// <summary>
    /// This agent current speed
    /// </summary>
    public float CurrentSpeed => Velocity.Length();
    
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
            MaximumSpeed, 
            StopSpeed,
            MaximumRotationalDegSpeed,
            StopRotationDegThreshold,
            MaximumAcceleration,
            MaximumDeceleration,
            0);
    }

    public override void _EnterTree()
    {
        _maximumRotationSpeedRadNormalized =
            Mathf.DegToRad(MaximumRotationalDegSpeed) / (2 * Mathf.Pi);
        _stopRotationRadThreshold = Mathf.DegToRad(StopRotationDegThreshold);
    }

    public override void _Ready()
    {
        _bodySprite.Modulate = AgentColor;
        _behaviorArgs = GetSteeringBehaviorArgs();
        _steeringBehavior = this.FindChild<ISteeringBehavior>();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_steeringBehavior == null || Engine.IsEditorHint()) return;
        
        // Update steering behavior args.
        _behaviorArgs.MaximumSpeed = MaximumSpeed;
        _behaviorArgs.StopSpeed = StopSpeed;
        _behaviorArgs.CurrentVelocity = Velocity;
        _behaviorArgs.MaximumRotationalSpeed = MaximumRotationalDegSpeed;
        _behaviorArgs.StopRotationThreshold = StopRotationDegThreshold;
        _behaviorArgs.MaximumAcceleration = MaximumAcceleration;
        _behaviorArgs.MaximumDeceleration = MaximumDeceleration;
        _behaviorArgs.DeltaTime = delta;
        
        // Get steering output.
        SteeringOutput steeringOutput = _steeringBehavior.GetSteering(_behaviorArgs);
        
        // Apply new steering output to our agent.
        // Velocity = steeringOutput.Linear.Length() > StopSpeed ? 
        //     steeringOutput.Linear:
        //     Vector2.Zero;
        Velocity = steeringOutput.Linear;
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
        else if (steeringOutput.Angular != 0)
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