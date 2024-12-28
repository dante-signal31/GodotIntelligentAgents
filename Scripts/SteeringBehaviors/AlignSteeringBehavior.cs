using Godot;
using System;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;

// It must be marked as Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
/// <summary>
/// <p> Align steering behaviour makes the agent look at the same direction than
/// a target GameObject. </p>
/// </summary>
public partial class AlignSteeringBehavior : Node, ISteeringBehavior, ITargeter
{
    [ExportCategory("CONFIGURATION:")]
    /// <summary>
    /// Target to align with.
    /// </summary>
    [Export] public Node2D Target { get; set; }
    /// <summary>
    /// Rotation to start to slow down (degress).
    /// </summary>
    [Export] private float _decelerationRadius;
    /// <summary>
    /// At this rotation from target angle will full stop (degress).
    /// </summary>
    [Export] public float ArrivingMargin { get; private set; }
    /// <summary>
    /// Deceleration curve.
    /// </summary>
    [Export] private Curve _decelerationCurve;
    /// <summary>
    /// At this rotation start angle will be at full speed (degress).
    /// </summary>
    [Export] private float _accelerationRadius;
    /// <summary>
    /// Acceleration curve.
    /// </summary>
    [Export] private Curve _accelerationCurve;

    private float _startOrientation;
    private float _rotationFromStartAbs;
    private float _arrivingMarginRad;
    private float _accelerationRadiusRad;
    private float _decelerationRadiusRad;
    private bool _idle = true;
    
    private float _targetOrientation;
    

    public override void _Ready()
    {
        _arrivingMarginRad = Mathf.DegToRad(ArrivingMargin);
        _accelerationRadiusRad = Mathf.DegToRad(_accelerationRadius);
        _decelerationRadiusRad = Mathf.DegToRad(_decelerationRadius);
    }

    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    { // I want smooth rotations, so I will use the same approach than in
      // ArriveSteeringBehavior.
        if (Target == null) return new SteeringOutput(Vector2.Zero, 0);
        
        _targetOrientation = Target.RotationDegrees;
        float currentOrientation = args.Orientation;
        float maximumRotationalSpeedRad = Mathf.DegToRad(args.MaximumRotationalSpeed);
        
        float toTargetRotationRad = Mathf.AngleDifference(
            Mathf.DegToRad(currentOrientation), 
            Mathf.DegToRad(_targetOrientation));
        int rotationSide = (toTargetRotationRad < 0) ? -1 : 1;
        float toTargetRotationRadAbs = Mathf.Abs(toTargetRotationRad);
        
        float newRotationalSpeedRad = 0.0f;
        
        if (_idle && toTargetRotationRadAbs < _arrivingMarginRad)
        { // If you are stopped and you are close enough to target rotation, you are done.
          // Just stay there.
            return new SteeringOutput(Vector2.Zero, 0);
        }

        if (_idle && _rotationFromStartAbs > 0)
        { // If you are stopped and you are not close enough to target rotation, you need
          // to start rotating. But first, you need to reset your rotation counter.
            _rotationFromStartAbs = 0;
        }
        
        if (toTargetRotationRadAbs >= _arrivingMarginRad && 
            _rotationFromStartAbs < _accelerationRadiusRad)
        { // Acceleration phase.
            if (_idle)
            {
                _startOrientation = currentOrientation;
                _idle = false;
            }
            _rotationFromStartAbs = MathF.Abs(Mathf.AngleDifference(
                Mathf.DegToRad(currentOrientation), 
                Mathf.DegToRad(_startOrientation)));
            // Acceleration curve should start at more than 0 or agent will not
            // start to move.
            float accelerationProgress = Mathf.InverseLerp(
                0, 
                _accelerationRadiusRad, 
                _rotationFromStartAbs);
            newRotationalSpeedRad = maximumRotationalSpeedRad * 
                                    _accelerationCurve.Sample(accelerationProgress) * 
                                 rotationSide;
        }
        else if (toTargetRotationRadAbs < _decelerationRadiusRad && 
                 toTargetRotationRadAbs >= _arrivingMarginRad)
        { // Deceleration phase.
            float decelerationProgress = Mathf.InverseLerp(
                _decelerationRadiusRad, 
                0, 
                toTargetRotationRadAbs);
            newRotationalSpeedRad = maximumRotationalSpeedRad * 
                                 _decelerationCurve.Sample(decelerationProgress) * 
                                 rotationSide;
        }
        else if (toTargetRotationRadAbs < _arrivingMarginRad)
        { // Stop phase.
            newRotationalSpeedRad = 0;
            _idle = true;
        }   
        else
        { // Full speed phase.
            newRotationalSpeedRad = maximumRotationalSpeedRad * rotationSide;
        }
        return new SteeringOutput(Vector2.Zero, Mathf.RadToDeg(newRotationalSpeedRad));
    }
}
