using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DirectWrite;
using SharpDX.DXGI;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode;

namespace DXToolKit.GUI {
	/// <summary>
	/// GUI Render texture used by all GUI elements to draw onto themselves
	/// </summary>
	public class GUIRenderTexture : DeviceComponent {
		/// <summary>
		/// Render target used to render onto the texture
		/// </summary>
		private RenderTarget m_texRenderTarget;

		/// <summary>
		/// Gets a reference to the rendertarget used by this render texture
		/// </summary>
		public RenderTarget RenderTarget => m_texRenderTarget;

		/// <summary>
		/// Gets a reference to the bitmap this rendertexture renders to
		/// </summary>
		public Bitmap Bitmap => m_bitmap;

		/// <summary>
		/// Width of the texture
		/// </summary>
		private int m_width;

		/// <summary>
		/// Height of the texture
		/// </summary>
		private int m_height;

		/// <summary>
		/// Gets the width of the texture. To change use Resize method.
		/// </summary>
		public int Width => m_width;

		/// <summary>
		/// Gets the height of the texture. To change use Resize method.
		/// </summary>
		public int Height => m_height;

		/// <summary>
		/// DXGI Surface for interoping between DirectX11 and Direct2D
		/// </summary>
		private Surface1 m_surface;

		/// <summary>
		/// DirectX11 texture
		/// </summary>
		private Texture2D m_texture;

		/// <summary>
		/// Direct2D texture
		/// </summary>
		private Bitmap m_bitmap;

		/// <summary>
		/// Rendertarget view used to quickly clear the texture
		/// </summary>
		private RenderTargetView m_renderTargetView;

		/// <summary>
		/// Creates a new instance of the GUIRenderTexture
		/// </summary>
		/// <param name="device">Device used to create the texture</param>
		/// <param name="width">Texture width (must be 1 or higher)</param>
		/// <param name="height">Texture height (must be 1 or higher)</param>
		public GUIRenderTexture(GraphicsDevice device, int width, int height) : base(device) {
			m_width = width;
			m_height = height;
			CreateBuffers();
		}

		/// <summary>
		/// Resizes the render texture
		/// </summary>
		/// <param name="width">Texture width (must be 1 or higher)</param>
		/// <param name="height">Texture height (must be 1 or higher)</param>
		public void Resize(int width, int height) {
			if (m_width != width || m_height != height) {
				m_width = width;
				m_height = height;
				CreateBuffers();
			}
		}

		/// <summary>
		/// Creates the buffers based on internal width/height values
		/// </summary>
		private void CreateBuffers() {
			ReleaseBuffers();
			m_texture = new Texture2D(m_device, new Texture2DDescription {
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

		/// <summary>
		/// Releases all resources used by this render texture
		/// </summary>
		private void ReleaseBuffers() {
			Utilities.Dispose(ref m_texture);
			Utilities.Dispose(ref m_surface);
			Utilities.Dispose(ref m_texRenderTarget);
			Utilities.Dispose(ref m_bitmap);
			Utilities.Dispose(ref m_renderTargetView);
		}

		/// <summary>
		/// Sets all pixels of the rendertexture to the input value
		/// </summary>
		/// <param name="color">The color to clear to</param>
		public void Clear(Color color) {
			m_context.ClearRenderTargetView(m_renderTargetView, color);
		}

		/// <inheritdoc />
		protected override void OnDispose() {
			ReleaseBuffers();
		}
	}
}