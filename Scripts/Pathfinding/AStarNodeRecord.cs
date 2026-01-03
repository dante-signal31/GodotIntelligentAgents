namespace GodotGameAIbyExample.Scripts.Pathfinding;

public class AStarNodeRecord: NodeRecord
{
    public float TotalEstimatedCostToTarget;
    
    public static readonly AStarNodeRecord AStarNodeRecordNull = new()
    {
        Node = null,
        Connection = null,
        CostSoFar = 0,
        TotalEstimatedCostToTarget = float.MaxValue
    };
}