using SharpDX;

namespace DXToolKit {
	/// <summary>
	/// Octree data that has a point for intersection checking
	/// </summary>
	public interface IOctreePoint : IOctreeData {
		/// <summary>
		/// Point the octree should use for intersection checking
		/// </summary>
		Vector3 Point { get; }
	}
}