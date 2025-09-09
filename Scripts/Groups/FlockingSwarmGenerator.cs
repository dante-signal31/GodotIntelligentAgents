using System.Collections.Generic;
using System.Linq;
using Godot;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.SteeringBehaviors;
using Vector2 = Godot.Vector2;

namespace GodotGameAIbyExample.Scripts.Groups;

// TODO: This component is actually not working as expected.
// When boids are added to the scene, and configured manually, they work right but when
// they are created automatically they don't. They converge to the edge of the swarm and
// they stay there. It's like wander is only giving movement going out of the swarm.

/// <summary>
/// <p>Node to create a flocking swarm automatically.</p>
/// <p>Provided boid scenes are created automatically at game start around this node
/// position. Boid locations are selected randomly after checking that places are actually
/// free of any other agent.</p>
/// </summary>
[Tool]
public partial class FlockingSwarmGenerator : Node2D, IGizmos
{
    [ExportCategory("SWARM CONFIGURATION:")]
    
    /// <summary>
    /// Number of boids to create.
    /// </summary>
    [Export] private int _swarmSize  = 10;

    /// <summary>
    /// The scene used to instantiate individual boid agents within the flocking swarm.
    /// This property is a reference to a PackedScene that defines the structure and
    /// behavior of each boid in the swarm.
    /// Boids are created automatically at runtime using this scene.
    /// </summary>
    [Export] private PackedScene _boidScene;

    /// <summary>
    /// <p>The radius within which individual boid agents are placed around this node's
    /// position when the flocking swarm is initialized. This value determines the
    /// spatial distribution of the boids and ensures they are positioned within a
    /// circular area of the specified radius.</p>
    /// <p>This value has the effect of creating a circle around the target point in which
    /// the flock moves. The flock is not going out of this circle.</p>
    /// </summary>
    [Export] private int _swarmRadius = 1000;

    /// <summary>
    /// Number of retries to find a free place for a newly created boid before giving
    /// up and let swarm as it is so far.
    /// </summary>
    [Export] private int _swarmGenerationRetries = 5;

    /// <summary>
    /// Layers to look for a possible obstacle to place a new boid.
    /// </summary>
    [Export(PropertyHint.Layers2DPhysics)] private uint _obstaclesForGenerationLayers;

    /// <summary>
    /// Whether to give a random starting rotation to every boid.
    /// </summary>
    [Export] private bool _randomizeBoidRotation;
    
    [ExportCategory("BOID GENERAL CONFIGURATION:")]
    
    /// <summary>
    /// The radius used to define the personal space around each boid in the swarm.
    /// This value represents the minimum distance other boids must maintain
    /// to avoid overlap.
    /// </summary>
    [Export] private int _boidRadius = 55;

    /// <summary>
    /// Defines the color of the boid agents in the flocking swarm.
    /// This color is used to visually represent the individual boids in the scene
    /// and can be customized through the Godot editor.
    /// </summary>
    [Export] private Color _boidColor = Colors.Green;

    /// <summary>
    /// The maximum speed a boid can achieve while moving within the flocking swarm.
    /// This value is used to limit individual boid velocity and maintain controlled
    /// motion within the swarm behavior.
    /// </summary>
    [Export] private float _boidMaximumSpeed = 200f;

    /// <summary>
    /// The speed at which a boid is considered to have stopped.
    /// This value is used to determine when a boid's velocity is low enough
    /// for it to be effectively stationary within the flocking behavior.
    /// </summary>
    [Export] private float _boidStopSpeed  = 1.0f;

    /// <summary>
    /// Defines the maximum rotational speed a boid in the flocking swarm can achieve.
    /// This value determines the upper limit on how quickly a boid can alter its
    /// rotational angle while navigating or responding to its environment.
    /// Measured in degrees per second.
    /// </summary>
    [Export] private float _boidMaximumRotationalSpeed = 1080f;

    /// <summary>
    /// The angle, in degrees, below which a boid's rotation effectively stops responding
    /// to rotation behaviors. This value is used to stabilize boid motion when the
    /// angular difference is negligible, preventing unnecessary rotational adjustments
    /// for smoother animations and movement.
    /// </summary>
    [Export] private float _boidStopRotationDegStop = 1.0f;

