using System.Collections.Generic;
using System.Threading.Tasks;
using GdUnit4;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Groups;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;
using static GdUnit4.Assertions;

namespace GodotGameAIbyExample.Tests;

[TestSuite, RequireGodotRuntime]
public class FixedFormationTests
{
    private const string TestScenePath = "res://Tests/TestLevels/" +
                                         "FormationTestLevel.tscn";

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
    /// Test that formation can make a full turn in unrealistic mode.
    /// </summary>
    [TestCase]
    public async Task FixedFormationNonRealisticTurnDownUpTest()
    { 
        // Get references to agent and target.
        
        UsherFormationAgent formationAgent = 
            (UsherFormationAgent) _sceneRunner.FindChild("UsherFormationAgent");

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
            formationAgent.FindChild<SeekSteeringBehavior>();
        
        // Set up agents before the test.
        formationAgent.RealisticTurns = false;
        
        target.GlobalPosition = position2.GlobalPosition;
        formationAgent.GlobalPosition = position1.GlobalPosition;
        seekSteeringBehavior.Target = target;

        formationAgent.Visible = true;
        formationAgent.ProcessMode = Node.ProcessModeEnum.Inherit;
        
        
        // Start test.
        
        // Assert that formation reached its target.
        await _sceneRunner.AwaitMillis(7000);
        AssertThat(
            formationAgent.GlobalPosition.DistanceTo(position2.GlobalPosition) < 50f
        ).IsTrue();
        
        
        // Move the target to another position.
        target.GlobalPosition = position3.GlobalPosition;
        
        // Assert that formation reached its target.
        await _sceneRunner.AwaitMillis(10000);
        AssertThat(
            formationAgent.GlobalPosition.DistanceTo(position3.GlobalPosition) < 50f
        ).IsTrue();
        
        
        // Move target to another position.
        target.GlobalPosition = position4.GlobalPosition;
        
        // Assert that formation reached its target.
        await _sceneRunner.AwaitMillis(5000);
        AssertThat(
            formationAgent.GlobalPosition.DistanceTo(position4.GlobalPosition) < 50f
        ).IsTrue();
        
        // Cleanup.
        formationAgent.Visible = false;
        formationAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
    }
    
     /// <summary>
    /// Test that formation can make a full turn in realistic mode.
    /// </summary>
    [TestCase]
    public async Task FixedFormationRealisticTurnDownUpTest()
    { 
        // Get references to agent and target.
        
        UsherFormationAgent formationAgent = 
            (UsherFormationAgent) _sceneRunner.FindChild("UsherFormationAgent");

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
            formationAgent.FindChild<SeekSteeringBehavior>();
        
        // Set up agents before the test.
        formationAgent.RealisticTurns = true;
        
        target.GlobalPosition = position2.GlobalPosition;
        formationAgent.GlobalPosition = position1.GlobalPosition;
        seekSteeringBehavior.Target = target;

        formationAgent.Visible = true;
        formationAgent.ProcessMode = Node.ProcessModeEnum.Inherit;
        
        
        // Start test.
        
        // Assert that formation reached its target.
        await _sceneRunner.AwaitMillis(7000);
        AssertThat(
            formationAgent.GlobalPosition.DistanceTo(position2.GlobalPosition) < 50f
        ).IsTrue();
        
        
        // Move target to another position.
        target.GlobalPosition = position3.GlobalPosition;
        
        // Assert that formation reached its target.
        await _sceneRunner.AwaitMillis(15000);
        AssertThat(
            formationAgent.GlobalPosition.DistanceTo(position3.GlobalPosition) < 50f
        ).IsTrue();
        
        
        // Move target to another position.
        target.GlobalPosition = position4.GlobalPosition;
        
        // Assert that formation reached its target.
        await _sceneRunner.AwaitMillis(11000);
        AssertThat(
            formationAgent.GlobalPosition.DistanceTo(position4.GlobalPosition) < 50f
        ).IsTrue();
        
        // Cleanup.
        formationAgent.Visible = false;
        formationAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
    }
     
    /// <summary>
    /// Test that formation can make a full turn in unrealistic mode.
    /// </summary>
    [TestCase]
    public async Task FixedFormationNonRealisticTurnUpDownTest()
    { 
        // Get references to agent and target.
        
        UsherFormationAgent formationAgent = 
            (UsherFormationAgent) _sceneRunner.FindChild("UsherFormationAgent");

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
            formationAgent.FindChild<SeekSteeringBehavior>();
        
        // Set up agents before the test.
        formationAgent.RealisticTurns = false;
        
        target.GlobalPosition = position3.GlobalPosition;
        formationAgent.GlobalPosition = position4.GlobalPosition;
        seekSteeringBehavior.Target = target;

        formationAgent.Visible = true;
        formationAgent.ProcessMode = Node.ProcessModeEnum.Inherit;
        
        
        // Start test.
        
        // Assert that formation reached its target.
        await _sceneRunner.AwaitMillis(5000);
        AssertThat(
            formationAgent.GlobalPosition.DistanceTo(position3.GlobalPosition) < 50f
        ).IsTrue();
        
        
        // Move the target to another position.
        target.GlobalPosition = position2.GlobalPosition;
        
        // Assert that formation reached its target.
        await _sceneRunner.AwaitMillis(10000);
        AssertThat(
            formationAgent.GlobalPosition.DistanceTo(position2.GlobalPosition) < 50f
        ).IsTrue();
        
        
        // Move target to another position.
        target.GlobalPosition = position1.GlobalPosition;
        
        // Assert that formation reached its target.
        await _sceneRunner.AwaitMillis(7000);
        AssertThat(
            formationAgent.GlobalPosition.DistanceTo(position1.GlobalPosition) < 50f
        ).IsTrue();
        
        
        // Cleanup.
        formationAgent.Visible = false;
        formationAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
    }
    
    /// <summary>
    /// Test that formation can make a full turn in realistic mode.
    /// </summary>
    [TestCase]
    public async Task FixedFormationRealisticTurnUpDownTest()
    { 
        // Get references to agent and target.
        
        UsherFormationAgent formationAgent = 
            (UsherFormationAgent) _sceneRunner.FindChild("UsherFormationAgent");

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
            formationAgent.FindChild<SeekSteeringBehavior>();
        
        // Set up agents before the test.
        formationAgent.RealisticTurns = true;
        
        target.GlobalPosition = position3.GlobalPosition;
        formationAgent.GlobalPosition = position4.GlobalPosition;
        seekSteeringBehavior.Target = target;

        formationAgent.Visible = true;
        formationAgent.ProcessMode = Node.ProcessModeEnum.Inherit;
        
        
        // Start test.
        
        // Assert that formation reached its target.
        await _sceneRunner.AwaitMillis(5000);
        AssertThat(
            formationAgent.GlobalPosition.DistanceTo(position3.GlobalPosition) < 50f
        ).IsTrue();
        
        
        // Move the target to another position.
        target.GlobalPosition = position2.GlobalPosition;
        
        // Assert that formation reached its target.
        await _sceneRunner.AwaitMillis(15000);
        AssertThat(
            formationAgent.GlobalPosition.DistanceTo(position2.GlobalPosition) < 50f
        ).IsTrue();
        
        
        // Move target to another position.
        target.GlobalPosition = position1.GlobalPosition;
        
        // Assert that formation reached its target.
        await _sceneRunner.AwaitMillis(11000);
        AssertThat(
            formationAgent.GlobalPosition.DistanceTo(position1.GlobalPosition) < 50f
        ).IsTrue();
        
        // Cleanup.
        formationAgent.Visible = false;
        formationAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
    }
}