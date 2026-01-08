using System.Linq;
using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

// It must be marked as a Tool to be found with my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to INavigationAgent will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
public partial class MeshNavigationAgent2D: NavigationAgent2D, INavigationAgent
{
    /// <summary>
    /// Event raised when the navigation map is ready to be queried.
    /// </summary>
    [Signal] public delegate void MeshNavigationReadyEventHandler();
    
    // Do not query when the map has never synchronized and is empty.
    public bool IsReady => (NavigationServer2D.MapGetIterationId(GetNavigationMap()) > 0);

    public Vector2[] PathToTarget => GetCurrentNavigationPath();

    public Vector2 PathFinalPosition
    {
        get
        {
            if (PathToTarget.Length > 0)
            {
                return PathToTarget.Last();
            }
            return Vector2.Zero;
        }
    }

    private bool _waitingForReady;

    public override void _Ready()
    {
        base._Ready();
        _waitingForReady = !IsReady;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (_waitingForReady)
        {
            if (!IsReady) return;
            EmitSignal(SignalName.MeshNavigationReady);
            _waitingForReady = false;
        }
    }
}