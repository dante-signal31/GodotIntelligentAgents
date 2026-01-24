namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Extends the NodeRecord structure with a region identifier to be used in graph regions
/// generation.
/// </summary>
public class RegionNodeRecord: NodeRecord
{
    public uint RegionId;
}