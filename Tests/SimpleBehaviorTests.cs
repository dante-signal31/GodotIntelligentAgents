using System.Linq;
using System.Threading.Tasks;
using GdUnit4;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;
using GodotGameAIbyExample.Scripts.Tools;
using static GdUnit4.Assertions;

namespace GodotGameAIbyExample.Tests;

[TestSuite]
public class SimpleBehaviorTests
{
    private const string TestScenePath = "res://Tests/TestLevels/" +
                                         "SimpleBehaviorTestLevel.tscn";

    private ISceneRunner _sceneRunner;
    
    [BeforeTest]
    public void LoadScene()
    {
        _sceneRunner = ISceneRunner.Load(TestScenePath);
        _sceneRunner.MaximizeView();
    }
    
    /// <summary>
    /// Test that SeekBehavior can reach a target.
    /// </summary>
    [TestCase]
    public async Task SeekBehaviorTest()
    {
        // Get references to agent and target.
        MovingAgent movingAgent = 
            (MovingAgent) _sceneRunner.FindChild("SeekMovingAgent");
        Marker2D agentStartPosition = 
            (Marker2D) _sceneRunner.FindChild("Position1");
        Target target = (Target) _sceneRunner.FindChild("Target");
        Marker2D targetPosition = 
            (Marker2D) _sceneRunner.FindChild("Position3");
        
        // Get reference to SteeringBehaviour.
        SeekSteeringBehavior steeringBehavior = 
            (SeekSteeringBehavior) movingAgent.FindChild(
                nameof(SeekSteeringBehavior));
        
        // Setup agents before the test.
        target.GlobalPosition = targetPosition.GlobalPosition;
        movingAgent.GlobalPosition = agentStartPosition.GlobalPosition;
        movingAgent.MaximumSpeed = 600.0f;
        movingAgent.StopSpeed = 1f;
        movingAgent.MaximumRotationalDegSpeed = 1080f;
        movingAgent.StopRotationDegThreshold = 1f;
        movingAgent.AgentColor = new Color(0, 1, 0);
        movingAgent.Visible = true;
        movingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Start test.

        // Give agent time to reach target.
        await _sceneRunner.AwaitMillis(2500);
        // Check if agent reached target.
        float distance = movingAgent.GlobalPosition.DistanceTo(target.GlobalPosition);
        AssertThat(distance <= steeringBehavior.ArrivalDistance).IsTrue();
    }
    
    /// <summary>
    /// Test that ArriveBehaviorNLA can reach a target and that it accelerates
    /// at the beginning and brakes at the end.
    /// </summary>
    [TestCase]
    public async Task ArriveBehaviorNLATest()
    {
        // Get references to agent and target.
        MovingAgent movingAgent = 
            (MovingAgent) _sceneRunner.FindChild("ArriveMovingAgentNLA");
        Marker2D agentStartPosition = 
            (Marker2D) _sceneRunner.FindChild("Position1");
        Target target = (Target) _sceneRunner.FindChild("Target");
        Marker2D targetPosition = 
            (Marker2D) _sceneRunner.FindChild("Position3");
        
        // Get reference to ArriveSteeringBehaviour.
        ArriveSteeringBehaviorNLA steeringBehavior = 
            movingAgent.FindChild<ArriveSteeringBehaviorNLA>();
        
        // Setup target and agent.
        target.GlobalPosition = targetPosition.GlobalPosition;
        movingAgent.GlobalPosition = agentStartPosition.GlobalPosition;
        movingAgent.MaximumSpeed = 600.0f;
        movingAgent.StopSpeed = 1f;
        movingAgent.MaximumRotationalDegSpeed = 1080f;
        movingAgent.StopRotationDegThreshold = 1f;
        movingAgent.AgentColor = new Color(0, 1, 0);
        movingAgent.Visible = true;
        steeringBehavior.Target = target;
        steeringBehavior.AccelerationRadius = 100.0f;
        steeringBehavior.BrakingRadius = 100.0f;
        steeringBehavior.ArrivalDistance = 1f;
        movingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Check that agent is accelerating at the beginning.
        while ( // Wait until agent starts is movement.
               !((agentStartPosition.GlobalPosition.DistanceTo(
                     movingAgent.GlobalPosition) >= 1) && 
                 (agentStartPosition.GlobalPosition.DistanceTo(
                     movingAgent.GlobalPosition) < steeringBehavior.AccelerationRadius))
              )
        {
            await _sceneRunner.AwaitIdleFrame();
        }
        AssertThat(movingAgent.CurrentSpeed > 0 && 
                   movingAgent.CurrentSpeed < movingAgent.MaximumSpeed).IsTrue();
        
        // Check that agent gets its full cruise speed.
        while ( // Wait until we get full speed.
               !(agentStartPosition.GlobalPosition.DistanceTo(
                   movingAgent.GlobalPosition) > steeringBehavior.AccelerationRadius)
              )
        {
            await _sceneRunner.AwaitIdleFrame();
        }
        AssertThat(Mathf.IsEqualApprox(
                movingAgent.CurrentSpeed, 
                movingAgent.MaximumSpeed, 
                1f))
            .IsTrue();
        
        // Check that agent is braking at the end.
        while ( // Wait until we start to brake.
               !(movingAgent.GlobalPosition.DistanceTo(
                   targetPosition.GlobalPosition) <= steeringBehavior.BrakingRadius)
              )
        {
            await _sceneRunner.AwaitIdleFrame();
        }
        await _sceneRunner.AwaitIdleFrame();
        await _sceneRunner.AwaitIdleFrame();
        AssertThat(movingAgent.CurrentSpeed > 0 && 
                   movingAgent.CurrentSpeed < movingAgent.MaximumSpeed).IsTrue();
        
        //Assert target was reached.
        // Give agent time to reach target.
        await _sceneRunner.AwaitMillis(1500);
        // Check if agent reached target.
        float distance = movingAgent.GlobalPosition.DistanceTo(target.GlobalPosition);
        AssertThat(distance <= steeringBehavior.ArrivalDistance).IsTrue();
    }
    
