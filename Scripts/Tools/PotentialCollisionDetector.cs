using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Sensors;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;

namespace GodotGameAIbyExample.Scripts.Tools;

// It must be marked as Tool to be found when used my custom extension
// method FindChild<T>(). Otherwise, FindChild casting will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
/// <summary>
/// <p>Node to detect future collisions with other agents nearby.</p>
/// </summary>
public partial class PotentialCollisionDetector : Node2D
{
    [ExportCategory("CONFIGURATION:")]
    /// <summary>
    /// Specifies the physics layers that the <see cref="PotentialCollisionDetector"/>
    /// will monitor for potential collisions. This variable is used to configure
    /// which 2D physics layers should be included in detection.
    /// </summary>
    // [Export(PropertyHint.Layers2DPhysics)] private uint _layersToDetect = 1;

    /// <summary>
    /// Represents the radius of an agent used for potential collision detection.
    /// </summary>
    /// <remarks>
    /// This property defines the distance around the agent that is considered for
    /// detecting potential collisions with other objects or agents.
    /// </remarks>
    [Export] public float AgentRadius { get; set; } = 50f;
    

    /// <summary>
    /// Indicates whether a potential collision has been detected if current agent
    /// keeps its heading.
    /// </summary>
    /// <remarks>
    /// This property returns true if the system has identified a potential collision
    /// scenario in the context of an agent's movement or interaction with other objects.
    /// </remarks>
    public bool PotentialCollisionDetected { get; private set; } = false;

    /// <summary>
    /// Identifies the nearest agent that poses a potential collision threat.
    /// </summary>
    /// <remarks>
    /// This property is updated during the physics process to hold a reference to the
    /// closest agent which may collide with the current agent. It facilitates collision
    /// prediction and subsequent decision-making processes.
    /// </remarks>
    public MovingAgent PotentialCollisionAgent { get; private set; }

    /// <summary>
    /// Represents the calculated time remaining before a potential collision occurs.
    /// </summary>
    /// <remarks>
    /// This property indicates the predicted time, in seconds, before an agent
    /// or object will potentially collide with another. It is typically used
    /// for path planning, collision avoidance, and steering behaviors in AI systems.
    /// </remarks>
    public float TimeToPotentialCollision { get; private set; }

    /// <summary>
    /// Represents the relative position at which a potential collision is detected is
    /// likely to happen if current agent keeps its heading.
    /// </summary>
    /// <remarks>
    /// This property provides the coordinates relative to the current agent's position
    /// indicating where a potential collision with another object or agent may occur.
    /// </remarks>
    public Vector2 RelativePositionAtPotentialCollision { get; private set; }

    /// <summary>
    /// Represents the separation distance maintained during a potential collision
    /// scenario.
    /// </summary>
    public float SeparationAtPotentialCollision { get; private set; }

    /// <summary>
    /// Denotes the relative position of the current agent to a potential collision agent.
    /// </summary>
    public Vector2 CurrentRelativePositionToPotentialCollisionAgent { get; private set; }

    /// <summary>
    /// Represents the relative velocity of the current agent with respect to
    /// a potential collision agent.
    /// </summary>
    public Vector2 CurrentRelativeVelocityToPotentialCollisionAgent { get; private set; }

    /// <summary>
    /// Represents the current distance to a potential collision agent.
    /// </summary>
    public float CurrentDistanceToPotentialCollisionAgent => 
        CurrentRelativePositionToPotentialCollisionAgent.Length();

    /// <summary>
    /// Specifies the distance within which potential collisions are evaluated.
    /// </summary>
    /// <remarks>
    /// This property determines the threshold distance used to identify whether objects
    /// are close enough to be considered for collision detection. 
    /// </remarks>
    public float CollisionDistance { get; private set; }
    
    /// <summary>
    /// Indicates whether the system is currently evaluating any potential collisions.
    /// </summary>
    public bool EvaluatingPotentialCollision => _detectedAgents.Count > 0;

    private ISensor _sensor;
    private CollisionShape2D _collisionShape;
    private MovingAgent _currentAgent;
    private HashSet<MovingAgent> _detectedAgents = new();
    
