using System;
using System.Collections.Generic;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Sensors;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

// It must be marked as Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
/// <summary>
/// <p>Node to offer an agent avoider steering behaviour based on the Avoid Nearest
/// Neighbor algorithm.</p>
/// <p>The difference with an obstacle avoidance algorithm is that obstacles don't move
/// while agents do.</p>
/// </summary>
public partial class ANNPassiveAgentAvoiderSteeringBehavior: Node2D, ISteeringBehavior
{
    [ExportCategory("CONFIGURATION:")]
    [Export] public float MinimumDistanceBetweenAgents = 200f;
    /// <summary>
    /// Threshold factor for determining when to use normal vector avoidance.
    /// When the dot product between avoidance and collision vectors exceeds this value 
    /// (positive or negative), the avoidance vector is replaced with a vector normal 
    /// to the collision agent's velocity to prevent chase or collision scenarios.
    /// </summary>
    [Export] public float TooAlignedFactor { get; set; }= 0.95f;
    
    [ExportCategory("DEBUG:")]
    [Export] public bool ShowGizmos = false;
    [Export] public Color GizmosColor;
    
    private VolumetricSensor _sensor;
    private Vector2 _currentAgentAvoidingVelocity = Vector2.Zero;
    private readonly Random _random = new();
    
    public override void _Ready()
    {
        _sensor = this.FindChild<VolumetricSensor>();
    }
    
    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        // No near agents? No need to avoid.
        if (!_sensor.AnyObjectDetected) return SteeringOutput.Zero;

        // Find the nearest agent.
        HashSet<Node2D> detectedObjects = _sensor.DetectedObjects;
        MovingAgent nearestAgent = null;
        float distance = float.MaxValue;
        Vector2 evasionVector = Vector2.Zero;
        foreach (Node2D detectedObject in detectedObjects)
        {
            if (!(detectedObject is MovingAgent detectedAgent)) continue;
            float currentDistance = GlobalPosition.DistanceTo(detectedAgent.GlobalPosition);
            if (currentDistance >= distance) continue;
            evasionVector = (GlobalPosition - detectedAgent.GlobalPosition).Normalized();
            distance = currentDistance;
            nearestAgent = detectedAgent;
        }
        
        // If the sensor detected anything, then we must have found the nearest agent.
        // But just in case...
        if (nearestAgent == null) return SteeringOutput.Zero;
        
        // Calculate the evasion vector and speed. The evasion vector is the vector
        // opposed to the relative vector from the agent to the nearest agent. The evasion
        // speed is higher the closer the agent is to the nearest agent, getting infinite
        // when they are at MinimumDistanceBetweenAgents.
        float addedRadius = nearestAgent.Radius + args.CurrentAgent.Radius;
        float evasionMagnitude = 1 / 
                                 Mathf.Max(
                                     Mathf.Epsilon, 
                                     distance - addedRadius - MinimumDistanceBetweenAgents);
        float evasionSpeed = Mathf.Min(
            args.MaximumSpeed, 
            evasionMagnitude * args.MaximumSpeed);
        
        // Finally, we can calculate the final evasion vector.
        _currentAgentAvoidingVelocity = evasionVector * evasionSpeed;
        
        // THE EDGE CASE:
        // This algorithm suffers the same edge problem than Millington's. The problem
        // with the original algorithm is that it does not seem to take in count the edge
        // case where the two agents are going one against the other directly, in opposite 
        // directions. The rest of the method fixes that.
        
        // One way to find out if the two agents are going one against the other in 
        // opposite directions is to check the dot product between the evasion vector
        // and the current velocity. If the absolute value of a dot product is near 1,
        // that means the two agents are going away or approaching, in both cases in
        // the same "line". In the first case, it wouldn't be a collision, but we want
        // an avoidance movement, not a chase.In the second case, that means that the
        // two agents are approaching in opposite directions.
        float alignmentFactor = Mathf.Abs(
            evasionVector.Dot(args.CurrentAgent.Velocity.Normalized())); 
        if (Mathf.Abs(alignmentFactor) >= TooAlignedFactor)
        {
            // If relative velocity is too aligned with evasionVector, then it means
            // we can end in a direct hit, so we try an evasion vector that is
            // perpendicular to the current agent's velocity.
            evasionVector = args.CurrentAgent.Velocity
                                         .Rotated(Mathf.Pi / 2)
                                         .Normalized() * 
                                     // Turn to one side or another randomly.
                                     (_random.Next(2) * 2 - 1); 
            _currentAgentAvoidingVelocity = evasionVector * args.MaximumSpeed;
        }
        

        return new SteeringOutput(_currentAgentAvoidingVelocity);
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
        if (_sensor == null) return;

        if (!_sensor.AnyObjectDetected) return;
        
        // Draw current agent avoider velocity.
        DrawLine(
            Vector2.Zero, 
            ToLocal(GlobalPosition + _currentAgentAvoidingVelocity), 
            GizmosColor);
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        VolumetricSensor coneSensor = this.FindChild<VolumetricSensor>();
        
        List<string> warnings = new();
        
        if (coneSensor == null)
        {
            warnings.Add("This node needs a child of type VolumetricSensor to work.");  
        }
        
        return warnings.ToArray();
    }
}