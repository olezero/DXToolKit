using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace DXToolKit.Engine {
	/// <summary>
	/// Basic render pipeline with a single render target view and a depth stencil
	/// </summary>
	public class BasicPipeline : DeviceComponent, IRenderPipeline {
		/// <summary>
		/// Gets a reference to the back buffer resource
		/// </summary>
		public Texture2D Backbuffer => m_backbuffer;

		/// <summary>
		/// Gets a reference to the depth buffer resource
		/// </summary>
		public Texture2D Depthbuffer => m_depthbuffer;

		/// <summary>
		/// Gets a reference to the RenderTargetView connected to the back buffer resource
		/// </summary>
		public RenderTargetView RenderTargetView => m_renderTargetView;

		/// <summary>
		/// Gets a reference to the DepthStencilView connected to the depth buffer resource
		/// </summary>
		public DepthStencilView DepthStencilView => m_depthStencilView;

		/// <summary>
		/// Back buffer 
		/// </summary>
		private Texture2D m_backbuffer;

		/// <summary>
		/// Depth buffer
		/// </summary>
		private Texture2D m_depthbuffer;

		/// <summary>
		/// Back buffer RTV
		/// </summary>
		private RenderTargetView m_renderTargetView;

		/// <summary>
		/// Depth buffer DSV
		/// </summary>
		private DepthStencilView m_depthStencilView;

		/// <summary>
		/// Full screen viewport
		/// </summary>
		private ViewportF m_viewport;

		/// <summary>
		/// Creates a new instance of the basic pipeline
		/// </summary>
		/// <param name="device">Device used to create resources</param>
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
			m_context.Rasterizer.SetViewport(m_viewport);

			var rtvs = m_context.OutputMerger.GetRenderTargets(OutputMergerStage.SimultaneousRenderTargetCount, out var depthView);
			m_context.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, 1.0F, 0);
			for (int i = 0; i < rtvs.Length; i++) {
				if (rtvs[i] != null) {
					m_context.ClearRenderTargetView(rtvs[i], clearColor ?? Color.Black);
					rtvs[i].Dispose();
					rtvs[i] = null;
				}
			}

			depthView.Dispose();
		}

		/// <summary>
		/// Calls swapchain present method.
		/// </summary>
		public virtual void Present(int syncInterval = 0) {
			m_swapchain.Present(syncInterval, PresentFlags.None);
		}

		/// <summary>
		/// Sets the stored depth stencil and render target view on the immediate context
		/// </summary>
		public void SetDefaultRenderTargets() {
			m_context.OutputMerger.SetRenderTargets(m_depthStencilView, m_renderTargetView);
		}

		/// <summary>
		/// Gets the stored render target view
		/// </summary>
		public RenderTargetView GetRenderTargetView() {
			return m_renderTargetView;
		}

		/// <summary>
		/// Gets the stored depth stencil view
		/// </summary>
		public DepthStencilView GetDepthStencilView() {
			return m_depthStencilView;
		}


		/// <inheritdoc />
		protected override void OnDispose() {
			DisposeTargets();
		}

		/// <summary>
		/// Simple setup targets
		/// </summary>
		private void SetupTargets() {
			m_backbuffer = m_swapchain.GetBackBuffer<Texture2D>(0);
			m_depthbuffer = new Texture2D(m_device, new Texture2DDescription {
				//Format = Format.D32_Float_S8X24_UInt,
				Format = Format.D32_Float,
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
		}

		/// <summary>
		/// Simple release resources
		/// </summary>
		private void DisposeTargets() {
			Utilities.Dispose(ref m_backbuffer);
			Utilities.Dispose(ref m_depthbuffer);
			Utilities.Dispose(ref m_renderTargetView);
			Utilities.Dispose(ref m_depthStencilView);
		}
	}
}