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
    
    public bool PotentialCollisionDetected => TimeToPotentialCollision < float.MaxValue;
    
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
            detections.Where(x => x is MovingAgent)
                .Cast<MovingAgent>()
                .ToArray());
        Array<MovingAgent> detectedAgentsInCone = new Array<MovingAgent>();
        foreach (MovingAgent agent in detectedAgents)
        {
            float distance = agent.GlobalPosition.DistanceTo(
                _currentAgent.GlobalPosition);
            float heading = Mathf.RadToDeg(
                _currentAgent.Forward.AngleTo(
                _currentAgent.ToLocal(agent.GlobalPosition)));
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
        
        float shortestTimeToCollision = float.MaxValue;
        MovingAgent closestCollidingAgentCandidate = null;
        float minSeparationAtClosestCollisionCandidate = float.MaxValue;
        Vector2 currentRelativePositionToPotentialCollisionAgent = Vector2.Zero;
        Vector2 currentRelativeVelocityToPotentialCollisionAgent = Vector2.Zero;
        
        foreach (MovingAgent target in targets)
        {
            // Calculate time to collision.
            Vector2 relativePosition = target.GlobalPosition - 
                                       _currentAgent.GlobalPosition;
            float currentDistance = relativePosition.Length();
            Vector2 relativeVelocity = target.Velocity - _currentAgent.Velocity;
            float relativeSpeed = relativeVelocity.Length();
            float timeToClosestPosition = relativePosition.Dot(relativeVelocity) / 
                                    (float) Mathf.Pow(relativeSpeed, 2.0);
            
            // I've used Millington algorithm as reference, but here mine differs his.
            // Millington algorithm substracts the relativeSpeed * timeToCollision from
            // the currentDistance. I guess it's an error because my calculations
            // results that relativeSpeed * timeToCollision should be added to the
            // currentDistance.
            float minSeparation = currentDistance + relativeSpeed * timeToClosestPosition;

            // If minSeparation is greater than _collisionDistance then we have no
            // collision at all, so we assess next target.
            if (minSeparation > CollisionDistance) continue;
            
            // OK, we have a candidate potential collision, but is it the nearest?
            if (0 < timeToClosestPosition && 
                timeToClosestPosition < shortestTimeToCollision)
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