    /// <summary>
    /// Test that ArriveBehaviorLA can reach a target and that it accelerates
    /// at the beginning and brakes at the end.
    /// </summary>
    [TestCase]
    public async Task ArriveBehaviorLATest()
    {
        // Get references to agent and target.
        MovingAgent movingAgent = 
            (MovingAgent) _sceneRunner.FindChild("ArriveMovingAgentLA");
        Marker2D agentStartPosition = 
            (Marker2D) _sceneRunner.FindChild("Position1");
        Target target = (Target) _sceneRunner.FindChild("Target");
        Marker2D targetPosition = 
            (Marker2D) _sceneRunner.FindChild("Position3");
        
        // Get reference to ArriveSteeringBehaviour.
        var steeringBehavior = movingAgent.FindChild<ArriveSteeringBehaviorLA>();
        
        // Setup target and agent.
        target.GlobalPosition = targetPosition.GlobalPosition;
        movingAgent.GlobalPosition = agentStartPosition.GlobalPosition;
        movingAgent.MaximumSpeed = 600.0f;
        movingAgent.StopSpeed = 1f;
        movingAgent.MaximumRotationalDegSpeed = 1080f;
        movingAgent.StopRotationDegThreshold = 1f;
        movingAgent.MaximumAcceleration = 400;
        movingAgent.MaximumDeceleration = 400;
        movingAgent.AgentColor = new Color(0, 1, 0);
        movingAgent.Visible = true;
        steeringBehavior.Target = target;
        steeringBehavior.ArrivalDistance = 1f;
        movingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Check that agent is accelerating at the beginning.
        while ( // Wait until agent starts is movement.
               !((agentStartPosition.GlobalPosition.DistanceTo(
                     movingAgent.GlobalPosition) >= 1))
              )
        {
            await _sceneRunner.AwaitIdleFrame();
        }
        AssertThat(movingAgent.CurrentSpeed > 0 && 
                   movingAgent.CurrentSpeed < movingAgent.MaximumSpeed).IsTrue();
        
        // Check that agent gets its full cruise speed.
        while (!Mathf.IsEqualApprox(movingAgent.CurrentSpeed, movingAgent.MaximumSpeed))
        { // Wait until we get full speed.
            GD.Print($"[SimpleBehaviorTests]CurrentSpeed: {movingAgent.CurrentSpeed} MaximumSpeed: {movingAgent.MaximumSpeed}");
            await _sceneRunner.AwaitIdleFrame();
        }
        AssertThat(Mathf.IsEqualApprox(
                movingAgent.CurrentSpeed, 
                movingAgent.MaximumSpeed, 
                1f))
            .IsTrue();
        
        // Check that agent is braking at the end.
        while (!(movingAgent.GlobalPosition.DistanceTo(
                   targetPosition.GlobalPosition) <= steeringBehavior.BrakingRadius))
        { // Wait until we start to brake.
            await _sceneRunner.AwaitIdleFrame();
        }
        await _sceneRunner.AwaitIdleFrame();
        await _sceneRunner.AwaitIdleFrame();
        AssertThat(movingAgent.CurrentSpeed > 0 && 
                   movingAgent.CurrentSpeed < movingAgent.MaximumSpeed).IsTrue();
        
        //Assert target was reached.
        // Give agent time to reach target.
        await _sceneRunner.AwaitMillis(1500);
        // Check if agent reached target.
        float distance = movingAgent.GlobalPosition.DistanceTo(target.GlobalPosition);
        AssertThat(distance <= steeringBehavior.ArrivalDistance).IsTrue();
    }

