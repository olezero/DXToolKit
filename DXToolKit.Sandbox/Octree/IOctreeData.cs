using SharpDX;

namespace DXToolKit {
	
	public interface IOctreeData {
	//	BoundingBox Bounds { get; }
	
	}

	public interface IOctreePoint : IOctreeData {
		Vector3 Point { get; }
	}

	public interface IOctreeBounds : IOctreeData {
		BoundingBox Bounds { get; }
	}
}