    public override void _EnterTree()
    {
        // Find out who our father is.
        _currentAgent = this.FindAncestor<MovingAgent>();
        CollisionDistance = 2 * AgentRadius;
        // Connect to sensor events.
        _sensor = this.FindChild<ISensor>();
        if (_sensor == null) return;
        _sensor.ObjectEnteredSensor += OnObjectEnteredSensor;
        _sensor.ObjectLeftSensor += OnObjectExitedSensor;
    }

    public override void _ExitTree()
    {
        if (_sensor == null) return;
        _sensor.ObjectEnteredSensor -= OnObjectEnteredSensor;
        _sensor.ObjectLeftSensor -= OnObjectExitedSensor;
    }


    // public override void _Ready()
    // {
    //     _sensor = this.FindChild<ISensor>();
    //     if (_sensor == null) return;
    //     Node2D sensorNode = (Node2D) _sensor;
    //     sensorNode.Connect(
    //         ConeSensor.SignalName.ObjectEnteredCone,
    //         new Callable(this, MethodName.OnObjectEnteredSensor));
    //     sensorNode.Connect(
    //         ConeSensor.SignalName.ObjectLeftCone,
    //         new Callable(this, MethodName.OnObjectExitedSensor));
    // }
    
    /// <summary>
    /// Event handler to use when another agent enters our detection area.
    /// </summary>
    /// <param name="otherObject">The agent who enters our detection area.</param>
    private void OnObjectEnteredSensor(Node2D otherObject)
    {
        if (otherObject is MovingAgent otherAgent)
        {
            _detectedAgents.Add(otherAgent);
        }
    }
    
    /// <summary>
    /// Event handler to use when another agent exits our detection area.
    /// </summary>
    /// <param name="otherAgent">The agent who exits our detection area.</param>
    private void OnObjectExitedSensor(Node2D otherObject)
    {
        if (otherObject is MovingAgent otherAgent)
        {
            if (!_detectedAgents.Contains(otherAgent)) return;
            _detectedAgents.Remove(otherAgent);
        }
    }
    
    /// <summary>
    /// Assess if the current agent can collide with any of the detected agents, with
    /// the given velocity and radius.
    /// </summary>
    /// <param name="currentVelocity">Velocity for the current agent.</param>
    /// <param name="currentRadius">Radius for the current agent.</param>
    /// <param name="collisionTime">If a potential collision is detected, returns time
    /// to suffer that collision if the given velocity is applied to the current agent.
    /// If no potential collision is detected, then a float.PositiveInfinity is
    /// returned in this parameter.</param>
    /// <returns>True if any potential collision is detected. False otherwise.</returns>
    public bool IsCollidingVelocity(
        Vector2 currentVelocity, 
        float currentRadius, 
        out float collisionTime)
    {
        Array<MovingAgent> otherAgents = GetDetectedAgents();
        foreach (MovingAgent otherAgent in otherAgents)
        {
            if (IsGoingToHappenACollision(
                otherAgent, 
                currentVelocity,
                currentRadius + otherAgent.Radius,
                out var relativePosition, 
                out var relativeVelocity, 
                out var timeToClosestPosition, 
                out var minRelativePosition, 
                out var minSeparation))
            {
                collisionTime = timeToClosestPosition;
                return true;
            }
        }
        // No collision detected for that currentVelocity with that currentRadius.
        collisionTime = float.PositiveInfinity;
        return false;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_sensor == null) return;
        
        Array<MovingAgent> targets = GetDetectedAgents();
        
        if (targets.Count == 0)
        {
            PotentialCollisionDetected = false;
            return;
        }
        
        float shortestTimeToCollision = float.MaxValue;
        Vector2 relativePositionAtPotentialCollision = Vector2.Zero;
        float minSeparationAtClosestCollisionCandidate = float.MaxValue;
        MovingAgent closestCollidingAgentCandidate = null;
        Vector2 currentRelativePositionToPotentialCollisionAgent = Vector2.Zero;
        Vector2 currentRelativeVelocityToPotentialCollisionAgent = Vector2.Zero;
        PotentialCollisionDetected = false;
        
