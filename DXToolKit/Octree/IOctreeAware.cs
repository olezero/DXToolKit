namespace DXToolKit {
	/// <summary>
	/// Adds the node the octree data is part of
	/// </summary>
	/// <typeparam name="T">The type of the object</typeparam>
	public interface IOctreeAware<T> where T : IOctreeData {
		/// <summary>
		/// Gets the parent current parent node of the object
		/// </summary>
		Octree<T> ParentNode { get; set; }
	}
}