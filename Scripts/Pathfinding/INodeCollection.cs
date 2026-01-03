namespace GodotGameAIbyExample.Scripts.Pathfinding;

public interface INodeCollection<T>
{
    /// <summary>
    /// Adds a node to the collection.
    /// </summary>
    /// <param name="node">The node to add to the collection.</param>
    public void Add(T node);

    /// <summary>
    /// Retrieves a node from the collection.
    /// </summary>
    /// <param name="node">The node to retrieve from the collection.</param>
    public T Get();
    
    /// <summary>
    /// Removes a node from the collection.
    /// </summary>
    /// <param name="node">The node to remove from the collection.</param>
    public void Remove(T node);


    /// <summary>
    /// Gets or sets the <see cref="NodeRecord"/> corresponding to the
    /// specified <see cref="GraphNode"/>.
    /// </summary>
    /// <param name="node">The <see cref="GraphNode"/> for which to get or set the
    /// associated <see cref="NodeRecord"/>.</param>
    /// <returns>The <see cref="NodeRecord"/> associated with the
    /// specified <see cref="GraphNode"/>.</returns>
    public T this[GraphNode node] { get; set; }
        
    /// <summary>
    /// Determines whether the collection contains the specified node.
    /// </summary>
    /// <param name="node">The node to locate in the collection.</param>
    /// <returns>True if the node is found in the collection; otherwise, false.</returns>
    public bool Contains(GraphNode node);

    /// <summary>
    /// Gets the number of elements currently contained in the collection.
    /// </summary>
    public int Count { get; }
}