    /// <summary>
    /// Test that FleeBehavior makes agent go away from its threath.
    /// </summary>
    [TestCase]
    public async Task FleeBehaviorTest()
    {
        // Get references to agent and target.
        MovingAgent movingAgent =
            (MovingAgent)_sceneRunner.FindChild("FleeMovingAgent");
        Marker2D agentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position2");
        Target target = (Target)_sceneRunner.FindChild("Target");
        Marker2D targetPosition =
            (Marker2D)_sceneRunner.FindChild("Position2");

        // Get reference to FleeSteeringBehaviour.
        FleeSteeringBehavior steeringBehavior =
            (FleeSteeringBehavior)movingAgent.FindChild(
                nameof(FleeSteeringBehavior));

        // Setup target and agent.
        target.GlobalPosition = targetPosition.GlobalPosition;
        steeringBehavior.PanicDistance = 200.0f;
        steeringBehavior.Threath = target;
        movingAgent.GlobalPosition = agentStartPosition.GlobalPosition;
        movingAgent.MaximumSpeed = 600.0f;
        movingAgent.StopSpeed = 1f;
        movingAgent.MaximumRotationalDegSpeed = 1080f;
        movingAgent.StopRotationDegThreshold = 1f;
        movingAgent.AgentColor = new Color(0, 1, 0);
        movingAgent.Visible = true;
        movingAgent.ProcessMode = Node.ProcessModeEnum.Always;

        // Place 5 targets in random positions and check that the agent flees.
        int testSamples = 5;
        for (int i = 0; i < testSamples; i++)
        {
            Vector2 randomPositionInLocalCircle = 
                RandomPointGenerator.GetRandomPointInCircle(steeringBehavior.PanicDistance);
            // Place target in random position.
            target.GlobalPosition = movingAgent.GlobalPosition +
                                    randomPositionInLocalCircle;

            // Give agent time to flee target.
            await _sceneRunner.AwaitMillis(1000);

            // Check if agent is fleeing target asserting that agent is now farther from
            // the target than before.
            float distance = movingAgent.GlobalPosition.DistanceTo(
                target.GlobalPosition);
            AssertThat(distance > steeringBehavior.PanicDistance).IsTrue();
        }
    }

