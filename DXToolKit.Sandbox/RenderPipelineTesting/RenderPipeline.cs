using DXToolKit.Engine;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace DXToolKit.Sandbox {
	public class RenderPipeline : DeviceComponent, IRenderPipeline {
		private RenderTargetView m_renderTargetView;

		private RenderTargetView m_colorRTV;
		private RenderTargetView m_normalRTV;
		private Texture2D m_colorbuffer;
		private Texture2D m_normalbuffer;
		private ShaderResourceView m_colorSRV;
		private ShaderResourceView m_normalSRV;

		private DXCamera m_camera;


		private Texture2D m_backbuffer;
		private Texture2D m_depthbuffer;
		private DepthStencilView m_depthStencilView;
		private ViewportF m_viewport;

		private SpriteBatch m_spriteBatch;

		public RenderPipeline(GraphicsDevice device) : base(device) {
			m_device.OnResizeBegin += ReleaseTargets;
			m_device.OnResizeEnd += SetupTargets;
			SetupTargets();
		}

		private void SetupTargets() {
			ReleaseTargets();
			m_backbuffer = m_swapchain.GetBackBuffer<Texture2D>(0);
			m_depthbuffer = new Texture2D(m_device, new Texture2DDescription {
				Format = Format.D32_Float_S8X24_UInt,
				Width = m_backbuffer.Description.Width,
				Height = m_backbuffer.Description.Height,
				Usage = ResourceUsage.Default,
				BindFlags = BindFlags.DepthStencil,
				MipLevels = 1,
				OptionFlags = ResourceOptionFlags.None,
				SampleDescription = m_backbuffer.Description.SampleDescription,
				CpuAccessFlags = CpuAccessFlags.None,
				ArraySize = 1,
			});
			m_renderTargetView = new RenderTargetView(m_device, m_backbuffer);
			m_depthStencilView = new DepthStencilView(m_device, m_depthbuffer);
			m_viewport = new ViewportF(0, 0, m_backbuffer.Description.Width, m_backbuffer.Description.Height, 0.0F, 1.0F);


			m_colorbuffer = new Texture2D(m_device, new Texture2DDescription {
				Format = Format.R8G8B8A8_UNorm,
				Height = EngineConfig.ScreenHeight,
				Width = EngineConfig.ScreenWidth,
				Usage = ResourceUsage.Default,
				ArraySize = 1,
				BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
				MipLevels = 1,
				OptionFlags = ResourceOptionFlags.None,
				SampleDescription = new SampleDescription(4, 0),
				CpuAccessFlags = CpuAccessFlags.None,
			});

			m_normalbuffer = new Texture2D(m_device, new Texture2DDescription {
				Format = Format.R8G8B8A8_UNorm,
				Height = EngineConfig.ScreenHeight,
				Width = EngineConfig.ScreenWidth,
				Usage = ResourceUsage.Default,
				ArraySize = 1,
				BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
				MipLevels = 1,
				OptionFlags = ResourceOptionFlags.None,
				SampleDescription = new SampleDescription(4, 0),
				CpuAccessFlags = CpuAccessFlags.None,
			});
			m_colorRTV = new RenderTargetView(m_device, m_colorbuffer);
			m_normalRTV = new RenderTargetView(m_device, m_normalbuffer);


			m_colorSRV = new ShaderResourceView(m_device, m_colorbuffer);
			m_normalSRV = new ShaderResourceView(m_device, m_normalbuffer);


			m_spriteBatch = new SpriteBatch(m_device);

			m_camera = new Camera3D();
			m_camera.Translate(EngineConfig.ScreenWidth / 2.0F, EngineConfig.ScreenHeight / 2.0F, -10);
			m_camera.OrthoWidth = EngineConfig.ScreenWidth;
			m_camera.OrthoHeight = EngineConfig.ScreenHeight;
			m_camera.IsOrthographic = true;
		}

		private void ReleaseTargets() {
			Utilities.Dispose(ref m_renderTargetView);
			Utilities.Dispose(ref m_backbuffer);
			Utilities.Dispose(ref m_depthbuffer);
			Utilities.Dispose(ref m_depthStencilView);


			Utilities.Dispose(ref m_normalbuffer);
			Utilities.Dispose(ref m_normalRTV);
			Utilities.Dispose(ref m_colorbuffer);
			Utilities.Dispose(ref m_colorRTV);

			Utilities.Dispose(ref m_colorSRV);
			Utilities.Dispose(ref m_normalSRV);
		}


		public void Begin(Color? clearColor = null) {
			m_context.OutputMerger.SetRenderTargets(m_depthStencilView, m_colorRTV, m_normalRTV);

			m_context.ClearRenderTargetView(m_colorRTV, Color.CornflowerBlue);
			m_context.ClearRenderTargetView(m_normalRTV, Color.Black);

			m_context.ClearRenderTargetView(m_renderTargetView, clearColor ?? Color.Black);
			m_context.ClearDepthStencilView(m_depthStencilView, DepthStencilClearFlags.Depth, 1.0F, 0);
			m_context.Rasterizer.SetViewport(m_viewport);
		}

		public void Present(int syncInterval = 0) {
			m_context.OutputMerger.SetRenderTargets(m_depthStencilView, m_renderTargetView);
			//m_spriteBatch.Draw(m_colorSRV, new RectangleF(0, EngineConfig.ScreenHeight / 2.0F, EngineConfig.ScreenWidth / 2.0F, EngineConfig.ScreenHeight / 2.0F));
			//m_spriteBatch.Render(m_camera);


			m_swapchain.Present(syncInterval, PresentFlags.None);
		}

		public void SetDefaultRenderTargets() {
			m_context.OutputMerger.SetRenderTargets(m_depthStencilView, m_renderTargetView);
		}

		public RenderTargetView GetRenderTargetView() {
			return m_renderTargetView;
		}

		public DepthStencilView GetDepthStencilView() {
			return m_depthStencilView;
		}

		protected override void OnDispose() {
			ReleaseTargets();
		}
	}
}