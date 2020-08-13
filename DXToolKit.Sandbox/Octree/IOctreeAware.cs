namespace DXToolKit {
	public interface IOctreeAware<T> where T : IOctreeData {
		Octree<T> ParentNode { get; set; }
	}
}