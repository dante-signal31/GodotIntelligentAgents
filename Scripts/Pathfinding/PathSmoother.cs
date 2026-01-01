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
/// </summary>
/// <remarks>
/// The PathSmoother node relies on a child IPathFinder node to initially compute the raw
/// path. It then applies additional logic for path smoothing and obstacle checking
/// before returning the final path. This class also provides configuration warnings to
/// ensure proper setup in the scene.
/// </remarks>
[Tool]
public partial class PathSmoother: Node2D, IPathFinder
{
    [ExportCategory("CONFIGURATION:")]
    private MapGraph _graph;
    [Export] public MapGraph Graph
    {
        get => _graph;
        set
        {
            _graph = value;
            if (_smoothedPathFinder != null) _smoothedPathFinder.Graph = value;
        }
    }
    
    [ExportCategory("DEBUG")]
    [Export] public bool ShowGizmos { get; set; } = false;
    [Export] public Color GizmosColor { get; set; } = Colors.GreenYellow;
    
    private IPathFinder _smoothedPathFinder;
    private CleanAreaChecker _cleanAreaChecker;
    private Path _smoothedPath;
    private Path _rawPath;
    
    public override void _Ready()
    {
        _smoothedPathFinder = this.FindChild<IPathFinder>();
    }
    
    public override void _EnterTree()
    {
        _cleanAreaChecker = new CleanAreaChecker(
            (Mathf.Min(Graph.CellSize.X, Graph.CellSize.Y)/2), 
            Graph.ObstaclesLayers, 
            this);
    }    
    
    public override void _ExitTree()
    {
        _cleanAreaChecker.Dispose();
        if (_smoothedPath != null) _smoothedPath.QueueFree();
    }
    
    public Path FindPath(Vector2 targetPosition)
    {
        _rawPath = _smoothedPathFinder.FindPath(targetPosition);
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
        Array<Vector2> smoothedPositions = new();
        smoothedPositions.Add(rawPath.TargetPositions[0]);
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
                // If there is a clear path from the starIndex position to the endIndex
                // position, then we can omit the positions between them from the smoothed
                // path.
                endIndex++;
                // If there was a clear path to the end of the path, then add that end to
                // the smoothed path before leaving the loop. That will complete the
                // smoothed path.
                if (endIndex >= rawPath.PathLength) 
                    smoothedPositions.Add(rawPath.TargetPositions[endIndex-1]);
                continue;
            }
            // Otherwise, add the previous position to the occluded one to the smoothed
            // path because it was the last we could get directly.
            smoothedPositions.Add(rawPath.TargetPositions[endIndex-1]);
            // Now we will ray trace from that position to find out if we can omit any of
            // the remaining positions.
            startIndex = endIndex;
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