using DXToolKit.Engine;
using SharpDX;

namespace DXToolKit.Engine {
	/// <summary>
	/// Base gizmo used to calculate some common things like color, pick direction, etc
	/// </summary>
	public abstract class GizmoBase : DeviceComponent {
		private bool m_dragging;
		private GizmoDirection m_direction;

		/// <inheritdoc />
		protected GizmoBase(GraphicsDevice device) : base(device) { }

		/// <summary>
		/// Gets a color based on direction and if it should highlight
		/// </summary>
		protected Color GetColor(GizmoDirection direction, bool highlight) {
			var alpha = highlight ? 0.9F : 0.5F;
			if (direction == GizmoDirection.X) return new Color(1.0F, 0.0F, 0.0F, alpha);
			if (direction == GizmoDirection.Y) return new Color(0.0F, 0.0F, 1.0F, alpha);
			if (direction == GizmoDirection.Z) return new Color(0.0F, 1.0F, 0.0F, alpha);
			return default;
		}

		/// <summary>
		/// Gets a mouse intersection based on a collider primitive, and the 3 different matrices for each direction
		/// </summary>
		/// <param name="camera">Camera used to calculate mouse ray</param>
		/// <param name="collider">Primitive used for collision checking</param>
		/// <param name="xMatrix"></param>
		/// <param name="yMatrix"></param>
		/// <param name="zMatrix"></param>
		/// <returns>A GizmoDirection based on closest collider</returns>
		protected GizmoDirection PickDirection(ref DXCamera camera, ref Primitive collider, ref Matrix xMatrix, ref Matrix yMatrix, ref Matrix zMatrix) {
			var w = EngineConfig.ScreenWidth;
			var h = EngineConfig.ScreenHeight;
			var m = Input.MousePosition;
			var xIntersect = collider.Intersects(m.X, m.Y, w, h, xMatrix, camera.ViewMatrix, camera.ProjectionMatrix, out var pX, out var dX);
			var yIntersect = collider.Intersects(m.X, m.Y, w, h, yMatrix, camera.ViewMatrix, camera.ProjectionMatrix, out var pY, out var dY);
			var zIntersect = collider.Intersects(m.X, m.Y, w, h, zMatrix, camera.ViewMatrix, camera.ProjectionMatrix, out var pZ, out var dZ);

			if (dX < dY && dX < dZ) {
				return GizmoDirection.X;
			}

			if (dY < dX && dY < dZ) {
				return GizmoDirection.Y;
			}

			if (dZ < dX && dZ < dY) {
				return GizmoDirection.Z;
			}

			return GizmoDirection.None;
		}


		/// <summary>
		/// Checks and handles drag of a gizmo
		/// </summary>
		protected GizmoDirection HandleDrag(ref DXCamera camera, ref Primitive collider, ref Matrix xMatrix, ref Matrix yMatrix, ref Matrix zMatrix, out bool dragging) {
			if (m_dragging == false) m_direction = GizmoDirection.None;
			if (Input.MousePressed(MouseButton.Left) == false) m_dragging = false;

			if (m_direction == GizmoDirection.None) {
				m_direction = PickDirection(ref camera, ref collider, ref xMatrix, ref yMatrix, ref zMatrix);
				if (m_direction != GizmoDirection.None && Input.MouseDown(MouseButton.Left)) {
					m_dragging = true;
				}
			}

			dragging = m_dragging;
			return m_direction;
		}

		/// <summary>
		/// Constructs X,Y,Z matrices based on a input world matrix
		/// </summary>
		protected (Matrix xMatrix, Matrix yMatrix, Matrix zMatrix) ConstructMatrices(Matrix world) {
			var xMatrix = Matrix.RotationZ(Mathf.DegToRad(90)) * world;
			var yMatrix = world;
			var zMatrix = Matrix.RotationX(Mathf.DegToRad(90)) * world;
			return (xMatrix, yMatrix, zMatrix);
		}

		/// <summary>
		/// Gets the amount of mouse movement across a given plane normal and position
		/// </summary>
		/// <param name="camera">Camera to use for view projection matrices</param>
		/// <param name="planeNormal">Plane normal</param>
		/// <param name="planePosition">Plane position</param>
		/// <returns>Distance in as a Vector3</returns>
		protected Vector3 MouseMove(DXCamera camera, Vector3 planeNormal, Vector3 planePosition) {
			var v = new ViewportF(0, 0, EngineConfig.ScreenWidth, EngineConfig.ScreenHeight);
			var m1 = Input.MousePosition;
			var m2 = Input.MousePosition - Input.MouseMove;
			var r1 = Ray.GetPickRay((int) m1.X, (int) m1.Y, v, camera.ViewProjection);
			var r2 = Ray.GetPickRay((int) m2.X, (int) m2.Y, v, camera.ViewProjection);
			var p = new Plane(planePosition, planeNormal);
			if (r1.Intersects(ref p, out Vector3 p1) && r2.Intersects(ref p, out Vector3 p2)) {
				var diff = p1 - p2;
				if (diff.LengthSquared() > 10000.0F) {
					return Vector3.Zero;
				}

				return p1 - p2;
			}

			return Vector3.Zero;
		}


		/// <summary>
		/// Gets the points of a mouse movement on a plane
		/// </summary>
		/// <param name="camera"></param>
		/// <param name="planeNormal"></param>
		/// <param name="planePosition"></param>
		/// <param name="p1"></param>
		/// <param name="p2"></param>
		/// <returns></returns>
		protected bool MouseMove(DXCamera camera, Vector3 planeNormal, Vector3 planePosition, out Vector3 p1, out Vector3 p2) {
			p1 = default;
			p2 = default;
			var v = new ViewportF(0, 0, EngineConfig.ScreenWidth, EngineConfig.ScreenHeight);
			var m1 = Input.MousePosition;
			var m2 = Input.MousePosition - Input.MouseMove;
			var r1 = Ray.GetPickRay((int) m1.X, (int) m1.Y, v, camera.ViewProjection);
			var r2 = Ray.GetPickRay((int) m2.X, (int) m2.Y, v, camera.ViewProjection);
			var p = new Plane(planePosition, planeNormal);
			return r1.Intersects(ref p, out p1) && r2.Intersects(ref p, out p2);
		}

		/// <summary>
		/// Update and render to gizmo
		/// </summary>
		/// <param name="camera">Camera used to generate pick rays aswell as View and Projection matrices</param>
		/// <param name="renderer">Primitive renderer used to render the gizmo</param>
		/// <param name="world">World offset of the gizmo</param>
		/// <returns>The calculated matrix based on gizmo</returns>
		public abstract Matrix Render(DXCamera camera, PrimitiveRenderer renderer, Matrix world);
	}
}