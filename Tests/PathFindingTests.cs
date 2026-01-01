using System.Threading.Tasks;
using GdUnit4;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Pathfinding;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;
using GodotGameAIbyExample.Scripts.Tools;
using static GdUnit4.Assertions;

namespace GodotGameAIbyExample.Tests;

[TestSuite, RequireGodotRuntime]
public class PathFindingTests
{
    private const string TestScenePath = "res://Tests/TestLevels/" +
                                         "PathFindingTestLevel.tscn";

    private ISceneRunner _sceneRunner;
    
    [BeforeTest]
    public void LoadScene()
    {
        _sceneRunner = ISceneRunner.Load(TestScenePath, autoFree: true);
        _sceneRunner.MaximizeView();
    }

    [AfterTest]
    public void DestroyScene()
    {
        _sceneRunner.Dispose();
    }
    
    /// <summary>
    /// Test the path following behavior.
    /// </summary>
    [TestCase]
    public async Task PathFollowingBehaviorTest()
    {
        // Get references to agent and target.
        MovingAgent pathFollowingAgent = 
            (MovingAgent) _sceneRunner.FindChild("PathFollowingMovingAgent");
        Path pathToFollow = 
            (Path) _sceneRunner.FindChild("TestPath");
        
        Marker2D position1 = 
            (Marker2D) _sceneRunner.FindChild("Position1");
        
        // Get references to behaviors.
        PathFollowingSteeringBehavior pathFollowingSteeringBehavior = 
            pathFollowingAgent.FindChild<PathFollowingSteeringBehavior>();
        
        // Set up elements before the test.
        pathFollowingAgent.GlobalPosition = position1.GlobalPosition;
        pathFollowingAgent.MaximumSpeed = 400.0f;
        pathFollowingAgent.StopSpeed = 1f;
        pathFollowingAgent.MaximumRotationalDegSpeed = 1080f;
        pathFollowingAgent.StopRotationDegThreshold = 1f;
        pathFollowingAgent.AgentColor = new Color(0, 1, 0);
        pathFollowingSteeringBehavior.FollowPath= pathToFollow;
        pathToFollow.Loop = false;
        pathFollowingAgent.Visible = true;
        pathToFollow.Visible = true;
        pathFollowingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Start test.
        
        // Assert that the path following agent can reach every target position in the
        // path.
        uint maximumWaitTime = 10000;
        uint waitStep = 1;
        foreach (Vector2 targetPosition in pathToFollow.TargetPositions)
        {
            bool targetReached = false;
            uint elapsedTime = 0;
            while (elapsedTime < maximumWaitTime)
            {
                await _sceneRunner.AwaitMillis(waitStep);
                elapsedTime += waitStep;
                targetReached = pathFollowingAgent.GlobalPosition.DistanceTo(targetPosition) < 30f;
                if (targetReached) break;
            }
            AssertThat(targetReached).IsTrue();
        }
        
        // Cleanup.
        pathToFollow.Visible = false;
        pathFollowingAgent.Visible = false;
        pathFollowingAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
    }
    
    /// <summary>
    /// Test the path following behavior loop feature.
    /// </summary>
    [TestCase]
    public async Task LoopPathFollowingBehaviorTest()
    {
        // Get references to agent and target.
        MovingAgent pathFollowingAgent = 
            (MovingAgent) _sceneRunner.FindChild("PathFollowingMovingAgent");
        Path pathToFollow = 
            (Path) _sceneRunner.FindChild("TestPath");
        
        Marker2D position1 = 
            (Marker2D) _sceneRunner.FindChild("Position1");
        
        // Get references to behaviors.
        PathFollowingSteeringBehavior pathFollowingSteeringBehavior = 
            pathFollowingAgent.FindChild<PathFollowingSteeringBehavior>();
        
        // Set up elements before the test.
        pathFollowingAgent.GlobalPosition = position1.GlobalPosition;
        pathFollowingAgent.MaximumSpeed = 400.0f;
        pathFollowingAgent.StopSpeed = 1f;
        pathFollowingAgent.MaximumRotationalDegSpeed = 1080f;
        pathFollowingAgent.StopRotationDegThreshold = 1f;
        pathFollowingAgent.AgentColor = new Color(0, 1, 0);
        pathFollowingSteeringBehavior.FollowPath= pathToFollow;
        pathToFollow.Loop = true;
        pathToFollow.Visible = true;
        pathFollowingAgent.Visible = true;
        pathFollowingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Start test.
        
        // Assert that the path following agent reach twice the first target position
        // in the path.
        uint maximumWaitTime = 10000;
        uint waitStep = 1;
        uint timesReached = 0;
        Vector2 firstTargetPosition = pathToFollow.TargetPositions[0];
        
        bool targetAlreadyReached = false;
        uint elapsedTime = 0;
        while (elapsedTime < maximumWaitTime)
        {
            await _sceneRunner.AwaitMillis(waitStep);
            elapsedTime += waitStep;
            if (pathFollowingAgent.GlobalPosition.DistanceTo(firstTargetPosition) < 30f)
            {
                // We want to increment the counter just once every time we get near
                // the target.
                if (!targetAlreadyReached)
                {
                    targetAlreadyReached = true;
                    timesReached++;
                }
            }
            else
            {
                targetAlreadyReached = false;
            }

            if (timesReached >= 2) break;
        }
        AssertThat(timesReached > 1).IsTrue();
        
        // Cleanup.
        pathToFollow.Visible = false;
        pathFollowingAgent.Visible = false;
        pathFollowingAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
    }
    