    /// <summary>
    /// Test that AlignBehavior can face in the same direction as other GameObject.
    /// </summary>
    [TestCase]
    public async Task AlignBehaviorTest()
    {
        // Get references to the target agent that will rotate an that our tested agent
        // will copy its alignment from.
        MovingAgent movingAgent =
            (MovingAgent)_sceneRunner.FindChild("SeekMovingAgent");
        Marker2D movingAgentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position2");
        MovingAgent alignAgent =
            (MovingAgent)_sceneRunner.FindChild("AlignMovingAgent");
        Marker2D alignAgentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position1");
        Target target = (Target) _sceneRunner.FindChild("Target");
        Marker2D Position3 = 
            (Marker2D) _sceneRunner.FindChild("Position3");
        Marker2D Position4 = 
            (Marker2D) _sceneRunner.FindChild("Position4");
        Marker2D Position5 = 
            (Marker2D) _sceneRunner.FindChild("Position5");
        
        // Get references to steering behavior from both agents.
        SeekSteeringBehavior seekSteeringBehavior =
            (SeekSteeringBehavior) movingAgent.FindChild(
                nameof(SeekSteeringBehavior));
        AlignSteeringBehavior alignSteeringBehavior =
            (AlignSteeringBehavior) alignAgent.FindChild(
                nameof(AlignSteeringBehavior));
        
        // Place and setup both agents before the test.
        movingAgent.GlobalPosition = movingAgentStartPosition.GlobalPosition;
        movingAgent.AgentColor = new Color(1, 0, 0);
        movingAgent.MaximumSpeed = 600.0f;
        movingAgent.StopSpeed = 1f;
        movingAgent.MaximumRotationalDegSpeed = 1080f;
        movingAgent.StopRotationDegThreshold = 1f;
        alignAgent.GlobalPosition = alignAgentStartPosition.GlobalPosition;
        alignAgent.AgentColor = new Color(0, 1, 0);
        alignAgent.MaximumSpeed = 600.0f;
        alignAgent.StopSpeed = 1f;
        alignAgent.MaximumRotationalDegSpeed = 180f;
        alignAgent.StopRotationDegThreshold = 1f;
        alignSteeringBehavior.Target = movingAgent;
        seekSteeringBehavior.Target = target;
        movingAgent.Visible = true;
        alignAgent.Visible = true;
        movingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        alignAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Start test.
        
        // Move seeker to face the first target.
        target.GlobalPosition = Position3.GlobalPosition;
        await _sceneRunner.AwaitMillis(2000);
        AssertThat(Mathf.IsEqualApprox(
            alignAgent.Orientation, 
            movingAgent.Orientation,
            alignSteeringBehavior.ArrivingMargin)).IsTrue();
        
        // Move seeker to face the second target.
        target.GlobalPosition = Position4.GlobalPosition;
        await _sceneRunner.AwaitMillis(2000);
        AssertThat(Mathf.IsEqualApprox(
            alignAgent.Orientation, 
            movingAgent.Orientation,
            alignSteeringBehavior.ArrivingMargin)).IsTrue();
        
        // Move seeker to face the third target.
        target.GlobalPosition = Position5.GlobalPosition;
        await _sceneRunner.AwaitMillis(2000);
        AssertThat(Mathf.IsEqualApprox(
            alignAgent.Orientation, 
            movingAgent.Orientation,
            alignSteeringBehavior.ArrivingMargin)).IsTrue();
    }

    /// <summary>
    /// Test that FaceBehavior can face towards a target while it moves.
    /// </summary>
    [TestCase]
    public async Task FaceBehaviorTest()
    {
        // Get references to agent and target.
        MovingAgent faceAgent =
            (MovingAgent)_sceneRunner.FindChild("FaceMovingAgent");
        Marker2D agentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position3");
        MovingAgent targetMovingAgent =
            (MovingAgent)_sceneRunner.FindChild("SeekMovingAgent");
        Marker2D targetMovingAgentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position6");
        Target targetOfTargetMovingAgent = (Target)_sceneRunner.FindChild("Target");
        Marker2D targetPosition =
            (Marker2D)_sceneRunner.FindChild("Position4");
        
        // Get references to steering behavior from both agents.
        SeekSteeringBehavior seekSteeringBehavior =
            (SeekSteeringBehavior) targetMovingAgent.FindChild(
                nameof(SeekSteeringBehavior));
        FaceSteeringBehavior faceSteeringBehavior =
            (FaceSteeringBehavior) faceAgent.FindChild(
                nameof(FaceSteeringBehavior));
        
        // Place and setup both agents before the test.
        faceAgent.GlobalPosition = agentStartPosition.GlobalPosition;
        faceAgent.AgentColor = new Color(0, 1, 0);
        faceAgent.MaximumSpeed = 600.0f;
        faceAgent.StopSpeed = 1f;
        faceAgent.MaximumRotationalDegSpeed = 180f;
        faceAgent.StopRotationDegThreshold = 1f;
        targetMovingAgent.GlobalPosition = targetMovingAgentStartPosition.GlobalPosition;
        targetMovingAgent.AgentColor = new Color(1, 0, 0);
        targetMovingAgent.MaximumSpeed = 600.0f;
        targetMovingAgent.StopSpeed = 1f;
        targetMovingAgent.MaximumRotationalDegSpeed = 180f;
        targetMovingAgent.StopRotationDegThreshold = 1f;
        faceSteeringBehavior.Target = targetMovingAgent;
        seekSteeringBehavior.Target = targetOfTargetMovingAgent;
        targetOfTargetMovingAgent.GlobalPosition = targetPosition.GlobalPosition;
        faceAgent.Visible = true;
        targetMovingAgent.Visible = true;
        faceAgent.ProcessMode = Node.ProcessModeEnum.Always;
        targetMovingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Start test.
        
        // Sample the tested face agent orientation in some moments of the
        // seeker travel, to check if it is still facing the seeker.
        float totalTestTimeInSeconds = 3.0f;
        int numberOfSamples = 5;
        float sampleInterval = totalTestTimeInSeconds / numberOfSamples;
        foreach (int _ in Enumerable.Range(1, numberOfSamples))
        {
            await _sceneRunner.AwaitMillis((uint)sampleInterval * 1000);
            Vector2 toFaceTargetVector = 
                targetMovingAgent.GlobalPosition - faceAgent.GlobalPosition;
            float currentAngle =  Mathf.RadToDeg(toFaceTargetVector.Angle());
            // 5 degrees tolerance, because target is constantly moving.
            AssertThat(
                Mathf.Abs(
                    currentAngle - 
                    faceAgent.Orientation) 
                <= 5).IsTrue(); 
        }
    }

