using System;
using Godot;

namespace GodotGameAIbyExample.Scripts.Sensors;

/// <summary>
/// <p>Class to represent many pieces of information about a detection result from
/// a ray cast.</p>
/// <p> I've made it inherit from Resource to make it a Variant type so it can be passed
/// as a signal parameter. </p>
/// </summary>
public partial class RayCastHit : Resource, IEquatable<RayCastHit>
{
    /// <summary>
    /// Hit global position.
    /// </summary>
    public Vector2 Position;
    
    /// <summary>
    /// Hit normal vector.
    /// </summary>
    public Vector2 Normal;
    
    /// <summary>
    /// Collider Node2D that was hit.
    /// </summary>
    public Node2D DetectedObject;
    
    /// <summary>
    /// Distance from the ray origin to the hit position.
    /// </summary>
    public float Distance;
    
    /// <summary>
    /// <p>Fraction of the ray length where hit happened.</p>
    /// <p>It is a value between 0 and 1, where 0 means the ray origin and 1 means the
    /// ray end.</p>
    /// </summary>
    public float Fraction;

    public bool Equals(RayCastHit other)
    {
        return Position.Equals(other.Position) &&
               Normal.Equals(other.Normal) &&
               Equals(DetectedObject, other.DetectedObject) &&
               Distance.Equals(other.Distance) &&
               Fraction.Equals(other.Fraction);
    }

    public override bool Equals(object obj)
    {
        return obj is RayCastHit other && 
               Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Position, 
            Normal, 
            DetectedObject, 
            Distance, 
            Fraction);
    }
}