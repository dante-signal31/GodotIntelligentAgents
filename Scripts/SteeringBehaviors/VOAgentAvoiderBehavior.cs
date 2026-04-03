using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Tools;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// <p>Node to offer an agent avoider steering behavior based on the Velocity Obstacle
/// algorithm.</p>
/// <p>The difference with an obstacle avoidance algorithm is that obstacles don't move
/// while agents do.</p>
/// </summary>
[Tool]
// It must be marked as a Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
public partial class VOAgentAvoiderBehavior: Node2D, ISteeringBehavior
{
    [ExportCategory("CONFIGURATION:")] 
    private uint _samplingDiscResolution = 100;
    /// <summary>
    /// Number of samples for the velocity sampling disc. The higher the number,
    /// the more accurate the calculated velocity will be, but the more expensive
    /// it will be to calculate.
    /// </summary>
    [Export] public uint SamplingDiscResolution
    {
        get => _samplingDiscResolution;
        set
        {
            _samplingDiscResolution = value;
            GenerateVelocitySamplingDisc(CurrentMaximumSpeed, value);
        }
    }

    /// <summary>
    /// The higher the value, the more aggressive the evasion will be.
    /// <remarks>Keep well above than the double of MinimumDistanceBetweenAgents.</remarks>
    /// </summary>
    [Export] public float EvasionStrength = 10f;
    
    [Export] public float MinimumDistanceBetweenAgents = 200f;
    
    [ExportCategory("DEBUG:")]
    [Export] public bool ShowGizmos;
    [Export] public Color GizmosColor;
    [Export] public Color NoCollisionVelocitiesColor;
    [Export] public Color CollisionVelocitiesColor;
    [Export] public int GizmoRadius = 20;
    
    [ExportGroup("WIRING:")]
    [Export] private Node2D _toTargetSteeringBehavior;
    
    private float _currentMaximumSpeed;
    private float CurrentMaximumSpeed
    {
        get => _currentMaximumSpeed;
        set
        {
            _currentMaximumSpeed = value;
            GenerateVelocitySamplingDisc(value, SamplingDiscResolution);
        }
    }


    private const float Phi = 1.618033988749895f;
    private readonly HashSet<Vector2> _velocitySamplingDisc = new();
    private ISteeringBehavior _steeringBehavior;
    private PotentialCollisionDetector _collisionDetector;
    private MovingAgent _currentAgent;
    private readonly HashSet<Vector2> _noCollisionCandidateVelocities = new();
    private readonly HashSet<Vector2> _collisionCandidateVelocities = new();
    private Vector2 _bestCandidateVelocity;
    
    /// <summary>
    /// Generate a cloud of relative positions. Every position represents a potential
    /// velocity vector for the agent. So, the cloud is supposed to be centered on the
    /// agent position, and it cannot extend further than the maximum speed.
    /// </summary>
    /// <param name="maximumSpeed">Maximum radius for the cloud of points.</param>
    /// <param name="resolution">Number of points for the cloud.</param>
    private void GenerateVelocitySamplingDisc(float maximumSpeed, uint resolution)
    {
        // The first and easiest way to generate the points would be to use two for loops
        // that iterate over angles and radiuses. But here’s a better solution, using the
        // most irrational number (the golden ratio) to uniformly generate points
        // on a disk.
        // Got from: https://jasonfantl.com/posts/Collision-Avoidance/#sampling-velocities
        for (int i = 1; i <= resolution; i++)
        {
            float radius = Mathf.Sqrt((float)i / resolution) * maximumSpeed;
            float angle = i * 2 * Mathf.Pi * Phi;
            Vector2 newPoint = new Vector2(
                radius * Mathf.Cos(angle), 
                radius * Mathf.Sin(angle));
            _velocitySamplingDisc.Add(newPoint);
        }
    }

    public override void _Ready()
    {
        _steeringBehavior = (ISteeringBehavior)_toTargetSteeringBehavior;
        _currentAgent = this.FindAncestor<MovingAgent>();
        _collisionDetector = this.FindChild<PotentialCollisionDetector>();
    }

    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        // If maximum speed has changed, then we need to regenerate the velocity
        // sampling disc. Just updating the property makes that.
        if (!Mathf.IsEqualApprox(args.MaximumSpeed, CurrentMaximumSpeed)) 
            CurrentMaximumSpeed = args.MaximumSpeed;
        
