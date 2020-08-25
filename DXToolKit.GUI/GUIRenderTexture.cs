using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DirectWrite;
using SharpDX.DXGI;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode;

namespace DXToolKit.GUI {
	public class GUIRenderTexture : DeviceComponent {
		private RenderTarget m_texRenderTarget;

		public RenderTarget RenderTarget => m_texRenderTarget;
		public Bitmap Bitmap => m_bitmap;

		private int m_width;
		private int m_height;

		public int Width => m_width;
		public int Height => m_height;

		private Surface1 m_surface;
		private Texture2D m_texture;
		private Bitmap m_bitmap;
		private RenderTargetView m_renderTargetView;

		public GUIRenderTexture(GraphicsDevice device, int width, int height) : base(device) {
			m_width = width;
			m_height = height;
			CreateBuffers();
		}

		public void Resize(int width, int height) {
			if (m_width != width || m_height != height) {
				m_width = width;
				m_height = height;
				CreateBuffers();
			}
		}

		private void CreateBuffers() {
			ReleaseBuffers();
			m_texture = new Texture2D(m_device, new Texture2DDescription() {
				Width = m_width,
				Height = m_height,
				Format = Format.R8G8B8A8_UNorm,
				Usage = ResourceUsage.Default,
				ArraySize = 1,
				BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
				MipLevels = 1,
				OptionFlags = ResourceOptionFlags.None,
				SampleDescription = new SampleDescription(1, 0),
				CpuAccessFlags = CpuAccessFlags.None,
			});
			m_surface = m_texture.QueryInterface<Surface1>();
			m_texRenderTarget = new RenderTarget(m_device.Factory, m_surface, new RenderTargetProperties {
				PixelFormat = new PixelFormat(Format.R8G8B8A8_UNorm, AlphaMode.Premultiplied),
			});
			m_bitmap = new Bitmap(m_device, m_surface, new BitmapProperties {
				PixelFormat = new PixelFormat(Format.R8G8B8A8_UNorm, AlphaMode.Premultiplied)
			});
			m_renderTarget.TextAntialiasMode = TextAntialiasMode.Default;
			m_renderTargetView = new RenderTargetView(m_device, m_texture);
		}

		private void ReleaseBuffers() {
			Utilities.Dispose(ref m_texture);
			Utilities.Dispose(ref m_surface);
			Utilities.Dispose(ref m_texRenderTarget);
			Utilities.Dispose(ref m_bitmap);
			Utilities.Dispose(ref m_renderTargetView);
		}

		public void Clear(Color color) {
			m_context.ClearRenderTargetView(m_renderTargetView, color);
		}

		protected override void OnDispose() {
			ReleaseBuffers();
		}
	}
}