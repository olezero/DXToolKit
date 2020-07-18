using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace DXToolKit.Engine {
	public class BasicPipeline : DeviceComponent, IRenderPipeline {
		public Texture2D Backbuffer => m_backbuffer;
		public Texture2D Depthbuffer => m_depthbuffer;

		public RenderTargetView RenderTargetView => m_renderTargetView;
		public DepthStencilView DepthStencilView => m_depthStencilView;

		private Texture2D m_backbuffer;
		private Texture2D m_depthbuffer;

		private RenderTargetView m_renderTargetView;
		private DepthStencilView m_depthStencilView;

		private ViewportF m_viewport;

		public BasicPipeline(GraphicsDevice device) : base(device) {
			SetupTargets();
			m_device.OnResizeBegin += DisposeTargets;
			m_device.OnResizeEnd += SetupTargets;
		}

		/// <summary>
		/// Sets and clears the render target and depth stencil.
		/// Also sets the rasterizer viewport to a full screen viewport.
		/// </summary>
		public virtual void Begin(Color? clearColor = null) {
			m_context.OutputMerger.SetRenderTargets(m_depthStencilView, m_renderTargetView);
			m_context.ClearRenderTargetView(m_renderTargetView, clearColor ?? Color.Black);
			m_context.ClearDepthStencilView(m_depthStencilView, DepthStencilClearFlags.Depth, 1.0F, 0);
			m_context.Rasterizer.SetViewport(m_viewport);
		}

		/// <summary>
		/// Calls swapchain present method.
		/// </summary>
		public virtual void Present(int syncInterval = 0) {
			m_swapchain.Present(syncInterval, PresentFlags.None);
		}

		public void SetDefaultRenderTargets() {
			m_context.OutputMerger.SetRenderTargets(m_depthStencilView, m_renderTargetView);
		}

		protected override void OnDispose() {
			DisposeTargets();
		}

		private void SetupTargets() {
			m_backbuffer = m_swapchain.GetBackBuffer<Texture2D>(0);
			m_depthbuffer = new Texture2D(m_device, new Texture2DDescription() {
				Format = Format.D32_Float_S8X24_UInt,
				Width = m_backbuffer.Description.Width,
				Height = m_backbuffer.Description.Height,
				Usage = ResourceUsage.Default,
				BindFlags = BindFlags.DepthStencil,
				MipLevels = 1,
				OptionFlags = ResourceOptionFlags.None,
				SampleDescription = new SampleDescription(4, 0),
				CpuAccessFlags = CpuAccessFlags.None,
				ArraySize = 1,
			});
			m_renderTargetView = new RenderTargetView(m_device, m_backbuffer);
			m_depthStencilView = new DepthStencilView(m_device, m_depthbuffer);
			m_viewport = new ViewportF(0, 0, m_backbuffer.Description.Width, m_backbuffer.Description.Height, 0.0F, 1.0F);
		}

		private void DisposeTargets() {
			Utilities.Dispose(ref m_backbuffer);
			Utilities.Dispose(ref m_depthbuffer);
			Utilities.Dispose(ref m_renderTargetView);
			Utilities.Dispose(ref m_depthStencilView);
		}
	}
}