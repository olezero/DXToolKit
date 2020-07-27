using DXToolKit.Engine;
using SharpDX;

namespace DXToolKit.Sandbox {
	public abstract class GizmoBase : DeviceComponent {
		protected GizmoBase(GraphicsDevice device) : base(device) { }
		protected override void OnDispose() { }
	}

	public class TransformGizmo : DeviceComponent {
		private PrimitiveRenderer m_renderer;

		public TransformGizmo(GraphicsDevice device) : base(device) {
			m_renderer = new PrimitiveRenderer(m_device);
		}

		public void Render(DXCamera camera) {
			Debug.Log("Rendering");

			m_renderer.Cube(Matrix.Identity, Color.White);

			m_renderer.Render(camera);
			
		}

		protected override void OnDispose() {
			Utilities.Dispose(ref m_renderer);
		}
	}

	internal class GizmoTestingSketch : Sketch {
		private TransformGizmo m_transformGizmo;
		private Camera3D m_camera;

		protected override void OnLoad() {
			m_camera = new Camera3D();
			m_camera.Position = new Vector3(0, 0, -5);
			m_camera.SmoothLerpToTarget(Vector3.Zero);
			m_camera.Rotate(Mathf.DegToRad(45), Mathf.DegToRad(20), 0);
			m_transformGizmo = new TransformGizmo(m_device);
		}

		protected override void Update() {
			m_camera.Update();
		}

		protected override void Render() {
			m_transformGizmo.Render(m_camera);
		}

		protected override void OnUnload() {
			Utilities.Dispose(ref m_transformGizmo);
		}
	}
}