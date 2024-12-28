using System.Threading.Tasks;
using GdUnit4;
using static GdUnit4.Assertions;
using Godot;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;
using GodotGameAIbyExample.Scripts.Tools;

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
        MovingAgent _movingAgent = 
            (MovingAgent) _sceneRunner.FindChild("SeekMovingAgent");
        Marker2D _agentStartPosition = 
            (Marker2D) _sceneRunner.FindChild("StartPosition1");
        Target _target = (Target) _sceneRunner.FindChild("Target");
        Marker2D _targetPosition = 
            (Marker2D) _sceneRunner.FindChild("TargetPosition1");
        
        // Get reference to SteeringBehaviour.
        SeekSteeringBehavior steeringBehavior = 
            (SeekSteeringBehavior) _movingAgent.FindChild(
                nameof(SeekSteeringBehavior));
        
        // Place target and agent.
        _target.GlobalPosition = _targetPosition.GlobalPosition;
        _movingAgent.GlobalPosition = _agentStartPosition.GlobalPosition;
        
        // Start test.
        _movingAgent.Visible = true;
        _movingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        // Give agent time to reach target.
        await _sceneRunner.AwaitMillis(2500);
        // Check if agent reached target.
        float distance = _movingAgent.GlobalPosition.DistanceTo(_target.GlobalPosition);
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
        MovingAgent _movingAgent = 
            (MovingAgent) _sceneRunner.FindChild("ArriveMovingAgent");
        Marker2D _agentStartPosition = 
            (Marker2D) _sceneRunner.FindChild("StartPosition1");
        Target _target = (Target) _sceneRunner.FindChild("Target");
        Marker2D _targetPosition = 
            (Marker2D) _sceneRunner.FindChild("TargetPosition1");
        
        // Get reference to ArriveSteeringBehaviour.
        ArriveSteeringBehavior steeringBehavior = 
            (ArriveSteeringBehavior) _movingAgent.FindChild(
                nameof(ArriveSteeringBehavior));
        
        // Place target and agent.
        _target.GlobalPosition = _targetPosition.GlobalPosition;
        _movingAgent.GlobalPosition = _agentStartPosition.GlobalPosition;
        
        // Start test.
        _movingAgent.Visible = true;
        _movingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Check that agent is accelerating at the beginning.
        while ( // Wait until agent starts is movement.
            !((_agentStartPosition.GlobalPosition.DistanceTo(
                   _movingAgent.GlobalPosition) >= 1) && 
              (_agentStartPosition.GlobalPosition.DistanceTo(
                  _movingAgent.GlobalPosition) < steeringBehavior.AccelerationRadius))
            )
        {
            await _sceneRunner.AwaitIdleFrame();
        }
        AssertThat(_movingAgent.CurrentSpeed > 0 && 
                   _movingAgent.CurrentSpeed < _movingAgent.MaximumSpeed).IsTrue();
        
        // Check that agent gets its full cruise speed.
        while ( // Wait until we get full speed.
            !(_agentStartPosition.GlobalPosition.DistanceTo(
                     _movingAgent.GlobalPosition) > steeringBehavior.AccelerationRadius)
            )
        {
            await _sceneRunner.AwaitIdleFrame();
        }
        AssertThat(Mathf.IsEqualApprox(
            _movingAgent.CurrentSpeed, 
            _movingAgent.MaximumSpeed, 
            1f))
            .IsTrue();
        
        // Check that agent is braking at the end.
        while ( // Wait until we start to brake.
               !(_movingAgent.GlobalPosition.DistanceTo(
                   _targetPosition.GlobalPosition) <= steeringBehavior.BrakingRadius)
              )
        {
            await _sceneRunner.AwaitIdleFrame();
        }
        await _sceneRunner.AwaitIdleFrame();
        await _sceneRunner.AwaitIdleFrame();
        AssertThat(_movingAgent.CurrentSpeed > 0 && 
                   _movingAgent.CurrentSpeed < _movingAgent.MaximumSpeed).IsTrue();
        
        //Assert target was reached.
        // Give agent time to reach target.
        await _sceneRunner.AwaitMillis(1500);
        // Check if agent reached target.
        float distance = _movingAgent.GlobalPosition.DistanceTo(_target.GlobalPosition);
        AssertThat(distance <= steeringBehavior.ArrivalDistance).IsTrue();
    }

    /// <summary>
    /// Test that FleeBehavior makes agent go away from its threath.
    /// </summary>
    [TestCase]
    public async Task FleeBehaviorTest()
    {
        // Get references to agent and target.
        MovingAgent _movingAgent =
            (MovingAgent)_sceneRunner.FindChild("FleeMovingAgent");
        Marker2D _agentStartPosition =
            (Marker2D)_sceneRunner.FindChild("StartPosition2");
        Target _target = (Target)_sceneRunner.FindChild("Target");
        Marker2D _targetPosition =
            (Marker2D)_sceneRunner.FindChild("TargetPosition1");

        // Get reference to FleeSteeringBehaviour.
        FleeSteeringBehavior steeringBehavior =
            (FleeSteeringBehavior)_movingAgent.FindChild(
                nameof(FleeSteeringBehavior));

        // Place target and agent.
        _target.GlobalPosition = _targetPosition.GlobalPosition;
        _movingAgent.GlobalPosition = _agentStartPosition.GlobalPosition;

        // Start test.
        _movingAgent.Visible = true;
        _movingAgent.ProcessMode = Node.ProcessModeEnum.Always;

        // Place 5 targets in random positions and check that the agent flees.
        int testSamples = 5;
        for (int i = 0; i < testSamples; i++)
        {
            Vector2 randomPositionInLocalCircle = 
                RandomPointGenerator.GetRandomPointInCircle(steeringBehavior.PanicDistance);
            // Place target in random position.
            _target.GlobalPosition = _movingAgent.GlobalPosition +
                                             randomPositionInLocalCircle;

            // Give agent time to flee target.
            await _sceneRunner.AwaitMillis(1000);

            // Check if agent is fleeing target asserting that agent is now farther from
            // the target than before.
            float distance = _movingAgent.GlobalPosition.DistanceTo(
                _target.GlobalPosition);
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

}