    /// <summary>
    /// The maximum acceleration value for a boid within the flocking swarm.
    /// This determines the upper limit of how quickly a boid can change its velocity
    /// in response to steering behaviors such as cohesion, alignment, or separation
    /// while maintaining stable and realistic movement dynamics.
    /// </summary>
    [Export] private float _boidMaximumAcceleration = 400f;

    /// <summary>
    /// The maximum rate at which a boid can reduce its velocity when responding to
    /// dynamic factors in the swarm, such as obstacle avoidance or alignment with
    /// neighboring boids. This value limits how quickly a boid can decelerate to ensure
    /// smooth and natural motion.
    /// </summary>
    [Export] private float _boidMaximumDeceleration = 200f;

    /// <summary>
    /// Determines whether the boid agents in the flock should automatically smooth
    /// their movement transitions. When enabled, movement and direction changes
    /// will appear more fluid, avoiding abrupt adjustments.
    /// </summary>
    [Export] private bool _boidAutoSmooth = true;

    /// <summary>
    /// The number of velocity samples used for smoothing the movement of boid agents
    /// within the flocking swarm. 
    /// </summary>
    [Export] private int _boidAutoSmoothSamples = 10;


    [ExportCategory("BOID SEPARATION CONFIGURATION")]
    
    /// <summary>
    /// The distance threshold used to determine the separation behavior of boid agents
    /// within the swarm. If the distance between two boids is less than this threshold,
    /// they adjust their positions to avoid crowding and maintain a balanced spacing,
    /// enhancing the overall flocking behavior.
    /// </summary>
    [Export] private float _boidSeparationThreshold = 400f;
    
    /// <summary>
    /// The separation algorithm used to determine the separation behavior of boid agents
    /// within the swarm.
    /// </summary>
    [Export] 
    private SeparationSteeringBehavior.SeparationAlgorithms _boidSeparationAlgorithm = 
        SeparationSteeringBehavior.SeparationAlgorithms.InverseSquare;
    
    /// <summary>
    /// Coefficient used to calculate the inverse square law separation algorithm.
    /// </summary>
    [Export] private float _boidSeparationDecayCoefficient = 200f;


    [ExportCategory("BOID COHESION CONFIGURATION")]
    
    /// <summary>
    /// <p>The distance within which boid agents begin to slow down as they approach
    /// the cohesion point in the flocking behavior. This value influences the
    /// formation and stability of the swarm by controlling how tightly the
    /// boids converge around their center of mass.</p>
    /// <p>Whithin the circle defined by _boidSeekArrivalDistance, cohesion behavior
    /// makes flock to stay compactly centered around the cohesion point and no
    /// further than this value.</p>
    /// </summary>
    [Export] private float _boidCohesionArrivalDistance = 500f;


    [ExportCategory("BOID SEEK CONFIGURATION")]
    
    /// <summary>
    /// <p>The target that each boid in the flock will seek towards.</p>
    /// <p>This value is used as the movement target of the flock as a whole.</p>
    /// </summary>
    [Export] private Node2D _boidSeekTarget;


    [ExportCategory("BOID WANDER CONFIGURATION")] 
    
    /// <summary>
    /// The distance at which a wandering boid begins stops as it approaches its target
    /// destination.
    /// </summary>
    [Export] private float _boidArrivalDistance = 10.0f;
    
    /// <summary>
    /// This is the radius of the constraining circle. KEEP IT UNDER wanderDistance!
    /// </summary>
    [Export] private float _boidWanderRadius = 300.0f;
    
    /// <summary>
    /// This is the distance the wander circle is projected in front of the agent.
    /// KEEP IT OVER wanderRadius!
    /// </summary>
    [Export] private float _boidWanderDistance = 600.0f;
    
    /// <summary>
    /// Maximum amount of random displacement that can be added to the target each
    /// second. KEEP IT OVER wanderRadius.
    /// </summary>
    [Export] private float _boidWanderJitter = 400.0f;
    
    /// <summary>
    /// Time in seconds to recalculate the wander position.
    /// </summary>
    [Export] private float _boidWanderRecalculation = 1.0f;
    
    
    [ExportCategory("DEBUG:")]
    [Export] public bool ShowGizmos { get; set; }
    [Export] public Color GizmosColor { get; set; }
    
