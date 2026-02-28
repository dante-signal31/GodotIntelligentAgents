using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// A node for smoothing regions in a map graph, allowing iterative relaxation and
/// optional random seeding of initial region data. 
/// </summary>
[Tool]
public partial class MapGraphRegionsSmoother: Node
{
    [ExportCategory("CONFIGURATION:")]
    
    /// <summary>
    /// Whether to use random seeds for the regions generation.
    /// </summary>
    /// <remarks>
    /// If true, only seeds set at MapGraphRegions will be used; whereas if false, then
    /// random seeds will be generated from this class with the same influence.
    /// </remarks>
    [Export] public bool RandomSeeds { get; set; } = true;
    [Export] public uint RandomSeedsAmount { get; set; } = 3;
    
    /// <summary>
    /// Number of iterations for the Lloyd relaxation algorithm.
    /// </summary>
    [Export] public uint RelaxationIterations { get; set; } = 3;
    
    [ExportCategory("CONFIGURATION:")]
    [Export] public MapGraphRegions MapGraphRegions;
    
    [ExportToolButton("Smooth Regions")]
    private Callable SmoothRegionsButton => Callable.From(SmoothRegions);

    

    /// <summary>
    /// Smooths regions in the map graph using iterative relaxation. Optionally,
    /// initializes the region seeds with random data if RandomSeeds is enabled.
    /// The smoothing applies a given number of relaxation iterations as specified
    /// by the RelaxationIterations property.
    /// </summary>
    /// <remarks>
    /// - If RandomSeeds is true, the method generates a list of randomized seeds
    /// before starting the smoothing process.
    /// - During each relaxation iteration, regions are updated, and seeds are
    /// relocated if more iterations remain.
    /// - The method depends on the MapGraphRegions object to handle the actual
    /// region generation based on the provided seeds.
    /// </remarks>
    private void SmoothRegions()
    {
        if (RandomSeeds) MapGraphRegions.Seeds = GenerateRandomSeeds();
        for (int i = 0; i < RelaxationIterations; i++)
        {
            MapGraphRegions.GenerateRegions();
            if (i < RelaxationIterations - 1) RelocateSeeds();
        }
    }

    /// <summary>
    /// Relocates region seeds to the nearest valid positions within their respective
    /// regions by calculating the average position of nodes in each region and snapping
    /// the seed to the nearest node to that position.
    /// </summary>
    private void RelocateSeeds()
    {
        foreach (RegionSeed seed in MapGraphRegions.Seeds)
        {
            uint regionId = MapGraphRegions.GetRegionByPosition(seed.Position);
            HashSet<uint> nodesInRegion = MapGraphRegions.NodesByRegion[regionId];
            Vector2 averagePosition = GetAveragePosition(nodesInRegion);
            // Average position can be inside an obstacle. So we must search for the
            // nearest node.
            PositionNode nearestNode = 
                MapGraphRegions.MapGraph.GetNodeAtNearestPosition(averagePosition);
            seed.Position = nearestNode.Position;
        }
    }

    /// <summary>
    /// Calculates the average position of all nodes within a specified region.
    /// The average is computed by summing the positions of the nodes and dividing
    /// by the total number of nodes in the region.
    /// </summary>
    /// <param name="nodesInRegion">
    /// A collection of node IDs representing the nodes within the region
    /// whose average position is to be calculated.
    /// </param>
    /// <returns>
    /// A <see cref="Vector2"/> representing the calculated average position
    /// of all nodes in the specified region.
    /// </returns>
    /// <remarks>
    /// This method assumes that the positional data of nodes can be accessed
    /// via the MapGraph associated with the MapGraphRegions object. The average
    /// position may not account for obstacles or invalid positions, which must
    /// be handled by the calling process if necessary.
    /// </remarks>
    private Vector2 GetAveragePosition(HashSet<uint> nodesInRegion)
    {
        Vector2 positionsSum = Vector2.Zero;
        uint positionsCount = 0;
        foreach (uint nodeId in nodesInRegion)
        {
            PositionNode node = MapGraphRegions.MapGraph.GetNodeById(nodeId);
            positionsSum += node.Position;
            positionsCount++;
        }
        return positionsSum / positionsCount;
    }

    /// <summary>
    /// Generates a collection of randomized region seeds to initialize the map graph
    /// regions. Each seed is assigned a unique spatial position and color, ensuring no
    /// duplicates.
    /// </summary>
    /// <returns>
    /// An array of generated RegionSeed objects, each carrying a position, an influence
    /// value, and a randomly assigned color.
    /// </returns>
    private Array<RegionSeed> GenerateRandomSeeds()
    {
        Array<RegionSeed> randomSeeds = new();
        Color mapGraphRegionsGizmoColor = MapGraphRegions.GizmosColor;
        HashSet<Color> selectedColors = new() { mapGraphRegionsGizmoColor };

        // Get all valid array positions from the graph nodes
        List<Vector2I> allNodesArrayPositions = 
            MapGraphRegions.MapGraph.ArrayPositionsToNodes.Keys.ToList();

        HashSet<int> alreadySelectedIndices = new();
        // Generate random seeds
        for (int i = 0; i < RandomSeedsAmount && i < allNodesArrayPositions.Count; i++)
        {
            // Select a random node from the graph.
            int randomIndex;
            do
            {
                randomIndex = GD.RandRange(0, allNodesArrayPositions.Count - 1);
                // I've had problems with seed collision, so I made sure to avoid selecting
                // the same position.
            } while (alreadySelectedIndices.Contains(randomIndex));
            alreadySelectedIndices.Add(randomIndex);
            Vector2I selectedArrayPosition = allNodesArrayPositions[randomIndex];
            PositionNode selectedNode = 
                MapGraphRegions.MapGraph.GetNodeAtArrayPosition(selectedArrayPosition);
            allNodesArrayPositions.RemoveAt(randomIndex);

            // Generate a random color that doesn't exist
            Color randomColor;
            do
            {
                randomColor = new Color(
                    (float)GD.RandRange(0.0, 1.0),
                    (float)GD.RandRange(0.0, 1.0),
                    (float)GD.RandRange(0.0, 1.0)
                );
            } while (selectedColors.Contains(randomColor));
            selectedColors.Add(randomColor);

            // Create the region seed
            RegionSeed seed = new()
            {
                Position = selectedNode.Position,
                Influence = 1,
                GizmoColor = randomColor
            };
            randomSeeds.Add(seed);
        }

        return randomSeeds;
    }
}