using System.Linq;
using System.Threading.Tasks;
using GdUnit4;
using Godot;
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
            (Marker2D) _sceneRunner.FindChild("StartPosition1");
        Target target = (Target) _sceneRunner.FindChild("Target");
        Marker2D targetPosition = 
            (Marker2D) _sceneRunner.FindChild("TargetPosition1");
        
        // Get reference to SteeringBehaviour.
        SeekSteeringBehavior steeringBehavior = 
            (SeekSteeringBehavior) movingAgent.FindChild(
                nameof(SeekSteeringBehavior));
        
        // Place target and agent.
        target.GlobalPosition = targetPosition.GlobalPosition;
        movingAgent.GlobalPosition = agentStartPosition.GlobalPosition;
        
        // Start test.
        movingAgent.Visible = true;
        movingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        // Give agent time to reach target.
        await _sceneRunner.AwaitMillis(2500);
        // Check if agent reached target.
        float distance = movingAgent.GlobalPosition.DistanceTo(target.GlobalPosition);
        AssertThat(distance <= steeringBehavior.ArrivalDistance).IsTrue();
    }
    
    /// <summary>
    /// Test that ArriveBehavior can reach a target and that it accelerates
    /// at the beginning and brakes at the end.
    /// </summary>
    [TestCase]
    public async Task ArriveBehaviorTest()
    {
        // Get references to agent and target.
        MovingAgent movingAgent = 
            (MovingAgent) _sceneRunner.FindChild("ArriveMovingAgent");
        Marker2D agentStartPosition = 
            (Marker2D) _sceneRunner.FindChild("StartPosition1");
        Target target = (Target) _sceneRunner.FindChild("Target");
        Marker2D targetPosition = 
            (Marker2D) _sceneRunner.FindChild("TargetPosition1");
        
        // Get reference to ArriveSteeringBehaviour.
        ArriveSteeringBehavior steeringBehavior = 
            (ArriveSteeringBehavior) movingAgent.FindChild(
                nameof(ArriveSteeringBehavior));
        
        // Place target and agent.
        target.GlobalPosition = targetPosition.GlobalPosition;
        movingAgent.GlobalPosition = agentStartPosition.GlobalPosition;
        
        // Start test.
        movingAgent.Visible = true;
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
    /// Test that FleeBehavior makes agent go away from its threath.
    /// </summary>
    [TestCase]
    public async Task FleeBehaviorTest()
    {
        // Get references to agent and target.
        MovingAgent movingAgent =
            (MovingAgent)_sceneRunner.FindChild("FleeMovingAgent");
        Marker2D agentStartPosition =
            (Marker2D)_sceneRunner.FindChild("StartPosition2");
        Target target = (Target)_sceneRunner.FindChild("Target");
        Marker2D targetPosition =
            (Marker2D)_sceneRunner.FindChild("TargetPosition1");

        // Get reference to FleeSteeringBehaviour.
        FleeSteeringBehavior steeringBehavior =
            (FleeSteeringBehavior)movingAgent.FindChild(
                nameof(FleeSteeringBehavior));

        // Place target and agent.
        target.GlobalPosition = targetPosition.GlobalPosition;
        movingAgent.GlobalPosition = agentStartPosition.GlobalPosition;

        // Start test.
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
            (Marker2D)_sceneRunner.FindChild("StartPosition2");
        MovingAgent alignAgent =
            (MovingAgent)_sceneRunner.FindChild("AlignMovingAgent");
        Marker2D alignAgentStartPosition =
            (Marker2D)_sceneRunner.FindChild("StartPosition1");
        Target target = (Target) _sceneRunner.FindChild("Target");
        Marker2D targetPosition1 = 
            (Marker2D) _sceneRunner.FindChild("TargetPosition1");
        Marker2D targetPosition2 = 
            (Marker2D) _sceneRunner.FindChild("TargetPosition2");
        Marker2D targetPosition3 = 
            (Marker2D) _sceneRunner.FindChild("TargetPosition3");
        
        // Get references to steering behavior from both agents.
        SeekSteeringBehavior seekSteeringBehavior =
            (SeekSteeringBehavior) movingAgent.FindChild(
                nameof(SeekSteeringBehavior));
        AlignSteeringBehavior alignSteeringBehavior =
            (AlignSteeringBehavior) alignAgent.FindChild(
                nameof(AlignSteeringBehavior));
        
        // Place and setup both agents before the test.
        movingAgent.GlobalPosition = movingAgentStartPosition.GlobalPosition;
        alignAgent.GlobalPosition = alignAgentStartPosition.GlobalPosition;
        alignSteeringBehavior.Target = movingAgent;
        seekSteeringBehavior.Target = target;
        movingAgent.Visible = true;
        alignAgent.Visible = true;
        movingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        alignAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Start test.
        
        // Move seeker to face the first target.
        target.GlobalPosition = targetPosition1.GlobalPosition;
        await _sceneRunner.AwaitMillis(2000);
        AssertThat(Mathf.IsEqualApprox(
            alignAgent.Orientation, 
            movingAgent.Orientation,
            alignSteeringBehavior.ArrivingMargin)).IsTrue();
        
        // Move seeker to face the second target.
        target.GlobalPosition = targetPosition2.GlobalPosition;
        await _sceneRunner.AwaitMillis(2000);
        AssertThat(Mathf.IsEqualApprox(
            alignAgent.Orientation, 
            movingAgent.Orientation,
            alignSteeringBehavior.ArrivingMargin)).IsTrue();
        
        // Move seeker to face the third target.
        target.GlobalPosition = targetPosition3.GlobalPosition;
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
            (Marker2D)_sceneRunner.FindChild("TargetPosition1");
        MovingAgent targetMovingAgent =
            (MovingAgent)_sceneRunner.FindChild("SeekMovingAgent");
        Marker2D targetMovingAgentStartPosition =
            (Marker2D)_sceneRunner.FindChild("TargetPosition4");
        Target targetOfTargetMovingAgent = (Target)_sceneRunner.FindChild("Target");
        Marker2D targetPosition =
            (Marker2D)_sceneRunner.FindChild("TargetPosition2");
        
        // Get references to steering behavior from both agents.
        SeekSteeringBehavior seekSteeringBehavior =
            (SeekSteeringBehavior) targetMovingAgent.FindChild(
                nameof(SeekSteeringBehavior));
        FaceSteeringBehavior faceSteeringBehavior =
            (FaceSteeringBehavior) faceAgent.FindChild(
                nameof(FaceSteeringBehavior));
        
        // Place and setup both agents before the test.
        faceAgent.GlobalPosition = agentStartPosition.GlobalPosition;
        targetMovingAgent.GlobalPosition = targetMovingAgentStartPosition.GlobalPosition;
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

}