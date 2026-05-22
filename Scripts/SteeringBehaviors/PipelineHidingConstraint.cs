using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Pathfinding;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// Represents a constraint used for pathfinding that allows an agent to hide from a
/// threat. This class ensures that the agent avoids being visible to a designated threat
/// by dynamically evaluating paths and suggesting alternative goals that provide cover.
/// </summary>
[Tool]
public partial class PipelineHidingConstraint: Node2D, IPipelineConstraint
{
    [ExportCategory("CONFIGURATION:")]
    private MovingAgent _threat;
    /// <summary>
    /// Agent to hide from.
    /// </summary>
    [Export] public MovingAgent Threat
    {
        get => _threat;
        set
        {
            _threat = value;
            if (_hidingPointsDetector != null)  
                _hidingPointsDetector.Threat = value;
            if (_rayCast2D != null)
                _rayCast2D.CollisionMask = Threat.CollisionLayer | ObstaclesLayers;
        }
    }
    
    private uint _obstaclesLayers = 1;
    /// <summary>
    /// At which physics layers do the obstacles belong to?
    /// </summary>
    [Export(PropertyHint.Layers2DPhysics)] public uint ObstaclesLayers
    {
        get => _obstaclesLayers;
        set
        {
            _obstaclesLayers = value;
            if (_hidingPointsDetector != null) 
                _hidingPointsDetector.ObstaclesLayers = value;
        }
    }

    /// <summary>
    /// How many used hiding points to remember to avoid loops?
    /// </summary>
    [Export] private int _usedHidingPointsMemorySize = 3;
    [Export] private float _usedHidingPointsRadius = 50f;
    
    private Tools.HidingPointsDetector _hidingPointsDetector;
    // private INavigationAgent _navigationAgent2D;
    private RayCast2D _rayCast2D;
    private Levels.Courtyard _currentLevel;
    private Vector2 _alternativeGoalPosition;
    private readonly Queue<Vector2> _alreadyUsedHidingPoints = new();

    private void LoadChildrenReferences()
    {
        _hidingPointsDetector = this.FindChild<Tools.HidingPointsDetector>();
        // _navigationAgent2D = this.FindChild<INavigationAgent>();
        _rayCast2D = this.FindChild<RayCast2D>();
    }
    
    public override void _Ready()
    {
        LoadChildrenReferences();
        Threat = _threat;
        InitializeHidingPointsDetector();
    }

    private void InitializeHidingPointsDetector()
    {
        Node2D currentRoot = GetTree().Root.FindChild<Node2D>();
        if (currentRoot == null) return;
        _currentLevel = currentRoot.FindChild<Levels.Courtyard>();
        // Next guard is needed to not receiving warnings when this node is opened in its
        // own scene.
        if (_currentLevel == null) return;
        _hidingPointsDetector.ObstaclesPositions = _currentLevel.ObstaclePositions;
    }

    /// <summary>
    /// Determines whether a given position is visible to the threat agent.
    /// </summary>
    /// <param name="position">The position to check visibility.</param>
    /// <returns>True if the position is visible to the threat agent;
    /// otherwise, false.</returns>
    private bool IsPositionVisibleByThreat(Vector2 position)
    {
        _rayCast2D.GlobalPosition = position;
        _rayCast2D.TargetPosition = ToLocal(Threat.GlobalPosition);
        _rayCast2D.ForceRaycastUpdate();
        if (_rayCast2D.IsColliding())
        {
            Node detectedCollider = (Node) _rayCast2D.GetCollider();
            return (detectedCollider.Name == Threat.Name);
        }
        
        return false;
    }

    /// <summary>
    /// Finds the nearest hiding position from a specified position.
    /// </summary>
    /// <param name="position">The position from which the search for the nearest
    /// hiding position starts.</param>
    /// <returns>The nearest hiding position as a <see cref="Vector2"/>.</returns>
    private Vector2 GetNearestHidingPosition(
        Vector2 position, 
        Vector2[] hidingPoints = null)
    {
        if (hidingPoints == null) 
            hidingPoints = _hidingPointsDetector.HidingPoints.ToArray();
        Vector2 nearestHidingPosition = Vector2.Zero;
        float nearestHidingDistance = float.MaxValue;
        foreach (Vector2 hidingPosition in hidingPoints)
        {
            float distance = position.DistanceTo(hidingPosition);
            if (distance < nearestHidingDistance)
            {
                nearestHidingDistance = distance;
                nearestHidingPosition = hidingPosition;
            }
        }
        return nearestHidingPosition;   
    }

