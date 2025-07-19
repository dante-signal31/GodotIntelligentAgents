using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GdUnit4;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;
using static GdUnit4.Assertions;

namespace GodotGameAIbyExample.Tests;

[TestSuite, RequireGodotRuntime]
public class SimpleBehaviorTests
{
    private const string TestScenePath = "res://Tests/TestLevels/" +
                                         "SimpleBehaviorTestLevel.tscn";

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
        
        // Set up agents before the test.
        target.GlobalPosition = targetPosition.GlobalPosition;
        steeringBehavior.Target = target;
        movingAgent.GlobalPosition = agentStartPosition.GlobalPosition;
        movingAgent.MaximumSpeed = 600.0f;
        movingAgent.StopSpeed = 1f;
        movingAgent.MaximumRotationalDegSpeed = 1080f;
        movingAgent.StopRotationDegThreshold = 1f;
        movingAgent.AgentColor = new Color(0, 1, 0);
        movingAgent.Visible = true;
        movingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Start test.

        // Give the agent time to reach target.
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
    /// Test that FleeBehavior makes agent go away from its threat.
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
        steeringBehavior.Threat = target;
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
                RandomExtensions.GetRandomPointInsideCircle(steeringBehavior.PanicDistance);
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
        // Get references to the target agent that will rotate and that our tested agent
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
            alignAgent.StopRotationDegThreshold)).IsTrue();
        
        // Move seeker to face the second target.
        target.GlobalPosition = Position4.GlobalPosition;
        await _sceneRunner.AwaitMillis(2000);
        AssertThat(Mathf.IsEqualApprox(
            alignAgent.Orientation, 
            movingAgent.Orientation,
            alignAgent.StopRotationDegThreshold)).IsTrue();
        
        // Move seeker to face the third target.
        target.GlobalPosition = Position5.GlobalPosition;
        await _sceneRunner.AwaitMillis(2000);
        AssertThat(Mathf.IsEqualApprox(
            alignAgent.Orientation, 
            movingAgent.Orientation,
            alignAgent.StopRotationDegThreshold)).IsTrue();
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
        MovingAgent arriveMovingAgent =
            (MovingAgent)_sceneRunner.FindChild("ArriveMovingAgentLA");
        Marker2D arriveMovingAgentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position6");
        Target targetOfArriveMovingAgent = (Target)_sceneRunner.FindChild("Target");
        Marker2D targetPosition =
            (Marker2D)_sceneRunner.FindChild("Position3");
        
        // Get references to steering behavior from both agents.
        ArriveSteeringBehaviorLA arriveSteeringBehavior =
            arriveMovingAgent.FindChild<ArriveSteeringBehaviorLA>();
        VelocityMatchingSteeringBehavior velocityMatchingSteeringBehavior =
            (VelocityMatchingSteeringBehavior) velocityMatchingAgent.FindChild(
                nameof(VelocityMatchingSteeringBehavior));
        
        // Setup agents before the test.
        targetOfArriveMovingAgent.GlobalPosition = targetPosition.GlobalPosition;
        velocityMatchingAgent.GlobalPosition = agentStartPosition.GlobalPosition;
        velocityMatchingAgent.MaximumSpeed = 200.0f;
        velocityMatchingAgent.MaximumRotationalDegSpeed = 180f;
        velocityMatchingAgent.StopRotationDegThreshold = 1f;
        velocityMatchingAgent.StopSpeed = 10f;
        velocityMatchingAgent.MaximumAcceleration = 800f;
        velocityMatchingAgent.MaximumDeceleration = 3000f;
        velocityMatchingAgent.Velocity = Vector2.Zero;
        arriveMovingAgent.GlobalPosition = arriveMovingAgentStartPosition.GlobalPosition;
        arriveMovingAgent.MaximumSpeed = 200f;
        arriveMovingAgent.StopSpeed = 1f;
        arriveMovingAgent.MaximumRotationalDegSpeed = 180f;
        arriveMovingAgent.StopRotationDegThreshold = 1f;
        arriveMovingAgent.MaximumAcceleration = 180f;
        arriveMovingAgent.MaximumDeceleration = 180f;
        arriveMovingAgent.AgentColor = new Color(1, 0, 0);
        arriveMovingAgent.Velocity = Vector2.Zero;
        velocityMatchingSteeringBehavior.Target = arriveMovingAgent;
        velocityMatchingSteeringBehavior.TimeToMatch = 0.1f;
        arriveSteeringBehavior.Target = targetOfArriveMovingAgent;
        velocityMatchingAgent.Visible = true;
        arriveMovingAgent.Visible = true;
        velocityMatchingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        arriveMovingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Start test.
        
        // Give time for the followed agent to try to reach its target
        // cruise velocity and assert velocity matcher agent has matched the velocity
        // of its target.
        while (!Mathf.IsEqualApprox(
                   arriveMovingAgent.CurrentSpeed, 
                   arriveMovingAgent.MaximumSpeed))
        {
            await _sceneRunner.AwaitIdleFrame();
        }
        await _sceneRunner.AwaitMillis(
            (uint)velocityMatchingSteeringBehavior.TimeToMatch * 1000);
        AssertThat(
            (velocityMatchingAgent.Velocity.Normalized() - arriveMovingAgent.Velocity.Normalized()).Length() < 0.01 &&
            Mathf.Abs(velocityMatchingAgent.Velocity.Length() - arriveMovingAgent.Velocity.Length()) < 15f)
            .IsTrue();
        
        // Wait until arriver brakes and asserts that the VelocityMatcher
        // has braked to.
        while (!Mathf.IsEqualApprox(
                   arriveMovingAgent.CurrentSpeed, 
                   0))
        {
            await _sceneRunner.AwaitIdleFrame();
        }
        await _sceneRunner.AwaitMillis(
            (uint)velocityMatchingSteeringBehavior.TimeToMatch * 1000);
        AssertThat(
                (velocityMatchingAgent.Velocity.Normalized() - arriveMovingAgent.Velocity.Normalized()).Length() < 1.2f &&
                Mathf.Abs(velocityMatchingAgent.Velocity.Length() - arriveMovingAgent.Velocity.Length()) < 40f)
            .IsTrue();
    }

    /// <summary>
    /// Test that PursuitBehavior can intercept its target.
    /// </summary>
    [TestCase]
    public async Task PursuitBehaviorTest()
    {
        // Get references to agent and target.
        MovingAgent pursueAgent =
            (MovingAgent)_sceneRunner.FindChild("PursueMovingAgent");
        Marker2D agentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position4");
        MovingAgent targetMovingAgent =
            (MovingAgent)_sceneRunner.FindChild("SeekMovingAgent");
        Marker2D targetMovingAgentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position6");
        Target targetOfTargetMovingAgent = (Target)_sceneRunner.FindChild("Target");
        Marker2D targetPosition =
            (Marker2D)_sceneRunner.FindChild("Position3");
        
        // Get references to steering behavior from both agents.
        SeekSteeringBehavior seekSteeringBehavior =
            targetMovingAgent.FindChild<SeekSteeringBehavior>();
        Scripts.SteeringBehaviors.PursueSteeringBehavior pursueSteeringBehavior =
            pursueAgent.FindChild<Scripts.SteeringBehaviors.PursueSteeringBehavior>();
        
        // Setup agents before the test.
        targetOfTargetMovingAgent.GlobalPosition = targetPosition.GlobalPosition;
        pursueAgent.GlobalPosition = agentStartPosition.GlobalPosition;
        pursueAgent.MaximumSpeed = 250.0f;
        pursueAgent.MaximumAcceleration = 400.0f;
        pursueAgent.MaximumRotationalDegSpeed = 180f;
        pursueAgent.StopRotationDegThreshold = 1f;
        pursueAgent.StopSpeed = 10f;
        pursueAgent.MaximumAcceleration = 200;
        pursueAgent.MaximumDeceleration = 400;
        targetMovingAgent.GlobalPosition = targetMovingAgentStartPosition.GlobalPosition;
        targetMovingAgent.MaximumSpeed = 200f;
        targetMovingAgent.StopSpeed = 1f;
        targetMovingAgent.MaximumRotationalDegSpeed = 180f;
        targetMovingAgent.StopRotationDegThreshold = 1f;
        targetMovingAgent.MaximumAcceleration = 180f;
        targetMovingAgent.MaximumDeceleration = 180f;
        targetMovingAgent.AgentColor = new Color(1, 0, 0);
        pursueSteeringBehavior.Target = targetMovingAgent;
        seekSteeringBehavior.Target = targetOfTargetMovingAgent;
        pursueAgent.Visible = true;
        targetMovingAgent.Visible = true;
        pursueAgent.ProcessMode = Node.ProcessModeEnum.Always;
        targetMovingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Give time for the chaser to get to the target.
        await _sceneRunner.AwaitMillis(3000);
        
        // Assert the target was reached.
        // We test for a distance equal to the radius of both agents, plus
        // a 0.1 of tolerance. That should be the distance of centers when
        // both agents are touching.
        AssertThat(
            pursueAgent.GlobalPosition.DistanceTo(targetMovingAgent.GlobalPosition) <= 
            (120f)
            ).IsTrue();
    }
    
    /// <summary>
    /// Test that OffsetFollowBehavior can follow its target keeping its offset.
    /// </summary>
    [TestCase]
    public async Task OffsetFollowBehaviorTest()
    {
        // Get references to agent and target.
        MovingAgent followAgent =
            (MovingAgent)_sceneRunner.FindChild("OffsetFollowMovingAgent");
        Node2D offsetFromTargetMarker = (Node2D)_sceneRunner.FindChild("OffsetFromTargetMarker");
        Marker2D agentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position4");
        MovingAgent targetMovingAgent =
            (MovingAgent)_sceneRunner.FindChild("SeekMovingAgent");
        Marker2D targetMovingAgentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position6");
        Target targetOfTargetMovingAgent = (Target)_sceneRunner.FindChild("Target");
        Marker2D targetPosition =
            (Marker2D)_sceneRunner.FindChild("Position3");
        Marker2D secondTargetPosition =
            (Marker2D)_sceneRunner.FindChild("Position4");
        
        // Get references to steering behavior from both agents.
        OffsetFollowBehavior offsetFollowBehavior =
            followAgent.FindChild<OffsetFollowBehavior>();
        SeekSteeringBehavior seekSteeringBehavior =
            targetMovingAgent.FindChild<SeekSteeringBehavior>();
        
        // Setup agents before the test.
        targetOfTargetMovingAgent.GlobalPosition = targetPosition.GlobalPosition;
        followAgent.GlobalPosition = agentStartPosition.GlobalPosition;
        followAgent.MaximumSpeed = 250.0f;
        followAgent.MaximumAcceleration = 400.0f;
        followAgent.MaximumRotationalDegSpeed = 180f;
        followAgent.StopRotationDegThreshold = 1f;
        followAgent.StopSpeed = 10f;
        followAgent.MaximumAcceleration = 200;
        followAgent.MaximumDeceleration = 400;

        targetMovingAgent.GlobalPosition = targetMovingAgentStartPosition.GlobalPosition;
        targetMovingAgent.MaximumSpeed = 200f;
        targetMovingAgent.StopSpeed = 1f;
        targetMovingAgent.MaximumRotationalDegSpeed = 180f;
        targetMovingAgent.StopRotationDegThreshold = 1f;
        targetMovingAgent.MaximumAcceleration = 180f;
        targetMovingAgent.MaximumDeceleration = 180f;
        targetMovingAgent.AgentColor = new Color(1, 0, 0);
        
        Vector2 offsetFromTarget = new Vector2(-100, 100);
        offsetFromTargetMarker.GlobalPosition = 
            targetMovingAgent.ToGlobal(offsetFromTarget);
        offsetFollowBehavior.UpdateOffsetFromTarget();
            
        seekSteeringBehavior.Target = targetOfTargetMovingAgent;
        offsetFollowBehavior.Target = targetMovingAgent;
        followAgent.Visible = true;
        targetMovingAgent.Visible = true;
        followAgent.ProcessMode = Node.ProcessModeEnum.Always;
        targetMovingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Give time for the follower to get to the target.
        await _sceneRunner.AwaitMillis(5000);
        
        // Assert follow agent is at the offset position from target agent.
        AssertThat(
            followAgent.GlobalPosition.DistanceTo(
                targetMovingAgent.ToGlobal(offsetFromTarget)) <= 
            (150f)
            ).IsTrue();
        
        // Move again target agent.
        targetOfTargetMovingAgent.GlobalPosition = secondTargetPosition.GlobalPosition;
        
        // Give time for the follower to get to the target again.
        await _sceneRunner.AwaitMillis(2000);
        
        // Assert follow agent is now again at the offset position from target agent.
        AssertThat(
            followAgent.GlobalPosition.DistanceTo(
                targetMovingAgent.ToGlobal(offsetFromTarget)) <= 
            (150f)
        ).IsTrue();
    }
    
    /// <summary>
    /// Test that AgentAvoiderBehavior can reach its target without touching another
    /// moving agent that goes across its path.
    /// </summary>
    [TestCase]
    public async Task AgentAvoiderBehaviorTestFirstScenario()
    {
        // Get references to agent and target.
        MovingAgent agentAvoider =
            (MovingAgent)_sceneRunner.FindChild("ActiveAgentAvoiderMovingAgent");
        Marker2D position3 =
            (Marker2D)_sceneRunner.FindChild("Position3");
        MovingAgent obstacleMovingAgent =
            (MovingAgent)_sceneRunner.FindChild("SeekMovingAgent");
        Marker2D position6 =
            (Marker2D)_sceneRunner.FindChild("Position6");
        Target targetOfAgentAvoiderMovingAgent = (Target)_sceneRunner.FindChild("Target");
        // Marker2D position1 =
        //     (Marker2D)_sceneRunner.FindChild("Position1");
        Marker2D position4 =
            (Marker2D)_sceneRunner.FindChild("Position4");
        Marker2D position9 =
            (Marker2D)_sceneRunner.FindChild("Position9");
        
        // Get references to steering behavior from both agents.
        ActiveAgentAvoiderSteeringBehavior agentAvoiderBehavior =
            agentAvoider.FindChild<ActiveAgentAvoiderSteeringBehavior>();
        SeekSteeringBehavior agentAvoiderSeekSteeringBehavior = 
            agentAvoider.FindChild<SeekSteeringBehavior>(recursive:true);
        SeekSteeringBehavior seekSteeringBehavior =
            obstacleMovingAgent.FindChild<SeekSteeringBehavior>();
        
        // Setup agents before the test.
        agentAvoider.MaximumSpeed = 250.0f;
        agentAvoider.MaximumAcceleration = 400.0f;
        agentAvoider.MaximumRotationalDegSpeed = 180f;
        agentAvoider.StopRotationDegThreshold = 1f;
        agentAvoider.StopSpeed = 10f;
        agentAvoider.MaximumAcceleration = 200;
        agentAvoider.MaximumDeceleration = 400;

        // obstacleMovingAgent.GlobalPosition = position6.GlobalPosition;
        obstacleMovingAgent.MaximumSpeed = 200f;
        obstacleMovingAgent.StopSpeed = 1f;
        obstacleMovingAgent.MaximumRotationalDegSpeed = 180f;
        obstacleMovingAgent.StopRotationDegThreshold = 1f;
        obstacleMovingAgent.MaximumAcceleration = 180f;
        obstacleMovingAgent.MaximumDeceleration = 180f;
        obstacleMovingAgent.AgentColor = new Color(1, 0, 0);
        
        agentAvoiderBehavior.AvoidanceTimeout = 0.5f;
        
        // FIRST SCENARIO:
        targetOfAgentAvoiderMovingAgent.GlobalPosition = position9.GlobalPosition;
        agentAvoider.GlobalPosition = position3.GlobalPosition;
        obstacleMovingAgent.GlobalPosition = position6.GlobalPosition;
        seekSteeringBehavior.Target = position4;
        agentAvoiderSeekSteeringBehavior.Target = targetOfAgentAvoiderMovingAgent;
        agentAvoider.Visible = true;
        obstacleMovingAgent.Visible = true;
        agentAvoider.ProcessMode = Node.ProcessModeEnum.Always;
        obstacleMovingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        // Assert we move without touching the obstacle agent.
        int steps = 8;
        for (int i=0; i < steps; i++)
        {
            await _sceneRunner.AwaitMillis(1000);
            AssertThat(
                agentAvoider.GlobalPosition.DistanceTo(
                    obstacleMovingAgent.GlobalPosition) > 
                150f
            ).IsTrue();
        }
        // Assert we reached target.
        AssertThat(
            agentAvoider.GlobalPosition.DistanceTo(
                targetOfAgentAvoiderMovingAgent.GlobalPosition) <
            10f
        ).IsTrue();
        // Disable test agents.
        agentAvoider.Visible = false;
        obstacleMovingAgent.Visible = false;
        agentAvoider.ProcessMode = Node.ProcessModeEnum.Disabled;
        obstacleMovingAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
    }
    
    /// <summary>
    /// Test that AgentAvoiderBehavior can reach its target without touching another
    /// moving agent that goes across its path.
    /// </summary>
    [TestCase]
    public async Task AgentAvoiderBehaviorTestSecondScenario()
    {
        // Get references to agent and target.
        MovingAgent agentAvoider =
            (MovingAgent)_sceneRunner.FindChild("ActiveAgentAvoiderMovingAgent");
        Marker2D position3 =
            (Marker2D)_sceneRunner.FindChild("Position3");
        MovingAgent obstacleMovingAgent =
            (MovingAgent)_sceneRunner.FindChild("SeekMovingAgent");
        Marker2D position6 =
            (Marker2D)_sceneRunner.FindChild("Position6");
        Target targetOfAgentAvoiderMovingAgent = (Target)_sceneRunner.FindChild("Target");
        // Marker2D position1 =
        //     (Marker2D)_sceneRunner.FindChild("Position1");
        Marker2D position4 =
            (Marker2D)_sceneRunner.FindChild("Position4");
        Marker2D position9 =
            (Marker2D)_sceneRunner.FindChild("Position9");
        
        // Get references to steering behavior from both agents.
        ActiveAgentAvoiderSteeringBehavior agentAvoiderBehavior =
            agentAvoider.FindChild<ActiveAgentAvoiderSteeringBehavior>();
        SeekSteeringBehavior agentAvoiderSeekSteeringBehavior = 
            agentAvoider.FindChild<SeekSteeringBehavior>(recursive:true);
        SeekSteeringBehavior seekSteeringBehavior =
            obstacleMovingAgent.FindChild<SeekSteeringBehavior>();
        
        // Setup agents before the test.
        agentAvoider.MaximumSpeed = 250.0f;
        agentAvoider.MaximumAcceleration = 400.0f;
        agentAvoider.MaximumRotationalDegSpeed = 180f;
        agentAvoider.StopRotationDegThreshold = 1f;
        agentAvoider.StopSpeed = 10f;
        agentAvoider.MaximumAcceleration = 200;
        agentAvoider.MaximumDeceleration = 400;

        // obstacleMovingAgent.GlobalPosition = position6.GlobalPosition;
        obstacleMovingAgent.MaximumSpeed = 200f;
        obstacleMovingAgent.StopSpeed = 1f;
        obstacleMovingAgent.MaximumRotationalDegSpeed = 180f;
        obstacleMovingAgent.StopRotationDegThreshold = 1f;
        obstacleMovingAgent.MaximumAcceleration = 180f;
        obstacleMovingAgent.MaximumDeceleration = 180f;
        obstacleMovingAgent.AgentColor = new Color(1, 0, 0);
        
        agentAvoiderBehavior.AvoidanceTimeout = 0.5f;
        
        // SECOND SCENARIO:
        targetOfAgentAvoiderMovingAgent.GlobalPosition = position9.GlobalPosition;
        agentAvoider.GlobalPosition = position3.GlobalPosition;
        obstacleMovingAgent.GlobalPosition = position4.GlobalPosition;
        seekSteeringBehavior.Target = position6;
        agentAvoiderSeekSteeringBehavior.Target = targetOfAgentAvoiderMovingAgent;
        agentAvoider.Visible = true;
        obstacleMovingAgent.Visible = true;
        agentAvoider.ProcessMode = Node.ProcessModeEnum.Always;
        obstacleMovingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        // Assert we move without touching the obstacle agent.
        int steps = 9;
        for (int i=0; i < steps; i++)
        {
            await _sceneRunner.AwaitMillis(1000);
            AssertThat(
                agentAvoider.GlobalPosition.DistanceTo(
                    obstacleMovingAgent.GlobalPosition) > 
                150f
            ).IsTrue();
        }
        // Assert we reached target.
        AssertThat(
            agentAvoider.GlobalPosition.DistanceTo(
                targetOfAgentAvoiderMovingAgent.GlobalPosition) <
            10f
        ).IsTrue();
        // Disable test agents.
        agentAvoider.Visible = false;
        obstacleMovingAgent.Visible = false;
        agentAvoider.ProcessMode = Node.ProcessModeEnum.Disabled;
        obstacleMovingAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
    }
    
    /// <summary>
    /// Test that AgentAvoiderBehavior can reach its target without touching another
    /// moving agent that goes across its path.
    /// </summary>
    [TestCase]
    public async Task AgentAvoiderBehaviorTestThirdScenario()
    {
        // Get references to agent and target.
        MovingAgent agentAvoider =
            (MovingAgent)_sceneRunner.FindChild("ActiveAgentAvoiderMovingAgent");
        Marker2D position3 =
            (Marker2D)_sceneRunner.FindChild("Position3");
        MovingAgent obstacleMovingAgent =
            (MovingAgent)_sceneRunner.FindChild("SeekMovingAgent");
        Marker2D position6 =
            (Marker2D)_sceneRunner.FindChild("Position6");
        Target targetOfAgentAvoiderMovingAgent = (Target)_sceneRunner.FindChild("Target");
        Marker2D position1 =
            (Marker2D)_sceneRunner.FindChild("Position1");
        Marker2D position4 =
            (Marker2D)_sceneRunner.FindChild("Position4");
        Marker2D position9 =
            (Marker2D)_sceneRunner.FindChild("Position9");
        
        // Get references to steering behavior from both agents.
        ActiveAgentAvoiderSteeringBehavior agentAvoiderBehavior =
            agentAvoider.FindChild<ActiveAgentAvoiderSteeringBehavior>();
        SeekSteeringBehavior agentAvoiderSeekSteeringBehavior = 
            agentAvoider.FindChild<SeekSteeringBehavior>(recursive:true);
        SeekSteeringBehavior seekSteeringBehavior =
            obstacleMovingAgent.FindChild<SeekSteeringBehavior>();
        
        // Setup agents before the test.
        agentAvoider.MaximumSpeed = 250.0f;
        agentAvoider.MaximumAcceleration = 400.0f;
        agentAvoider.MaximumRotationalDegSpeed = 180f;
        agentAvoider.StopRotationDegThreshold = 1f;
        agentAvoider.StopSpeed = 10f;
        agentAvoider.MaximumAcceleration = 200;
        agentAvoider.MaximumDeceleration = 400;

        // obstacleMovingAgent.GlobalPosition = position6.GlobalPosition;
        obstacleMovingAgent.MaximumSpeed = 200f;
        obstacleMovingAgent.StopSpeed = 1f;
        obstacleMovingAgent.MaximumRotationalDegSpeed = 180f;
        obstacleMovingAgent.StopRotationDegThreshold = 1f;
        obstacleMovingAgent.MaximumAcceleration = 180f;
        obstacleMovingAgent.MaximumDeceleration = 180f;
        obstacleMovingAgent.AgentColor = new Color(1, 0, 0);
        
        agentAvoiderBehavior.AvoidanceTimeout = 0.5f;
        
        // THIRD SCENARIO:
        targetOfAgentAvoiderMovingAgent.GlobalPosition = position3.GlobalPosition;
        agentAvoider.GlobalPosition = position9.GlobalPosition;
        obstacleMovingAgent.GlobalPosition = position4.GlobalPosition;
        seekSteeringBehavior.Target = position6;
        agentAvoiderSeekSteeringBehavior.Target = targetOfAgentAvoiderMovingAgent;
        agentAvoider.Visible = true;
        obstacleMovingAgent.Visible = true;
        agentAvoider.ProcessMode = Node.ProcessModeEnum.Always;
        obstacleMovingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        // Assert we move without touching the obstacle agent.
        int steps = 9;
        for (int i=0; i < steps; i++)
        {
            await _sceneRunner.AwaitMillis(1000);
            AssertThat(
                agentAvoider.GlobalPosition.DistanceTo(
                    obstacleMovingAgent.GlobalPosition) > 
                110f
            ).IsTrue();
        }
        // Assert we reached target.
        AssertThat(
            agentAvoider.GlobalPosition.DistanceTo(
                targetOfAgentAvoiderMovingAgent.GlobalPosition) <
            10f
        ).IsTrue();
        // Disable test agents.
        agentAvoider.Visible = false;
        obstacleMovingAgent.Visible = false;
        agentAvoider.ProcessMode = Node.ProcessModeEnum.Disabled;
        obstacleMovingAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
    }
    
    /// <summary>
    /// Test that AgentAvoiderBehavior can reach its target without touching another
    /// moving agent that goes across its path.
    /// </summary>
    [TestCase]
    public async Task AgentAvoiderBehaviorTestFourthScenario()
    {
        // Get references to agent and target.
        MovingAgent agentAvoider =
            (MovingAgent)_sceneRunner.FindChild("ActiveAgentAvoiderMovingAgent");
        Marker2D position11 =
            (Marker2D)_sceneRunner.FindChild("Position11");
        MovingAgent obstacleMovingAgent =
            (MovingAgent)_sceneRunner.FindChild("SeekMovingAgent");
        Marker2D position12 =
            (Marker2D)_sceneRunner.FindChild("Position12");
        Target targetOfAgentAvoiderMovingAgent = (Target)_sceneRunner.FindChild("Target");
        // Marker2D position1 =
        //     (Marker2D)_sceneRunner.FindChild("Position1");
        // Marker2D position4 =
        //     (Marker2D)_sceneRunner.FindChild("Position4");
        // Marker2D position9 =
        //     (Marker2D)_sceneRunner.FindChild("Position9");
        
        // Get references to steering behavior from both agents.
        ActiveAgentAvoiderSteeringBehavior agentAvoiderBehavior =
            agentAvoider.FindChild<ActiveAgentAvoiderSteeringBehavior>();
        SeekSteeringBehavior agentAvoiderSeekSteeringBehavior = 
            agentAvoider.FindChild<SeekSteeringBehavior>(recursive:true);
        SeekSteeringBehavior seekSteeringBehavior =
            obstacleMovingAgent.FindChild<SeekSteeringBehavior>();
        
        // Setup agents before the test.
        agentAvoider.MaximumSpeed = 250.0f;
        agentAvoider.MaximumAcceleration = 400.0f;
        agentAvoider.MaximumRotationalDegSpeed = 180f;
        agentAvoider.StopRotationDegThreshold = 1f;
        agentAvoider.StopSpeed = 10f;
        agentAvoider.MaximumAcceleration = 200;
        agentAvoider.MaximumDeceleration = 400;
        
        obstacleMovingAgent.MaximumSpeed = 200f;
        obstacleMovingAgent.StopSpeed = 1f;
        obstacleMovingAgent.MaximumRotationalDegSpeed = 180f;
        obstacleMovingAgent.StopRotationDegThreshold = 1f;
        obstacleMovingAgent.MaximumAcceleration = 180f;
        obstacleMovingAgent.MaximumDeceleration = 180f;
        obstacleMovingAgent.AgentColor = new Color(1, 0, 0);
        
        agentAvoiderBehavior.AvoidanceTimeout = 0.5f;
        
        // FOURTH SCENARIO:
        targetOfAgentAvoiderMovingAgent.GlobalPosition = position11.GlobalPosition;
        agentAvoider.GlobalPosition = position12.GlobalPosition;
        obstacleMovingAgent.GlobalPosition = position11.GlobalPosition;
        seekSteeringBehavior.Target = position12;
        agentAvoiderSeekSteeringBehavior.Target = targetOfAgentAvoiderMovingAgent;
        agentAvoider.Visible = true;
        obstacleMovingAgent.Visible = true;
        agentAvoider.ProcessMode = Node.ProcessModeEnum.Always;
        obstacleMovingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        // Assert we move without touching the obstacle agent.
        int steps = 9;
        for (int i=0; i < steps; i++)
        {
            await _sceneRunner.AwaitMillis(1000);
            AssertThat(
                agentAvoider.GlobalPosition.DistanceTo(
                    obstacleMovingAgent.GlobalPosition) > 
                150f
            ).IsTrue();
        }
        // Assert we reached target.
        AssertThat(
            agentAvoider.GlobalPosition.DistanceTo(
                targetOfAgentAvoiderMovingAgent.GlobalPosition) <
            10f
        ).IsTrue();
        // Disable test agents.
        agentAvoider.Visible = false;
        obstacleMovingAgent.Visible = false;
        agentAvoider.ProcessMode = Node.ProcessModeEnum.Disabled;
        obstacleMovingAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
    }
    
    /// <summary>
    /// Test that AgentAvoiderBehavior can reach its target without touching another
    /// moving agent that goes across its path.
    /// </summary>
    [TestCase]
    public async Task AgentAvoiderBehaviorTestFifthScenario()
    {
        // Get references to agent and target.
        MovingAgent agentAvoider =
            (MovingAgent)_sceneRunner.FindChild("ActiveAgentAvoiderMovingAgent");
        Marker2D position11 =
            (Marker2D)_sceneRunner.FindChild("Position11");
        MovingAgent obstacleMovingAgent =
            (MovingAgent)_sceneRunner.FindChild("SeekMovingAgent");
        Marker2D position12 =
            (Marker2D)_sceneRunner.FindChild("Position12");
        Target targetOfAgentAvoiderMovingAgent = (Target)_sceneRunner.FindChild("Target");
        // Marker2D position1 =
        //     (Marker2D)_sceneRunner.FindChild("Position1");
        // Marker2D position4 =
        //     (Marker2D)_sceneRunner.FindChild("Position4");
        // Marker2D position9 =
        //     (Marker2D)_sceneRunner.FindChild("Position9");
        
        // Get references to steering behavior from both agents.
        ActiveAgentAvoiderSteeringBehavior agentAvoiderBehavior =
            agentAvoider.FindChild<ActiveAgentAvoiderSteeringBehavior>();
        SeekSteeringBehavior agentAvoiderSeekSteeringBehavior = 
            agentAvoider.FindChild<SeekSteeringBehavior>(recursive:true);
        SeekSteeringBehavior seekSteeringBehavior =
            obstacleMovingAgent.FindChild<SeekSteeringBehavior>();
        
        // Setup agents before the test.
        agentAvoider.MaximumSpeed = 250.0f;
        agentAvoider.MaximumAcceleration = 400.0f;
        agentAvoider.MaximumRotationalDegSpeed = 180f;
        agentAvoider.StopRotationDegThreshold = 1f;
        agentAvoider.StopSpeed = 10f;
        agentAvoider.MaximumAcceleration = 200;
        agentAvoider.MaximumDeceleration = 400;

        obstacleMovingAgent.GlobalPosition = position12.GlobalPosition;
        obstacleMovingAgent.MaximumSpeed = 200f;
        obstacleMovingAgent.StopSpeed = 1f;
        obstacleMovingAgent.MaximumRotationalDegSpeed = 180f;
        obstacleMovingAgent.StopRotationDegThreshold = 1f;
        obstacleMovingAgent.MaximumAcceleration = 180f;
        obstacleMovingAgent.MaximumDeceleration = 180f;
        obstacleMovingAgent.AgentColor = new Color(1, 0, 0);
        
        agentAvoiderBehavior.AvoidanceTimeout = 0.5f;
        
        // FIFTH SCENARIO:
        targetOfAgentAvoiderMovingAgent.GlobalPosition = position12.GlobalPosition;
        agentAvoider.GlobalPosition = position11.GlobalPosition;
        obstacleMovingAgent.GlobalPosition = position12.GlobalPosition;
        seekSteeringBehavior.Target = position11;
        agentAvoiderSeekSteeringBehavior.Target = targetOfAgentAvoiderMovingAgent;
        agentAvoider.Visible = true;
        obstacleMovingAgent.Visible = true;
        agentAvoider.ProcessMode = Node.ProcessModeEnum.Always;
        obstacleMovingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        // Assert we move without touching the obstacle agent.
        int steps = 9;
        for (int i=0; i < steps; i++)
        {
            await _sceneRunner.AwaitMillis(1000);
            AssertThat(
                agentAvoider.GlobalPosition.DistanceTo(
                    obstacleMovingAgent.GlobalPosition) > 
                150f
            ).IsTrue();
        }
        // Assert we reached target.
        AssertThat(
            agentAvoider.GlobalPosition.DistanceTo(
                targetOfAgentAvoiderMovingAgent.GlobalPosition) <
            10f
        ).IsTrue();
        // Disable test agents.
        agentAvoider.Visible = false;
        obstacleMovingAgent.Visible = false;
        agentAvoider.ProcessMode = Node.ProcessModeEnum.Disabled;
        obstacleMovingAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
    }
    
    /// <summary>
    /// Test that InterposeMatchingBehavior can place and agent between two moving agents.
    /// </summary>
    [TestCase]
    public async Task InterposeBehaviorTest()
    {
        // Get references to agent and target.
        MovingAgent velocityMatchingAgent =
            (MovingAgent)_sceneRunner.FindChild("VelocityMatchingMovingAgent");
        Marker2D agentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position7");
        MovingAgent targetMovingAgent =
            (MovingAgent)_sceneRunner.FindChild("ArriveMovingAgentLA");
        Marker2D targetMovingAgentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position8");
        Target targetOfTargetMovingAgent = (Target)_sceneRunner.FindChild("Target");
        Marker2D targetPosition =
            (Marker2D)_sceneRunner.FindChild("Position3");
        MovingAgent interposeAgent = 
            (MovingAgent)_sceneRunner.FindChild("InterposeMovingAgent");
        Marker2D interposeAgentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position9");
        
        // Get references to steering behavior from every agent.
        ArriveSteeringBehaviorLA arriveSteeringBehavior =
            targetMovingAgent.FindChild<ArriveSteeringBehaviorLA>();
        VelocityMatchingSteeringBehavior velocityMatchingSteeringBehavior =
            (VelocityMatchingSteeringBehavior) velocityMatchingAgent.FindChild(
                nameof(VelocityMatchingSteeringBehavior));
        InterposeSteeringBehavior interposeSteeringBehavior =
            interposeAgent.FindChild<InterposeSteeringBehavior>();
        
        // Setup agents before the test.
        targetOfTargetMovingAgent.GlobalPosition = targetPosition.GlobalPosition;
        velocityMatchingAgent.GlobalPosition = agentStartPosition.GlobalPosition;
        velocityMatchingAgent.MaximumSpeed = 200.0f;
        velocityMatchingAgent.MaximumAcceleration = 400.0f;
        velocityMatchingAgent.MaximumRotationalDegSpeed = 180f;
        velocityMatchingAgent.StopRotationDegThreshold = 1f;
        velocityMatchingAgent.StopSpeed = 10f;
        velocityMatchingAgent.MaximumAcceleration = 200;
        velocityMatchingAgent.MaximumDeceleration = 400;
        velocityMatchingAgent.AgentColor = Colors.Firebrick;
        targetMovingAgent.GlobalPosition = targetMovingAgentStartPosition.GlobalPosition;
        targetMovingAgent.MaximumSpeed = 200f;
        targetMovingAgent.StopSpeed = 1f;
        targetMovingAgent.MaximumRotationalDegSpeed = 180f;
        targetMovingAgent.StopRotationDegThreshold = 1f;
        targetMovingAgent.MaximumAcceleration = 180f;
        targetMovingAgent.MaximumDeceleration = 180f;
        targetMovingAgent.AgentColor = new Color(1, 0, 0);
        interposeAgent.GlobalPosition = interposeAgentStartPosition.GlobalPosition;
        velocityMatchingSteeringBehavior.Target = targetMovingAgent;
        velocityMatchingSteeringBehavior.TimeToMatch = 0.1f;
        arriveSteeringBehavior.Target = targetOfTargetMovingAgent;
        interposeSteeringBehavior.AgentA = velocityMatchingAgent;
        interposeSteeringBehavior.AgentB = targetMovingAgent;
        interposeSteeringBehavior.ArrivalDistance = 10f;
        velocityMatchingAgent.Visible = true;
        targetMovingAgent.Visible = true;
        interposeAgent.Visible = true;
        velocityMatchingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        targetMovingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        interposeAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Start test.
        
        // Give time for the followed agent to try to reach its target
        // cruise velocity and assert interposed agent in the middle of the two agents.
        while (!Mathf.IsEqualApprox(
                   targetMovingAgent.CurrentSpeed, 
                   targetMovingAgent.MaximumSpeed))
        {
            await _sceneRunner.AwaitIdleFrame();
        }
        await _sceneRunner.AwaitMillis(
            (uint)velocityMatchingSteeringBehavior.TimeToMatch * 1000);
        AssertThat(
            (interposeAgent.GlobalPosition - InterposeSteeringBehavior.GetMidPoint(
                velocityMatchingAgent.GlobalPosition, 
                targetMovingAgent.GlobalPosition)).Length() <= 
            interposeSteeringBehavior.ArrivalDistance).IsTrue();
        
        // Wait until arriver brakes and asserts that the interpose agent stays in
        // the middle.
        while (!Mathf.IsEqualApprox(
                   targetMovingAgent.CurrentSpeed, 
                   0))
        {
            await _sceneRunner.AwaitIdleFrame();
        }
        await _sceneRunner.AwaitMillis(
            (uint)velocityMatchingSteeringBehavior.TimeToMatch * 1000);
        AssertThat(
            (interposeAgent.GlobalPosition - InterposeSteeringBehavior.GetMidPoint(
                velocityMatchingAgent.GlobalPosition, 
                targetMovingAgent.GlobalPosition)).Length() <= 
            interposeSteeringBehavior.ArrivalDistance).IsTrue();
    }
    
    /// <summary>
    /// Test that EvadeBehavior can keep away its agent from its chaser.
    /// </summary>
    [TestCase]
    public async Task EvadeBehaviorTest()
    {
        // Get references to agent and target.
        MovingAgent seekAgent =
            (MovingAgent)_sceneRunner.FindChild("SeekMovingAgent");
        Marker2D seekAgentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position6");
        MovingAgent evadeAgent =
            (MovingAgent)_sceneRunner.FindChild("EvadeMovingAgent");
        Marker2D evadeMovingAgentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position5");
        
        // Get references to steering behavior from both agents.
        SeekSteeringBehavior seekSteeringBehavior =
            seekAgent.FindChild<SeekSteeringBehavior>();
        EvadeSteeringBehavior evadeSteeringBehavior =
            evadeAgent.FindChild<EvadeSteeringBehavior>();
        
        // Setup agents before the test.
        seekAgent.GlobalPosition = seekAgentStartPosition.GlobalPosition;
        seekAgent.MaximumSpeed = 200.0f;
        seekAgent.MaximumAcceleration = 400.0f;
        seekAgent.MaximumRotationalDegSpeed = 180f;
        seekAgent.StopRotationDegThreshold = 1f;
        seekAgent.StopSpeed = 10f;
        seekAgent.MaximumAcceleration = 200;
        seekAgent.MaximumDeceleration = 400;
        seekAgent.AgentColor = new Color(1, 0, 0);
        seekSteeringBehavior.Target = evadeAgent;
        evadeAgent.GlobalPosition = evadeMovingAgentStartPosition.GlobalPosition;
        evadeAgent.MaximumSpeed = 200f;
        evadeAgent.StopSpeed = 1f;
        evadeAgent.MaximumRotationalDegSpeed = 180f;
        evadeAgent.StopRotationDegThreshold = 1f;
        evadeAgent.MaximumAcceleration = 180f;
        evadeAgent.MaximumDeceleration = 180f;
        evadeSteeringBehavior.Threat = seekAgent;
        evadeSteeringBehavior.PanicDistance = 200;
        seekAgent.Visible = true;
        evadeAgent.Visible = true;
        seekAgent.ProcessMode = Node.ProcessModeEnum.Always;
        evadeAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Give time for the chaser to try to reach evader.
        await _sceneRunner.AwaitMillis(2000);
        
        // Assert the target was not reached.
        AssertThat(
            seekAgent.GlobalPosition.DistanceTo(evadeAgent.GlobalPosition) >=
            (evadeSteeringBehavior.PanicDistance)
            ).IsTrue();
    }
    
    /// <summary>
    /// Test that SeparationMatchingBehavior can separate its agent from two moving agents
    /// using a linear algorithm.
    /// </summary>
    [TestCase]
    public async Task SeparationBehaviorLinearTest()
    {
        // Get references to agent and target.
        MovingAgent velocityMatchingAgent =
            (MovingAgent)_sceneRunner.FindChild("VelocityMatchingMovingAgent");
        Marker2D agentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position7");
        MovingAgent targetMovingAgent =
            (MovingAgent)_sceneRunner.FindChild("ArriveMovingAgentLA");
        Marker2D targetMovingAgentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position8");
        Target targetOfTargetMovingAgent = (Target)_sceneRunner.FindChild("Target");
        Marker2D targetPosition =
            (Marker2D)_sceneRunner.FindChild("Position5");
        MovingAgent separationAgent = 
            (MovingAgent)_sceneRunner.FindChild("SeparationMovingAgent");
        Marker2D separationAgentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position2");
        
        // Get references to steering behavior from every agent.
        ArriveSteeringBehaviorLA arriveSteeringBehavior =
            targetMovingAgent.FindChild<ArriveSteeringBehaviorLA>();
        VelocityMatchingSteeringBehavior velocityMatchingSteeringBehavior =
            (VelocityMatchingSteeringBehavior) velocityMatchingAgent.FindChild(
                nameof(VelocityMatchingSteeringBehavior));
        SeparationSteeringBehavior separationSteeringBehavior =
            separationAgent.FindChild<SeparationSteeringBehavior>();
        
        // Setup agents before the test.
        targetOfTargetMovingAgent.GlobalPosition = targetPosition.GlobalPosition;
        velocityMatchingAgent.GlobalPosition = agentStartPosition.GlobalPosition;
        velocityMatchingAgent.MaximumSpeed = 200.0f;
        velocityMatchingAgent.MaximumAcceleration = 400.0f;
        velocityMatchingAgent.MaximumRotationalDegSpeed = 180f;
        velocityMatchingAgent.StopRotationDegThreshold = 1f;
        velocityMatchingAgent.StopSpeed = 10f;
        velocityMatchingAgent.MaximumAcceleration = 200;
        velocityMatchingAgent.MaximumDeceleration = 400;
        velocityMatchingAgent.AgentColor = Colors.Firebrick;
        targetMovingAgent.GlobalPosition = targetMovingAgentStartPosition.GlobalPosition;
        targetMovingAgent.MaximumSpeed = 200f;
        targetMovingAgent.StopSpeed = 1f;
        targetMovingAgent.MaximumRotationalDegSpeed = 180f;
        targetMovingAgent.StopRotationDegThreshold = 1f;
        targetMovingAgent.MaximumAcceleration = 180f;
        targetMovingAgent.MaximumDeceleration = 180f;
        targetMovingAgent.AgentColor = new Color(1, 0, 0);
        separationAgent.GlobalPosition = separationAgentStartPosition.GlobalPosition;
        velocityMatchingSteeringBehavior.Target = targetMovingAgent;
        velocityMatchingSteeringBehavior.TimeToMatch = 0.1f;
        arriveSteeringBehavior.Target = targetOfTargetMovingAgent;
        separationSteeringBehavior.Threats.Add(velocityMatchingAgent);
        separationSteeringBehavior.Threats.Add(targetMovingAgent);
        separationSteeringBehavior.SeparationThreshold = 410f;
        separationSteeringBehavior.DecayCoefficient = 20f;
        separationSteeringBehavior.SeparationAlgorithm =
            SeparationSteeringBehavior.SeparationAlgorithms.Linear;
        velocityMatchingAgent.Visible = true;
        targetMovingAgent.Visible = true;
        separationAgent.Visible = true;
        velocityMatchingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        targetMovingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        separationAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Start test.
        
        // Assert that both agents start under separation threshold.
        AssertThat(
            velocityMatchingAgent.GlobalPosition.DistanceTo(
                separationAgent.GlobalPosition) <=
            separationSteeringBehavior.SeparationThreshold
        ).IsTrue();
        AssertThat(
            targetMovingAgent.GlobalPosition.DistanceTo(
                separationAgent.GlobalPosition) <=
            separationSteeringBehavior.SeparationThreshold).IsTrue();
        
        // Let separation agent time to go away from the two agents.
        await _sceneRunner.AwaitMillis(4000);
        
        // Assert that both agents are above separation threshold.
        AssertThat(
            velocityMatchingAgent.GlobalPosition.DistanceTo(
                separationAgent.GlobalPosition) >=
            separationSteeringBehavior.SeparationThreshold
        ).IsTrue();
        AssertThat(
            targetMovingAgent.GlobalPosition.DistanceTo(
                separationAgent.GlobalPosition) >=
            separationSteeringBehavior.SeparationThreshold).IsTrue();
    }
    
    /// <summary>
    /// Test that SeparationMatchingBehavior can separate its agent from two moving agents
    /// using an inverse square algorithm.
    /// </summary>
    [TestCase]
    public async Task SeparationBehaviorInverseSquareTest()
    {
        // Get references to agent and target.
        MovingAgent velocityMatchingAgent =
            (MovingAgent)_sceneRunner.FindChild("VelocityMatchingMovingAgent");
        Marker2D agentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position7");
        MovingAgent targetMovingAgent =
            (MovingAgent)_sceneRunner.FindChild("ArriveMovingAgentLA");
        Marker2D targetMovingAgentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position8");
        Target targetOfTargetMovingAgent = (Target)_sceneRunner.FindChild("Target");
        Marker2D targetPosition =
            (Marker2D)_sceneRunner.FindChild("Position5");
        MovingAgent separationAgent = 
            (MovingAgent)_sceneRunner.FindChild("SeparationMovingAgent");
        Marker2D separationAgentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position2");
        
        // Get references to steering behavior from every agent.
        ArriveSteeringBehaviorLA arriveSteeringBehavior =
            targetMovingAgent.FindChild<ArriveSteeringBehaviorLA>();
        VelocityMatchingSteeringBehavior velocityMatchingSteeringBehavior =
            (VelocityMatchingSteeringBehavior) velocityMatchingAgent.FindChild(
                nameof(VelocityMatchingSteeringBehavior));
        SeparationSteeringBehavior separationSteeringBehavior =
            separationAgent.FindChild<SeparationSteeringBehavior>();
        
        // Setup agents before the test.
        targetOfTargetMovingAgent.GlobalPosition = targetPosition.GlobalPosition;
        velocityMatchingAgent.GlobalPosition = agentStartPosition.GlobalPosition;
        velocityMatchingAgent.MaximumSpeed = 200.0f;
        velocityMatchingAgent.MaximumAcceleration = 400.0f;
        velocityMatchingAgent.MaximumRotationalDegSpeed = 180f;
        velocityMatchingAgent.StopRotationDegThreshold = 1f;
        velocityMatchingAgent.StopSpeed = 10f;
        velocityMatchingAgent.MaximumAcceleration = 200;
        velocityMatchingAgent.MaximumDeceleration = 400;
        velocityMatchingAgent.AgentColor = Colors.Firebrick;
        targetMovingAgent.GlobalPosition = targetMovingAgentStartPosition.GlobalPosition;
        targetMovingAgent.MaximumSpeed = 200f;
        targetMovingAgent.StopSpeed = 1f;
        targetMovingAgent.MaximumRotationalDegSpeed = 180f;
        targetMovingAgent.StopRotationDegThreshold = 1f;
        targetMovingAgent.MaximumAcceleration = 180f;
        targetMovingAgent.MaximumDeceleration = 180f;
        targetMovingAgent.AgentColor = new Color(1, 0, 0);
        separationAgent.GlobalPosition = separationAgentStartPosition.GlobalPosition;
        velocityMatchingSteeringBehavior.Target = targetMovingAgent;
        velocityMatchingSteeringBehavior.TimeToMatch = 0.1f;
        arriveSteeringBehavior.Target = targetOfTargetMovingAgent;
        separationSteeringBehavior.Threats.Add(velocityMatchingAgent);
        separationSteeringBehavior.Threats.Add(targetMovingAgent);
        separationSteeringBehavior.SeparationThreshold = 410f;
        separationSteeringBehavior.DecayCoefficient = 20f;
        separationSteeringBehavior.SeparationAlgorithm =
            SeparationSteeringBehavior.SeparationAlgorithms.InverseSquare;
        velocityMatchingAgent.Visible = true;
        targetMovingAgent.Visible = true;
        separationAgent.Visible = true;
        velocityMatchingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        targetMovingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        separationAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Start test.
        
        // Assert that both agents start under separation threshold.
        AssertThat(
            velocityMatchingAgent.GlobalPosition.DistanceTo(
                separationAgent.GlobalPosition) <=
            separationSteeringBehavior.SeparationThreshold
        ).IsTrue();
        AssertThat(
            targetMovingAgent.GlobalPosition.DistanceTo(
                separationAgent.GlobalPosition) <=
            separationSteeringBehavior.SeparationThreshold).IsTrue();
        
        // Let time separation agent to go away from the two agents.
        await _sceneRunner.AwaitMillis(4000);
        
        // Assert that both agents are above separation threshold.
        // Assert that both agents start under separation threshold.
        AssertThat(
            velocityMatchingAgent.GlobalPosition.DistanceTo(
                separationAgent.GlobalPosition) >=
            separationSteeringBehavior.SeparationThreshold
        ).IsTrue();
        AssertThat(
            targetMovingAgent.GlobalPosition.DistanceTo(
                separationAgent.GlobalPosition) >=
            separationSteeringBehavior.SeparationThreshold).IsTrue();
    }
    
    
    /// <summary>
    /// Test that GroupAlignBehavior calculates the average between two target agent's
    /// orientations.
    /// </summary>
    [TestCase]
    public async Task GroupAlignBehaviorTest()
    {
        // Get references to agent and target.
        MovingAgent seekMovingAgent =
            (MovingAgent)_sceneRunner.FindChild("SeekMovingAgent");
        Marker2D seekStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position4");
        MovingAgent arriveMovingAgent =
            (MovingAgent)_sceneRunner.FindChild("ArriveMovingAgentLA");
        Marker2D arriveMovingAgentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position8");
        Target targetOfArriveMovingAgent = (Target)_sceneRunner.FindChild("Target");
        Marker2D targetPosition =
            (Marker2D)_sceneRunner.FindChild("Position7");
        MovingAgent groupAlignAgent = 
            (MovingAgent)_sceneRunner.FindChild("GroupAlignMovingAgent");
        Marker2D groupAlignAgentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position5");
        
        // Get references to steering behavior from every agent.
        ArriveSteeringBehaviorLA arriveSteeringBehavior =
            arriveMovingAgent.FindChild<ArriveSteeringBehaviorLA>();
        SeekSteeringBehavior seekSteeringBehavior =
            (SeekSteeringBehavior) seekMovingAgent.FindChild(
                nameof(SeekSteeringBehavior));
        GroupAlignSteeringBehavior groupAlignSteeringBehavior =
            groupAlignAgent.FindChild<GroupAlignSteeringBehavior>();
        
        // Setup agents before the test.
        targetOfArriveMovingAgent.GlobalPosition = targetPosition.GlobalPosition;
        seekMovingAgent.GlobalPosition = seekStartPosition.GlobalPosition;
        // Leave seekMovingAgent static. Y only want it to look at right.
        seekMovingAgent.MaximumSpeed = 0f;
        seekMovingAgent.MaximumRotationalDegSpeed = 0f;
        seekMovingAgent.AgentColor = Colors.Firebrick;
        arriveMovingAgent.GlobalPosition = arriveMovingAgentStartPosition.GlobalPosition;
        arriveMovingAgent.MaximumSpeed = 200f;
        arriveMovingAgent.StopSpeed = 1f;
        arriveMovingAgent.MaximumRotationalDegSpeed = 180f;
        arriveMovingAgent.StopRotationDegThreshold = 1f;
        arriveMovingAgent.MaximumAcceleration = 180f;
        arriveMovingAgent.MaximumDeceleration = 180f;
        arriveMovingAgent.AgentColor = new Color(1, 0, 0);
        groupAlignAgent.GlobalPosition = groupAlignAgentStartPosition.GlobalPosition;
        seekSteeringBehavior.Target = arriveMovingAgent;
        arriveSteeringBehavior.Target = targetOfArriveMovingAgent;
        groupAlignSteeringBehavior.Targets.Clear();
        groupAlignSteeringBehavior.Targets.Add(seekMovingAgent);
        groupAlignSteeringBehavior.Targets.Add(arriveMovingAgent);
        groupAlignSteeringBehavior.DecelerationRadius = 5f;
        groupAlignSteeringBehavior.AccelerationRadius = 5f;
        seekMovingAgent.Visible = true;
        arriveMovingAgent.Visible = true;
        groupAlignAgent.Visible = true;
        seekMovingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        arriveMovingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        groupAlignAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Start test.
        
        // Assert that group align agent starts with 0 rotation.
        AssertThat(Mathf.IsEqualApprox(groupAlignAgent.RotationDegrees, 0f)).IsTrue();
        
        // Let time arrive agent to go to its target.
        await _sceneRunner.AwaitMillis(4000);
        
        // Assert that group align agent is no longer at 0 rotation but at the average
        // of other two agents rotation.
        AssertThat(Mathf.IsEqualApprox(
            groupAlignAgent.RotationDegrees, 
            0f))
            .IsFalse();
        float rotationAverage = (seekMovingAgent.GlobalRotationDegrees + arriveMovingAgent.GlobalRotationDegrees) / 2f;
        AssertThat(Mathf.Abs(
                rotationAverage -
                groupAlignAgent.GlobalRotationDegrees) <= groupAlignAgent.StopRotationDegThreshold)
            .IsTrue();
    }
    
    /// <summary>
    /// Test that CohesionMatchingBehavior can place and agent in the center of mass of
    /// a 3 agent group.
    /// </summary>
    [TestCase]
    public async Task CohesionBehaviorTest()
    {
        // Get references to agent and target.
        MovingAgent velocityMatchingAgent =
            (MovingAgent)_sceneRunner.FindChild("VelocityMatchingMovingAgent");
        Marker2D velocityMatchingAgentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position8");
        MovingAgent arriveAgent =
            (MovingAgent)_sceneRunner.FindChild("ArriveMovingAgentLA");
        Marker2D arriveAgentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position7");
        Marker2D arriveAgentTarget =
            (Marker2D)_sceneRunner.FindChild("Position4");
        MovingAgent seekAgent =
            (MovingAgent)_sceneRunner.FindChild("SeekMovingAgent");
        Marker2D seekAgentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position2");
        Marker2D seekAgentTarget =
            (Marker2D)_sceneRunner.FindChild("Position3");
        MovingAgent cohesionAgent = 
            (MovingAgent)_sceneRunner.FindChild("CohesionMovingAgent");
        Marker2D cohesionAgentStartPosition =
            (Marker2D)_sceneRunner.FindChild("Position9");
        
        // Get references to steering behavior from every agent.
        ArriveSteeringBehaviorLA arriveSteeringBehavior =
            arriveAgent.FindChild<ArriveSteeringBehaviorLA>();
        VelocityMatchingSteeringBehavior velocityMatchingSteeringBehavior =
            velocityMatchingAgent.FindChild<VelocityMatchingSteeringBehavior>();
        SeekSteeringBehavior seekSteeringBehavior =
            seekAgent.FindChild<SeekSteeringBehavior>();
        CohesionSteeringBehavior cohesionSteeringBehavior =
            cohesionAgent.FindChild<CohesionSteeringBehavior>();
        
        // Setup agents before the test.
        velocityMatchingAgent.GlobalPosition = velocityMatchingAgentStartPosition.GlobalPosition;
        velocityMatchingAgent.MaximumSpeed = 200.0f;
        velocityMatchingAgent.MaximumAcceleration = 400.0f;
        velocityMatchingAgent.MaximumRotationalDegSpeed = 180f;
        velocityMatchingAgent.StopRotationDegThreshold = 1f;
        velocityMatchingAgent.StopSpeed = 10f;
        velocityMatchingAgent.MaximumAcceleration = 200;
        velocityMatchingAgent.MaximumDeceleration = 400;
        velocityMatchingAgent.AgentColor = new Color(1, 0, 0);
        arriveAgent.GlobalPosition = arriveAgentStartPosition.GlobalPosition;
        arriveAgent.MaximumSpeed = 200f;
        arriveAgent.StopSpeed = 1f;
        arriveAgent.MaximumRotationalDegSpeed = 180f;
        arriveAgent.StopRotationDegThreshold = 1f;
        arriveAgent.MaximumAcceleration = 180f;
        arriveAgent.MaximumDeceleration = 180f;
        arriveAgent.AgentColor = new Color(1, 0, 0);
        seekAgent.GlobalPosition = seekAgentStartPosition.GlobalPosition;
        seekAgent.MaximumSpeed = 200f;
        seekAgent.StopSpeed = 1f;
        seekAgent.MaximumRotationalDegSpeed = 180f;
        seekAgent.StopRotationDegThreshold = 1f;
        seekAgent.MaximumAcceleration = 180f;
        seekAgent.MaximumDeceleration = 180f;
        seekAgent.AgentColor = new Color(1, 0, 0);
        cohesionAgent.GlobalPosition = cohesionAgentStartPosition.GlobalPosition;
        cohesionAgent.MaximumSpeed = 200f;
        cohesionAgent.StopSpeed = 1f;
        cohesionAgent.MaximumRotationalDegSpeed = 180f;
        cohesionAgent.StopRotationDegThreshold = 1f;
        cohesionAgent.MaximumAcceleration = 180f;
        cohesionAgent.MaximumDeceleration = 180f;
        cohesionAgent.AgentColor = new Color(0, 1, 0);
        velocityMatchingSteeringBehavior.Target = arriveAgent;
        velocityMatchingSteeringBehavior.TimeToMatch = 0.1f;
        seekSteeringBehavior.Target = seekAgentTarget;
        seekSteeringBehavior.ArrivalDistance = 10;
        arriveSteeringBehavior.Target = arriveAgentTarget;
        arriveSteeringBehavior.ArrivalDistance = 10;
        cohesionSteeringBehavior.Targets.Clear();
        cohesionSteeringBehavior.Targets.Add(arriveAgent);
        cohesionSteeringBehavior.Targets.Add(velocityMatchingAgent);
        cohesionSteeringBehavior.Targets.Add(seekAgent);
        cohesionSteeringBehavior.ArrivalDistance = 10f;
        velocityMatchingAgent.Visible = true;
        arriveAgent.Visible = true;
        cohesionAgent.Visible = true;
        seekAgent.Visible = true;
        velocityMatchingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        arriveAgent.ProcessMode = Node.ProcessModeEnum.Always;
        cohesionAgent.ProcessMode = Node.ProcessModeEnum.Always;
        seekAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Start test.
        
        // Check that agent starts out of place.
        AssertThat(
                cohesionAgent.GlobalPosition ==
                cohesionSteeringBehavior.AveragePosition)
            .IsFalse();
        
        // Let it time to reach its position.
        await _sceneRunner.AwaitMillis(7000);
        
        // Check that agent now is in place.
        AssertThat(
                (cohesionAgent.GlobalPosition -
                cohesionSteeringBehavior.AveragePosition).Length() <= 
                cohesionSteeringBehavior.ArrivalDistance)
            .IsTrue();
    }
    
    /// <summary>
    /// Test that SeekBehavior can reach a target.
    /// </summary>
    [TestCase]
    public async Task WanderBehaviorTest()
    {
        // Get references to agent and target.
        MovingAgent wanderAgent = 
            (MovingAgent) _sceneRunner.FindChild("WanderMovingAgent");
        Marker2D agentStartPosition = 
            (Marker2D) _sceneRunner.FindChild("Position10");
        
        // Get reference to SteeringBehaviour.
        WanderSteeringBehavior wanderSteeringBehavior = 
            (WanderSteeringBehavior) wanderAgent.FindChild(
                nameof(WanderSteeringBehavior));
        
        // Setup agents before the test.
        wanderAgent.GlobalPosition = agentStartPosition.GlobalPosition;
        wanderAgent.MaximumSpeed = 100.0f;
        wanderAgent.StopSpeed = 1f;
        wanderAgent.MaximumRotationalDegSpeed = 1080f;
        wanderAgent.StopRotationDegThreshold = 1f;
        wanderAgent.AgentColor = new Color(0, 1, 0);
        wanderAgent.Visible = true;
        wanderAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        
        // Start test.
        int numberOfTestSamples = 5;
        List<Vector2> previousVelocities = new();
        foreach (var _ in Enumerable.Range(0, numberOfTestSamples))
        {
            // Give time the wander agent to move.
            await _sceneRunner.AwaitMillis(1000);
            // Sample its velocity.
            Vector2 currentVelocity = wanderAgent.Velocity;
            // Check that velocity is different from the previous ones.
            AssertThat(previousVelocities.Contains(currentVelocity)).IsFalse();
            // Store current velocity to be checked against in the next samples.
            previousVelocities.Add(currentVelocity);
        }
    }
}