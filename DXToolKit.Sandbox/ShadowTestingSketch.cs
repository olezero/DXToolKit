using System.Runtime.InteropServices;
using DXToolKit.Engine;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DirectInput;
using SharpDX.DXGI;

namespace DXToolKit.Sandbox {
	public class DepthRenderer : DeviceComponent {
		private MultiRenderTarget m_rtv;
		private Shader m_shader;
		private MatrixBuffer m_matrixBuffer;
		private InputLayout m_inputLayout;

		public ShaderResourceView DepthSRV => m_rtv.SRVs[0];
		public ShaderResourceView DepthStencilSRV => m_rtv.DepthSrv;

		public DepthRenderer(GraphicsDevice device, IRenderPipeline pipeline) : base(device) {
			m_rtv = new MultiRenderTarget(m_device, 1, pipeline, 2048, 2048, new SampleDescription(1, 0));
			m_shader = new Shader(m_device, new ShaderDescription {
				file = @"C:\Programming\HLSLShaders\depth.hlsl",
				vsEntry = "DepthVertexShader",
				psEntry = "DepthPixelShader",
			});
			m_shader.EnableWatcher();
			m_matrixBuffer = new MatrixBuffer(m_device);
			m_inputLayout = new InputLayout(m_device, m_shader.VertexShaderBytecode, new[] {
				new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
			});
		}

		public void Begin() {
			m_rtv.Begin();
		}

		public void Apply(Matrix world, Matrix view, Matrix proj) {
			m_shader.Apply();
			m_context.InputAssembler.InputLayout = m_inputLayout;
			m_context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
			m_matrixBuffer.Set(world, view, proj, false);
			m_matrixBuffer.Apply(m_context.VertexShader);
		}

		public void End() {
			m_rtv.End();
		}

		protected override void OnDispose() {
			Utilities.Dispose(ref m_rtv);
			Utilities.Dispose(ref m_shader);
			Utilities.Dispose(ref m_matrixBuffer);
			Utilities.Dispose(ref m_inputLayout);
		}
	}


	public class ShadowTestingSketch : Sketch {
		[StructLayout(LayoutKind.Sequential)]
		private struct MatrixBufferType {
			public Matrix world;
			public Matrix view;
			public Matrix proj;
			public Matrix lightView;
			public Matrix lightProj;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct LightBufferType2 {
			public Vector3 Position;
			public float Padding;
		}

		[StructLayout(LayoutKind.Sequential)]
		struct LightBufferType {
			public Vector4 ambientColor;
			public Vector4 diffuseColor;
		}

		private Camera3D m_camera;
		private Shader m_shader;
		private Model m_planeModel;
		private Model m_cubeModel;
		private Model m_sphereModel;
		private ShaderResourceView m_texture;
		private ShaderResourceView m_lightDepth;
		private SamplerState m_clampSampler;
		private SamplerState m_wrapSampler;
		private InputLayout m_inputLayout;

		private ConstantBuffer<MatrixBufferType> m_matrixBuffer;
		private ConstantBuffer<LightBufferType> m_lightBuffer;
		private ConstantBuffer<LightBufferType2> m_lightBuffer2;

		private Vector3 m_lightPosition;
		private MatrixBufferType m_matrices;
		private DepthRenderer m_depthRenderer;
		private TextureRenderer m_textureRenderer;
		private TransformGizmo m_transformGizmo;


		protected override void OnLoad() {
			m_camera = new Camera3D();
			m_camera.LoadFromFile("camera");
			m_camera.SmoothLerpToTarget(Vector3.Zero);
			Debug.SetD3DCamera(m_camera);

			m_planeModel = new Model(m_device, PrimitiveFactory.Plane(5, 5, 10, 10));
			m_cubeModel = new Model(m_device, PrimitiveFactory.Cube());
			m_sphereModel = new Model(m_device, PrimitiveFactory.Sphere(0.5F));

			m_shader = new Shader(m_device, new ShaderDescription {
				file = @"C:\Programming\HLSLShaders\shadow.hlsl",
				vsEntry = "ShadowVertexShader",
				psEntry = "ShadowPixelShader",
			});
			m_shader.EnableWatcher();


			m_matrixBuffer = new ConstantBuffer<MatrixBufferType>(m_device, new MatrixBufferType());
			m_lightBuffer = new ConstantBuffer<LightBufferType>(m_device, new LightBufferType {
				ambientColor = new Vector4(0.2F),
				diffuseColor = new Vector4(0.8F),
			});

			m_lightPosition = new Vector3(5, 5, 5);
			m_lightBuffer2 = new ConstantBuffer<LightBufferType2>(m_device, new LightBufferType2 {
				Position = m_lightPosition,
			});
			m_matrices = new MatrixBufferType {
				lightView = Matrix.Transpose(Matrix.LookAtLH(m_lightPosition, Vector3.Zero, Vector3.Up)),
				lightProj = Matrix.Transpose(Matrix.PerspectiveFovLH(Mathf.Pi / 2.0F, 1.0F, 1.0F, 1000.0F)),
			};

			var size = 128;
			var colors = new Color[size * size];
			for (int i = 0; i < colors.Length; i++) {
				colors[i].R = 255;
				colors[i].G = 255;
				colors[i].B = 255;
				colors[i].A = 255;
			}

			m_texture = TextureHelper.CreateSRV(m_device, ref colors, size, size);
			m_clampSampler = new SamplerState(m_device, new SamplerStateDescription {
				Filter = Filter.MinMagMipLinear,
				AddressU = TextureAddressMode.Clamp,
				AddressV = TextureAddressMode.Clamp,
				AddressW = TextureAddressMode.Clamp,
			});
			m_wrapSampler = new SamplerState(m_device, new SamplerStateDescription {
				Filter = Filter.MinMagMipLinear,
				AddressU = TextureAddressMode.Wrap,
				AddressV = TextureAddressMode.Wrap,
				AddressW = TextureAddressMode.Wrap,
			});

			SetupInputLayout();
			m_shader.CompileEnd += (sender, success, message) => {
				if (success) SetupInputLayout();
			};

			m_shader.VertexShade += stage => {
				m_context.InputAssembler.InputLayout = m_inputLayout;
				m_context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
				stage.SetConstantBuffer(0, m_matrixBuffer);
				stage.SetConstantBuffer(1, m_lightBuffer2);
			};

			m_shader.PixelShade += stage => {
				stage.SetSamplers(0, 2, m_clampSampler, m_wrapSampler);
				stage.SetShaderResource(0, m_texture);
				stage.SetShaderResource(1, m_lightDepth);
				stage.SetConstantBuffer(0, m_lightBuffer);
			};

			//DXApp.Current.ClearColor = Color.CornflowerBlue;


			m_depthRenderer = new DepthRenderer(m_device, Pipeline);
			m_textureRenderer = new TextureRenderer(m_device);
			m_transformGizmo = new TransformGizmo(m_device);
		}


