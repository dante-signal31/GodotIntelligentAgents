using System.Linq;
using Godot;

namespace GodotGameAIbyExample.Scripts.Pathfinding;

// It must be marked as Tool to be found with my custom extension
// method FindChild<T>(). Otherwise, FindChild casting to INavigationAgent will fail. It
// seems and old Godot C# problem:
// https://github.com/godotengine/godot/issues/36395
[Tool]
public partial class MeshNavigationAgent2D: NavigationAgent2D, INavigationAgent
{
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
}