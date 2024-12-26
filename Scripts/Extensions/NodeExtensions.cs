using Godot;

namespace GodotGameAIbyExample.Scripts.Extensions;

/// <summary>
/// Method extensions for the Node class.
/// </summary>
public static class NodeExtensions
{
    public static T FindChild<T>(this Node parentNode, bool recursive = false) where T: class
    {
        foreach (Node child in parentNode.GetChildren())
        {
            if (child is T node)
            {
                return node;
            }
            if (recursive)
            {
                T foundNode = child.FindChild<T>(true);
                if (foundNode != null)
                {
                    return foundNode;
                }
            }
        }
        return null;
    }
}