using Godot;

namespace GodotGameAIbyExample.Scripts.Extensions;

/// <summary>
/// Method extensions for the Node class.
/// </summary>
public static class NodeExtensions
{
    /// <summary>
    /// <p>Find first child node of given T type.</p>
    /// </summary>
    /// <param name="parentNode">Parent of the searched node.</param>
    /// <param name="recursive">Whether to search recursively.</param>
    /// <typeparam name="T">Type of the node to search.</typeparam>
    /// <returns>Found node or null if nothing has been found.</returns>
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
    
    /// <summary>
    /// <p>Find first ancestor node of given T type.</p>
    /// </summary>
    /// <param name="childNode">Descendant of the searched node.</param>
    /// <typeparam name="T">Type of the node to search.</typeparam>
    /// <returns>Found node or null if nothing has been found.</returns>
    public static T FindAncestor<T>(this Node childNode) where T: class
    {
        Node parentNode = childNode.GetParent();
        while (parentNode != null)
        {
            if (parentNode is T node)
            {
                return node;
            }
            parentNode = parentNode.GetParent();
        }
        return null;
    }
}