using DXToolKit.Engine;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DirectInput;
using SharpDX.DXGI;

namespace DXToolKit.Sandbox {
	public class RTV : DeviceComponent {
		private Texture2D m_buffer;
		private RenderTargetView m_rtv;
		private ShaderResourceView m_srv;

		public Texture2D Texture => m_buffer;
		public RenderTargetView RenderTargetView => m_rtv;
		public ShaderResourceView ShaderResourceView => m_srv;

		public RTV(GraphicsDevice device) : base(device) {
			var bb = m_swapchain.GetBackBuffer<Texture2D>(0);
			m_buffer = new Texture2D(m_device, new Texture2DDescription {
				Format = Format.R32G32B32A32_Float,
				Width = bb.Description.Width,
				Height = bb.Description.Height,
				Usage = ResourceUsage.Default,
				BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
				OptionFlags = ResourceOptionFlags.None,
				SampleDescription = bb.Description.SampleDescription,
				ArraySize = 1,
				MipLevels = 1,
				CpuAccessFlags = CpuAccessFlags.None,
			});
			m_rtv = new RenderTargetView(m_device, m_buffer);
			m_srv = new ShaderResourceView(m_device, m_buffer);
			Utilities.Dispose(ref bb);
		}

		public void Clear(Color? color = null) {
			m_context.ClearRenderTargetView(m_rtv, color ?? Color.Transparent);
		}

		protected override void OnDispose() {
			Utilities.Dispose(ref m_buffer);
			Utilities.Dispose(ref m_rtv);
			Utilities.Dispose(ref m_srv);
		}
	}

	public class MultiRTV : DeviceComponent {
		private Texture2D m_depthbuffer;
		private DepthStencilView m_depthStencilView;
		private IRenderPipeline m_pipeline;
		private RTV[] m_rtvs;

		public RTV Color => m_rtvs[0];
		public RTV Normal => m_rtvs[1];
		public RTV Depth => m_rtvs[2];
		public RTV Position => m_rtvs[3];

		public MultiRTV(GraphicsDevice device, IRenderPipeline basePipeline) : base(device) {
			m_pipeline = basePipeline;
			m_device.OnResizeBegin += ReleaseTargets;
			m_device.OnResizeEnd += SetupTargets;
			SetupTargets();
		}

		private void SetupTargets() {
			ReleaseTargets();
			m_rtvs = new[] {
				new RTV(m_device),
				new RTV(m_device),
				new RTV(m_device),
				new RTV(m_device),
			};

			var bb = m_swapchain.GetBackBuffer<Texture2D>(0);

			m_depthbuffer = new Texture2D(m_device, new Texture2DDescription {
				Format = Format.D32_Float_S8X24_UInt,
				Width = bb.Description.Width,
				Height = bb.Description.Height,
				Usage = ResourceUsage.Default,
				BindFlags = BindFlags.DepthStencil,
				MipLevels = 1,
				OptionFlags = ResourceOptionFlags.None,
				SampleDescription = bb.Description.SampleDescription,
				CpuAccessFlags = CpuAccessFlags.None,
				ArraySize = 1,
			});
			m_depthStencilView = new DepthStencilView(m_device, m_depthbuffer);

			Utilities.Dispose(ref bb);
		}

		private void ReleaseTargets() {
			if (m_rtvs != null) {
				foreach (var rtv in m_rtvs) rtv?.Dispose();
			}

			m_rtvs = null;
			Utilities.Dispose(ref m_depthbuffer);
			Utilities.Dispose(ref m_depthStencilView);
		}

		public void Begin() {
			var renderTargetViews = new RenderTargetView[m_rtvs.Length];
			for (int i = 0; i < m_rtvs.Length; i++) {
				m_rtvs[i].Clear(SharpDX.Color.White);
				renderTargetViews[i] = m_rtvs[i].RenderTargetView;
			}

			m_context.ClearDepthStencilView(m_depthStencilView, DepthStencilClearFlags.Depth, 1.0F, 0);

			m_context.OutputMerger.SetRenderTargets(m_depthStencilView, renderTargetViews);
		}

		public void End() {
			// Reset render targets to default for further rendering
			m_pipeline.SetDefaultRenderTargets();
		}

		protected override void OnDispose() {
			ReleaseTargets();
		}
	}

	public class MyDiffuseShader : Shader {
		private MatrixBuffer m_matrixBuffer;
		private InputLayout m_inputLayout;

		public MyDiffuseShader(GraphicsDevice device, ShaderDescription description) : base(device, description) {
			m_matrixBuffer = new MatrixBuffer(m_device);
		}

