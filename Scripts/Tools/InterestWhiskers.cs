using System.Collections.Generic;
using Godot;

namespace GodotGameAIbyExample.Scripts.Tools;

[Tool]
public partial class InterestWhiskers: Node2D
{
    public struct Interest
    {
        public float Value;
        public Vector2 Direction;
    }
    
    private readonly List<RayEnds> _interestWhiskers = new();
    private readonly List<float> _interests = new();
    
    public int Count => _interestWhiskers.Count;

    public void ReloadWhiskers(List<RayEnds> interestEnds)
    {
        _interestWhiskers.Clear();
        _interestWhiskers.AddRange(interestEnds);
        _interests.Clear();
        _interests.AddRange(new float[_interestWhiskers.Count]);
    }

    public void CalculateInterests(Vector2 idealVelocity)
    {
        // idealVelocity is in global coordinates, while ray ends come in local
        // coordinates.
        int index = 0;
        foreach (RayEnds interestWhisker in _interestWhiskers)
        {
            Vector2 normalizedInterestWhisker = 
                (ToGlobal(interestWhisker.End) - ToGlobal(interestWhisker.Start))
                .Normalized();
            _interests[index] = 
                Mathf.Max(0, normalizedInterestWhisker.Dot(idealVelocity));
            index++;
        }
    }
    
    public Interest GetInterest(int index)
    {
        return new Interest
        {
            Value = _interests[index], 
            Direction = ToGlobal(_interestWhiskers[index].End) - 
                        ToGlobal(_interestWhiskers[index].Start)
        };
    }

    public List<Interest> GetInterests()
    {
        List<Interest> interests = new();
        for (int i = 0; i < Count; i++)
        {
            interests.Add(GetInterest(i));
        }
        return interests;
    }
}