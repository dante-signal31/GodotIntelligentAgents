using System.Threading.Tasks;
using GdUnit4;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Sensors;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;
using static GdUnit4.Assertions;

namespace GodotGameAIbyExample.Tests;

[TestSuite, RequireGodotRuntime]
public class SensorTests
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
    public async Task ConeSensorTest()
    {
        // Get references to test agents.
        MovingAgent coneSensorMovingAgent = 
            (MovingAgent)_sceneRunner.FindChild("ConeSensorMovingAgent");
        MovingAgent hideMovingAgent =
            (MovingAgent)_sceneRunner.FindChild("HideMovingAgent");
        MovingAgent seekMovingAgent = 
            (MovingAgent)_sceneRunner.FindChild("SeekMovingAgent");
        MovingAgent pathFollowingMovingAgent = 
            (MovingAgent)_sceneRunner.FindChild("PathFollowingMovingAgent");
        
        Scripts.Tools.Target target =
            (Scripts.Tools.Target)_sceneRunner.FindChild("Target");
        Marker2D position4 =
            (Marker2D)_sceneRunner.FindChild("Position4");
        Marker2D position2 =
            (Marker2D)_sceneRunner.FindChild("Position2");
        Marker2D position6 =
            (Marker2D)_sceneRunner.FindChild("Position6");
        Marker2D position1 =
            (Marker2D)_sceneRunner.FindChild("Position1");
        Marker2D position9 =
            (Marker2D)_sceneRunner.FindChild("Position9");
        Marker2D position8 =
            (Marker2D)_sceneRunner.FindChild("Position8");

        // Get reference to sensor.
        ConeSensor coneSensor =
            coneSensorMovingAgent.FindChild<ConeSensor>();
        
        // Setup agents before the test.
        coneSensorMovingAgent.MaximumSpeed = 0.0f;
        coneSensorMovingAgent.GlobalPosition = position4.GlobalPosition;
        coneSensorMovingAgent.RotationDegrees = 0;
        coneSensorMovingAgent.Visible = true;
        
        hideMovingAgent.GlobalPosition = position1.GlobalPosition;
        hideMovingAgent.AgentColor = new Color(1, 0, 0);
        hideMovingAgent.MaximumSpeed = 0.0f;
        hideMovingAgent.Visible = true;
        
        seekMovingAgent.GlobalPosition = position9.GlobalPosition;
        seekMovingAgent.AgentColor = new Color(1, 0, 0);
        seekMovingAgent.MaximumSpeed = 0.0f;
        seekMovingAgent.Visible = true;
        
        pathFollowingMovingAgent.GlobalPosition = position8.GlobalPosition;
        pathFollowingMovingAgent.AgentColor = new Color(1, 0, 0);
        pathFollowingMovingAgent.MaximumSpeed = 0.0f;
        pathFollowingMovingAgent.Visible = true;
        
        coneSensorMovingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        hideMovingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        seekMovingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        pathFollowingMovingAgent.ProcessMode = Node.ProcessModeEnum.Always;

        // Start test.

        // The coneSensorAgents looks away from the other agents. So, it should detect
        // none.
        await _sceneRunner.AwaitMillis(1000);
        AssertThat(coneSensor.DetectedObjects.Count == 0).IsTrue();

        // Change to a position where it should see the three of the other agents.
        coneSensorMovingAgent.GlobalPosition = position2.GlobalPosition;
        coneSensorMovingAgent.RotationDegrees = -120;
        // Give cone sensor time to detect agents.
        await _sceneRunner.AwaitMillis(1000);
        AssertThat(coneSensor.DetectedObjects.Count == 3).IsTrue();
        
        // Change to a position where it should only one of the agents because the
        // other two are behind an obstacle. 
        coneSensorMovingAgent.GlobalPosition = position6.GlobalPosition;
        coneSensorMovingAgent.RotationDegrees = 45;
        // Give cone sensor time to detect agents.
        await _sceneRunner.AwaitMillis(1000);
        AssertThat(coneSensor.DetectedObjects.Count == 1).IsTrue();
        
        // Cleanup.
        coneSensorMovingAgent.Visible = false;
        coneSensorMovingAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
        hideMovingAgent.Visible = false;
        hideMovingAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
        seekMovingAgent.Visible = false;
        seekMovingAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
        pathFollowingMovingAgent.Visible = false;
        pathFollowingMovingAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
    }
}