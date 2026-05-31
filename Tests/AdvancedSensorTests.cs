using System.Linq;
using System.Threading.Tasks;
using GdUnit4;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Sensors;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;
using static GdUnit4.Assertions;

namespace GodotGameAIbyExample.Tests;

[TestSuite, RequireGodotRuntime]
public class AdvancedSensorTests
{
    private const string TestScenePath = "res://Tests/TestLevels/" +
                                         "AdvancedSensesTestLevel.tscn";

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
    /// Test that SoundEmitterMovingAgent is chased when it gets near SoundChaserAgents.
    /// </summary>
    [TestCase]
    public async Task SoundEmittingSensorTest()
    {
        // Get references to test agents.
        MovingAgent soundEmittingMovingAgent =
            (MovingAgent)_sceneRunner.FindChild("SoundEmitterMovingAgent");
        MovingAgent soundChaserMovingAgent =
            (MovingAgent)_sceneRunner.FindChild("SoundChaserMovingAgent");
        MovingAgent soundChaserMovingAgent2 =
            (MovingAgent)_sceneRunner.FindChild("SoundChaserMovingAgent2");
        MovingAgent soundChaserMovingAgent3 =
            (MovingAgent)_sceneRunner.FindChild("SoundChaserMovingAgent3");

        Scripts.Tools.Target target =
            (Scripts.Tools.Target)_sceneRunner.FindChild("Target");
        
        Marker2D position1 =
            (Marker2D)_sceneRunner.FindChild("Position1");
        Marker2D position2 =
            (Marker2D)_sceneRunner.FindChild("Position2");
        Marker2D position3 =
            (Marker2D)_sceneRunner.FindChild("Position3");
        Marker2D position4 =
            (Marker2D)_sceneRunner.FindChild("Position4");
        Marker2D position5 =
            (Marker2D)_sceneRunner.FindChild("Position5");
        Marker2D position6 =
            (Marker2D)_sceneRunner.FindChild("Position6");
        Marker2D position7 =
            (Marker2D)_sceneRunner.FindChild("Position7");
        Marker2D position8 =
            (Marker2D)_sceneRunner.FindChild("Position8");

        // Get references.
        SeekSteeringBehavior emitterSeekSteeringBehavior =
            soundEmittingMovingAgent.FindChild<SeekSteeringBehavior>(recursive: true);
        RegionSenseSoundSignalEmitter signalEmitter =
            soundEmittingMovingAgent.FindChild<RegionSenseSoundSignalEmitter>(recursive: true);
        RegionSenseSoundSensor chaserSensor = 
            soundChaserMovingAgent.FindChild<RegionSenseSoundSensor>(recursive: true);
        RegionSenseSoundSensor chaserSensor2 = 
            soundChaserMovingAgent2.FindChild<RegionSenseSoundSensor>(recursive: true);
        RegionSenseSoundSensor chaserSensor3 = 
            soundChaserMovingAgent3.FindChild<RegionSenseSoundSensor>(recursive: true);

        // Setup agents before the test.ç
        target.GlobalPosition = position1.GlobalPosition;
        
        soundEmittingMovingAgent.MaximumSpeed = 400f;
        soundEmittingMovingAgent.GlobalPosition = position1.GlobalPosition;
        soundEmittingMovingAgent.RotationDegrees = 0;
        soundEmittingMovingAgent.Visible = true;
        emitterSeekSteeringBehavior.Target = target;
        signalEmitter.ModalityMaximumRange = 1000f;
        signalEmitter.ModalityAttenuation = 0.9f;
        signalEmitter.ModalityInverseTransmissionSpeed = 2 / 100f;
        signalEmitter.EmissionPeriod = 0.3f;
        signalEmitter.AutoStartEmission = true;
        signalEmitter.SignalStrength = 100f;

        soundChaserMovingAgent.GlobalPosition = position3.GlobalPosition;
        soundChaserMovingAgent.AgentColor = new Color(1, 0, 0);
        soundChaserMovingAgent.MaximumSpeed = 300.0f;
        soundChaserMovingAgent.Visible = true;
        chaserSensor.DetectionBufferSize = 100;
        chaserSensor.MinimumStrengthDetectionThreshold = 40;
        chaserSensor.DetectionExpirationTime = 1.0f;
        chaserSensor.CleaningPeriod = 0.3f;
        
        soundChaserMovingAgent2.GlobalPosition = position4.GlobalPosition;
        soundChaserMovingAgent2.AgentColor = new Color(1, 0, 0);
        soundChaserMovingAgent2.MaximumSpeed = 300.0f;
        soundChaserMovingAgent2.Visible = true;
        chaserSensor2.DetectionBufferSize = 100;
        chaserSensor2.MinimumStrengthDetectionThreshold = 40;
        chaserSensor2.DetectionExpirationTime = 1.0f;
        chaserSensor2.CleaningPeriod = 0.3f;
        
        soundChaserMovingAgent3.GlobalPosition = position2.GlobalPosition;
        soundChaserMovingAgent3.AgentColor = new Color(1, 0, 0);
        soundChaserMovingAgent3.MaximumSpeed = 300.0f;
        soundChaserMovingAgent3.Visible = true;
        chaserSensor3.DetectionBufferSize = 100;
        chaserSensor3.MinimumStrengthDetectionThreshold = 30;
        chaserSensor3.DetectionExpirationTime = 1.0f;
        chaserSensor3.CleaningPeriod = 0.3f;

        soundEmittingMovingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        soundChaserMovingAgent.ProcessMode = Node.ProcessModeEnum.Always;
        soundChaserMovingAgent2.ProcessMode = Node.ProcessModeEnum.Always;
        soundChaserMovingAgent3.ProcessMode = Node.ProcessModeEnum.Always;

        // Start test.

        // The soundEmitter has not moved yet, nor any of the chasers.
        await _sceneRunner.AwaitMillis(5000);
        AssertThat(
            Mathf.IsEqualApprox(position1.GlobalPosition.DistanceTo(soundEmittingMovingAgent.GlobalPosition), 0)).IsTrue();
        AssertThat(
            Mathf.IsEqualApprox(position2.GlobalPosition.DistanceTo(soundChaserMovingAgent3.GlobalPosition), 0)).IsTrue();
        AssertThat(
            Mathf.IsEqualApprox(position3.GlobalPosition.DistanceTo(soundChaserMovingAgent.GlobalPosition), 0)).IsTrue();
        AssertThat(
            Mathf.IsEqualApprox(position4.GlobalPosition.DistanceTo(soundChaserMovingAgent2.GlobalPosition), 0)).IsTrue();
        
        // Change to a position where one of the chasers can hear us.
        target.GlobalPosition = position5.GlobalPosition;
        await _sceneRunner.AwaitMillis(7000);
        target.GlobalPosition = position6.GlobalPosition;
        await _sceneRunner.AwaitMillis(5000);
        // Did we make the chaser move?
        AssertThat(
            position2.GlobalPosition.DistanceTo(soundChaserMovingAgent3.GlobalPosition) > 100).IsTrue();
        // The other two should not have moved.
        AssertThat(
            Mathf.IsEqualApprox(position3.GlobalPosition.DistanceTo(soundChaserMovingAgent.GlobalPosition), 0)).IsTrue();
        AssertThat(
            Mathf.IsEqualApprox(position4.GlobalPosition.DistanceTo(soundChaserMovingAgent2.GlobalPosition), 0)).IsTrue();
        
        // Change to a position where the second can hear us.
        target.GlobalPosition = position7.GlobalPosition;
        await _sceneRunner.AwaitMillis(2000);
        target.GlobalPosition = position8.GlobalPosition;
        await _sceneRunner.AwaitMillis(3000);
        target.GlobalPosition = position1.GlobalPosition;
        await _sceneRunner.AwaitMillis(2000);
        // Did we make the chaser move?
        AssertThat(
            position3.GlobalPosition.DistanceTo(soundChaserMovingAgent.GlobalPosition) > 100).IsTrue();
        // The last chased should not have moved.
        AssertThat(
            Mathf.IsEqualApprox(position4.GlobalPosition.DistanceTo(soundChaserMovingAgent2.GlobalPosition), 0)).IsTrue();

        // Cleanup.
        soundEmittingMovingAgent.Visible = false;
        soundEmittingMovingAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
        soundChaserMovingAgent.Visible = false;
        soundChaserMovingAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
        soundChaserMovingAgent2.Visible = false;
        soundChaserMovingAgent2.ProcessMode = Node.ProcessModeEnum.Disabled;
        soundChaserMovingAgent3.Visible = false;
        soundChaserMovingAgent3.ProcessMode = Node.ProcessModeEnum.Disabled;
    }
}