using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct2D1;

namespace DXToolKit.Engine {
	public sealed class GUIColorPalette : DeviceComponent {
		public static GUIColorPalette Current;

		private Dictionary<GUIColor, Dictionary<GUIBrightness, SolidColorBrush>> m_brushes;

		public GUIColorPalette(GraphicsDevice device, GUIColorPaletteDescription description) : base(device) {
			// Create brushes
			CreateBrushes(description);

			// Set current to the first created palette
			if (Current == null) {
				Current = this;
			}
		}

		protected override void OnDispose() {
			ReleaseBrushes();
		}

		public SolidColorBrush this[GUIColor color, GUIBrightness brightness] => m_brushes[color][brightness];

		public SolidColorBrush GetBrush(GUIColor color, GUIBrightness brightness) {
			return this[color, brightness];
		}


		private void CreateBrushes(GUIColorPaletteDescription description) {
			ReleaseBrushes();
			m_brushes = new Dictionary<GUIColor, Dictionary<GUIBrightness, SolidColorBrush>>();
			foreach (GUIColor guiColor in Enum.GetValues(typeof(GUIColor))) {
				m_brushes.Add(guiColor, CreateBrushes(description[guiColor]));
			}
		}

		private Dictionary<GUIBrightness, SolidColorBrush> CreateBrushes(Color baseColor) {
			var black = Color.Black;
			var white = Color.White;
			var result = new Dictionary<GUIBrightness, SolidColorBrush> {
				{GUIBrightness.Darkest, new SolidColorBrush(m_device, Color.Lerp(baseColor, black, 0.2F))},
				{GUIBrightness.Dark, new SolidColorBrush(m_device, Color.Lerp(baseColor, black, 0.1F))},
				{GUIBrightness.Normal, new SolidColorBrush(m_device, baseColor)},
				{GUIBrightness.Bright, new SolidColorBrush(m_device, Color.Lerp(baseColor, white, 0.1F))},
				{GUIBrightness.Brightest, new SolidColorBrush(m_device, Color.Lerp(baseColor, white, 0.2F))}
			};
			return result;
		}

		private void ReleaseBrushes() {
			if (m_brushes != null) {
				foreach (var brushlib in m_brushes) {
					if (brushlib.Value != null) {
						foreach (var brush in brushlib.Value) {
							brush.Value?.Dispose();
						}

						brushlib.Value?.Clear();
					}
				}

				m_brushes?.Clear();
			}
		}

		public void DebugRender(RenderTarget renderTarget, RectangleF destination) {
			renderTarget.BeginDraw();
			var widthPer = destination.Width / 5.0F;
			var heightPer = destination.Height / 6.0F;

			var xOffset = destination.X;
			var yOffset = destination.Y;

			foreach (var brushLib in m_brushes) {
				foreach (var brush in brushLib.Value) {
					renderTarget.FillRectangle(new RectangleF(xOffset, yOffset, widthPer, heightPer), brush.Value);
					xOffset += widthPer;
				}

				yOffset += heightPer;
				xOffset = destination.X;
			}

			renderTarget.EndDraw();
		}
	}
}