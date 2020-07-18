using System;
using System.Drawing;
using System.Windows.Forms;
using SharpDX.DXGI;
using SharpDX.Windows;

namespace DXToolKit.Engine {
	public static class Graphics {
		private static GraphicsDevice m_device;
		private static RenderForm m_renderForm;
		private static FactoryCollection m_factoryCollection;

		public static RenderForm Renderform => m_renderForm;
		public static GraphicsDevice Device => m_device;
		public static FactoryCollection Factory => m_factoryCollection;

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

		public static void Shutdown() {
			m_device?.Dispose();
			m_renderForm?.Dispose();
			m_factoryCollection?.Dispose();
		}
	}
}