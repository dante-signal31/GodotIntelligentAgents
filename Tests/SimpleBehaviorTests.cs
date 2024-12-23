using System.Threading.Tasks;
using GdUnit4;
using static GdUnit4.Assertions;
using Godot;

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
        
        // Place target and agent.
        _target.GlobalPosition = _targetPosition.GlobalPosition;
        _movingAgent.GlobalPosition = _agentStartPosition.GlobalPosition;
        
        // Start test.
        _movingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        // Give agent time to reach target.
        await _sceneRunner.AwaitMillis(2500);
        // Check if agent reached target.
        float distance = _movingAgent.GlobalPosition.DistanceTo(_target.GlobalPosition);
        // 4 pixels is an acceptable distance.
        AssertThat(distance < 4f).IsTrue();
    }
}