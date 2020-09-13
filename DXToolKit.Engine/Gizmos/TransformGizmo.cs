using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DirectInput;

namespace DXToolKit.Engine {
	/// <summary>
	/// Gizmo used for transforms (Translation, Rotation and Scaling)
	/// </summary>
	public class TransformGizmo : DeviceComponent {
		private enum Mode {
			Translation = 0,
			Rotation = 1,
			Scale = 2,
		}

		private PrimitiveRenderer m_renderer;
		private TranslationGizmo m_translationGizmo;
		private RotationGizmo m_rotationGizmo;
		private ScaleGizmo m_scaleGizmo;
		private BlendState m_blendState;
		private PixelShader m_pixelShader;
		private Mode m_mode;
		private bool m_local;

		private Matrix m_translationMatrix = Matrix.Identity;
		private Matrix m_scaleMatrix = Matrix.Identity;
		private Matrix m_rotationMatrix = Matrix.Identity;

		/// <summary>
		/// Gets the computed transformation of the gizmo
		/// </summary>
		public Matrix Transformation => m_scaleMatrix * m_rotationMatrix * m_translationMatrix;

		/// <summary>
		/// Creates a new instance of the transform gizmo
		/// </summary>
		public TransformGizmo(GraphicsDevice device) : base(device) {
			m_renderer = new PrimitiveRenderer(m_device);
			m_translationGizmo = new TranslationGizmo(m_device);
			m_rotationGizmo = new RotationGizmo(m_device);
			m_scaleGizmo = new ScaleGizmo(m_device);
			var blendDesc = BlendStateDescription.Default();
			blendDesc.RenderTarget[0].IsBlendEnabled = true;
			blendDesc.RenderTarget[0].SourceBlend = BlendOption.SourceAlpha;
			blendDesc.RenderTarget[0].DestinationBlend = BlendOption.InverseSourceAlpha;
			m_blendState = new BlendState(m_device, blendDesc);
			var src = ShaderBytecode.Compile(SHADER_SOURCE, "PS", "ps_5_0");
			m_pixelShader = new PixelShader(m_device, src);
			Utilities.Dispose(ref src);
			m_mode = Mode.Translation;
		}

		/// <summary>
		/// Updates and renders the Gizmo
		/// </summary>
		public void Render(DXCamera camera) {
			m_context.OutputMerger.SetBlendState(m_blendState);

			if (Input.KeyDown(Key.W)) {
				if (m_mode == Mode.Translation) {
					m_local = !m_local;
				} else {
					m_local = false;
				}

				m_mode = Mode.Translation;
			}

			if (Input.KeyDown(Key.R)) {
				if (m_mode == Mode.Rotation) {
					m_local = !m_local;
				} else {
					m_local = false;
				}

				m_mode = Mode.Rotation;
			}

			if (Input.KeyDown(Key.E)) {
				if (m_mode == Mode.Scale) {
					m_local = !m_local;
				} else {
					m_local = false;
				}

				m_mode = Mode.Scale;
			}


			var worldMatrix = m_local ? m_rotationMatrix * m_translationMatrix : m_translationMatrix;

			if (m_mode == Mode.Scale) {
				m_scaleMatrix = m_scaleGizmo.Render(camera, m_renderer, m_rotationMatrix * m_translationMatrix);
			}

			// If world transform, do just dont include rotation matrix 
			if (m_mode == Mode.Translation) {
				m_translationMatrix *= m_translationGizmo.Render(camera, m_renderer, worldMatrix);
			}

			if (m_mode == Mode.Rotation) {
				m_rotationMatrix *= m_rotationGizmo.Render(camera, m_renderer, worldMatrix);
			}

			m_renderer.Render(camera, null, m_pixelShader);
			m_context.OutputMerger.SetBlendState(null);
		}

		/// <inheritdoc />
		protected override void OnDispose() {
			Utilities.Dispose(ref m_renderer);
			Utilities.Dispose(ref m_translationGizmo);
			Utilities.Dispose(ref m_rotationGizmo);
			Utilities.Dispose(ref m_scaleGizmo);
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
}