    /// <summary>
    /// Test the Dijkstra pathfinder behavior.
    /// </summary>
    [TestCase]
    public async Task DijkstraPathFindingBehaviorTest()
    {
        // Get references to agent and target.
        MovingAgent dijkstraPathfindingAgent = 
            (MovingAgent) _sceneRunner.FindChild("DijkstraPathFinderMovingAgent");
        
        Marker2D position1 = 
            (Marker2D) _sceneRunner.FindChild("Position1");
        Marker2D position2 = 
            (Marker2D) _sceneRunner.FindChild("Position2");
        Marker2D position3 = 
            (Marker2D) _sceneRunner.FindChild("Position3");
        
        Target target = (Target) _sceneRunner.FindChild("Target");
        
        Path pathToFollow = 
            (Path) _sceneRunner.FindChild("TestPath");
        
        // Get references to behaviors.
        PathFinderSteeringBehavior pathFinderSteeringBehavior = 
            dijkstraPathfindingAgent.FindChild<PathFinderSteeringBehavior>();
        
        // Set up elements before the test.
        pathToFollow.Visible = false;
        dijkstraPathfindingAgent.GlobalPosition = position1.GlobalPosition;
        dijkstraPathfindingAgent.MaximumSpeed = 400.0f;
        dijkstraPathfindingAgent.StopSpeed = 1f;
        dijkstraPathfindingAgent.MaximumRotationalDegSpeed = 1080f;
        dijkstraPathfindingAgent.StopRotationDegThreshold = 1f;
        dijkstraPathfindingAgent.AgentColor = new Color(0, 1, 0);
        pathFinderSteeringBehavior.PathTarget = target;
        dijkstraPathfindingAgent.Visible = true;
        dijkstraPathfindingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Start test.
        
        // Assert that the pathfinder agent can reach the first target.
        target.GlobalPosition = position2.GlobalPosition;
        await _sceneRunner.AwaitMillis(7000);
        AssertThat(
            dijkstraPathfindingAgent.GlobalPosition.DistanceTo(target.GlobalPosition) < 30f
            ).IsTrue();
        
        // Assert that the pathfinder agent can reach the second target.
        target.GlobalPosition = position3.GlobalPosition;
        await _sceneRunner.AwaitMillis(6000);
        AssertThat(
            dijkstraPathfindingAgent.GlobalPosition.DistanceTo(target.GlobalPosition) < 30f
        ).IsTrue();
        
        // Cleanup.
        dijkstraPathfindingAgent.Visible = false;
        dijkstraPathfindingAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
    }
    
