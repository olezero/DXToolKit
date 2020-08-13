using SharpDX;

namespace DXToolKit {
	/// <summary>
	/// Octree data that has a bounding box for intersection checking
	/// </summary>
	public interface IOctreeBounds : IOctreeData {
		/// <summary>
		/// Bounds the octree should use for intersection checking
		/// </summary>
		BoundingBox Bounds { get; }
	}
}