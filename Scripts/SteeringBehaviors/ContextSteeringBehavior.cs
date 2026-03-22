using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Sensors;
using GodotGameAIbyExample.Scripts.Tools;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

[Tool]
public partial class ContextSteeringBehavior: Node2D, ISteeringBehavior
{
    private class InterestComparer : IComparer<InterestWhiskers.Interest>
    {
        public int Compare(
            InterestWhiskers.Interest x, 
            InterestWhiskers.Interest y)
        {
            // Compare the values.
            int result = y.Value.CompareTo(x.Value);
            if (result != 0) return result;
            
            // If the values are equal, give preference to the minimum angle.
            float angleX = Mathf.Abs(x.Direction.Angle());
            float angleY = Mathf.Abs(y.Direction.Angle());
            result = angleX.CompareTo(angleY);
            if (result != 0) return result;
            
            // Final deterministic tie-breaker.
            return x.Direction.X.CompareTo(y.Direction.X);
        }
    }
    
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

    [Export] private uint _addedInterests = 2;
    
    [ExportCategory("DEBUG:")]
    [Export] private bool ShowGizmos { get; set; }
    [Export] private Color GizmoColor { get; set; }
    [Export] private Color GizmoColorInterest { get; set; }
    
    private ITargeterSteeringBehavior _behavior;
    private WhiskersSensor _dangerSensor;
    private Tools.InterestWhiskers _interestWhisker;
    private List<Tools.InterestWhiskers.Interest> _interests = new();
    private Vector2 _currentSteeringVector;
    private MovingAgent _currentAgent;
    
    public override void _Ready()
    {
        GetChildrenReferences();
        ConfigureWhiskers();
        _currentAgent = this.FindAncestor<MovingAgent>();
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
        
        // We want to decisively avoid obstacles. Therefore, we will filter the vectors
        // of interest that lie on the side where more obstacles have been detected.
        List<bool> detectionMask = _dangerSensor.DetectionMask;
        int centerBitIndex = detectionMask.Count / 2;

        // Analyze both halves and set the half with more trues to be completely true.
        int leftTrues = CountTruesInRange(detectionMask, 0, centerBitIndex);
        int rightTrues = CountTruesInRange(
            detectionMask, 
            centerBitIndex, 
            detectionMask.Count);

        if (leftTrues > rightTrues)
        {
            SetRangeToTrue(detectionMask, 0, centerBitIndex);
        }
        else if (rightTrues > leftTrues)
        {
            SetRangeToTrue(detectionMask, centerBitIndex, detectionMask.Count);
        }
        
        // Get every interest vector where an obstacle is NOT detected. Sort the
        // resulting interests by their value to get the highest ones later.
        List<InterestWhiskers.Interest> validInterests = new();
        int index = 0;
        foreach (InterestWhiskers.Interest interest in _interests)
        {
            if (detectionMask[index++]) continue;
            validInterests.Add(interest);
        }
        validInterests.Sort(new InterestComparer());
        
        // Once filtered, we use the highest interest nuanced by the lower interests.
        _currentSteeringVector = Vector2.Zero;
        int highestInterestIndex = 0;
        foreach (InterestWhiskers.Interest interest in validInterests)
        {
            if (highestInterestIndex++ >= _addedInterests) break;
            _currentSteeringVector += interest.Direction * interest.Value;
        }
        
        _currentSteeringVector = _currentSteeringVector.Normalized() * args.MaximumSpeed;
        // If you are walking through hell, keep walking.
        if (_currentSteeringVector.Length() == 0) 
            _currentSteeringVector = _currentAgent.Forward * args.MaximumSpeed;
        return new SteeringOutput(_currentSteeringVector, steering.Angular);
    }
    
    /// <summary>
    /// Counts the number of trues in the given range of the mask.
    /// </summary>
    /// <param name="mask">Mask to assess.</param>
    /// <param name="start">Index where assess starts.</param>
    /// <param name="end">Index where assess ends (exclusive).</param>
    /// <returns>Amount of bits set to true in given range of the mask.</returns>
    private int CountTruesInRange(List<bool> mask, int start, int end)
    {
        int count = 0;
        for (int i = start; i < end; i++)
        {
            if (mask[i]) count++;
        }
        return count;
    }

    /// <summary>
    /// Sets the given range of bits of the mask to true.
    /// </summary>
    /// <param name="mask">Mask whose bits we are going to set.</param>
    /// <param name="start">Index where to start to set bits.</param>
    /// <param name="end">Index where to end bit setting (exclusive).</param>
    private void SetRangeToTrue(List<bool> mask, int start, int end)
    {
        for (int i = start; i < end; i++)
        {
            mask[i] = true;
        }
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
        foreach (InterestWhiskers.Interest interest in _interests)
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