    private void RegisterUsedHidingPoint(Vector2 hidingPosition)
    {
        if (_alreadyUsedHidingPoints.Count >= _usedHidingPointsMemorySize)
            _alreadyUsedHidingPoints.Dequeue();
        _alreadyUsedHidingPoints.Enqueue(hidingPosition);   
    }

    private bool IsHidingPointUsed(Vector2 hidingPosition)
    {
        foreach (Vector2 alreadyUsedHidingPoint in _alreadyUsedHidingPoints)
        {
            if (hidingPosition.DistanceTo(alreadyUsedHidingPoint) < _usedHidingPointsRadius) return true;
        }
        return false;
    }

    private Vector2[] GetUnusedHidingPoints()
    {
        HashSet<Vector2> unusedHidingPoints = new();
        foreach (Vector2 hidingPoint in _hidingPointsDetector.HidingPoints)
        {
            if (!IsHidingPointUsed(hidingPoint)) unusedHidingPoints.Add(hidingPoint);
        }
        return unusedHidingPoints.ToArray();
    }
    
    public bool IsViolated(Path pathToGoal, PipelineGoal goal, SteeringBehaviorArgs args)
    {
        // If we are already heading to a hiding point, then we don't need to check
        // whether we are visible. We chose the nearest hiding point, and we may need to
        // go through a visible zone to get to it.
        if (pathToGoal.TargetPositions.Count > 1 && 
            pathToGoal.TargetPositions[^1].DistanceTo(_alternativeGoalPosition) < 
            _usedHidingPointsRadius) 
            return false;
        
        // Check whether any of path points is visible from the threat agent.
        // Discard the first point because it is the current position.
        Array<Vector2> pathPoints = pathToGoal.TargetPositions[1..];
        foreach (Vector2 pathPosition in pathPoints)
        {
            if (IsPositionVisibleByThreat(pathPosition)) return true;
        }
        return false;
    }

    public PipelineGoal SuggestGoal(
        Path pathToGoal, 
        PipelineGoal goal, 
        SteeringBehaviorArgs args)
    {
        // If any of the path points is visible from the threat agent,look for the hiding
        // point nearest to that path point. That hiding point will become the new
        // positional goal. When that goal is reached, a new evaluation will be performed
        // to get the high-level goal, but at that moment the threat could have moved and
        // a new hidden path can be achieved. To avoid loops in the hiding path, we
        // remember the hiding points that we have already used to not use them again.
        _alternativeGoalPosition = Vector2.Zero;
        bool violationFound = false;
        foreach (Vector2 pathPosition in  pathToGoal.TargetPositions)
        {
            if (!IsPositionVisibleByThreat(pathPosition)) continue;
            violationFound = true;
            _alternativeGoalPosition = GetNearestHidingPosition(
                pathPosition,
                GetUnusedHidingPoints());
            RegisterUsedHidingPoint(_alternativeGoalPosition);
            break;
        }
        
        if (!violationFound) return goal;

        PipelineGoal hiddenGoal = goal.GetGoalCopy();
        hiddenGoal.Position = _alternativeGoalPosition;
        return hiddenGoal;
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        LoadChildrenReferences();

        List<string> warnings = new();
        
        if (_hidingPointsDetector == null)
        {
            warnings.Add("This node needs a child of type HidingPointsDetector to work.");
        }
        // if (_navigationAgent2D == null)
        // {
        //     warnings.Add("This node needs a child that complies with the " +
        //                  "INavigationAgent interface to work.");
        // }
        if (_rayCast2D == null)
        {
            warnings.Add("This node needs a child of type RayCast2D to work.");
        }
        
        return warnings.ToArray();
    }
}