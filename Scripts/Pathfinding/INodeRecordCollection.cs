namespace GodotGameAIbyExample.Scripts.Pathfinding;

public interface INodeRecordCollection<T>
{
    /// <summary>
    /// Adds a node record to the collection.
    /// </summary>
    /// <param name="record">The node record to add to the collection.</param>
    public void Add(T record);

    /// <summary>
    /// Retrieves a node record from the collection.
    /// </summary>
    public T Get();

    /// <summary>
    /// Clears the collection contents.
    /// </summary>
    public void Clear();
    
    /// <summary>
    /// Removes a node record from the collection.
    /// </summary>
    /// <param name="record">The node record to remove from the collection.</param>
    public void Remove(T record);

    /// <summary>
    /// Updates the node record value in the collection.
    /// </summary>
    /// <param name="record">The node record to be updated.</param>
    public void RefreshRecord(T record);
    
    /// <summary>
    /// Gets or sets the <see cref="NodeRecord"/> corresponding to the
    /// specified <see cref="PositionNode"/>.
    /// </summary>
    /// <param name="node">The <see cref="PositionNode"/> for which to get or set the
    /// associated <see cref="NodeRecord"/>.</param>
    /// <returns>The <see cref="NodeRecord"/> associated with the
    /// specified <see cref="PositionNode"/>.</returns>
    public T this[PositionNode node] { get; set; }
        
    /// <summary>
    /// Determines whether the collection contains corresponding node record to
    /// the specified node.
    /// </summary>
    /// <param name="node">The node to locate its corresponding node record in the
    /// collection.</param>
    /// <returns>True if the node record is found in the collection; otherwise, ç
    /// false.</returns>
    public bool Contains(PositionNode node);

    /// <summary>
    /// Gets the number of elements currently contained in the collection.
    /// </summary>
    public int Count { get; }
}