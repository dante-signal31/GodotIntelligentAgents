using Godot;
using System;
using System.Collections.Generic;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;

// It must be marked as Tool to be found by MovingAgent when it uses my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to ISteeringBehavior will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
/// <summary>
/// <p>Node to offer an Interpose steering behaviour.</p>
/// <p>Interpose make an agent to place itself between two other agents.</p>
/// <p>It's an usual protection behavior. E.g. a bodyguard.</p>
/// </summary>
public partial class InterposeSteeringBehavior : Node2D, ISteeringBehavior
{
    [ExportCategory("CONFIGURATION:")] 
    [Export] public MovingAgent AgentA { get; set; }
    [Export] public MovingAgent AgentB { get; set; }
    /// <summary>
    /// Distance at which we give our goal as reached and we stop our agent.
    /// </summary>
    [Export] public float ArrivalDistance { get; set; }
    
    [ExportCategory("DEBUG:")]
    /// <summary>
    /// Make visible position marker.
    /// </summary>
    [Export] private bool PredictedPositionMarkerVisible { get; set; }
    
    private SeekSteeringBehavior _seekSteeringBehavior;
    private Node2D _predictedPositionMarker;
    private Vector2 _previousPositionAgentA;
    private Vector2 _previousPositionAgentB;

    public override void _EnterTree()
    {
        _predictedPositionMarker = new Node2D();
        _predictedPositionMarker.GlobalPosition = GetMidPoint(
            AgentA.GlobalPosition, 
            AgentB.GlobalPosition);
    }

    public override void _ExitTree()
    {
        _predictedPositionMarker.QueueFree();
    }

    public override void _Ready()
    {
        _seekSteeringBehavior = this.FindChild<SeekSteeringBehavior>();
        _seekSteeringBehavior.Target = _predictedPositionMarker;
        _seekSteeringBehavior.ArrivalDistance = ArrivalDistance;
    }

    /// <summary>
    /// Get midway point between two positions.
    /// </summary>
    /// <param name="position1">First position.</param>
    /// <param name="position2">Second position.</param>
    /// <returns>Midpoint position.</returns>
    public static Vector2 GetMidPoint(Vector2 position1, Vector2 position2)
    {
        Vector2 vectorBetweeenPositions = position2 - position1;
        return position1 + vectorBetweeenPositions / 2;
    }
    
    public SteeringOutput GetSteering(SteeringBehaviorArgs args)
    {
        Vector2 currentPosition = args.CurrentAgent.GlobalPosition;
        float maximumSpeed = args.MaximumSpeed;

        if (AgentA.GlobalPosition != _previousPositionAgentA ||
            AgentB.GlobalPosition != _previousPositionAgentB)
        {
            Vector2 midPoint = GetMidPoint(AgentA.GlobalPosition, AgentB.GlobalPosition);
        
            // If target agents where static, how much time we'd need to get to midPoint?
            float TimeToReachMidPoint = (midPoint - currentPosition).Length() / maximumSpeed;
        
            // But actually agents won't be static, so while we move to midPoint, they will
            // move too. So, we must figure out where target agents are going to be after
            // TimeToReachMidPoint has passed. To get that we'll assume both target agents
            // are going to continue on a straight trajectory (so, no velocity change), so 
            // we'll extrapolate their future position using their current velocity.
            Vector2 futurePositionOfAgentA = AgentA.GlobalPosition + 
                                             AgentA.Velocity * TimeToReachMidPoint;
            Vector2 futurePositionOfAgentB = AgentB.GlobalPosition + 
                                             AgentB.Velocity * TimeToReachMidPoint;
        
            // Now we have the future position of target agents, we can get the estimated
            // future midpoint position.
            Vector2 futureMidPoint = GetMidPoint(
                futurePositionOfAgentA, 
                futurePositionOfAgentB);
        
            // So, to not been left behind, we must go to the future midpoint.
            _predictedPositionMarker.GlobalPosition = futureMidPoint;

            _previousPositionAgentA = AgentA.GlobalPosition;
            _previousPositionAgentB = AgentB.GlobalPosition;
        }
        
        return _seekSteeringBehavior.GetSteering(args);
    }
    
    public override void _Process(double delta)
    {
        if (PredictedPositionMarkerVisible) DrawGizmos();
    }

    private void DrawGizmos()
    {
        QueueRedraw();
    }
    
    public override void _Draw()
    {
        if (_predictedPositionMarker == null || 
            !PredictedPositionMarkerVisible ||
            Engine.IsEditorHint()) return;
        DrawLine(
            ToLocal(AgentA.GlobalPosition), 
            ToLocal(_predictedPositionMarker.GlobalPosition), 
            AgentA.AgentColor);
        DrawCircle(
            ToLocal(_predictedPositionMarker.GlobalPosition),
            30f, 
            Colors.Burlywood, 
            filled: false);
        DrawLine(
            ToLocal(AgentB.GlobalPosition),  
            ToLocal(_predictedPositionMarker.GlobalPosition), 
            AgentB.AgentColor);
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        _seekSteeringBehavior = this.FindChild<SeekSteeringBehavior>();

        List<string> warnings = new();
        
        if (_seekSteeringBehavior == null)
        {
            warnings.Add("This node needs a child of type SeekSteeringBehavior to work.");
        }
        
        return warnings.ToArray();
    }
}
