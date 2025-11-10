using System.Threading.Tasks;
using GdUnit4;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;
using static GdUnit4.Assertions;

namespace GodotGameAIbyExample.Tests;

[TestSuite, RequireGodotRuntime]
public class ObstacleBehaviorTests
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
    /// Test that HideBehavior hides from a moving SeekBehavior.
    /// </summary>
    [TestCase]
    public async Task HideBehaviorTest()
    {
        // Get references to agent and target.
        Scripts.SteeringBehaviors.MovingAgent seekAgent = 
            (Scripts.SteeringBehaviors.MovingAgent) _sceneRunner.FindChild("SeekMovingAgent");
        Scripts.SteeringBehaviors.MovingAgent hideAgent = 
            (Scripts.SteeringBehaviors.MovingAgent) _sceneRunner.FindChild("HideMovingAgent");
        Target target = (Target) _sceneRunner.FindChild("Target");
        Marker2D position1 = 
            (Marker2D) _sceneRunner.FindChild("Position1");
        Marker2D position2 = 
            (Marker2D) _sceneRunner.FindChild("Position2");
        Marker2D position3 = 
            (Marker2D) _sceneRunner.FindChild("Position3");
        Marker2D position4 = 
            (Marker2D) _sceneRunner.FindChild("Position4");
        
        // Get references to behaviors.
        SeekSteeringBehavior seekSteeringBehavior = 
            seekAgent.FindChild<SeekSteeringBehavior>();
        HideSteeringBehavior hideSteeringBehavior =
            hideAgent.FindChild<HideSteeringBehavior>();
        
        // Setup agents before the test.
        target.GlobalPosition = position2.GlobalPosition;
        seekAgent.GlobalPosition = position1.GlobalPosition;
        seekAgent.MaximumSpeed = 600.0f;
        seekAgent.StopSpeed = 1f;
        seekAgent.MaximumRotationalDegSpeed = 1080f;
        seekAgent.StopRotationDegThreshold = 1f;
        seekAgent.AgentColor = new Color(1, 0, 0);
        seekSteeringBehavior.Target = target;
        seekSteeringBehavior.ArrivalDistance = 3f;
        seekAgent.Visible = true;
        seekAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        hideAgent.GlobalPosition = position2.GlobalPosition;
        hideAgent.MaximumSpeed = 600.0f;
        hideAgent.StopSpeed = 1f;
        hideAgent.MaximumRotationalDegSpeed = 1080f;
        hideAgent.StopRotationDegThreshold = 1f;
        hideAgent.AgentColor = new Color(0, 1, 0);
        hideSteeringBehavior.Threat = seekAgent;
        hideSteeringBehavior.ArrivalDistance = 3f;
        // Layer 4, only obstacles.
        hideSteeringBehavior.ObstaclesLayers = 8;
        hideSteeringBehavior.SeparationFromObstacles = 30f;
        hideSteeringBehavior.AgentRadius = 50f;
        // Layer 4, only obstacles.
        hideSteeringBehavior.NotEmptyGroundLayers = 8;
        hideSteeringBehavior.ShowGizmos = true;
        hideAgent.Visible = true;
        hideAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Start test.
        
        // Assert that seek agent can see hide agent.
        await _sceneRunner.AwaitIdleFrame();
        AssertThat(hideSteeringBehavior.VisibleByThreat).IsTrue();
        
        // Give hide agent time to hide.
        await _sceneRunner.AwaitMillis(2000);
        
        // Assert that seek agent can no longer see hide agent.
        AssertThat(hideSteeringBehavior.VisibleByThreat).IsFalse();
        
        // Move seek agent to another position.
        target.GlobalPosition = position3.GlobalPosition;
        
        // Give agent time to hide again.
        await _sceneRunner.AwaitMillis(2000);
        
        // Assert that seek agent can no longer see hide agent.
        AssertThat(hideSteeringBehavior.VisibleByThreat).IsFalse();
        
        // Move seek agent to another position.
        target.GlobalPosition = position4.GlobalPosition;
        
        // Give agent time to hide again.
        await _sceneRunner.AwaitMillis(2000);
        
        // Assert that seek agent can no longer see hide agent.
        AssertThat(hideSteeringBehavior.VisibleByThreat).IsFalse();
        
        // Cleanup.
        seekAgent.Visible = false;
        seekAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
        hideAgent.Visible = false;
        hideAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
    }
    
    /// <summary>
    /// Test that WallAvoiderBehavior can move an agent from a point A to a point B
    /// avoiding obstacles.
    /// </summary>
    [TestCase]
    public async Task WallAvoiderBehaviorTest()
    {
        // Get references to agent and target.
        Scripts.SteeringBehaviors.MovingAgent wallAvoiderAgent = 
            (Scripts.SteeringBehaviors.MovingAgent) _sceneRunner.FindChild("WallAvoiderMovingAgent");
        Target target = (Target) _sceneRunner.FindChild("Target");
        Marker2D position1 = 
            (Marker2D) _sceneRunner.FindChild("Position2");
        Marker2D position5 = 
            (Marker2D) _sceneRunner.FindChild("Position5");
        
        // Get references to behaviors.
        ActiveWallAvoiderSteeringBehavior wallAvoiderSteeringBehavior = 
            wallAvoiderAgent.FindChild<ActiveWallAvoiderSteeringBehavior>();
        
        // Setup agents before the test.
        target.GlobalPosition = position5.GlobalPosition;
        wallAvoiderAgent.GlobalPosition = position1.GlobalPosition;
        wallAvoiderAgent.MaximumSpeed = 200.0f;
        wallAvoiderAgent.StopSpeed = 1f;
        wallAvoiderAgent.MaximumRotationalDegSpeed = 1080f;
        wallAvoiderAgent.StopRotationDegThreshold = 1f;
        wallAvoiderAgent.AgentColor = new Color(1, 0, 0);
        wallAvoiderSteeringBehavior.Target = target;
        wallAvoiderAgent.Visible = true;
        wallAvoiderAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Start test.
        
        // Give hide agent time to reach target.
        await _sceneRunner.AwaitMillis(10000);
        
        // Assert that wall avoider has reached target.
        AssertThat(
            wallAvoiderAgent.GlobalPosition.DistanceTo(target.GlobalPosition) <
            20.0f).IsTrue();
        
        // Cleanup.
        wallAvoiderAgent.Visible = false;
        wallAvoiderAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
    }
    
    /// <summary>
    /// Test that WallAvoiderBehavior can move an agent from a point A to a point B
    /// avoiding obstacles and using auto-smoothed movement.
    /// </summary>
    [TestCase]
    public async Task AutoSmoothedWallAvoiderBehaviorTest()
    {
        // Get references to agent and target.
        Scripts.SteeringBehaviors.MovingAgent wallAvoiderAgent = 
            (Scripts.SteeringBehaviors.MovingAgent) _sceneRunner.FindChild("WallAvoiderMovingAgent");
        Target target = (Target) _sceneRunner.FindChild("Target");
        Marker2D position1 = 
            (Marker2D) _sceneRunner.FindChild("Position2");
        Marker2D position5 = 
            (Marker2D) _sceneRunner.FindChild("Position5");
        
        // Get references to behaviors.
        ActiveWallAvoiderSteeringBehavior wallAvoiderSteeringBehavior = 
            wallAvoiderAgent.FindChild<ActiveWallAvoiderSteeringBehavior>();
        
        // Setup agents before the test.
        target.GlobalPosition = position5.GlobalPosition;
        wallAvoiderAgent.GlobalPosition = position1.GlobalPosition;
        wallAvoiderAgent.MaximumSpeed = 200.0f;
        wallAvoiderAgent.StopSpeed = 1f;
        wallAvoiderAgent.MaximumRotationalDegSpeed = 1080f;
        wallAvoiderAgent.StopRotationDegThreshold = 1f;
        wallAvoiderAgent.AutoSmooth = true;
        wallAvoiderAgent.AutoSmoothSamples = 10;
        wallAvoiderAgent.AgentColor = new Color(1, 0, 0);
        wallAvoiderSteeringBehavior.Target = target;
        wallAvoiderAgent.Visible = true;
        wallAvoiderAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Start test.
        
        // Give hide agent time to reach target.
        await _sceneRunner.AwaitMillis(10000);
        
        // Assert that wall avoider has reached target.
        AssertThat(
            wallAvoiderAgent.GlobalPosition.DistanceTo(target.GlobalPosition) <
            20.0f).IsTrue();
        
        // Cleanup.
        wallAvoiderAgent.Visible = false;
        wallAvoiderAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
    }
    
    /// <summary>
    /// Test that SmoothedWallAvoiderBehavior can move an agent from a point A to a point B
    /// avoiding obstacles.
    /// </summary>
    [TestCase]
    public async Task SmoothedWallAvoiderBehaviorTest()
    {
        // Get references to agent and target.
        Scripts.SteeringBehaviors.MovingAgent smoothedWallAvoiderAgent = 
            (Scripts.SteeringBehaviors.MovingAgent) _sceneRunner.FindChild("SmoothedWallAvoiderMovingAgent");
        Target target = (Target) _sceneRunner.FindChild("Target");
        Marker2D position1 = 
            (Marker2D) _sceneRunner.FindChild("Position1");
        Marker2D position5 = 
            (Marker2D) _sceneRunner.FindChild("Position5");
        
        // Get references to behaviors.
        SmoothedWallAvoiderSteeringBehavior smoothedWallAvoiderSteeringBehavior = 
            smoothedWallAvoiderAgent.FindChild<SmoothedWallAvoiderSteeringBehavior>();
        
        // Setup agents before the test.
        target.GlobalPosition = position5.GlobalPosition;
        smoothedWallAvoiderAgent.GlobalPosition = position1.GlobalPosition;
        smoothedWallAvoiderAgent.MaximumSpeed = 100.0f;
        smoothedWallAvoiderAgent.StopSpeed = 1f;
        smoothedWallAvoiderAgent.MaximumRotationalDegSpeed = 1080f;
        smoothedWallAvoiderAgent.StopRotationDegThreshold = 1f;
        smoothedWallAvoiderAgent.AgentColor = new Color(1, 0, 0);
        smoothedWallAvoiderAgent.Visible = true;
        smoothedWallAvoiderSteeringBehavior.Target = target;
        smoothedWallAvoiderSteeringBehavior.ShowGizmos = true;
        smoothedWallAvoiderAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Start test.
        
        // Give hide agent time to reach target.
        await _sceneRunner.AwaitMillis(23000);
        
        // Assert that wall avoider has reached target.
        AssertThat(
            smoothedWallAvoiderAgent.GlobalPosition.DistanceTo(target.GlobalPosition) <
            50.0f).IsTrue();
        
        // Cleanup.
        smoothedWallAvoiderSteeringBehavior.ShowGizmos = false;
        await _sceneRunner.AwaitIdleFrame();
        smoothedWallAvoiderAgent.Visible = false;
        smoothedWallAvoiderAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
    }
    
    /// <summary>
    /// Test that WeightBlendedHideWallAvoiderBehavior can hide from a moving SeekBehavior
    /// and reach its final destination.
    /// </summary>
    // I don't know why this test works if run alone, but fails if batch executes with
    // every other test.
    [TestCase]
    public async Task WeightBlendedHideWallAvoiderBehaviorTest()
    {
        // Get references to agent and target.
        Scripts.SteeringBehaviors.MovingAgent seekAgent = 
            (Scripts.SteeringBehaviors.MovingAgent) _sceneRunner.FindChild("SeekMovingAgent");
        Scripts.SteeringBehaviors.MovingAgent hideAgent = 
            (Scripts.SteeringBehaviors.MovingAgent) _sceneRunner.FindChild("WeightBlendedHideWallAvoiderMovingAgent");
        Target target = (Target) _sceneRunner.FindChild("Target");
        Marker2D position1 = 
            (Marker2D) _sceneRunner.FindChild("Position1");
        Marker2D position2 = 
            (Marker2D) _sceneRunner.FindChild("Position2");
        Marker2D position3 = 
            (Marker2D) _sceneRunner.FindChild("Position3");
        Marker2D position4 = 
            (Marker2D) _sceneRunner.FindChild("Position4");
        
        // Get references to behaviors.
        SeekSteeringBehavior seekSteeringBehavior = 
            seekAgent.FindChild<SeekSteeringBehavior>();
        HideSteeringBehavior hideSteeringBehavior =
            hideAgent.FindChild<HideSteeringBehavior>(recursive: true);
        ActiveWallAvoiderSteeringBehavior wallAvoiderSteeringBehavior = 
            hideAgent.FindChild<ActiveWallAvoiderSteeringBehavior>(recursive: true);
        
        // Setup agents before the test.
        target.GlobalPosition = position2.GlobalPosition;
        seekAgent.GlobalPosition = position1.GlobalPosition;
        seekAgent.MaximumSpeed = 200.0f;
        seekAgent.StopSpeed = 1f;
        seekAgent.MaximumRotationalDegSpeed = 1080f;
        seekAgent.StopRotationDegThreshold = 1f;
        seekAgent.AgentColor = new Color(1, 0, 0);
        seekSteeringBehavior.Target = target;
        seekSteeringBehavior.ArrivalDistance = 3f;
        seekAgent.Visible = true;
        seekAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        hideAgent.GlobalPosition = position3.GlobalPosition;
        hideAgent.MaximumSpeed = 150.0f;
        hideAgent.StopSpeed = 1f;
        hideAgent.MaximumRotationalDegSpeed = 1080f;
        hideAgent.StopRotationDegThreshold = 1f;
        hideAgent.AgentColor = new Color(0, 1, 0);
        hideSteeringBehavior.Threat = seekAgent;
        hideSteeringBehavior.ArrivalDistance = 3f;
        // Layer 4, only agents.
        hideSteeringBehavior.ObstaclesLayers = 8;
        hideSteeringBehavior.SeparationFromObstacles = 30f;
        hideSteeringBehavior.AgentRadius = 50f;
        // Layer 4, only obstacles.
        hideSteeringBehavior.NotEmptyGroundLayers = 8;
        hideSteeringBehavior.ShowGizmos = true;
        hideAgent.Visible = true;
        hideAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Set HiderWallAvoider agent its final destination.
        wallAvoiderSteeringBehavior.Target = position2;
        
        // Start test.
        
        // Assert that seek agent can see hide agent.
        await _sceneRunner.AwaitMillis(200);
        AssertThat(hideSteeringBehavior.VisibleByThreat).IsTrue();
        
        // Give hide agent time to hide.
        await _sceneRunner.AwaitMillis(3000);
        
        // Assert that seek agent can no longer see hide agent.
        AssertThat(hideSteeringBehavior.VisibleByThreat).IsFalse();
        
        // Move seek agent to another position.
        target.GlobalPosition = position3.GlobalPosition;
        
        // Give agent time to hide again.
        await _sceneRunner.AwaitMillis(5000);
        
        // Assert that seek agent can no longer see hide agent.
        AssertThat(hideSteeringBehavior.VisibleByThreat).IsFalse();
        
        // Move seek agent to another position.
        target.GlobalPosition = position4.GlobalPosition;
        
        // Give agent time to hide again.
        await _sceneRunner.AwaitMillis(3000);
        
        // Assert that seek agent can no longer see hide agent.
        AssertThat(hideSteeringBehavior.VisibleByThreat).IsFalse();
        
        // Give agent time to reach its final destination.
        await _sceneRunner.AwaitMillis(2000);
        AssertThat(
            hideAgent.GlobalPosition.DistanceTo(position2.GlobalPosition) <
            50.0f).IsTrue();
        
        // Cleanup.
        seekAgent.Visible = false;
        seekAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
        hideAgent.Visible = false;
        hideAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
    }
    
    /// <summary>
    /// Test that PriorityWeightBlendedHideWallAvoiderBehavior can hide from a moving SeekBehavior
    /// and reach its final destination.
    /// </summary>
    // I don't know why this test works if run alone, but fails if batch executes with
    // every other test.
    [TestCase]
    public async Task PriorityWeightBlendedHideWallAvoiderBehaviorTest()
    {
        // Get references to agent and target.
        Scripts.SteeringBehaviors.MovingAgent seekAgent = 
            (Scripts.SteeringBehaviors.MovingAgent) _sceneRunner.FindChild("SeekMovingAgent");
        Scripts.SteeringBehaviors.MovingAgent hideAgent = 
            (Scripts.SteeringBehaviors.MovingAgent) _sceneRunner.FindChild("PriorityWeightBlendedHideWallAvoiderMovingAgent");
        Target target = (Target) _sceneRunner.FindChild("Target");
        Marker2D position1 = 
            (Marker2D) _sceneRunner.FindChild("Position1");
        Marker2D position2 = 
            (Marker2D) _sceneRunner.FindChild("Position2");
        Marker2D position3 = 
            (Marker2D) _sceneRunner.FindChild("Position3");
        Marker2D position4 = 
            (Marker2D) _sceneRunner.FindChild("Position4");
        
        // Get references to behaviors.
        SeekSteeringBehavior seekSteeringBehavior = 
            seekAgent.FindChild<SeekSteeringBehavior>();
        HideSteeringBehavior hideSteeringBehavior =
            hideAgent.FindChild<HideSteeringBehavior>(recursive: true);
        ArriveSteeringBehaviorLA arriveSteeringBehavior = 
            hideAgent.FindChild<ArriveSteeringBehaviorLA>(recursive: true);
        
        // Setup agents before the test.
        target.GlobalPosition = position2.GlobalPosition;
        seekAgent.GlobalPosition = position1.GlobalPosition;
        seekAgent.MaximumSpeed = 200.0f;
        seekAgent.StopSpeed = 1f;
        seekAgent.MaximumRotationalDegSpeed = 1080f;
        seekAgent.StopRotationDegThreshold = 1f;
        seekAgent.AgentColor = new Color(1, 0, 0);
        seekSteeringBehavior.Target = target;
        seekSteeringBehavior.ArrivalDistance = 3f;
        seekAgent.Visible = true;
        seekAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        hideAgent.GlobalPosition = position3.GlobalPosition;
        hideAgent.MaximumSpeed = 150.0f;
        hideAgent.StopSpeed = 1f;
        hideAgent.MaximumRotationalDegSpeed = 1080f;
        hideAgent.StopRotationDegThreshold = 1f;
        hideAgent.AgentColor = new Color(0, 1, 0);
        hideSteeringBehavior.Threat = seekAgent;
        hideSteeringBehavior.ArrivalDistance = 3f;
        // Layer 4, only obstacles.
        hideSteeringBehavior.ObstaclesLayers = 8;
        hideSteeringBehavior.SeparationFromObstacles = 30f;
        hideSteeringBehavior.AgentRadius = 50f;
        // Layer 4, only obstacles.
        hideSteeringBehavior.NotEmptyGroundLayers = 8;
        hideSteeringBehavior.ShowGizmos = true;
        hideAgent.Visible = true;
        hideAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Set HiderWallAvoider agent its final destination.
        arriveSteeringBehavior.Target = position2;
        
        // Start test.
        
        // Assert that seek agent can see hide agent.
        await _sceneRunner.AwaitMillis(200);
        AssertThat(hideSteeringBehavior.VisibleByThreat).IsTrue();
        
        // Give hide agent time to hide.
        await _sceneRunner.AwaitMillis(3000);
        
        // Assert that seek agent can no longer see hide agent.
        AssertThat(hideSteeringBehavior.VisibleByThreat).IsFalse();
        
        // Move seek agent to another position.
        target.GlobalPosition = position3.GlobalPosition;
        
        // Give agent time to hide again.
        await _sceneRunner.AwaitMillis(5000);
        
        // Assert that seek agent can no longer see hide agent.
        AssertThat(hideSteeringBehavior.VisibleByThreat).IsFalse();
        
        // Move seek agent to another position.
        target.GlobalPosition = position4.GlobalPosition;
        
        // Give agent time to hide again.
        await _sceneRunner.AwaitMillis(5000);
        
        // Assert that seek agent can no longer see hide agent.
        AssertThat(hideSteeringBehavior.VisibleByThreat).IsFalse();
        
        // Give agent time to reach its final destination.
        await _sceneRunner.AwaitMillis(2000);
        AssertThat(
            hideAgent.GlobalPosition.DistanceTo(position2.GlobalPosition) <
            50.0f).IsTrue();
        
        // Cleanup.
        seekAgent.Visible = false;
        seekAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
        hideAgent.Visible = false;
        hideAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
    }
    
    /// <summary>
    /// Test that PriorityDitheringBlendedHideWallAvoiderBehavior can hide from a moving SeekBehavior
    /// and reach its final destination.
    /// </summary>
    // I don't know why this test works if run alone, but fails if batch executes with
    // every other test.
    [TestCase]
    public async Task PriorityDitheringBlendedHideWallAvoiderBehaviorTest()
    {
        // Get references to agent and target.
        Scripts.SteeringBehaviors.MovingAgent seekAgent = 
            (Scripts.SteeringBehaviors.MovingAgent) _sceneRunner.FindChild("SeekMovingAgent");
        Scripts.SteeringBehaviors.MovingAgent hideAgent = 
            (Scripts.SteeringBehaviors.MovingAgent) _sceneRunner.FindChild("PriorityDitheringBlendedHideWallAvoiderMovingAgent");
        Target target = (Target) _sceneRunner.FindChild("Target");
        Marker2D position1 = 
            (Marker2D) _sceneRunner.FindChild("Position1");
        Marker2D position2 = 
            (Marker2D) _sceneRunner.FindChild("Position2");
        Marker2D position3 = 
            (Marker2D) _sceneRunner.FindChild("Position3");
        Marker2D position4 = 
            (Marker2D) _sceneRunner.FindChild("Position4");
        
        // Get references to behaviors.
        SeekSteeringBehavior seekSteeringBehavior = 
            seekAgent.FindChild<SeekSteeringBehavior>();
        HideSteeringBehavior hideSteeringBehavior =
            hideAgent.FindChild<HideSteeringBehavior>(recursive: true);
        SeekSteeringBehavior hideSeekSteeringBehavior = 
            hideAgent.FindChild<SeekSteeringBehavior>(recursive: true);
        
        // Setup agents before the test.
        target.GlobalPosition = position2.GlobalPosition;
        seekAgent.GlobalPosition = position1.GlobalPosition;
        seekAgent.MaximumSpeed = 200.0f;
        seekAgent.StopSpeed = 1f;
        seekAgent.MaximumRotationalDegSpeed = 1080f;
        seekAgent.StopRotationDegThreshold = 1f;
        seekAgent.AgentColor = new Color(1, 0, 0);
        seekSteeringBehavior.Target = target;
        seekSteeringBehavior.ArrivalDistance = 3f;
        seekAgent.Visible = true;
        seekAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        hideAgent.GlobalPosition = position3.GlobalPosition;
        hideAgent.MaximumSpeed = 150.0f;
        hideAgent.StopSpeed = 1f;
        hideAgent.MaximumRotationalDegSpeed = 1080f;
        hideAgent.StopRotationDegThreshold = 1f;
        hideAgent.AgentColor = new Color(0, 1, 0);
        hideSteeringBehavior.Threat = seekAgent;
        hideSteeringBehavior.ArrivalDistance = 3f;
        // Layer 4, only obstacles.
        hideSteeringBehavior.ObstaclesLayers = 8;
        hideSteeringBehavior.SeparationFromObstacles = 30f;
        hideSteeringBehavior.AgentRadius = 50f;
        // Layer 4, only obstacles.
        hideSteeringBehavior.NotEmptyGroundLayers = 8;
        hideSteeringBehavior.ShowGizmos = true;
        hideAgent.Visible = true;
        hideAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Set HiderWallAvoider agent its final destination.
        hideSeekSteeringBehavior.Target = position2;
        
        // Start test.
        
        // Assert that seek agent can see hide agent.
        await _sceneRunner.AwaitMillis(200);
        AssertThat(hideSteeringBehavior.VisibleByThreat).IsTrue();
        
        // Give hide agent time to hide.
        await _sceneRunner.AwaitMillis(3000);
        
        // Assert that seek agent can no longer see hide agent.
        AssertThat(hideSteeringBehavior.VisibleByThreat).IsFalse();
        
        // Move seek agent to another position.
        target.GlobalPosition = position3.GlobalPosition;
        
        // Give agent time to hide again.
        await _sceneRunner.AwaitMillis(5000);
        
        // Assert that seek agent can no longer see hide agent.
        AssertThat(hideSteeringBehavior.VisibleByThreat).IsFalse();
        
        // Move seek agent to another position.
        target.GlobalPosition = position4.GlobalPosition;
        
        // Give agent time to hide again.
        await _sceneRunner.AwaitMillis(5000);
        
        // Assert that seek agent can no longer see hide agent.
        AssertThat(hideSteeringBehavior.VisibleByThreat).IsFalse();
        
        // Give agent time to reach its final destination.
        await _sceneRunner.AwaitMillis(5000);
        AssertThat(
            hideAgent.GlobalPosition.DistanceTo(position2.GlobalPosition) <
            50.0f).IsTrue();
        
        // Cleanup.
        seekAgent.Visible = false;
        seekAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
        hideAgent.Visible = false;
        hideAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
    }
}