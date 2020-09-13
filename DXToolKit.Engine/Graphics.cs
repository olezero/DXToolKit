using System;
using System.Drawing;
using System.Windows.Forms;
using SharpDX.DXGI;
using SharpDX.Windows;

namespace DXToolKit.Engine {
	/// <summary>
	/// Static application wide Graphics object
	/// </summary>
	public static class Graphics {
		private static GraphicsDevice m_device;
		private static RenderForm m_renderForm;
		private static FactoryCollection m_factoryCollection;

		/// <summary>
		/// Gets the render form used by the application
		/// </summary>
		public static RenderForm Renderform => m_renderForm;

		/// <summary>
		/// Gets the graphics device used by the application
		/// </summary>
		public static GraphicsDevice Device => m_device;

		/// <summary>
		/// Gets a factory collection used by the application
		/// </summary>
		public static FactoryCollection Factory => m_factoryCollection;

		/// <summary>
		/// Sets up the graphics object
		/// </summary>
		/// <param name="cmdArgs">Command line arguments</param>
		public static void Setup(string[] cmdArgs) {
			var hidden = false;
			var targetWidth = -1;
			var targetHeight = -1;
			var title = "SharpDX";

			if (cmdArgs != null) {
				foreach (var arg in cmdArgs) {
					if (arg.Contains("hidden")) {
						hidden = true;
					}

					if (arg.Contains("width")) {
						var widthStr = arg.Substring(arg.LastIndexOf('=') + 1);
						if (int.TryParse(widthStr, out var width)) {
							targetWidth = width;
						}
					}

					if (arg.Contains("height")) {
						var heightStr = arg.Substring(arg.LastIndexOf('=') + 1);
						if (int.TryParse(heightStr, out var height)) {
							targetHeight = height;
						}
					}

					if (arg.Contains("windowTitle")) {
						title = arg.Substring(arg.LastIndexOf('=') + 1);
					}
				}
			}

			m_renderForm = hidden ? new QuietRenderForm(title) : new RenderForm(title);
			m_renderForm.BackColor = Color.Black;

			if (targetWidth != -1 && targetHeight != -1) {
				// Set renderform size
				m_renderForm.ClientSize = new Size(targetWidth, targetHeight);

				// Update engine config
				EngineConfig.SetConfig(
					new ModeDescription(
						targetWidth,
						targetHeight,
						new Rational(165, 1),
						Format.R8G8B8A8_UNorm
					)
				);

				m_renderForm.StartPosition = FormStartPosition.Manual;
				m_renderForm.Location = Point.Empty;
			}


			var dxgiFactory = new Factory1();
			var d2dFactory = new SharpDX.Direct2D1.Factory1();
			var dwFactory = new SharpDX.DirectWrite.Factory1();
			m_factoryCollection = new FactoryCollection(dxgiFactory, d2dFactory, dwFactory);
			m_device = new GraphicsDevice(m_factoryCollection, m_renderForm, EngineConfig.GetModedescription());

			m_renderForm.UserResized += (sender, args) => {
				var width = m_renderForm.ClientSize.Width;
				var height = m_renderForm.ClientSize.Height;

				if (EngineConfig.ScreenWidth != width || EngineConfig.ScreenHeight != height) {
					m_device.Resize(new ModeDescription(width, height, new Rational(165, 1), Format.R8G8B8A8_UNorm));
				}
			};
		}

		/// <summary>
		/// Disposes of all unmanaged resources
		/// </summary>
		public static void Shutdown() {
			m_device?.Dispose();
			m_renderForm?.Dispose();
			m_factoryCollection?.Dispose();
		}
	}
}