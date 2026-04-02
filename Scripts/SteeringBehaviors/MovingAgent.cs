using System;
using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Tools;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

// Needs to be a Tool to be able to override _GetConfigurationWarnings() and find out
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
            if (_bodySprite == null) return;
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
    [Export] public SmoothingMethods SmoothingMethod { get; set; } = 
        SmoothingMethods.Average;

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
            UpdateSmoothingWeights();
        }
    }
    
    private Curve _smoothingCurve = new Curve();
    /// <summary>
    /// Curve to be used if Weighted Moving Average smoothing method is selected.
    /// </summary>
    [Export]
    public Curve SmoothingCurve
    {
        get => _smoothingCurve;
        set
        {
            _smoothingCurve = value;
            UpdateSmoothingWeights();
        }
    }

    /// <summary>
    /// Convergence rate for exponential smoothing.
    /// </summary>
    [Export] public float ExponentialConvergenceRate = 0.6f;
    
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

    public float Radius
    {
        get
        {
            if (_agentShape == null) return 0.0f;
            if (_agentShape.Shape is CircleShape2D circleShape)
            {
                return circleShape.Radius;
            }
            return 0.0f;
        }
    }
    
    protected SteeringBehaviorArgs BehaviorArgs;
    private float _maximumRotationSpeedRadNormalized;
    private float _stopRotationRadThreshold;
    private MovingWindow _lastRotations;
    private CollisionShape2D _agentShape;
    private float _lastRotation;
    private float[] _weights;

    protected virtual SteeringBehaviorArgs GetSteeringBehaviorArgs()
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
        if (_bodySprite != null) _bodySprite.Modulate = AgentColor;
        BehaviorArgs = GetSteeringBehaviorArgs();
        _agentShape = this.FindChild<CollisionShape2D>();
        UpdateSmoothingWeights();
    }

    private void UpdateSmoothingWeights()
    {
        _weights = ValueSmoother.SampleWeights(AutoSmoothSamples, SmoothingCurve);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (SteeringBehavior == null || Engine.IsEditorHint()) return;
        
        UpdateSteeringBehaviorArgs(delta);

        // Get steering output.
        SteeringOutput steeringOutput = SteeringBehavior.GetSteering(BehaviorArgs);
        
        // Agent faces to the velocity direction.
        if (steeringOutput.Angular == 0 && steeringOutput.Linear != Vector2.Zero)
        {
            if (AutoSmooth)
            {
                // If no explicit angular steering, but autoSmoothing is desired, we will
                // smooth the rotation.
                float rotationNeeded = Forward.AngleTo(steeringOutput.Linear);
                _lastRotations.Add(rotationNeeded);
                float smoothedRotation = SmoothingMethod switch
                {
                    SmoothingMethods.Average => 
                        ValueSmoother.Average(_lastRotations.Values),
                    SmoothingMethods.WeightedMovingAverage =>
                        ValueSmoother.WeightedMovingAverage(_lastRotations, _weights),
                    SmoothingMethods.Exponential =>
                        ValueSmoother.Exponential(
                            _lastRotation, 
                            rotationNeeded, 
                            ExponentialConvergenceRate),
                    _ => throw new ArgumentOutOfRangeException()
                };
                _lastRotation = smoothedRotation;
                Vector2 smoothedHeading = Forward.Rotated(smoothedRotation);
                steeringOutput = 
                    new SteeringOutput(
                        smoothedHeading.Normalized() * 
                        steeringOutput.Linear.Length());
                SetRotation(smoothedHeading, delta);
            }
            else
            {
                // If no explicit angular steering and no autoSmoothing desired, we will
                // just look at the direction we are moving, but clamping our rotation by
                // our rotational speed.
                SetRotation(Velocity, delta);
                
            }
        }
        // Agent moves in a direction while faces another direction (e.g., strafing)
        else if (steeringOutput.Angular != 0)
        {
            // In this case, our steering wants us to face and move in different
            // directions. Steering checks that no threshold is surpassed.
            GlobalRotationDegrees += steeringOutput.Angular * (float)delta;
        }
        // Apply new steering output to our agent. I don't enforce the StopSpeed because
        // I've found it more flexible to do it at steering behavior level.
        Velocity = steeringOutput.Linear;
        MoveAndSlide();
    }

    protected virtual void UpdateSteeringBehaviorArgs(double delta)
    {
        // Update steering behavior args.
        BehaviorArgs.MaximumSpeed = MaximumSpeed;
        BehaviorArgs.StopSpeed = StopSpeed;
        BehaviorArgs.CurrentVelocity = Velocity;
        BehaviorArgs.MaximumRotationalSpeed = MaximumRotationalDegSpeed;
        BehaviorArgs.StopRotationThreshold = StopRotationDegThreshold;
        BehaviorArgs.MaximumAcceleration = MaximumAcceleration;
        BehaviorArgs.MaximumDeceleration = MaximumDeceleration;
        BehaviorArgs.DeltaTime = delta;
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