    private ShapeCast2D _cleanAreaChecker;
    private bool _boidsCreated;
    
    public override void _EnterTree()
    {
        InitCleanAreaChecker();
        
    }

    /// <summary>
    /// <p>Initializes the clean area checker used to verify that placement locations
    /// for boids are free from obstacles or other agents.</p>
    /// <p>Configures the checker with a circular shape based on the boid radius,
    /// sets up collision detection, and enables the shape cast for usage.</p>
    /// </summary>
    private void InitCleanAreaChecker()
    {
        CircleShape2D cleanAreaShapeCircle = new();
        cleanAreaShapeCircle.Radius = _boidRadius;
        _cleanAreaChecker = new ShapeCast2D();
        _cleanAreaChecker.CollisionMask = _obstaclesForGenerationLayers;
        _cleanAreaChecker.Shape = cleanAreaShapeCircle;
        _cleanAreaChecker.CollideWithBodies = true;
        _cleanAreaChecker.TargetPosition = Vector2.Zero;
        _cleanAreaChecker.ExcludeParent = true;
        _cleanAreaChecker.Enabled = true;
    }
    
    public override void _Ready()
    {
        if (Engine.IsEditorHint()) return;
        
        CallDeferred(Node.MethodName.AddChild, _cleanAreaChecker);
    }

    /// <summary>
    /// <p>Creates a group of boid agents to form a swarm based on the configured swarm
    /// size. Each boid is instantiated at a valid, randomly selected position near the
    /// node, ensuring no overlap with other agents.</p>
    /// <p>Once created, the boids are configured with appropriate steering behaviors.</p>
    /// </summary>
    private void CreateBoids()
    {
        List<MovingAgent> createdBoids = new();
        
        // Create boids.
        for (int i=0; i < _swarmSize; i++)
        {
            (bool locationFound, Vector2 spawnPosition) = FindSpawnPosition();
            
            if (locationFound)
            {
                MovingAgent newBoid = CreateBoid(i, spawnPosition);
                createdBoids.Add(newBoid);
            }
        }
        
        // Configure created boids.
        InitializeBoidSteeringBehaviors(createdBoids);
    }

    /// <summary>
    /// Initializes and configures the steering behaviors for a given list of boid agents.
    /// Sets up properties such as movement parameters and steering behavior thresholds,
    /// and assigns relevant targets for behaviors like separation and cohesion.
    /// </summary>
    /// <param name="createdBoids">The list of boid agents to configure. Each boid will
    /// have its steering behaviors initialized and linked with other boids in the
    /// group.</param>
    private void InitializeBoidSteeringBehaviors(List<MovingAgent> createdBoids)
    {
        foreach (MovingAgent boid in createdBoids)
        {
            Node2D steeringBehavior = (Node2D) boid.FindChild<ISteeringBehavior>();
            SeparationSteeringBehavior separationBehavior = 
                steeringBehavior.FindChild<SeparationSteeringBehavior>();
            CohesionSteeringBehavior cohesionBehavior = 
                steeringBehavior.FindChild<CohesionSteeringBehavior>();
            SeekSteeringBehavior seekBehavior = 
                steeringBehavior.FindChild<SeekSteeringBehavior>();
            WanderSteeringBehavior wanderBehavior = 
                steeringBehavior.FindChild<WanderSteeringBehavior>();
            
            // MovingAgent properties
            boid.AgentColor = _boidColor;
            boid.MaximumSpeed = _boidMaximumSpeed;
            boid.StopSpeed = _boidStopSpeed;
            boid.MaximumRotationalDegSpeed = _boidMaximumRotationalSpeed;
            boid.StopRotationDegThreshold = _boidStopRotationDegStop;
            boid.MaximumAcceleration = _boidMaximumAcceleration;
            boid.MaximumDeceleration = _boidMaximumDeceleration;
            boid.AutoSmooth = _boidAutoSmooth;
            boid.AutoSmoothSamples = _boidAutoSmoothSamples;
            
            // SteeringBehavior properties
            separationBehavior.SeparationThreshold = _boidSeparationThreshold;
            separationBehavior.SeparationAlgorithm = _boidSeparationAlgorithm;
            separationBehavior.DecayCoefficient = _boidSeparationDecayCoefficient;
            cohesionBehavior.ArrivalDistance = _boidCohesionArrivalDistance;
            seekBehavior.Target = _boidSeekTarget;
            seekBehavior.ArrivalDistance = _swarmRadius;
            wanderBehavior.ArrivalDistance = _boidArrivalDistance;
            wanderBehavior.WanderRadius = _boidWanderRadius;
            wanderBehavior.WanderDistance = _boidWanderDistance;
            wanderBehavior.WanderJitter = _boidWanderJitter;
            wanderBehavior.WanderRecalculationTime = _boidWanderRecalculation;
            
            
            // Register all the rest boids in every boid's separation and cohesion
            // behavior.
            List<MovingAgent> otherBoids = createdBoids
                .Where(b => b != boid)
                .ToList();
            separationBehavior.Threats.AddRange(otherBoids.ToArray());
            cohesionBehavior.Targets.AddRange(otherBoids.ToArray());
        }
    }

