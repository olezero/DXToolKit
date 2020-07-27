using DXToolKit.Engine;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;

namespace DXToolKit.Sandbox {
	public enum GizmoDirection {
		None = -1,
		X = 0,
		Y = 1,
		Z = 2
	}

	public abstract class GizmoBase : DeviceComponent {
		protected GizmoBase(GraphicsDevice device) : base(device) { }

		protected Color GetColor(GizmoDirection direction, bool highlight) {
			var alpha = highlight ? 0.8F : 0.3F;
			if (direction == GizmoDirection.X) return new Color(1.0F, 0.0F, 0.0F, alpha);
			if (direction == GizmoDirection.Y) return new Color(0.0F, 0.0F, 1.0F, alpha);
			if (direction == GizmoDirection.Z) return new Color(0.0F, 1.0F, 0.0F, alpha);
			return default;
		}

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


		private bool m_dragging;
		private GizmoDirection m_direction;

		protected GizmoDirection HandleDrag(ref DXCamera camera, ref Primitive collider, ref Matrix xMatrix, ref Matrix yMatrix, ref Matrix zMatrix) {
			if (Input.MousePressed(MouseButton.Left) == false) m_dragging = false;
			if (m_dragging == false) m_direction = GizmoDirection.None;
			if (m_direction == GizmoDirection.None) {
				m_direction = PickDirection(ref camera, ref collider, ref xMatrix, ref yMatrix, ref zMatrix);
				if (m_direction != GizmoDirection.None && Input.MouseDown(MouseButton.Left)) {
					m_dragging = true;
				}
			}

			return m_direction;
		}

		protected (Matrix xMatrix, Matrix yMatrix, Matrix zMatrix) ConstructMatrices(Matrix world) {
			var xMatrix = Matrix.RotationZ(Mathf.DegToRad(90)) * world;
			var yMatrix = world;
			var zMatrix = Matrix.RotationX(Mathf.DegToRad(90)) * world;
			return (xMatrix, yMatrix, zMatrix);
		}

		public abstract void Render(DXCamera camera, PrimitiveRenderer renderer, Matrix world);
	}

	public class TranslationGizmo : GizmoBase {
		private Primitive m_line;
		private Primitive m_cap;
		private Primitive m_collider;
		private GizmoDirection m_direction;

		private float m_arrowLength = 0.5F;
		private float m_capLength = 0.1F;
		private float m_lineRadius = 0.005F;
		private float m_capRadius = 0.02F;

		public TranslationGizmo(GraphicsDevice device) : base(device) {
			m_line = PrimitiveFactory.Cone(m_arrowLength - m_capLength, m_lineRadius, m_lineRadius, 8);
			m_cap = PrimitiveFactory.Cone(m_capLength, m_capRadius, 0.0F, 8);
			m_collider = PrimitiveFactory.Cone(m_arrowLength, 0.1F, 0.1F, 8);
		}

		public override void Render(DXCamera camera, PrimitiveRenderer renderer, Matrix world) {
			var (xMatrix, yMatrix, zMatrix) = ConstructMatrices(world);

			m_direction = HandleDrag(ref camera, ref m_collider, ref xMatrix, ref yMatrix, ref zMatrix);
			
			
			
			
			

			DrawArrow(renderer, ref xMatrix, GizmoDirection.X);
			DrawArrow(renderer, ref yMatrix, GizmoDirection.Y);
			DrawArrow(renderer, ref zMatrix, GizmoDirection.Z);
		}

		private void DrawArrow(PrimitiveRenderer renderer, ref Matrix world, GizmoDirection direction) {
			var clr = GetColor(direction, direction == m_direction);
			renderer.CustomPrimitive(m_line, world, clr);
			renderer.CustomPrimitive(m_cap, Matrix.Translation(0, m_arrowLength - m_capLength, 0) * world, clr);
		}

		protected override void OnDispose() { }
	}

	public class TransformGizmo : DeviceComponent {
		private PrimitiveRenderer m_renderer;
		private TranslationGizmo m_translationGizmo;
		private BlendState m_blendState;
		private PixelShader m_pixelShader;

		public TransformGizmo(GraphicsDevice device) : base(device) {
			m_renderer = new PrimitiveRenderer(m_device);
			m_translationGizmo = new TranslationGizmo(m_device);
			var blendDesc = BlendStateDescription.Default();
			blendDesc.RenderTarget[0].IsBlendEnabled = true;
			blendDesc.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
			blendDesc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
			m_blendState = new BlendState(m_device, blendDesc);
			var src = ShaderBytecode.Compile(SHADER_SOURCE, "PS", "ps_5_0");
			m_pixelShader = new PixelShader(m_device, src);
			Utilities.Dispose(ref src);
		}

		public void Render(DXCamera camera) {
			m_context.OutputMerger.SetBlendState(m_blendState);
			m_translationGizmo.Render(camera, m_renderer, Matrix.RotationY(1) * Matrix.RotationZ(2));
			m_renderer.Render(camera, null, m_pixelShader);
			m_context.OutputMerger.SetBlendState(null);
		}

		protected override void OnDispose() {
			Utilities.Dispose(ref m_renderer);
			Utilities.Dispose(ref m_translationGizmo);
			Utilities.Dispose(ref m_blendState);
			Utilities.Dispose(ref m_pixelShader);
		}

		private const string SHADER_SOURCE = @"
			struct PSInn {
				float4 pos 		: SV_POSITION;
				float4 color 	: COLOR;
				float3 normal 	: NORMAL;
			};

			float4 PS(PSInn input) : SV_TARGET {
			    return input.color;
			}
		";
	}

	internal class GizmoTestingSketch : Sketch {
		private TransformGizmo m_transformGizmo;
		private Camera3D m_camera;

		protected override void OnLoad() {
			m_camera = new Camera3D();
			m_camera.Position = new Vector3(0, 0, -1.5F);
			m_camera.SmoothLerpToTarget(Vector3.Zero);
			m_camera.Rotate(Mathf.DegToRad(60 + 90), Mathf.DegToRad(20), 0);
			m_transformGizmo = new TransformGizmo(m_device);
			Debug.SetD3DCamera(m_camera);
		}

		protected override void Update() {
			m_camera.Update();
		}

		protected override void Render() {
			ToggleDepth();
			m_transformGizmo.Render(m_camera);
			ToggleDepth();
			Debug.Box(Vector3.One * -0.5F, Vector3.One * 0.5F, Color.White, Matrix.Identity);
		}

		protected override void OnUnload() {
			Utilities.Dispose(ref m_transformGizmo);
		}
	}
}