		protected override void OnCompileEnd(Shader sender, bool success, string errorMessage = null) {
			if (success) {
				Utilities.Dispose(ref m_inputLayout);
				m_inputLayout = new InputLayout(m_device, VertexShaderBytecode, new[] {
					new InputElement("POSITION", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0),
					new InputElement("NORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0),
					new InputElement("TEXCOORD", 0, Format.R32G32_Float, InputElement.AppendAligned, 0),
				});
			}
		}

		protected override void OnVertexShade(VertexShaderStage vertexShaderStage) {
			m_context.InputAssembler.InputLayout = m_inputLayout;
			m_context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
			m_matrixBuffer.Apply(vertexShaderStage);
		}

		public void SetWorldViewProj(Matrix world, Matrix view, Matrix proj) {
			m_matrixBuffer.Set(ref world, ref view, ref proj);
		}
	}

	public class BaseScene : Scene {
		private Camera3D m_camera3D;
		private Model m_model;
		private Model m_planeModel;
		private DiffuseShader m_shader;
		private MultiRTV m_rtv;
		private SpriteBatch m_spriteBatch;
		private TransformGizmo m_transformGizmo;
		private SketchPipeline m_sketchPipeline;
		private TextureRenderer m_textureRenderer;
		private MyDiffuseShader m_diffuse;
		private MultiRenderTarget m_multiRenderTarget;
		private Shader m_pixelShader;
		private SamplerState m_pointSampler;
		private SamplerState m_ansoSampler;

		protected override void OnLoad() {
			m_camera3D = new Camera3D();
			m_camera3D.LoadFromFile("camera");
			m_camera3D.SmoothLerpToTarget(Vector3.Zero);

			Debug.SetD3DCamera(m_camera3D);
			m_transformGizmo = new TransformGizmo(m_device);


			var tube = PrimitiveFactory.Tube(1, 1, 1, 0.5f, 0.5f, 128);
			var plane = PrimitiveFactory.Plane(10, 10, 10, 10);

			m_model = new Model(m_device, tube.Positions, tube.Normals, tube.UVs, tube.Indices);
			m_planeModel = new Model(m_device, plane.Positions, plane.Normals, plane.UVs, plane.Indices);

			m_shader = new DiffuseShader(m_device);
			m_rtv = new MultiRTV(m_device, DXApp.Current.RenderPipeline);
			m_spriteBatch = new SpriteBatch(m_device);

			if (DXApp.Current.RenderPipeline is SketchPipeline basicPipeline) {
				m_sketchPipeline = basicPipeline;
			}


			DXApp.Current.ClearColor = Color.CornflowerBlue;


			m_textureRenderer = new TextureRenderer(m_device);

			m_diffuse = new MyDiffuseShader(m_device, new ShaderDescription {
				file = @"C:\Programming\HLSLShaders\diffuse.fx",
				vsEntry = "VS",
				psEntry = "PS"
			});


			m_multiRenderTarget = new MultiRenderTarget(m_device, 4, m_sketchPipeline);

			m_pixelShader = new Shader(m_device, new ShaderDescription() {
				file = @"C:\Programming\HLSLShaders\pixel.fx",
				psEntry = "PS",
			});
			m_pixelShader.EnableWatcher();

			m_pointSampler = new SamplerState(m_device, new SamplerStateDescription() {
				Filter = Filter.MinMagMipPoint,
				AddressU = TextureAddressMode.Clamp,
				AddressV = TextureAddressMode.Clamp,
				AddressW = TextureAddressMode.Clamp,
			});
			m_ansoSampler = new SamplerState(m_device, new SamplerStateDescription() {
				Filter = Filter.Anisotropic,
				AddressU = TextureAddressMode.Clamp,
				AddressV = TextureAddressMode.Clamp,
				AddressW = TextureAddressMode.Clamp,
				MaximumAnisotropy = 16,
			});

			m_pixelShader.PixelShade += stage => {
				stage.SetSampler(0, m_pointSampler);
				stage.SetSampler(1, m_ansoSampler);
				stage.SetShaderResources(0, 4, m_multiRenderTarget.SRVs);
			};
		}

		protected override void Update() {
			m_camera3D.Update();
			if (Input.KeyDown(Key.F1)) {
				m_camera3D.SaveToFile("camera");
			}
		}

