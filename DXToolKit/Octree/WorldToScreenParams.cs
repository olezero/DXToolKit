using SharpDX;

namespace DXToolKit {
	/// <summary>
	/// Parameters needed to project bounding boxes to screen space
	/// </summary>
	public struct WorldToScreenParams {
		public WorldToScreenParams(DXCamera camera, int screenWidth, int screenHeight) {
			X = 0;
			Y = 0;
			Width = screenWidth;
			Height = screenHeight;
			MinZ = 0.0F;
			MaxZ = 1.0F;
			ViewFrustum = camera.CameraFrustum;
			WorldViewProjection = camera.ViewProjection;
		}

		/// <summary>
		/// The X position of the viewport.
		/// </summary>
		public float X;

		/// <summary>
		/// The Y position of the viewport.
		/// </summary>
		public float Y;

		/// <summary>
		/// The width of the viewport.
		/// </summary>
		public float Width;

		/// <summary>
		/// The height of the viewport.
		/// </summary>
		public float Height;

		/// <summary>
		/// The minimum depth of the viewport.
		/// </summary>
		public float MinZ;

		/// <summary>
		/// The maximum depth of the viewport.
		/// </summary>
		public float MaxZ;

		/// <summary>
		/// The combined world-view-projection matrix.
		/// </summary>
		public Matrix WorldViewProjection;

		/// <summary>
		/// Camera view frustum used to cull bounds that are behind the camera
		/// </summary>
		public BoundingFrustum ViewFrustum;
	}
}