		protected override void Update() {
			m_camera.Update();
			if (Input.KeyDown(Key.F1)) {
				m_camera.SaveToFile("camera");
				Debug.Log("Camera position saved", 1000);
			}
		}

		protected override void Render() {
			RenderDepth();
			RenderScene();

			//m_textureRenderer.Draw(m_lightDepth, new RectangleF(0, 0, EngineConfig.ScreenWidth, EngineConfig.ScreenHeight));
			//m_textureRenderer.Draw(m_depthRenderer.DepthSRV, new RectangleF(0, 0, EngineConfig.ScreenWidth, EngineConfig.ScreenHeight));
			//Debug.Cube(Matrix.Identity, Color.White);
		}


		private void RenderScene() {
			m_matrices.world = Matrix.Transpose(Matrix.Identity);
			m_matrices.view = Matrix.Transpose(m_camera.ViewMatrix);
			m_matrices.proj = Matrix.Transpose(m_camera.ProjectionMatrix);
			m_matrixBuffer.Write(m_matrices);
			m_lightBuffer2.Write(new LightBufferType2 {
				Position = m_lightPosition,
			});

			// Apply shader
			m_shader.Apply();

			m_matrices.world = Matrix.Transpose(Matrix.Translation(0, -0.5F, 0));
			m_matrixBuffer.Write(m_matrices);
			m_planeModel.Render();

			m_matrices.world = Matrix.Transpose(m_transformGizmo.Transformation);
			m_matrixBuffer.Write(m_matrices);
			m_cubeModel.Render();

			m_matrices.world = Matrix.Transpose(Matrix.Translation(-1, 0, 0));
			m_matrixBuffer.Write(m_matrices);
			m_sphereModel.Render();

			ToggleDepth();
			m_transformGizmo.Render(m_camera);
			ToggleDepth();
		}


		private float sinwave;

		private void RenderDepth() {
			sinwave += Time.DeltaTime * 0.2F;
			m_lightPosition.Y = 3;
			m_lightPosition.Z = 3;
			m_lightPosition.X = Mathf.Sin(sinwave) * 3;

			m_matrices.lightView = Matrix.Transpose(Matrix.LookAtLH(m_lightPosition, Vector3.Zero, Vector3.Up));
			m_matrices.lightProj = Matrix.Transpose(Matrix.PerspectiveFovLH(Mathf.Pi / 2.0F, EngineConfig.ScreenWidth / (float) EngineConfig.ScreenHeight, 1.0F, 100.0F));

			m_depthRenderer.Begin();
			m_depthRenderer.Apply(Matrix.Transpose(Matrix.Translation(0, -0.5F, 0)), m_matrices.lightView, m_matrices.lightProj);
			m_planeModel.Render();
			m_depthRenderer.Apply(Matrix.Transpose(m_transformGizmo.Transformation), m_matrices.lightView, m_matrices.lightProj);
			m_cubeModel.Render();
			m_depthRenderer.Apply(Matrix.Transpose(Matrix.Translation(-1, 0F, 0)), m_matrices.lightView, m_matrices.lightProj);
			m_sphereModel.Render();
			m_depthRenderer.End();

			m_lightDepth = m_depthRenderer.DepthStencilSRV;
		}


		private void SetupInputLayout() {
			if (m_shader.IsValid) {
				Utilities.Dispose(ref m_inputLayout);
				m_inputLayout = new InputLayout(m_device, m_shader.VertexShaderBytecode, new[] {
					new InputElement("POSITION", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0),
					new InputElement("NORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0),
					new InputElement("TEXCOORD", 0, Format.R32G32_Float, InputElement.AppendAligned, 0),
				});
			}
		}

		protected override void OnUnload() {
			Utilities.Dispose(ref m_planeModel);
			Utilities.Dispose(ref m_cubeModel);
			Utilities.Dispose(ref m_sphereModel);
			Utilities.Dispose(ref m_shader);
			Utilities.Dispose(ref m_texture);

			Utilities.Dispose(ref m_clampSampler);
			Utilities.Dispose(ref m_wrapSampler);

			Utilities.Dispose(ref m_inputLayout);

			Utilities.Dispose(ref m_matrixBuffer);
			Utilities.Dispose(ref m_lightBuffer);
			Utilities.Dispose(ref m_lightBuffer2);

			Utilities.Dispose(ref m_depthRenderer);
			Utilities.Dispose(ref m_textureRenderer);

			Utilities.Dispose(ref m_lightDepth);

			Utilities.Dispose(ref m_transformGizmo);
		}
	}
}