    // TODO: Normalize position markers names. A it is, it's a mess.
    
    /// <summary>
    /// Test that VelocityMatchingBehavior can can copy its target's velocity.
    /// </summary>
    [TestCase]
    public async Task VelocityMatchingBehaviorTest()
    {
        // Get references to agent and target.
        MovingAgent velocityMatchingAgent =
            (MovingAgent)_sceneRunner.FindChild("VelocityMatchingMovingAgent");
        Marker2D agentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position1");
        MovingAgent targetMovingAgent =
            (MovingAgent)_sceneRunner.FindChild("ArriveMovingAgentLA");
        Marker2D targetMovingAgentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position6");
        Target targetOfTargetMovingAgent = (Target)_sceneRunner.FindChild("Target");
        Marker2D targetPosition =
            (Marker2D)_sceneRunner.FindChild("Position3");
        
        // Get references to steering behavior from both agents.
        ArriveSteeringBehaviorLA arriveSteeringBehavior =
            targetMovingAgent.FindChild<ArriveSteeringBehaviorLA>();
        VelocityMatchingSteeringBehavior velocityMatchingSteeringBehavior =
            (VelocityMatchingSteeringBehavior) velocityMatchingAgent.FindChild(
                nameof(VelocityMatchingSteeringBehavior));
        
        // Setup agents before the test.
        targetOfTargetMovingAgent.GlobalPosition = targetPosition.GlobalPosition;
        velocityMatchingAgent.MaximumSpeed = 200.0f;
        velocityMatchingAgent.MaximumAcceleration = 400.0f;
        velocityMatchingAgent.MaximumRotationalDegSpeed = 180f;
        velocityMatchingAgent.StopRotationDegThreshold = 1f;
        velocityMatchingAgent.StopSpeed = 10f;
        velocityMatchingAgent.MaximumAcceleration = 200;
        velocityMatchingAgent.MaximumDeceleration = 400;
        targetMovingAgent.MaximumSpeed = 200f;
        targetMovingAgent.StopSpeed = 1f;
        targetMovingAgent.MaximumRotationalDegSpeed = 180f;
        targetMovingAgent.StopRotationDegThreshold = 1f;
        targetMovingAgent.MaximumAcceleration = 180f;
        targetMovingAgent.MaximumDeceleration = 180f;
        targetMovingAgent.AgentColor = new Color(1, 0, 0);
        velocityMatchingSteeringBehavior.Target = targetMovingAgent;
        arriveSteeringBehavior.Target = targetOfTargetMovingAgent;
        velocityMatchingAgent.Visible = true;
        targetMovingAgent.Visible = true;
        velocityMatchingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        targetMovingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Start test.
        
        // Give time for the followed agent to try to reach its target
        // cruise velocity and assert velocity matcher agent has matched the velocity
        // of its target.
        while (!Mathf.IsEqualApprox(
                   targetMovingAgent.CurrentSpeed, 
                   targetMovingAgent.MaximumSpeed))
        {
            await _sceneRunner.AwaitIdleFrame();
        }
        await _sceneRunner.AwaitMillis(
            (uint)velocityMatchingSteeringBehavior.TimeToMatch * 1000);
        AssertThat(velocityMatchingAgent.Velocity == targetMovingAgent.Velocity);
        
        // Wait until arriver brakes and asserts that the VelocityMatcher
        // has braked to.
        while (!Mathf.IsEqualApprox(
                   targetMovingAgent.CurrentSpeed, 
                   0))
        {
            await _sceneRunner.AwaitIdleFrame();
        }
        await _sceneRunner.AwaitMillis(
            (uint)velocityMatchingSteeringBehavior.TimeToMatch * 1000);
        AssertThat(velocityMatchingAgent.Velocity == targetMovingAgent.Velocity);
    }
}