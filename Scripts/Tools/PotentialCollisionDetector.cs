using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using GodotGameAIbyExample.addons.InteractiveRanges.ConeRange;
using GodotGameAIbyExample.Scripts.Extensions;

namespace GodotGameAIbyExample.Scripts.Tools;

// It must be marked as Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
/// <summary>
/// <p>Node to detect future collisions with other agents nearby.</p>
/// </summary>
public partial class PotentialCollisionDetector : Node2D
{
    [ExportCategory("CONFIGURATION:")]
    [Export(PropertyHint.Layers2DPhysics)] private uint _layersToDetect = 1;
    [Export] public float AgentRadius { get; set; }
    
    public bool PotentialCollisionDetected { get; private set; }
    
    public MovingAgent PotentialCollisionAgent { get; private set; }
    
    public float TimeToPotentialCollision { get; private set; }
    
    public float MinimumSeparationAtPotentialCollision { get; private set; }
    
    public Vector2 CurrentRelativePositionToPotentialCollisionAgent { get; private set; }
    public Vector2 CurrentRelativeVelocityToPotentialCollisionAgent { get; private set; }
    
    public float CurrentDistanceToPotentialCollisionAgent => 
        CurrentRelativePositionToPotentialCollisionAgent.Length();
    
    public float CollisionDistance { get; private set; }

    private ConeRange _coneRange;
    private Area2D _detectionArea;
    private CollisionShape2D _collisionShape;
    private MovingAgent _currentAgent;
    
    public override void _EnterTree()
    {
        // Find out who our father is.
        _currentAgent = this.FindAncestor<MovingAgent>();
    }

    public override void _Ready()
    {
        _coneRange = this.FindChild<ConeRange>();
        _detectionArea = this.FindChild<Area2D>();
        _detectionArea.CollisionMask = _layersToDetect;
        _collisionShape = _detectionArea.FindChild<CollisionShape2D>();
        CollisionDistance = 2 * AgentRadius;
        _coneRange.Connect(
            ConeRange.SignalName.Updated, 
            new Callable(this, MethodName.OnConeRangeUpdated));
        UpdateDetectionArea();
    }

    private void OnConeRangeUpdated()
    {
        UpdateDetectionArea();
    }

    private void UpdateDetectionArea()
    {
        if (_collisionShape == null || _coneRange == null) return;
        
        RectangleShape2D rectangleShape = new RectangleShape2D();
        
        rectangleShape.Size = new Vector2(
            _coneRange.Range, 
            _coneRange.Range * 
            Mathf.Sin(float.DegreesToRadians(_coneRange.SemiConeDegrees)) * 
            2);
        
        // Set position offset to center the rectangle properly
        // Move it half its length to the right since we want it to grow in that direction
        _collisionShape.Position = new Vector2(_coneRange.Range / 2, 0);
        
        _collisionShape.Shape = rectangleShape;
    }

    private Array<MovingAgent> GetDetectedAgents()
    {
        Array<Node2D> detections = _detectionArea.GetOverlappingBodies();
        Array<MovingAgent> detectedAgents = new Array<MovingAgent>(
            detections.Where(x => 
                    x is MovingAgent && 
                    // Don't take in count our own agent.
                    ((MovingAgent)x).Name != _currentAgent.Name)
                .Cast<MovingAgent>()
                .ToArray());
        Array<MovingAgent> detectedAgentsInCone = new Array<MovingAgent>();
        foreach (MovingAgent agent in detectedAgents)
        {
            float distance = agent.GlobalPosition.DistanceTo(
                _currentAgent.GlobalPosition);
            float heading = Mathf.RadToDeg(
                _currentAgent.Forward.AngleToPoint(
                agent.GlobalPosition));
            if (distance < _coneRange.Range && heading < _coneRange.SemiConeDegrees)
            {
                detectedAgentsInCone.Add(agent);
            }
        }
        return detectedAgentsInCone;
    }

    public override void _PhysicsProcess(double delta)
    {
        Array<MovingAgent> targets = GetDetectedAgents();
        if (targets.Count == 0)
        {
            PotentialCollisionDetected = false;
            return;
        }
        
        float shortestTimeToCollision = float.MaxValue;
        float minSeparationAtClosestCollisionCandidate = float.MaxValue;
        MovingAgent closestCollidingAgentCandidate = null;
        Vector2 currentRelativePositionToPotentialCollisionAgent = Vector2.Zero;
        Vector2 currentRelativeVelocityToPotentialCollisionAgent = Vector2.Zero;
        PotentialCollisionDetected = false;
        
        foreach (MovingAgent target in targets)
        {
            // Calculate time to collision.
            Vector2 relativePosition = target.GlobalPosition - 
                                       _currentAgent.GlobalPosition;
            float currentDistance = relativePosition.Length();
            // I've used Millington algorithm as reference, but here mine differs his.
            // Millington algorithm substracts the _currentAgent.Velocity from
            // target.Velocity. I guess it's an error because, in my calculations,
            // further dot product would get a negative result for a collision approach,
            // that wouldn't be correct because timeToClosestPosition should be negative
            // if agents go away from each other and positive if they go towards each
            // other. So, I made relativeVelocity the opposite to Millington's algorithm.
            Vector2 relativeVelocity = _currentAgent.Velocity - target.Velocity;
            float relativeSpeed = relativeVelocity.Length();
            
            float timeToClosestPosition = relativePosition.Dot(relativeVelocity) / 
                                    (float) Mathf.Pow(relativeSpeed, 2.0);

            // They are moving away, so no collision possible.
            if (timeToClosestPosition < 0) continue;
            
            float minSeparation = currentDistance - relativeSpeed * timeToClosestPosition;

            // If minSeparation is greater than _collisionDistance then we have no
            // collision at all, so we assess next target.
            if (minSeparation > CollisionDistance) continue;
            
            // OK, we have a candidate potential collision, but is it the nearest?
            if (timeToClosestPosition < shortestTimeToCollision)
            {
                shortestTimeToCollision = timeToClosestPosition;
                closestCollidingAgentCandidate = target;
                minSeparationAtClosestCollisionCandidate = minSeparation;
                currentRelativePositionToPotentialCollisionAgent = relativePosition;
                currentRelativeVelocityToPotentialCollisionAgent = relativeVelocity;
            }
        }
        
        // Offer data of the current nearest agent collision candidate.
        TimeToPotentialCollision = shortestTimeToCollision;
        MinimumSeparationAtPotentialCollision = minSeparationAtClosestCollisionCandidate;
        PotentialCollisionAgent = closestCollidingAgentCandidate;
        CurrentRelativePositionToPotentialCollisionAgent = 
            currentRelativePositionToPotentialCollisionAgent;
        CurrentRelativeVelocityToPotentialCollisionAgent = 
            currentRelativeVelocityToPotentialCollisionAgent;
        PotentialCollisionDetected = PotentialCollisionAgent != null;
    }

    public override string[] _GetConfigurationWarnings()
    {
        ConeRange coneRange =
            this.FindChild<ConeRange>();

        Area2D detectionArea =
            this.FindChild<Area2D>();

        List<string> warnings = new();

        if (coneRange == null)
        {
            warnings.Add("This node needs a child ConeRange node to work. ");
        }

        if (detectionArea == null)
        {
            warnings.Add("This node needs a child Area2D node to work");
        }

        return warnings.ToArray();
    }
}