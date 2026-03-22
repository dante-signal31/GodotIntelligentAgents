using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Sensors;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

[Tool]
public partial class ContextSteeringBehavior: Node2D, ISteeringBehavior
{
    [ExportCategory("CONFIGURATION:")]
    
    private uint _contextResolution = 10;
    /// <summary>
    /// <p>Number of whiskers to use for context.</p>
    /// <p>Whiskers will be set complying its internal rules. So final amount of whiskers
    /// may not be exactly equal to this value.</p> 
    /// </summary>
    [Export] private uint ContextResolution
    {
        get => _contextResolution;
        set
        {
            _contextResolution = value;
            if (_behavior == null) GetChildrenReferences();
            ConfigureWhiskers();
        }
    }
    
    private float _contextRadius = 100f;
    /// <summary>
    /// Radius of the context circle.
    /// </summary>
    [Export] private float ContextRadius
    {
        get => _contextRadius;
        set
        {
            _contextRadius = value;
            if (_behavior == null) GetChildrenReferences();
            ConfigureWhiskers();
        }
    }
    
    [ExportCategory("DEBUG:")]
    [Export] private bool ShowGizmos { get; set; }
    [Export] private Color GizmoColor { get; set; }
    [Export] private Color GizmoColorInterest { get; set; }
    
    private ITargeterSteeringBehavior _behavior;
    private WhiskersSensor _dangerSensor;
    private Tools.InterestWhiskers _interestWhisker;
    private List<Tools.InterestWhiskers.Interest> _interests = new();
    private Vector2 _currentSteeringVector;
    
    public override void _Ready()
    {
        GetChildrenReferences();
        ConfigureWhiskers();
    }

    private void ConfigureWhiskers()
    {
        ConfigureDangerSensor();
        ConfigureInterestWhisker();
    }

    private void ConfigureInterestWhisker()
    {
        _interestWhisker?.ReloadWhiskers(_dangerSensor.RayEnds);
    }

    private void GetChildrenReferences()
    {
        _behavior = this.FindChild<ITargeterSteeringBehavior>();
        _dangerSensor = this.FindChild<WhiskersSensor>();
        _interestWhisker = this.FindChild<Tools.InterestWhiskers>();
    }

    private void ConfigureDangerSensor()
    {
        if (_dangerSensor == null) return;
        _dangerSensor.SensorResolution = 
            (uint) Mathf.Max(0, Mathf.Ceil(((int)ContextResolution - 3.0f) / 2));
        _dangerSensor.Range = ContextRadius;
    }

    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        SteeringOutput steering = _behavior.GetSteering(args);
        _currentSteeringVector = steering.Linear;
        // If there is no danger, we don't need to do anything. Just go straight to
        // the target.
        if (!_dangerSensor.IsAnyObjectDetected) return steering;
        
        // Get fresh interests.
        _interestWhisker.CalculateInterests(steering.Linear);
        _interests = _interestWhisker.GetInterests();
        
        // Add every interest vector where an obstacle is NOT detected.
        List<bool> detectionMask = _dangerSensor.DetectionMask;
        _currentSteeringVector = Vector2.Zero;
        int index = 0;
        foreach (Tools.InterestWhiskers.Interest interest in _interests)
        {
            if (detectionMask[index++]) continue;
            _currentSteeringVector += interest.Direction * interest.Value;
        }
        
        // TODO: Implement edge case when the target and the obstacle are right forward.
        _currentSteeringVector = _currentSteeringVector.Normalized() * args.MaximumSpeed;
        return new SteeringOutput(_currentSteeringVector, steering.Angular);
    }
    
    public override void _Process(double delta)
    {
        DrawGizmos();
    }

    private void DrawGizmos()
    {
        QueueRedraw();
    }
    
    public override void _Draw()
    {
        if (!ShowGizmos) return;

        // Draw valid interests.
        int index = 0;
        foreach (Tools.InterestWhiskers.Interest interest in _interests)
        {
            if (_dangerSensor.DetectionMask[index++] || 
                !_dangerSensor.IsAnyObjectDetected) continue;
            DrawLine(
                Vector2.Zero,  
                ToLocal(GlobalPosition + 
                        interest.Direction.Normalized() * interest.Value), 
                GizmoColorInterest);
        }
        
        // Draw resulting steering.
        DrawLine(
            Vector2.Zero, 
            ToLocal(GlobalPosition + _currentSteeringVector), 
            GizmoColor,
            10f);
        
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        ITargeterSteeringBehavior behavior = this.FindChild<ITargeterSteeringBehavior>();
        WhiskersSensor dangerSensor = this.FindChild<WhiskersSensor>();
        Tools.InterestWhiskers interestWhisker = this.FindChild<Tools.InterestWhiskers>();
        
        
        List<string> warnings = new();

        if (behavior == null)
        {
            warnings.Add("This node needs a child of that comply with " +
                         "ITargeterSteeringBehavior to work. "); 
        }

        if (dangerSensor == null)
        {
            warnings.Add("This node needs a child of type WhiskersSensor to work. "); 
        }

        if (interestWhisker == null)
        {
            warnings.Add("This node needs a child of type InterestWhiskers to work. ");
        }
        
        return warnings.ToArray();
    }
}