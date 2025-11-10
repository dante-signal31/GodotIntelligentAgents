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
public class TwoLevelFormationTests
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
    public async Task TwoLevelFormationNonRealisticTurnDownUpTest()
    { 
        // Get references to agent and target.
        UsherWaiterFormationAgent formationAgent = 
            (UsherWaiterFormationAgent) _sceneRunner.FindChild("UsherWaiterObstacleManagedTwoLevelFormationAgent");

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
        formationAgent.MaximumLaggingBehindDistance = 500;
        formationAgent.MaximumSpeed = 200;
        
        target.GlobalPosition = position2.GlobalPosition;
        // Try to keep the tested formation at position1 inside test level, or
        // initial offset between formation origin and member average point will
        // go wrong. That is because in these test suites agents are not created where they
        // begin to work; instead, they are created at one place and teleported afterward,
        // and that can create problems not present at the real game.
        formationAgent.GlobalPosition = position1.GlobalPosition;
        seekSteeringBehavior.Target = target;

        formationAgent.Visible = true;
        formationAgent.ProcessMode = Node.ProcessModeEnum.Inherit;
        
        
        // Start test.
        
        await _sceneRunner.AwaitMillis(7000);
        
        // Assert that formation reached its target.
        AssertThat(
            formationAgent.GlobalPosition.DistanceTo(position2.GlobalPosition) < 50f
        ).IsTrue();
        
        // Assert that members reached their ushers.
        for (int i=0; i < formationAgent.Formation.Members.Count; i++) 
        {
            Node2D member = formationAgent.Formation.Members[i];
            Vector2 usherPosition = formationAgent.ToGlobal(
                formationAgent.Formation.MemberPositions[i]);
            AssertThat(
                member.GlobalPosition.DistanceTo(usherPosition) < 50f
            ).IsTrue();
        }
        
        
        // Move the target to another position.
        target.GlobalPosition = position3.GlobalPosition;
        await _sceneRunner.AwaitMillis(11000);
        
        // Assert that formation reached its target.
        AssertThat(
            formationAgent.GlobalPosition.DistanceTo(position3.GlobalPosition) < 50f
        ).IsTrue();
        
        // Assert that members reached their ushers.
        for (int i=0; i < formationAgent.Formation.Members.Count; i++) 
        {
            Node2D member = formationAgent.Formation.Members[i];
            Vector2 usherPosition = formationAgent.ToGlobal(
                formationAgent.Formation.MemberPositions[i]);
            AssertThat(
                member.GlobalPosition.DistanceTo(usherPosition) < 50f
            ).IsTrue();
        }
        
        
        // Move target to another position.
        target.GlobalPosition = position4.GlobalPosition;
        await _sceneRunner.AwaitMillis(7000);
        
        // Assert that formation reached its target.
        AssertThat(
            formationAgent.GlobalPosition.DistanceTo(position4.GlobalPosition) < 50f
        ).IsTrue();
        
        // Assert that members reached their ushers.
        for (int i=0; i < formationAgent.Formation.Members.Count; i++) 
        {
            Node2D member = formationAgent.Formation.Members[i];
            Vector2 usherPosition = formationAgent.ToGlobal(
                formationAgent.Formation.MemberPositions[i]);
            AssertThat(
                member.GlobalPosition.DistanceTo(usherPosition) < 50f
            ).IsTrue();
        }
        
        // Cleanup.
        formationAgent.Visible = false;
        formationAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
        for (int i=0; i < formationAgent.Formation.Members.Count; i++) 
        {
            formationAgent.Formation.Members[i].QueueFree();
        }
        formationAgent.Formation.Members.Clear();
    }
    
    /// <summary>
    /// Test that formation can make a full turn in realistic mode.
    /// </summary>
    [TestCase]
    public async Task TwoLevelFormationRealisticTurnDownUpTest()
    { 
        // Get references to agent and target.
        UsherWaiterFormationAgent formationAgent = 
            (UsherWaiterFormationAgent) _sceneRunner.FindChild("UsherWaiterObstacleManagedTwoLevelFormationAgent");

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
        formationAgent.MaximumLaggingBehindDistance = 200;
        formationAgent.MaximumSpeed = 400;
        
        
        target.GlobalPosition = position2.GlobalPosition;
        // Try to keep the tested formation at position1 inside test level, or
        // initial offset between formation origin and member average point will
        // go wrong. That is because in these test suites agents are not created where they
        // begin to work; instead, they are created at one place and teleported afterward,
        // and that can create problems not present at the real game.
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
        
        // Assert that members reached their ushers.
        for (int i=0; i < formationAgent.Formation.Members.Count; i++) 
        {
            Node2D member = formationAgent.Formation.Members[i];
            Vector2 usherPosition = formationAgent.ToGlobal(
                formationAgent.Formation.MemberPositions[i]);
            AssertThat(
                member.GlobalPosition.DistanceTo(usherPosition) < 50f
            ).IsTrue();
        }
        
        // Move target to another position.
        target.GlobalPosition = position3.GlobalPosition;
        
        // Assert that formation reached its target.
        await _sceneRunner.AwaitMillis(15000);
        AssertThat(
            formationAgent.GlobalPosition.DistanceTo(position3.GlobalPosition) < 50f
        ).IsTrue();
        
        // Assert that members reached their ushers.
        for (int i=0; i < formationAgent.Formation.Members.Count; i++) 
        {
            Node2D member = formationAgent.Formation.Members[i];
            Vector2 usherPosition = formationAgent.ToGlobal(
                formationAgent.Formation.MemberPositions[i]);
            AssertThat(
                member.GlobalPosition.DistanceTo(usherPosition) < 50f
            ).IsTrue();
        }
        
        // Move target to another position.
        target.GlobalPosition = position4.GlobalPosition;
        
        // Assert that formation reached its target.
        await _sceneRunner.AwaitMillis(11000);
        AssertThat(
            formationAgent.GlobalPosition.DistanceTo(position4.GlobalPosition) < 50f
        ).IsTrue();
        
        // Assert that members reached their ushers.
        for (int i=0; i < formationAgent.Formation.Members.Count; i++) 
        {
            Node2D member = formationAgent.Formation.Members[i];
            Vector2 usherPosition = formationAgent.ToGlobal(
                formationAgent.Formation.MemberPositions[i]);
            AssertThat(
                member.GlobalPosition.DistanceTo(usherPosition) < 50f
            ).IsTrue();
        }
        
        // Cleanup.
        formationAgent.Visible = false;
        formationAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
        for (int i=0; i < formationAgent.Formation.Members.Count; i++) 
        {
            formationAgent.Formation.Members[i].QueueFree();
        }
        formationAgent.Formation.Members.Clear();
    }
    
    /// <summary>
    /// Test that formation can move across obstacles.
    /// </summary>
    [TestCase]
    public async Task TwoLevelFormationMovementAcrossObstaclesTest()
    { 
        // Get references to agent and target.
        UsherWaiterFormationAgent formationAgent = 
            (UsherWaiterFormationAgent) _sceneRunner.FindChild("UsherWaiterObstacleManagedTwoLevelFormationAgent");

        Target target = (Target) _sceneRunner.FindChild("Target");
        Marker2D position1 = 
            (Marker2D) _sceneRunner.FindChild("Position1");
        Marker2D position4 = 
            (Marker2D) _sceneRunner.FindChild("Position4");
        Marker2D position5 = 
            (Marker2D) _sceneRunner.FindChild("Position5");
        
        // Get references to behaviors.
        SeekSteeringBehavior seekSteeringBehavior = 
            formationAgent.FindChild<SeekSteeringBehavior>();
        
        // Set up agents before the test.
        formationAgent.RealisticTurns = false;
        formationAgent.MaximumLaggingBehindDistance = 500;
        formationAgent.MaximumSpeed = 200;
        
        
        target.GlobalPosition = position4.GlobalPosition;
        // Try to keep the tested formation at position1 inside test level, or
        // initial offset between formation origin and member average point will
        // go wrong. That is because in these test suites agents are not created where they
        // begin to work; instead, they are created at one place and teleported afterward,
        // and that can create problems not present at the real game.
        formationAgent.GlobalPosition = position1.GlobalPosition;
        seekSteeringBehavior.Target = target;

        formationAgent.Visible = true;
        formationAgent.ProcessMode = Node.ProcessModeEnum.Inherit;
        
        
        // Start test.
        
        // Assert that formation reached its target.
        await _sceneRunner.AwaitMillis(15000);
        AssertThat(
            formationAgent.GlobalPosition.DistanceTo(position4.GlobalPosition) < 50f
        ).IsTrue();
        
        // Assert that members reached their ushers.
        for (int i=0; i < formationAgent.Formation.Members.Count; i++) 
        {
            Node2D member = formationAgent.Formation.Members[i];
            Vector2 usherPosition = formationAgent.ToGlobal(
                formationAgent.Formation.MemberPositions[i]);
            AssertThat(
                member.GlobalPosition.DistanceTo(usherPosition) < 50f
            ).IsTrue();
        }
        
        // Move target to another position.
        target.GlobalPosition = position5.GlobalPosition;
        
        // Assert that formation reached its target.
        await _sceneRunner.AwaitMillis(30000);
        AssertThat(
            formationAgent.GlobalPosition.DistanceTo(position5.GlobalPosition) < 50f
        ).IsTrue();
        
        // Assert that members reached their ushers.
        for (int i=0; i < formationAgent.Formation.Members.Count; i++) 
        {
            Node2D member = formationAgent.Formation.Members[i];
            Vector2 usherPosition = formationAgent.ToGlobal(
                formationAgent.Formation.MemberPositions[i]);
            AssertThat(
                member.GlobalPosition.DistanceTo(usherPosition) < 50f
            ).IsTrue();
        }
        
        // Cleanup.
        formationAgent.Visible = false;
        formationAgent.ProcessMode = Node.ProcessModeEnum.Disabled;
        for (int i=0; i < formationAgent.Formation.Members.Count; i++) 
        {
            formationAgent.Formation.Members[i].QueueFree();
        }
        formationAgent.Formation.Members.Clear();
    }
}