using DXToolKit.Engine;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;

namespace DXToolKit.Sandbox {
	public class MultiRenderTarget : DeviceComponent {
		private readonly int BUFFER_COUNT;
		private readonly ShaderResourceView[] m_srvs;
		private readonly RenderTargetView[] m_rtvs;
		private readonly Texture2D[] m_buffers;
		private readonly IRenderPipeline m_pipeline;
		private DepthStencilView m_depthStencilView;
		private ShaderResourceView m_depthSRV;
		private Texture2D m_depthbuffer;

		public int BufferCount => BUFFER_COUNT;
		public Texture2D[] Buffers => m_buffers;
		public RenderTargetView[] RTVs => m_rtvs;
		public ShaderResourceView[] SRVs => m_srvs;
		public DepthStencilView DepthStencilView => m_depthStencilView;
		public Texture2D Depthbuffer => m_depthbuffer;
		public ShaderResourceView DepthSrv => m_depthSRV;

		private int? m_width = null;
		private int? m_height = null;
		private SampleDescription? m_sampleDescription;
		private RawViewportF[] m_viewports;

		public MultiRenderTarget(GraphicsDevice device, int count, IRenderPipeline pipeline,
			int? width = null, int? height = null, SampleDescription? sampleDescription = null) : base(device) {
			BUFFER_COUNT = count;
			m_width = width;
			m_height = height;
			m_sampleDescription = sampleDescription;

			m_buffers = new Texture2D[count];
			m_rtvs = new RenderTargetView[count];
			m_srvs = new ShaderResourceView[count];
			m_pipeline = pipeline;

			SetupTargets();

			m_device.OnResizeBegin += ReleaseTargets;
			m_device.OnResizeEnd += SetupTargets;
		}

		private void SetupTargets() {
			ReleaseTargets();

			var bb = m_swapchain.GetBackBuffer<Texture2D>(0);
			int targetWidth = bb.Description.Width;
			int targetHeight = bb.Description.Height;
			var sampleDesc = m_sampleDescription ?? bb.Description.SampleDescription;
			if (m_width != null && m_height != null) {
				targetWidth = (int) m_width;
				targetHeight = (int) m_height;
			}

			var bufferDescription = new Texture2DDescription {
				Format = Format.R8G8B8A8_UNorm,
				Width = targetWidth,
				Height = targetHeight,
				SampleDescription = sampleDesc,
				Usage = ResourceUsage.Default,
				BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
				ArraySize = 1,
				MipLevels = 1,
				OptionFlags = ResourceOptionFlags.None,
				CpuAccessFlags = CpuAccessFlags.None
			};
			for (int i = 0; i < BUFFER_COUNT; i++) {
				m_buffers[i] = new Texture2D(m_device, bufferDescription);
				m_rtvs[i] = new RenderTargetView(m_device, m_buffers[i]);
				m_srvs[i] = new ShaderResourceView(m_device, m_buffers[i]);
			}

			m_depthbuffer = new Texture2D(m_device, new Texture2DDescription {
				//Format = Format.R24G8_Typeless,
				Format = Format.R32_Typeless,
				Width = targetWidth,
				Height = targetHeight,
				SampleDescription = sampleDesc,
				Usage = ResourceUsage.Default,
				ArraySize = 1,
				BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
				MipLevels = 1,
				OptionFlags = ResourceOptionFlags.None,
				CpuAccessFlags = CpuAccessFlags.None
			});
			m_depthStencilView = new DepthStencilView(m_device, m_depthbuffer, new DepthStencilViewDescription {
				Dimension = DepthStencilViewDimension.Texture2DMultisampled,
				//Format = Format.D24_UNorm_S8_UInt,
				Format = Format.D32_Float,
				Flags = DepthStencilViewFlags.None,
				Texture2D = new DepthStencilViewDescription.Texture2DResource {
					MipSlice = 0,
				},
			});
			m_depthSRV = new ShaderResourceView(m_device, m_depthbuffer, new ShaderResourceViewDescription {
				// Format = Format.R24_UNorm_X8_Typeless,
				Format = Format.R32_Float,
				Dimension = ShaderResourceViewDimension.Texture2DMultisampled,
				Texture2D = new ShaderResourceViewDescription.Texture2DResource {
					MipLevels = 1,
					MostDetailedMip = 0,
				},
			});
			Utilities.Dispose(ref bb);
		}

		private void ReleaseTargets() {
			Utilities.Dispose(ref m_depthbuffer);
			Utilities.Dispose(ref m_depthStencilView);
			Utilities.Dispose(ref m_depthSRV);
			for (int i = 0; i < BUFFER_COUNT; i++) {
				Utilities.Dispose(ref m_srvs[i]);
				Utilities.Dispose(ref m_rtvs[i]);
				Utilities.Dispose(ref m_buffers[i]);
				m_srvs[i] = null;
				m_rtvs[i] = null;
				m_buffers[i] = null;
			}
		}


		public void Begin() {
			if (m_width != null && m_height != null) {
				m_viewports = m_context.Rasterizer.GetViewports<RawViewportF>();
				m_context.Rasterizer.SetViewport(new ViewportF(0, 0, (float) m_width, (float) m_height));
			}

			m_context.OutputMerger.SetRenderTargets(m_depthStencilView, m_rtvs);
			for (int i = 0; i < BUFFER_COUNT; i++) {
				m_context.ClearRenderTargetView(m_rtvs[i], Color.Black);
			}

			m_context.ClearDepthStencilView(m_depthStencilView, DepthStencilClearFlags.Depth, 1.0F, 0);
		}

		public void End() {
			m_pipeline.SetDefaultRenderTargets();

			if (m_width != null && m_height != null) {
				m_context.Rasterizer.SetViewports(m_viewports, m_viewports.Length);
			}
		}

		protected override void OnDispose() {
			ReleaseTargets();
		}
	}
}