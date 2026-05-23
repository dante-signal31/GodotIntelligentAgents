using System.Collections.Generic;
using Godot;
using Godot.Collections;
using GodotGameAIbyExample.Scripts.Extensions;
using GodotGameAIbyExample.Scripts.Tools;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// A utility class for path smoothing in navigation systems. This class is designed to
/// refine raw paths produced by an associated child pathfinding node (IPathFinder) into
/// smoother paths by removing unnecessary nodes and creating more efficient routes.
/// <remarks>This class only works at the PositionNode level. Do not use at
/// RegionNode level.</remarks>
/// </summary>
[Tool]
public partial class PathSmoother: Node2D, IPathFinder
{
    [ExportCategory("CONFIGURATION:")] 
    [Export] private MapGraph _graph;
    
    [ExportCategory("DEBUG")]
    [Export] public bool ShowGizmos { get; set; }
    [Export] public Color GizmosColor { get; set; } = Colors.GreenYellow;
    
    private IPathFinder _smoothedPathFinder;
    private CleanAreaChecker _cleanAreaChecker;
    private Path _smoothedPath;
    private Path _rawPath;

    public IPositionGraph Graph
    {
        get => _graph;
        set => _graph = (MapGraph) value;
    }
    
    public override void _Ready()
    {
        _smoothedPathFinder = this.FindChild<IPathFinder>();
    }
    
    public override void _EnterTree()
    {
        _cleanAreaChecker = new CleanAreaChecker(
            (Mathf.Min(_graph.CellSize.X, _graph.CellSize.Y)/2), 
            _graph.ObstaclesLayers, 
            _graph);
    }    
    
    public override void _ExitTree()
    {
        _cleanAreaChecker.Dispose();
        if (_smoothedPath != null) _smoothedPath.QueueFree();
    }
    
    public Path FindPath(Vector2 targetPosition, Vector2 fromPosition=default)
    {
        _rawPath = _smoothedPathFinder.FindPath(targetPosition, fromPosition);
        if (_rawPath == null) return null;
        CleanPreviousSmoothedPath();
        _smoothedPath = SmoothPath(_rawPath);
        ShowSmoothedPath();
        return _smoothedPath;
    }

    /// <summary>
    /// Displays the smoothed path in the scene tree for debugging and visualization
    /// purposes.
    /// </summary>
    private void ShowSmoothedPath()
    {
        _smoothedPath.Name = $"{Name} - Smoothed Path";
        _smoothedPath.ShowGizmos = ShowGizmos;
        _smoothedPath.GizmosColor = GizmosColor;
        GetTree().Root.AddChild(_smoothedPath);
    }

    /// <summary>
    /// Removes the previously generated smoothed path from the scene tree and frees
    /// its resources.
    /// </summary>
    private void CleanPreviousSmoothedPath()
    {
        if (_smoothedPath == null) return;
        GetTree().Root.RemoveChild(_smoothedPath);
        _smoothedPath.QueueFree();
    }

    /// <summary>
    /// Smooths the provided path by reducing unnecessary waypoints while maintaining
    /// the path's functionality, ensuring a cleaner and more efficient route.
    /// </summary>
    /// <param name="rawPath">The original path containing a sequence of positions that
    /// may include redundant waypoints.</param>
    /// <returns>A new path with redundant waypoints removed, resulting in a more direct
    /// and optimized path.</returns>
    private Path SmoothPath(Path rawPath)
    {
        // With paths of length 2 or less, there's nothing to smooth.
        if (rawPath.PathLength <= 2) return rawPath;
        
        Path smoothedPath = new();
        smoothedPath.Loop = false;
        Array<Vector2> smoothedPositions = new() { rawPath.TargetPositions[0] };
        int startIndex = 0;
        int endIndex = 2;
        
        do
        {
            // We do a ShapeCast instead of a RayCast because we want to avoid hitting
            // corners and partial obstacles.
            if (_cleanAreaChecker.IsCleanPath(
                    rawPath.TargetPositions[startIndex],
                    rawPath.TargetPositions[endIndex]))
            {
                // If there was a clear path to the end of the path, then add that end to
                // the smoothed path before leaving the loop. That will complete the
                // smoothed path.
                if (endIndex >= rawPath.PathLength - 1) 
                    smoothedPositions.Add(rawPath.TargetPositions[endIndex]);
                endIndex++;
                // If there was a clear path from the starIndex position to the endIndex
                // position, and the endIndex was not the end of the path, then we can
                // omit the positions between them from the smoothed path.
                continue;
            }
            if (endIndex == rawPath.PathLength - 1)
            {
                // If we were at the end of the path, then add the last position to the
                // smoothed path and smooth no more.
                smoothedPositions.Add(rawPath.TargetPositions[endIndex]);
                break;
            }
            // Otherwise, add the previous position to the occluded one to the smoothed
            // path because it was the last we could get directly.
            smoothedPositions.Add(rawPath.TargetPositions[endIndex-1]);
            // Now we will ray trace from that position to find out if we can omit
            // any of the remaining positions.
            startIndex = endIndex-1;
            endIndex++;
        } while (endIndex < rawPath.PathLength);
        
        smoothedPath.LoadPathData(smoothedPositions);
        return smoothedPath;
    }
    
    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new();
        
        _smoothedPathFinder= this.FindChild<IPathFinder>();
        if (_smoothedPathFinder == null)
        {
            warnings.Add("This node needs a child of type IPathFinder to work.");
        }
        
        return warnings.ToArray();
    }
}