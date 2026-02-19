using System;
using System.Collections.Generic;
using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

/// <summary>
/// Graph node implementation with four edges.
/// </summary>
[Tool]
public partial class GraphNode: Resource
{
    private static readonly HashSet<uint> AssignedIds = new();
    private static readonly Random Random = new();

    [Export] public uint Id { get; protected set; } = GenerateUniqueId();
    
    [Export] public Godot.Collections.Dictionary<uint, GraphConnection> Connections = 
        new();

    private static uint GenerateUniqueId()
    {
        uint newId;
        byte[] buffer = new byte[4];
        do
        {
            Random.NextBytes(buffer);
            newId = BitConverter.ToUInt32(buffer, 0);
        } while (AssignedIds.Contains(newId));

        AssignedIds.Add(newId);
        return newId;
    }

    public void AddConnection(
        uint endNodeId, 
        float cost, 
        uint orientation)
    {
        GraphConnection graphConnection = new();
        graphConnection.StartNodeId = Id;
        graphConnection.EndNodeId = endNodeId;
        graphConnection.Cost = cost;
        Connections[orientation] = graphConnection;
    }
}