		protected override void Render() {
			var width = EngineConfig.ScreenWidth / 5.0F;
			var height = EngineConfig.ScreenHeight / 5.0F;

			m_multiRenderTarget.Begin();
			m_diffuse.SetWorldViewProj(m_transformGizmo.Transformation, m_camera3D.ViewMatrix, m_camera3D.ProjectionMatrix);
			m_diffuse.Apply(m_context);
			m_model.Render();

			m_diffuse.SetWorldViewProj(Matrix.Identity, m_camera3D.ViewMatrix, m_camera3D.ProjectionMatrix);
			m_planeModel.Render();

			m_diffuse.SetWorldViewProj(Matrix.Translation(0, 0, 5), m_camera3D.ViewMatrix, m_camera3D.ProjectionMatrix);
			m_model.Render();
			m_multiRenderTarget.End();


			//m_context.Rasterizer.SetViewport(0, 0, EngineConfig.ScreenWidth, EngineConfig.ScreenHeight, 0, 1);
			m_diffuse.SetWorldViewProj(m_transformGizmo.Transformation, m_camera3D.ViewMatrix, m_camera3D.ProjectionMatrix);
			m_diffuse.Apply(m_context);
			m_model.Render();
			m_diffuse.SetWorldViewProj(Matrix.RotationX(Mathf.DegToRad(10)), m_camera3D.ViewMatrix, m_camera3D.ProjectionMatrix);
			m_planeModel.Render();


			m_pixelShader.Apply();
			m_textureRenderer.Draw(new RectangleF(0, 0, EngineConfig.ScreenWidth, EngineConfig.ScreenHeight));

			m_sketchPipeline.IsDepthEnabled = false;
			m_transformGizmo.Render(m_camera3D);
			m_sketchPipeline.IsDepthEnabled = true;

			for (int i = 0; i < m_multiRenderTarget.BufferCount; i++) {
				m_textureRenderer.Draw(m_multiRenderTarget.SRVs[i], new RectangleF(EngineConfig.ScreenWidth - width, i * height, width, height));
			}

			m_textureRenderer.Draw(m_multiRenderTarget.DepthSrv, new RectangleF(EngineConfig.ScreenWidth - width, height * m_multiRenderTarget.BufferCount, width, height));


			/*
			m_rtv.Begin();
			//m_shader.Render(m_transformGizmo.Transformation, m_camera3D.ViewMatrix, m_camera3D.ProjectionMatrix, m_model.TriangleCount);

			m_diffuse.SetWorldViewProj(m_transformGizmo.Transformation, m_camera3D.ViewMatrix, m_camera3D.ProjectionMatrix);
			m_diffuse.Apply(m_context);
			m_model.Render();

			//m_shader.Render(Matrix.Identity, m_camera3D.ViewMatrix, m_camera3D.ProjectionMatrix, m_model.TriangleCount);
			//m_planeModel.Render();
			m_rtv.End();


			// Apply vertex and index buffers
			m_model.Render();
			// Apply shader
			m_shader.Render(m_transformGizmo.Transformation, m_camera3D.ViewMatrix, m_camera3D.ProjectionMatrix, m_model.TriangleCount);
			// Debug.Cube(Matrix.Identity, Color.White);
			*/


			/*
			m_spriteBatch.Draw(m_rtv.Color.ShaderResourceView, new RectangleF(0, 0, width, height));
			m_spriteBatch.Draw(m_rtv.Normal.ShaderResourceView, new RectangleF(0, height, width, height));
			m_spriteBatch.Draw(m_rtv.Depth.ShaderResourceView, new RectangleF(0, height * 2, width, height));
			m_spriteBatch.Draw(m_rtv.Position.ShaderResourceView, new RectangleF(0, height * 3, width, height));
			m_spriteBatch.Render();
			*/


			// Need a pixel shader that takes all RTV's and produces something cool.


			/*

			m_sketchPipeline.IsDepthEnabled = false;
			m_transformGizmo.Render(m_camera3D);
			m_sketchPipeline.IsDepthEnabled = true;
			m_textureRenderer.Draw(m_rtv.Depth.ShaderResourceView, new RectangleF(0, 0, width, height));
			*/
		}

		protected override void OnUnload() {
			Utilities.Dispose(ref m_model);
			Utilities.Dispose(ref m_shader);
			Utilities.Dispose(ref m_rtv);
			Utilities.Dispose(ref m_planeModel);
			Utilities.Dispose(ref m_textureRenderer);
			Utilities.Dispose(ref m_spriteBatch);
			Utilities.Dispose(ref m_diffuse);
			Utilities.Dispose(ref m_multiRenderTarget);
			Utilities.Dispose(ref m_pixelShader);
			Utilities.Dispose(ref m_pointSampler);
			Utilities.Dispose(ref m_ansoSampler);
		}
	}
}