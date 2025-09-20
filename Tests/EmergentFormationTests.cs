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
public class EmergentFormationTests
{
    private const string TestScenePath = "res://Tests/TestLevels/" +
                                         "EmergentFormationTestLevel.tscn";

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
    /// Test that EmergentFormation keep formation partners together while moving.
    /// </summary>
    [TestCase]
    public async Task EmergentFormationTest()
    {
        // // Get references to agent and target.
        Node emergentFormationGroup = (Node) _sceneRunner.FindChild("EmergentFormation");
        Scripts.SteeringBehaviors.MovingAgent leaderMovingAgent = 
            (Scripts.SteeringBehaviors.MovingAgent) _sceneRunner.FindChild("LeaderMovingAgent");
        Scripts.SteeringBehaviors.MovingAgent wingmanMovingAgent = 
            (Scripts.SteeringBehaviors.MovingAgent) _sceneRunner.FindChild("WingmanMovingAgent");
        Scripts.SteeringBehaviors.MovingAgent wingmanMovingAgent2 = 
            (Scripts.SteeringBehaviors.MovingAgent) _sceneRunner.FindChild("WingmanMovingAgent2");
        Scripts.SteeringBehaviors.MovingAgent wingmanMovingAgent3 = 
            (Scripts.SteeringBehaviors.MovingAgent) _sceneRunner.FindChild("WingmanMovingAgent3");
        Scripts.SteeringBehaviors.MovingAgent wingmanMovingAgent4 = 
            (Scripts.SteeringBehaviors.MovingAgent) _sceneRunner.FindChild("WingmanMovingAgent4");
        Scripts.SteeringBehaviors.MovingAgent wingmanMovingAgent5 = 
            (Scripts.SteeringBehaviors.MovingAgent) _sceneRunner.FindChild("WingmanMovingAgent5");
        
        List<Scripts.SteeringBehaviors.MovingAgent> wingmanList = new();
        wingmanList.Add(wingmanMovingAgent);
        wingmanList.Add(wingmanMovingAgent2);
        wingmanList.Add(wingmanMovingAgent3);
        wingmanList.Add(wingmanMovingAgent4);
        wingmanList.Add(wingmanMovingAgent5);

        Target target = (Target) _sceneRunner.FindChild("Target");
        Marker2D position1 = 
            (Marker2D) _sceneRunner.FindChild("Position1");
        Marker2D position2 = 
            (Marker2D) _sceneRunner.FindChild("Position2");
        Marker2D position3 = 
            (Marker2D) _sceneRunner.FindChild("Position3");
        Marker2D position4 = 
            (Marker2D) _sceneRunner.FindChild("Position4");
        Marker2D position5 = 
            (Marker2D) _sceneRunner.FindChild("Position5");
        Marker2D position6 = 
            (Marker2D) _sceneRunner.FindChild("Position6");
        Marker2D position7 = 
            (Marker2D) _sceneRunner.FindChild("Position7");
        Marker2D position8 = 
            (Marker2D) _sceneRunner.FindChild("Position8");
        Marker2D position9 = 
            (Marker2D) _sceneRunner.FindChild("Position9");
        
        // Get references to behaviors.
        ArriveSteeringBehaviorNLA arriveSteeringBehavior = 
            leaderMovingAgent.FindChild<ArriveSteeringBehaviorNLA>();
        
        // Setup agents before the test.
        target.GlobalPosition = position1.GlobalPosition;
        leaderMovingAgent.GlobalPosition = position1.GlobalPosition;
        wingmanMovingAgent.GlobalPosition = position6.GlobalPosition;
        wingmanMovingAgent2.GlobalPosition = position2.GlobalPosition;
        wingmanMovingAgent3.GlobalPosition = position3.GlobalPosition;
        wingmanMovingAgent4.GlobalPosition = position4.GlobalPosition;
        wingmanMovingAgent5.GlobalPosition = position5.GlobalPosition;
        
        arriveSteeringBehavior.Target = target;
        leaderMovingAgent.Visible = true;
        wingmanMovingAgent.Visible = true;
        wingmanMovingAgent2.Visible = true;
        wingmanMovingAgent3.Visible = true;
        wingmanMovingAgent4.Visible = true;
        wingmanMovingAgent5.Visible = true;
        emergentFormationGroup.ProcessMode = Node.ProcessModeEnum.Inherit;
        
        
        // Start test.
        
        // Assert that every wingman has found a partner and is located at the offset.
        await _sceneRunner.AwaitMillis(7000);
        foreach (Scripts.SteeringBehaviors.MovingAgent wingman in wingmanList)
        {
            EmergentFormation emergentFormation = wingman.FindChild<EmergentFormation>();
            AssertThat(emergentFormation.Partner != null).IsTrue();
            AssertThat(
                wingman.GlobalPosition.DistanceTo(
                    emergentFormation.Partner.ToGlobal(emergentFormation.PartnerOffset)) <
                emergentFormation.CleanAreaRadius
            ).IsTrue();
        }
        
        // Move leader to another position.
        target.GlobalPosition = position7.GlobalPosition;
        
        // Assert that every wingman keeps a partner and is located at the offset after
        // movement.
        await _sceneRunner.AwaitMillis(8000);
        foreach (Scripts.SteeringBehaviors.MovingAgent wingman in wingmanList)
        {
            EmergentFormation emergentFormation = wingman.FindChild<EmergentFormation>();
            AssertThat(emergentFormation.Partner != null).IsTrue();
            AssertThat(
                wingman.GlobalPosition.DistanceTo(
                    emergentFormation.Partner.ToGlobal(emergentFormation.PartnerOffset)) <
                emergentFormation.CleanAreaRadius
            ).IsTrue();
        }
        
        // Move leader to another position.
        target.GlobalPosition = position8.GlobalPosition;
        
        // Assert that every wingman keeps a partner and is located at the offset after
        // movement.
        await _sceneRunner.AwaitMillis(8000);
        foreach (Scripts.SteeringBehaviors.MovingAgent wingman in wingmanList)
        {
            EmergentFormation emergentFormation = wingman.FindChild<EmergentFormation>();
            AssertThat(emergentFormation.Partner != null).IsTrue();
            AssertThat(
                wingman.GlobalPosition.DistanceTo(
                    emergentFormation.Partner.ToGlobal(emergentFormation.PartnerOffset)) <
                emergentFormation.CleanAreaRadius
            ).IsTrue();
        }
        
        // Move leader to another position.
        target.GlobalPosition = position9.GlobalPosition;
        
        // Assert that every wingman keeps a partner and is located at the offset after
        // movement.
        await _sceneRunner.AwaitMillis(9000);
        foreach (Scripts.SteeringBehaviors.MovingAgent wingman in wingmanList)
        {
            EmergentFormation emergentFormation = wingman.FindChild<EmergentFormation>();
            AssertThat(emergentFormation.Partner != null).IsTrue();
            AssertThat(
                wingman.GlobalPosition.DistanceTo(
                    emergentFormation.Partner.ToGlobal(emergentFormation.PartnerOffset)) <
                emergentFormation.CleanAreaRadius
            ).IsTrue();
        }
    }
}