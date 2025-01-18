using Godot;
using System;
using System.Collections.Generic;
using Godot.Collections;

public partial class Courtyard : Node2D
{
    private const string ObstacleKey = "Obstacle";
    
    [ExportCategory("CONFIGURATION:")] 
    [Export] private TileMapLayer _obstacleLayer;

    public List<Vector2> ObstaclePositions { get; } = new();
    
    public override void _Ready()
    {
        Array<Vector2I> candidatePositions = _obstacleLayer.GetUsedCells();
        // TODO: Likely everything in this layer is going to be an obstacle, so I think this check can be removed later.
        foreach (Vector2I position in candidatePositions)
        {
            TileData tileData = _obstacleLayer.GetCellTileData(position);
            bool isObstacle = (bool) tileData.GetCustomData(ObstacleKey);
            if (isObstacle) ObstaclePositions.Add(position);
        }
    }
}