        SteeringOutput steeringToTargetVelocity = _steeringBehavior.GetSteering(args);
     
        _noCollisionCandidateVelocities.Clear();
        _collisionCandidateVelocities.Clear();
        
        // If there is no potential collision, then we don't need to do anything. Just go
        // straight to the target.
        if (!_collisionDetector.EvaluatingPotentialCollision)
        {
            _bestCandidateVelocity = steeringToTargetVelocity.Linear;
            return steeringToTargetVelocity;
        }

        // If a collision is going to happen in the future, then calculate the avoiding
        // vector nearest to the ideal velocity to target.
        float lowestPenalty = float.MaxValue;
        float candidateVectorDivergence;
        float candidateCollisionTime;
        _bestCandidateVelocity = Vector2.Zero;
        foreach (Vector2 candidateVelocity in _velocitySamplingDisc)
        {
            // Add candidate velocity to one of the lists used for debugging.
            // TODO: Implement minimum distance between agents.
            if (_collisionDetector.IsCollidingVelocity(
                    candidateVelocity,
                    _currentAgent.Radius + MinimumDistanceBetweenAgents,
                    out var collisionTime))
            {
                _collisionCandidateVelocities.Add(candidateVelocity);
            }
            else
            {
                _noCollisionCandidateVelocities.Add(candidateVelocity);
            }
            float vectorDivergence =
                (steeringToTargetVelocity.Linear - candidateVelocity).Length();
            float penalty = vectorDivergence + (EvasionStrength / collisionTime);
            if (penalty < lowestPenalty)
            {
                lowestPenalty = penalty;
                candidateVectorDivergence = vectorDivergence;
                candidateCollisionTime = collisionTime;
                _bestCandidateVelocity = candidateVelocity;
            }
        }
        
        // Return the best candidate velocity as part of the steering output.
        return new SteeringOutput(
            _bestCandidateVelocity, 
            steeringToTargetVelocity.Angular);
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
        // Draw candidate velocities cloud.
        // 
        // First, no collision candidates.
        foreach (Vector2 velocity in _noCollisionCandidateVelocities)
        {
            DrawCircle(
                ToLocal(GlobalPosition + velocity), 
                GizmoRadius, 
                NoCollisionVelocitiesColor);
        }
        // Next, collision candidates.
        foreach (Vector2 velocity in _collisionCandidateVelocities)
        {
            DrawCircle(
                ToLocal(GlobalPosition + velocity), 
                GizmoRadius, 
                CollisionVelocitiesColor);
        }
        
        // Draw current selected velocity.
        DrawLine(
            Vector2.Zero, 
            ToLocal(GlobalPosition + _bestCandidateVelocity), 
            GizmosColor);
        
        // Draw velocity obstacle cone towards nearest collision agent.
        MovingAgent nearestCollisionAgent = _collisionDetector.PotentialCollisionAgent;
        if (nearestCollisionAgent == null) return;
        Vector2 toNearestCollisionAgent =
            nearestCollisionAgent.GlobalPosition - GlobalPosition;
        float nearestCollisionAgentRadius = nearestCollisionAgent.Radius;
        float semiAngle = Mathf.Atan(
            (nearestCollisionAgentRadius + _currentAgent.Radius + MinimumDistanceBetweenAgents) / 
            toNearestCollisionAgent.Length());
        Vector2 coneSide1 = toNearestCollisionAgent.Normalized().Rotated(semiAngle);
        Vector2 coneSide2 = toNearestCollisionAgent.Normalized().Rotated(-semiAngle);
        DrawLine(Vector2.Zero, ToLocal(GlobalPosition + coneSide1 * _currentMaximumSpeed), CollisionVelocitiesColor);
        DrawLine(Vector2.Zero, ToLocal(GlobalPosition + coneSide2 * _currentMaximumSpeed), CollisionVelocitiesColor);
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        PotentialCollisionDetector collisionDetector = 
            this.FindChild<PotentialCollisionDetector>();
        
        List<string> warnings = new();
        
        if (collisionDetector == null)
        {
            warnings.Add("This node needs a child of type PotentialCollisionDetector to " +
                         "work. ");  
        }
        
        return warnings.ToArray();
    }
}