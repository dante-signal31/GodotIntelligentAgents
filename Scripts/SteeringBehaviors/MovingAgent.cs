using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Tools;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

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
    [Export] public float MaximumSpeed { get; set; } = 100;

    /// <summary>
    /// When speed is less than this value, we consider the agent stopped.
    /// </summary>
    [Export] public float StopSpeed { get; set; } = 1;

    /// <summary>
    /// The agent maximum rotational speed in degrees.
    /// </summary>
    [Export] public float MaximumRotationalDegSpeed { get; set; } = 1080;

    /// <summary>
    /// Rotation will stop when the difference in degrees between the current rotation and
    /// the current forward vector is less than this value.
    /// </summary>
    [Export] public float StopRotationDegThreshold { get; set; } = 1; 
    
    /// <summary>
    /// Maximum acceleration for this agent.
    /// </summary>
    [Export] public float MaximumAcceleration { get; set; }
    
    /// <summary>
    /// Maximum deceleration for this agent.
    /// </summary>
    [Export] public float MaximumDeceleration { get; set; }
    
    /// <summary>
    /// Smooth heading averaging velocity vector.
    /// </summary>
    [Export] public bool AutoSmooth { get; set; }

    private int _autoSmoothSamples = 10;
    /// <summary>
    /// How many samples to use to smooth heading.
    /// </summary>
    [Export]
    public int AutoSmoothSamples
    {
        get => _autoSmoothSamples;
        set
        {
            _autoSmoothSamples = value;
            if (!AutoSmooth) return;
            _lastRotations = new MovingWindow(value);
        }
    }
    
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
    /// <summary>
    /// This agent current steering behavior.
    /// </summary>
    public ISteeringBehavior SteeringBehavior {
        get
        {
            // I cannot leave this as a usual assignment at _Ready(), because some
            // nodes call it from their _Ready() methods and I were suffering race 
            // conditions.
            if (_steeringBehavior == null)
            {
                _steeringBehavior = this.FindChild<ISteeringBehavior>();
            }
            return _steeringBehavior;
        }
    }
    
    private SteeringBehaviorArgs _behaviorArgs;
    private float _maximumRotationSpeedRadNormalized;
    private float _stopRotationRadThreshold;
    private MovingWindow _lastRotations;

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
        
        if (!AutoSmooth) return;
        _lastRotations = new MovingWindow(AutoSmoothSamples);
    }

    public override void _Ready()
    {
        _bodySprite.Modulate = AgentColor;
        _behaviorArgs = GetSteeringBehaviorArgs();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (SteeringBehavior == null || Engine.IsEditorHint()) return;
        
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
        SteeringOutput steeringOutput = SteeringBehavior.GetSteering(_behaviorArgs);
        
        // Apply new steering output to our agent. I don't enforce the StopSpeed because
        // I've found more flexible to do it at steering behavior level.
        Velocity = steeringOutput.Linear;
        
        if (steeringOutput.Angular == 0 && Velocity != Vector2.Zero)
        {
            if (AutoSmooth)
            {
                // If no explicit angular steering, but autoSmoothing is desired, we will
                // smooth the heading by averaging the last few rotations.
                float rotationNeeded = Forward.AngleTo(steeringOutput.Linear);
                _lastRotations.Add(rotationNeeded);
                float averageRotation = _lastRotations.Average;
                Vector2 averageHeading = Forward.Rotated(averageRotation);
                SetRotation(averageHeading, delta);
            }
            else
            {
                // If no explicit angular steering and no autoSmoothing desired, we will
                // just look at the direction we are moving, but clamping our rotation by
                // our rotational speed.
                SetRotation(Velocity, delta);
                
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

    /// <summary>
    /// Rotates the agent towards the specified heading vector, adhering to the agent's
    /// rotational speed and rotation threshold constraints.
    /// </summary>
    /// <param name="heading">
    /// A 2D vector representing the target direction the agent should face.
    /// </param>
    /// <param name="delta">
    /// The frame's elapsed time, used to calculate the agent's interpolated rotation.
    /// </param>
    private void SetRotation(Vector2 heading, double delta)
    {
        float totalRotationNeeded = Forward.AngleTo(heading);
        if (Mathf.Abs(totalRotationNeeded) > _stopRotationRadThreshold)
        {
            float newHeading = Mathf.LerpAngle(
                Forward.Angle(),
                heading.Angle(),
                _maximumRotationSpeedRadNormalized * (float) delta);
            float rotation = newHeading - Forward.Angle();
            Vector2 rotatedForward = Forward.Rotated(rotation);
            LookAt(rotatedForward + GlobalPosition);
        }
    }

    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new();
        
        if (SteeringBehavior == null)
        {
            warnings.Add("This node needs a child of type SteeringBehavior to work.");
        }

        return warnings.ToArray();
    }
}