        foreach (MovingAgent target in targets)
        {
            if (!IsGoingToHappenACollision(
                    target, 
                    _currentAgent.Velocity,
                    CollisionDistance,
                    out var relativePosition, 
                    out var relativeVelocity, 
                    out var timeToClosestPosition, 
                    out var minRelativePosition, 
                    out var minSeparation)) continue;

            // OK, we have a candidate potential collision, but is it the nearest?
            if (timeToClosestPosition < shortestTimeToCollision)
            {
                shortestTimeToCollision = timeToClosestPosition;
                closestCollidingAgentCandidate = target;
                relativePositionAtPotentialCollision = minRelativePosition;
                minSeparationAtClosestCollisionCandidate = minSeparation;
                currentRelativePositionToPotentialCollisionAgent = relativePosition;
                currentRelativeVelocityToPotentialCollisionAgent = relativeVelocity;
            }
        }
        
        // Offer data of the current nearest agent collision candidate.
        TimeToPotentialCollision = shortestTimeToCollision;
        RelativePositionAtPotentialCollision = relativePositionAtPotentialCollision;
        SeparationAtPotentialCollision = minSeparationAtClosestCollisionCandidate;
        PotentialCollisionAgent = closestCollidingAgentCandidate;
        CurrentRelativePositionToPotentialCollisionAgent = 
            currentRelativePositionToPotentialCollisionAgent;
        CurrentRelativeVelocityToPotentialCollisionAgent = 
            currentRelativeVelocityToPotentialCollisionAgent;
        PotentialCollisionDetected = PotentialCollisionAgent != null;
    }

    /// <summary>
    /// Gets all agents detected by the sensor.
    /// </summary>
    /// <returns>An array with the MovingAgent components of detected agents.</returns>
    private Array<MovingAgent> GetDetectedAgents()
    {
        return new Array<MovingAgent>(
            _sensor.DetectedObjects.Where(x => 
                    x is MovingAgent && 
                    // Don't take in count our own agent.
                    ((MovingAgent)x).Name != _currentAgent.Name)
                .Cast<MovingAgent>()
                .ToArray());
    }

    private bool IsGoingToHappenACollision(
        MovingAgent target,
        Vector2 currentVelocity,
        float collisionDistance,
        out Vector2 relativePosition,
        out Vector2 relativeVelocity, 
        out float timeToClosestPosition,
        out Vector2 minRelativePosition, 
        out float minSeparation)
    {
        // Calculate time to collision.
        relativePosition = target.GlobalPosition - 
                           _currentAgent.GlobalPosition;
        relativeVelocity = target.Velocity - currentVelocity;
        float relativeSpeed = relativeVelocity.Length();
            
        // I've used Millington algorithm as reference, but here mine differs his.
        // Millington algorithm uses de positive dot product between relativePosition
        // and relativeVelocity. I guess it's an error because, in my calculations,
        // that would get a positive result for a non-collision approach,
        // that wouldn't be correct because timeToClosestPosition should be negative
        // if agents go away from each other and positive if they go towards each
        // other.
        // Besides, I've found sources where this formula is defined and they 
        // multiply by -1.0 the numerator:
        // https://medium.com/@knave/collision-avoidance-the-math-1f6cdf383b5c
        //
        // So, I've multiplied by -1.0 the numerator.
        timeToClosestPosition = -1 * relativePosition.Dot(relativeVelocity) / 
                                (float) Mathf.Pow(relativeSpeed, 2.0);
        
        // Here too, my implementation differs from Millington's. He calculates
        // miSeparation substracting relativeSpeed * timeToClosestPosition from the 
        // modulus of relative position. My tests show that you must do instead the
        // operations summing with vectors and later get the module.
        minRelativePosition = relativePosition + relativeVelocity * timeToClosestPosition; 
        minSeparation = minRelativePosition.Length();
        
        // They are moving away, so no collision possible.
        if (timeToClosestPosition < 0)
        {
            minSeparation = 0;
            return false;
        }

        // If minSeparation is greater than CollisionDistance then we have no
        // collision at all, so we assess next target.
        if (minSeparation > collisionDistance) return false;
        return true;
    }

    public override string[] _GetConfigurationWarnings()
    {
        ISensor sensor = this.FindChild<ISensor>();

        List<string> warnings = new();

        if (sensor == null)
        {
            warnings.Add("This node needs a child ISensor node to work. ");
        }

        return warnings.ToArray();
    }
}