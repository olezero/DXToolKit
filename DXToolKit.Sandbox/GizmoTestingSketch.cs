using DXToolKit.Engine;
using SharpDX;
using SharpDX.DirectInput;

namespace DXToolKit.Sandbox {
	internal class GizmoTestingSketch : Sketch {
		private TransformGizmo m_transformGizmo;
		private Camera3D m_camera;

		private PrimitiveRenderer m_renderer;
		private Primitive m_tube;

		private FullscreenQuad m_quad;
		protected override void OnLoad() {
			m_camera = new Camera3D();
			m_quad = new FullscreenQuad(m_device);
			/*
			m_camera.Position = new Vector3(0, 0, -1.5F);
			m_camera.SmoothLerpToTarget(Vector3.Zero);
			m_camera.Rotate(Mathf.DegToRad(60 + 90), Mathf.DegToRad(20), 0);
			*/

			m_transformGizmo = new TransformGizmo(m_device);
			Debug.SetD3DCamera(m_camera);

			ToggleBackCulling();
			//ToggleWireframe();


			/*
			m_renderer = new PrimitiveRenderer(m_device);
			m_tube = PrimitiveFactory.Tube(
				height: 1.0F,
				topOuterRadius: 1.0F,
				bottomOuterRadius: 1.0F,
				topInnerRadius: 1.0F,
				bottomInnerRadius: 1.0F,
				sides: 128
			);
			*/

			m_camera.LoadFromFile("camera");
			m_camera.SmoothLerpToTarget(Vector3.Zero);
		}

		protected override void Update() {
			m_camera.Update();
			if (Input.KeyDown(Key.F1)) {
				m_camera.SaveToFile("camera");
			}
		}

		protected override void Render() {
			
			
			ToggleDepth();
			m_transformGizmo.Render(m_camera);
			ToggleDepth();
			//Debug.Box(Vector3.One * -0.5F, Vector3.One * 0.5F, Color.White, Matrix.Identity);

			/*
			m_renderer.CustomPrimitive(m_tube, Matrix.Identity, Color.White);
			m_renderer.Render(m_camera);
			Debug.Box(Vector3.One * -0.5F, Vector3.One * 0.5F, Color.White);
			*/
			Debug.Box(Vector3.One * -0.5F, Vector3.One * 0.5F, Color.White, m_transformGizmo.Transformation);

			Debug.Cube(m_transformGizmo.Transformation, Color.White);
			Debug.Cube(m_transformGizmo.Transformation * Matrix.Translation(0, 1, 0), Color.White);
			Debug.Plane(Vector3.Up, Vector3.Zero, 10, Color.White);
			
		}

		protected override void OnUnload() {
			Utilities.Dispose(ref m_transformGizmo);
			Utilities.Dispose(ref m_renderer);
			Utilities.Dispose(ref m_quad);
		}
	}
}