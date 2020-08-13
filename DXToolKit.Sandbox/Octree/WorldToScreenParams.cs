using SharpDX;

namespace DXToolKit {
	/// <summary>
	/// Parameters needed to project bounding boxes to screen space
	/// </summary>
	public struct WorldToScreenParams {
		/// <summary>
		/// The X position of the viewport.
		/// </summary>
		public float x;

		/// <summary>
		/// The Y position of the viewport.
		/// </summary>
		public float y;

		/// <summary>
		/// The width of the viewport.
		/// </summary>
		public float width;

		/// <summary>
		/// The height of the viewport.
		/// </summary>
		public float height;

		/// <summary>
		/// The minimum depth of the viewport.
		/// </summary>
		public float minZ;

		/// <summary>
		/// The maximum depth of the viewport.
		/// </summary>
		public float maxZ;

		/// <summary>
		/// The combined world-view-projection matrix.
		/// </summary>
		public Matrix worldViewProjection;

		/// <summary>
		/// Camera view frustum used to cull bounds that are behind the camera
		/// </summary>
		public BoundingFrustum ViewFrustum;
	}
}