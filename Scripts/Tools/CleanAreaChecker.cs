using Godot;
using System;

namespace GodotGameAIbyExample.Scripts.Tools;

// TODO: Use this class in HidingPointsDetector.cs
/// <summary>
/// Helper class to check if a given position is within a clean area, meaning it is not
/// colliding with any objects in the specified detection layers.
/// </summary>
public class CleanAreaChecker : IDisposable
{
    private float _radius;
    /// <summary>
    /// Gets the radius of the clean area checker.
    /// </summary>
    /// <returns>Radius of the clean area checker.</returns>
    public float Radius
    {
        get => _radius;
        set
        {
            CircleShape2D cleanAreaShapeCircle = new();
            cleanAreaShapeCircle.Radius = value;
            _cleanAreaChecker.Shape = cleanAreaShapeCircle;
            _radius = value;
        }
    }
    
    private uint _detectionLayers;
    /// <summary>
    /// Gets the detection layers of the clean area checker.
    /// </summary>
    /// <returns>Detection layers of the clean area checker.</returns>
    public uint DetectionLayers
    {
        get => _detectionLayers;
        set {
            _cleanAreaChecker.CollisionMask = value;
            _detectionLayers = value;
        }
    }
    
    private ShapeCast2D _cleanAreaChecker = new();

    public CleanAreaChecker(
        float radius, 
        uint detectionLayers,
        Node parent,
        bool excludeParent = true)
    {
        Radius = radius;
        DetectionLayers = detectionLayers;
        _cleanAreaChecker.CollideWithBodies = true;
        _cleanAreaChecker.TargetPosition = Vector2.Zero;
        _cleanAreaChecker.ExcludeParent = excludeParent;
        _cleanAreaChecker.Enabled = true;
        parent.GetTree().Root.CallDeferred(Node.MethodName.AddChild,_cleanAreaChecker);
    }

    /// <summary>
    /// Checks if a given position is within a clean area, meaning it is not colliding
    /// with any objects in the specified detection layers.
    /// </summary>
    /// <param name="position">The global position to check for cleanliness.</param>
    /// <returns>
    /// True if the area at the specified position is clean (not colliding with any
    /// objects in the detection layers), otherwise false.
    /// </returns>
    public bool IsCleanArea(Vector2 position)
    {
        _cleanAreaChecker.GlobalPosition = position;
        // Force Godot to update the transform of the ShapeCast2D in the physics engine.
        _cleanAreaChecker.ForceUpdateTransform(); 
        // This call is rather expensive. Try to use the least possible.
        _cleanAreaChecker.ForceShapecastUpdate();
        return (!_cleanAreaChecker.IsColliding());
    }

    /// <summary>
    /// Determines whether a straight path between two points is clear of any collisions
    /// with objects in the specified detection layers.
    /// </summary>
    /// <param name="start">The starting global position of the path to check.</param>
    /// <param name="end">The ending global position of the path to check.</param>
    /// <returns>
    /// True if the path between the specified start and end positions is clear (not
    /// colliding with any objects in the detection layers), otherwise false.
    /// </returns>
    public bool IsCleanPath(Vector2 start, Vector2 end)
    {
        _cleanAreaChecker.GlobalPosition = start;
        // Force Godot to update the transform of the ShapeCast2D in the physics engine.
        _cleanAreaChecker.ForceUpdateTransform(); 
        
        _cleanAreaChecker.TargetPosition = end - start;
        
        // This call is rather expensive. Try to use the least possible.
        _cleanAreaChecker.ForceShapecastUpdate();
        bool isClean = !_cleanAreaChecker.IsColliding();
        
        _cleanAreaChecker.TargetPosition = Vector2.Zero;
        return (isClean);
    }
    
    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing && _cleanAreaChecker != null)
        {
            _cleanAreaChecker.QueueFree();
            _cleanAreaChecker = null;
        }

        _disposed = true;
    }

    ~CleanAreaChecker()
    {
        Dispose(false);
    }
    
}