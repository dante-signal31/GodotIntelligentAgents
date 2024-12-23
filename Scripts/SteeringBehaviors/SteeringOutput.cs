using Godot;

namespace GodotGameAIbyExample.Scripts.SteeringBehaviors;

/// <summary>
/// Velocities to move agent.
/// </summary>
public class SteeringOutput
{
    /// <summary>
    /// Zero steering output.
    /// </summary>
    public static SteeringOutput Zero = new SteeringOutput();
    
    /// <summary>
    /// Linear acceleration vector.
    /// </summary>
    public Vector2 Linear { get; }
    
    /// <summary>
    /// Angular rotation.
    /// </summary>
    public float Angular { get; }

    public SteeringOutput(Vector2 linear=new Vector2(), float angular=0)
    {
        Linear = linear;
        Angular = angular;
    }
    
    /// <summary>
    /// Operator for adding two SteeringOutput objects.
    /// </summary>
    /// <param name="a">One steering output.</param>
    /// <param name="b">Another steering output.</param>
    /// <returns>Resulting steering output.</returns>
    public static SteeringOutput operator +(SteeringOutput a, SteeringOutput b)
    {
        return new SteeringOutput(
            a.Linear + b.Linear, 
            a.Angular + b.Angular);
    }
    
    /// <summary>
    /// Operator for multiplying a SteeringOutput object and a float.
    /// </summary>
    /// <param name="a">One steering output.</param>
    /// <param name="b">A float to multiply.</param>
    /// <returns>Resulting steering output.</returns>
    public static SteeringOutput operator *(SteeringOutput a, float b)
    {
        return new SteeringOutput(a.Linear * b, a.Angular * b);
    }
    
    /// <summary>
    /// Equality comparison with another object.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public override bool Equals(object other)
    {
        if (other is SteeringOutput output)
        {
            return Linear.Equals(output.Linear) && Mathf.IsEqualApprox(
                Angular, 
                output.Angular);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Linear.GetHashCode() ^ Angular.GetHashCode();
    }
}