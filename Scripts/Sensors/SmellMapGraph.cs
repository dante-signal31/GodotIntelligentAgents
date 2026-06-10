using Godot;
using GodotGameAIbyExample.Scripts.Pathfinding;

namespace GodotGameAIbyExample.Scripts.Sensors;

/// <summary>
/// Represents a specialized map graph designed for handling smell attenuation data.
/// Extends the functionality of the <see cref="MapGraph"/> class to incorporate
/// smell-related costs for pathfinding operations within the graph structure.
/// </summary>
/// <remarks>
/// In this map graph, the connection costs are the attenuation the signal suffers after
/// traversing the connection. The effect of this attenuation is calculated by multiplying
/// the attenuation value by the start intensity of the signal.
/// </remarks>
[Tool]
public partial class SmellMapGraph: MapGraph
{
    protected override float GetPositionCost(Vector2 neighborGlobalPosition) => 
        GetPositionCustomFloatData(neighborGlobalPosition, "SmellAttenuation");
}