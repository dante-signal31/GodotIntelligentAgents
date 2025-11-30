using System.Threading.Tasks;
using GdUnit4;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Pathfinding;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;
using static GdUnit4.Assertions;

namespace GodotGameAIbyExample.Tests;

[TestSuite, RequireGodotRuntime]
public class PathFollowingTests
{
    private const string TestScenePath = "res://Tests/TestLevels/" +
                                         "ObstacleTestLevel.tscn";

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
        
        Marker2D position7 = 
            (Marker2D) _sceneRunner.FindChild("Position7");
        
        // Get references to behaviors.
        PathFollowingSteeringBehavior pathFollowingSteeringBehavior = 
            pathFollowingAgent.FindChild<PathFollowingSteeringBehavior>();
        
        // Set up elements before the test.
        pathFollowingAgent.GlobalPosition = position7.GlobalPosition;
        pathFollowingAgent.MaximumSpeed = 400.0f;
        pathFollowingAgent.StopSpeed = 1f;
        pathFollowingAgent.MaximumRotationalDegSpeed = 1080f;
        pathFollowingAgent.StopRotationDegThreshold = 1f;
        pathFollowingAgent.AgentColor = new Color(1, 0, 0);
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
        
        Marker2D position7 = 
            (Marker2D) _sceneRunner.FindChild("Position7");
        
        // Get references to behaviors.
        PathFollowingSteeringBehavior pathFollowingSteeringBehavior = 
            pathFollowingAgent.FindChild<PathFollowingSteeringBehavior>();
        
        // Set up elements before the test.
        pathFollowingAgent.GlobalPosition = position7.GlobalPosition;
        pathFollowingAgent.MaximumSpeed = 400.0f;
        pathFollowingAgent.StopSpeed = 1f;
        pathFollowingAgent.MaximumRotationalDegSpeed = 1080f;
        pathFollowingAgent.StopRotationDegThreshold = 1f;
        pathFollowingAgent.AgentColor = new Color(1, 0, 0);
        pathFollowingSteeringBehavior.FollowPath= pathToFollow;
        pathToFollow.Loop = true;
        pathToFollow.Visible = true;
        pathFollowingAgent.Visible = true;
        pathFollowingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Start test.
        
        // Assert that the path following agent reach twice the firs target position
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
}