    /// <summary>
    /// Test the AStar pathfinder behavior.
    /// </summary>
    [TestCase]
    public async Task AStarPathFindingBehaviorTest()
    {
        // Get references to agent and target.
        MovingAgent aStarPathfindingAgent = 
            (MovingAgent) _sceneRunner.FindChild("AStarPathFinderMovingAgent");
        
        Marker2D position1 = 
            (Marker2D) _sceneRunner.FindChild("Position1");
        Marker2D position2 = 
            (Marker2D) _sceneRunner.FindChild("Position2");
        Marker2D position3 = 
            (Marker2D) _sceneRunner.FindChild("Position3");
        
        Target target = (Target) _sceneRunner.FindChild("Target");
        
        Path pathToFollow = 
            (Path) _sceneRunner.FindChild("TestPath");
        
        // Get references to behaviors.
        PathFinderSteeringBehavior pathFinderSteeringBehavior = 
            aStarPathfindingAgent.FindChild<PathFinderSteeringBehavior>();
        
        // Set up elements before the test.
        pathToFollow.Visible = false;
        aStarPathfindingAgent.GlobalPosition = position1.GlobalPosition;
        aStarPathfindingAgent.MaximumSpeed = 400.0f;
        aStarPathfindingAgent.StopSpeed = 1f;
        aStarPathfindingAgent.MaximumRotationalDegSpeed = 1080f;
        aStarPathfindingAgent.StopRotationDegThreshold = 1f;
        aStarPathfindingAgent.AgentColor = new Color(0, 1, 0);
        pathFinderSteeringBehavior.PathTarget = target;
        aStarPathfindingAgent.Visible = true;
        aStarPathfindingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Start test.
        
        // Assert that the pathfinder agent can reach the first target.
        target.GlobalPosition = position2.GlobalPosition;
        await _sceneRunner.AwaitMillis(7000);
        AssertThat(
            aStarPathfindingAgent.GlobalPosition.DistanceTo(target.GlobalPosition) < 30f
            ).IsTrue();
        
        // Assert that the pathfinder agent can reach the second target.
        target.GlobalPosition = position3.GlobalPosition;
        await _sceneRunner.AwaitMillis(6000);
        AssertThat(
            aStarPathfindingAgent.GlobalPosition.DistanceTo(target.GlobalPosition) < 30f
        ).IsTrue();
        
        // Cleanup.
        aStarPathfindingAgent.Visible = false;
        aStarPathfindingAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
    }
    
    
    /// <summary>
    /// Test the Smoothed AStar pathfinder behavior.
    /// </summary>
    [TestCase]
    public async Task SmoothedAStarPathFindingBehaviorTest()
    {
        // Get references to agent and target.
        MovingAgent smoothedAStarPathfindingAgent = 
            (MovingAgent) _sceneRunner.FindChild("SmoothedAStarPathFinderMovingAgent");
        
        Marker2D position1 = 
            (Marker2D) _sceneRunner.FindChild("Position1");
        Marker2D position2 = 
            (Marker2D) _sceneRunner.FindChild("Position2");
        Marker2D position3 = 
            (Marker2D) _sceneRunner.FindChild("Position3");
        
        Target target = (Target) _sceneRunner.FindChild("Target");
        
        Path pathToFollow = 
            (Path) _sceneRunner.FindChild("TestPath");
        
        // Get references to behaviors.
        PathFinderSteeringBehavior pathFinderSteeringBehavior = 
            smoothedAStarPathfindingAgent.FindChild<PathFinderSteeringBehavior>();
        
        // Set up elements before the test.
        pathToFollow.Visible = false;
        smoothedAStarPathfindingAgent.GlobalPosition = position1.GlobalPosition;
        smoothedAStarPathfindingAgent.MaximumSpeed = 400.0f;
        smoothedAStarPathfindingAgent.StopSpeed = 1f;
        smoothedAStarPathfindingAgent.MaximumRotationalDegSpeed = 1080f;
        smoothedAStarPathfindingAgent.StopRotationDegThreshold = 1f;
        smoothedAStarPathfindingAgent.AgentColor = new Color(0, 1, 0);
        pathFinderSteeringBehavior.PathTarget = target;
        smoothedAStarPathfindingAgent.Visible = true;
        smoothedAStarPathfindingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Start test.
        
        // Assert that the pathfinder agent can reach the first target.
        target.GlobalPosition = position2.GlobalPosition;
        await _sceneRunner.AwaitMillis(7000);
        AssertThat(
            smoothedAStarPathfindingAgent.GlobalPosition.DistanceTo(target.GlobalPosition) < 30f
            ).IsTrue();
        
        // Assert that the pathfinder agent can reach the second target.
        target.GlobalPosition = position3.GlobalPosition;
        await _sceneRunner.AwaitMillis(6000);
        AssertThat(
            smoothedAStarPathfindingAgent.GlobalPosition.DistanceTo(target.GlobalPosition) < 30f
        ).IsTrue();
        
        // Cleanup.
        smoothedAStarPathfindingAgent.Visible = false;
        smoothedAStarPathfindingAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
    }
}