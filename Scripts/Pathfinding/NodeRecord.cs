namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Structure needed for the algorithm to keep track of the calculations
/// to reach every node.
/// </summary>
public class NodeRecord
{
    public PositionNode Node;
    public GraphConnection Connection;
    public float CostSoFar;
    
    public static readonly NodeRecord NodeRecordNull = new() 
    {
        Node = null,
        Connection = null,
        CostSoFar = 0
    };
}