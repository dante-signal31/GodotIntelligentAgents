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
    }
    
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
}