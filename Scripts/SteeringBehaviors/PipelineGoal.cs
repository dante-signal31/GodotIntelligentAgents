using System;
using Godot;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// Represents a goal to be used in a steering behavior pipeline. This class stores
/// positional, rotational, and speed information.
/// </summary>
public class PipelineGoal: IEquatable<PipelineGoal>
{
    private Vector2 _position = Vector2.Inf;
    /// <summary>
    /// Goal for the position channel.
    /// </summary>
    public Vector2 Position
    {
        get => _position;
        set
        {
            _position = value;
            HasPosition = true;
        }
    } 
    
    private float _rotation = 0;
    /// <summary>
    /// Goal for the rotation channel.
    /// </summary>
    public float Rotation
    {
        get => _rotation;
        set
        {
            _rotation = value;
            HasRotation = true;
        }
    }
    
    private float _speed;
    /// <summary>
    /// Goal for the velocity channel.
    /// </summary>
    public float Speed
    {
        get => _speed;
        set
        {
            _speed = value;
            HasSpeed = true;
        }
    }
    
    /// <summary>
    /// Whether this goal sets the position channel.
    /// </summary>
    public bool HasPosition { get; private set; }
    
    /// <summary>
    /// Whether this goal sets the rotation channel.
    /// </summary>
    public bool HasRotation { get; private set; }
    
    /// <summary>
    /// Whether this goal sets the speed channel.
    /// </summary>
    public bool HasSpeed { get; private set; }

    /// <summary>
    /// Update the goal channels with the values from another goal.
    /// </summary>
    /// <param name="otherGoal">Goal to use to update the current one.</param>
    public void UpdateChannels(PipelineGoal otherGoal)
    {
        // Remember, channels already defined by a targeter cannot be overwritten by
        // another targeter. That's why we first check if the channel is already defined.
        if (!HasPosition && otherGoal.HasPosition) Position = otherGoal.Position;
        if (!HasRotation && otherGoal.HasRotation) Rotation = otherGoal.Rotation;
        if (!HasSpeed && otherGoal.HasSpeed) Speed = otherGoal.Speed;
    }


    /// <summary>
    /// Creates and returns a copy of the current PipelineGoal instance.
    /// </summary>
    /// <returns>A new instance of PipelineGoal with the same position, rotation,
    /// and speed values as the current instance, if those values are defined.</returns>
    public PipelineGoal GetGoalCopy()
    {
        PipelineGoal copy = new();
        if (HasPosition) copy.Position = Position;
        if (HasRotation) copy.Rotation = Rotation;
        if (HasSpeed) copy.Speed = Speed;
        return copy;
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current PipelineGoal.
    /// </summary>
    /// <param name="obj">The object to compare with the current PipelineGoal.</param>
    /// <returns>true if Position, Rotation, and Velocity match; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        return obj is PipelineGoal other && Equals(other);
    }

    /// <summary>
    /// Determines whether the specified PipelineGoal is equal to the current instance.
    /// </summary>
    public bool Equals(PipelineGoal? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return Position.Equals(other.Position)
               && Mathf.IsEqualApprox(Rotation, other.Rotation)
               && Speed.Equals(other.Speed);
    }

    /// <summary>
    /// Returns a hash code for this PipelineGoal.
    /// </summary>
    /// <returns>A hash code for the current PipelineGoal.</returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Position, Rotation, Speed);
    }

    /// <summary>
    /// Determines whether two PipelineGoal instances are equal.
    /// </summary>
    public static bool operator ==(PipelineGoal? left, PipelineGoal? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two PipelineGoal instances are not equal.
    /// </summary>
    public static bool operator !=(PipelineGoal? left, PipelineGoal? right)
    {
        return !(left == right);
    }
}