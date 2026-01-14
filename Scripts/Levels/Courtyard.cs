using Godot;
using Godot.Collections;

namespace GodotGameAIbyExample.Scripts.Levels;

/// <summary>
/// Represents a Courtyard node in the game, serving as a specialized GameObject for
/// managing walkable regions and obstacle positions within the level.
/// </summary>
[Tool]
public partial class Courtyard : Node2D
{
    private const string ObstacleKey = "Obstacle";
    
    [ExportCategory("CONFIGURATION:")] 
    [Export] private TileMapLayer _obstacleLayer;
    [Export] private int _walkableRegionWeldThreshold = 100;

    public Array<Vector2> ObstaclePositions { get; } = new();
    
    public override void _Ready()
    {
        WeldWalkableRegions();
        ExtractObstaclePositions();
    }

    /// <summary>
    /// Adjusts the edge connection margin of the navigation map to weld adjacent
    /// walkable regions.
    /// </summary>
    private void WeldWalkableRegions()
    {
        Rid mapRid = GetWorld2D().NavigationMap;
        NavigationServer2D.MapSetEdgeConnectionMargin(mapRid, _walkableRegionWeldThreshold);
    }

    /// <summary>
    /// Extracts the global positions of obstacles from the configured obstacle layer
    /// and populates the ObstaclePositions collection with those positions.
    /// </summary>
    private void ExtractObstaclePositions()
    {
        Array<Vector2I> candidatePositions = _obstacleLayer.GetUsedCells();
        // TODO: Likely everything in this layer is going to be an obstacle, so I think this check can be removed later.
        foreach (Vector2I position in candidatePositions)
        {
            TileData tileData = _obstacleLayer.GetCellTileData(position);
            bool isObstacle = (bool) tileData.GetCustomData(ObstacleKey);
            if (isObstacle)
            {
                Vector2 globalPosition = ToGlobal(_obstacleLayer.MapToLocal(position));
                ObstaclePositions.Add(globalPosition);
            }
        }
    }
}