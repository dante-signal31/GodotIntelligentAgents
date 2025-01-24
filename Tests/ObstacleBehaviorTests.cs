using System.Threading.Tasks;
using GdUnit4;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;
using static GdUnit4.Assertions;

namespace GodotGameAIbyExample.Tests;

[TestSuite]
public class ObstacleBehaviorTests
{
    private const string TestScenePath = "res://Tests/TestLevels/" +
                                         "ObstacleTestLevel.tscn";

    private ISceneRunner _sceneRunner;
    
    [BeforeTest]
    public void LoadScene()
    {
        _sceneRunner = ISceneRunner.Load(TestScenePath);
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
    public async Task HideBehaviorTest()
    {
        // Get references to agent and target.
        MovingAgent seekAgent = 
            (MovingAgent) _sceneRunner.FindChild("SeekMovingAgent");
        MovingAgent hideAgent = 
            (MovingAgent) _sceneRunner.FindChild("HideMovingAgent");
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
            seekAgent.FindChild<SeekSteeringBehavior>();
        HideSteeringBehavior hideSteeringBehavior =
            hideAgent.FindChild<HideSteeringBehavior>();
        
        // Setup agents before the test.
        target.GlobalPosition = position2.GlobalPosition;
        seekAgent.GlobalPosition = position1.GlobalPosition;
        seekAgent.MaximumSpeed = 600.0f;
        seekAgent.StopSpeed = 1f;
        seekAgent.MaximumRotationalDegSpeed = 1080f;
        seekAgent.StopRotationDegThreshold = 1f;
        seekAgent.AgentColor = new Color(1, 0, 0);
        seekSteeringBehavior.Target = target;
        seekSteeringBehavior.ArrivalDistance = 3f;
        seekAgent.Visible = true;
        seekAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        hideAgent.GlobalPosition = position2.GlobalPosition;
        hideAgent.MaximumSpeed = 600.0f;
        hideAgent.StopSpeed = 1f;
        hideAgent.MaximumRotationalDegSpeed = 1080f;
        hideAgent.StopRotationDegThreshold = 1f;
        hideAgent.AgentColor = new Color(0, 1, 0);
        hideSteeringBehavior.Threat = seekAgent;
        hideSteeringBehavior.ArrivalDistance = 3f;
        // Layer 2, only obstacles.
        hideSteeringBehavior.ObstaclesLayers = 2;
        hideSteeringBehavior.SeparationFromObstacles = 30f;
        hideSteeringBehavior.AgentRadius = 50f;
        // Layer 2, only obstacles.
        hideSteeringBehavior.NotEmptyGroundLayers = 2;
        hideSteeringBehavior.ShowGizmos = true;
        hideAgent.Visible = true;
        hideAgent.ProcessMode = Node.ProcessModeEnum.Always;
        
        // Setup tester raycast.
        RayCast2D rayTest = new RayCast2D();
        // Layer 2 and 1, obstacles and agents.
        rayTest.CollisionMask = 3;
        rayTest.CollideWithBodies = true;
        rayTest.CollideWithAreas = false;
        rayTest.HitFromInside = false;
        seekAgent.AddChild(rayTest);
        // Make ray start at the center of its parent node.
        rayTest.Position = Vector2.Zero;
        rayTest.TargetPosition = Vector2.Zero;
        rayTest.Enabled = true;
        
        // Start test.
        
        // Assert that seek agent can see hide agent.
        rayTest.TargetPosition = rayTest.ToLocal(hideAgent.GlobalPosition);
        rayTest.ForceRaycastUpdate();
        AssertThat(rayTest.IsColliding()).IsTrue();
        AssertThat(((Node2D)rayTest.GetCollider()).Name == hideAgent.Name).IsTrue();
        
        // Give hide agent time to hide.
        await _sceneRunner.AwaitMillis(2000);
        
        // Assert that seek agent can no longer see hide agent.
        rayTest.TargetPosition = rayTest.ToLocal(hideAgent.GlobalPosition);
        rayTest.ForceRaycastUpdate();
        AssertThat(!rayTest.IsColliding() || 
                   ((Node2D)rayTest.GetCollider()).Name != hideAgent.Name).IsTrue();
        
        // Move seek agent to another position.
        target.GlobalPosition = position3.GlobalPosition;
        
        // Give agent time to hide agent to hide again target.
        await _sceneRunner.AwaitMillis(2000);
        
        // Assert that seek agent can no longer see hide agent.
        rayTest.TargetPosition = rayTest.ToLocal(hideAgent.GlobalPosition);
        rayTest.ForceRaycastUpdate();
        AssertThat(!rayTest.IsColliding() || 
                   ((Node2D)rayTest.GetCollider()).Name != hideAgent.Name).IsTrue();
        
        // Move seek agent to another position.
        target.GlobalPosition = position4.GlobalPosition;
        
        // Give agent time to hide agent to hide again target.
        await _sceneRunner.AwaitMillis(2000);
        
        // Assert that seek agent can no longer see hide agent.
        rayTest.TargetPosition = rayTest.ToLocal(hideAgent.GlobalPosition);
        rayTest.ForceRaycastUpdate();
        AssertThat(!rayTest.IsColliding() || 
                   ((Node2D)rayTest.GetCollider()).Name != hideAgent.Name).IsTrue();
    }
}