    /// <summary>
    /// Creates a new boid instance, sets its properties such as name, position,
    /// and optionally randomizes its rotation, then adds it to the scene as a child node.
    /// </summary>
    /// <param name="i">The index number of the boid, used for naming the boid
    /// uniquely.</param>
    /// <param name="spawnPosition">The global position where the boid will be placed
    /// in the scene.</param>
    /// <returns>The newly created boid instance configured with the provided
    /// parameters.</returns>
    private MovingAgent CreateBoid(int i, Vector2 spawnPosition)
    {
        // Create the boid if a free location was found.
        var boid = _boidScene.Instantiate<MovingAgent>();
        boid.Name = $"Boid_{i}";
                
        // Add boid to the scene.
        AddChild(boid);
        
        // Boid position and rotation.
        boid.GlobalPosition = spawnPosition;
        if (_randomizeBoidRotation)
        {
            boid.Rotation = (float)GD.RandRange(0, Mathf.Tau);
        }

        return boid;
    }

    /// <summary>
    /// Attempts to find a valid spawn position for a new boid by checking if a randomly
    /// generated position is free from obstacles.
    /// </summary>
    /// <returns>A tuple containing a boolean indicating whether a valid position was
    /// found and the corresponding spawn position. If no position is found, the returned
    /// position will be (0, 0).</returns>
    private (bool, Vector2) FindSpawnPosition()
    {
        for (int tries = 0; tries < _swarmGenerationRetries; tries++)
        {
            Vector2 randomLocalPosition =
                RandomExtensions.GetRandomPointInsideCircle(_swarmRadius);
            Vector2 randomGlobalPosition =
                GlobalPosition + randomLocalPosition;
            if (!IsCleanPoint(randomGlobalPosition)) continue;
            return (true, randomGlobalPosition);
        }
        return (false, Vector2.Zero);
    }

    /// <summary>
    /// Whether this point is free from obstacles to be a valid spawn point.
    /// </summary>
    /// <param name="spawnPoint">Position to check</param>
    /// <returns>True if position is free from obstacles, false otherwise</returns>
    private bool IsCleanPoint(Vector2 spawnPoint)
    {
        _cleanAreaChecker.GlobalPosition = spawnPoint;
        // This call is rather expensive. Try to use the least possible.
        _cleanAreaChecker.ForceShapecastUpdate();
        return (!_cleanAreaChecker.IsColliding());
    }
    
    public override void _Process(double delta)
    {
        if (ShowGizmos) DrawGizmos();
        
        if (Engine.IsEditorHint()) return;
        
        if (!_boidsCreated)
        {
            // CreateBoids cannot be called at Ready() because _cleanAreaChecker is not
            // added to the tree until the first idle frame.
            CreateBoids();
            _boidsCreated = true;
        }
    }

    private void DrawGizmos()
    {
        QueueRedraw();
    }
    
    public override void _Draw()
    {
        if (!ShowGizmos) return;

        if (Engine.IsEditorHint())
        {
            DrawCircle(
                Vector2.Zero, 
                _swarmRadius, 
                GizmosColor,
                filled: false);
        }
    }
}