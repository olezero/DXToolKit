using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace DXToolKit.Engine {
	public abstract class GUIDrawTools {
		public static GUIDrawTools Current;

		private GUIDrawParameters m_currentParameters;

		protected GUIDrawTools() {
			if (Current == null) {
				Current = this;
			}
		}

		public void SetParams(ref GUIDrawParameters drawParameters) {
			m_currentParameters = drawParameters;
		}
		
		public abstract void Rectangle(RenderTarget renderTarget, RectangleF bounds, GUIColorPalette palette, GUIColor color, GUIBrightness brightness);
		public abstract void Border(RenderTarget renderTarget, RectangleF bounds, GUIColorPalette palette, GUIColor color, GUIBrightness brightness);
		public abstract void Text(RenderTarget renderTarget, Vector2 offset, TextLayout textLayout, GUIColorPalette palette, GUIColor color, GUIBrightness brightness);

		public void Rectangle(GUIColor color, GUIBrightness brightness) {
			Rectangle(m_currentParameters.RenderTarget, m_currentParameters.Bounds, GUIColorPalette.Current, color, brightness);
		}

		public void Border(GUIColor color, GUIBrightness brightness) {
			Border(m_currentParameters.RenderTarget, m_currentParameters.Bounds, GUIColorPalette.Current, color, brightness);
		}

		public void Text(Vector2? offset = null, GUIColor? color = null, GUIBrightness? brightness = null) {
			var textOffset = m_currentParameters.TextOffset;
			if (offset is Vector2 v2Offset) {
				textOffset += v2Offset;
			}

			Text(
				m_currentParameters.RenderTarget,
				textOffset,
				m_currentParameters.TextLayout,
				GUIColorPalette.Current, color ?? m_currentParameters.TextColor,
				brightness ?? m_currentParameters.TextBrightness
			);
		}

		public ForegroundDrawTools Foreground = new ForegroundDrawTools();
		public BackgroundDrawTools Background = new BackgroundDrawTools();

		private void prect(GUIColor color, GUIBrightness brightness) {
			Rectangle(m_currentParameters.RenderTarget, m_currentParameters.Bounds, GUIColorPalette.Current, color, brightness);
		}

		private void pborder(GUIColor color, GUIBrightness brightness) {
			Border(m_currentParameters.RenderTarget, m_currentParameters.Bounds, GUIColorPalette.Current, color, brightness);
		}

		public abstract class AreaDrawTools {
			protected abstract GUIColor GetColor(GUIDrawParameters parameters);

			public void Rectangle(GUIBrightness? brightness = null) {
				Current.prect(GetColor(Current.m_currentParameters), brightness ?? Current.m_currentParameters.Brightness);
			}

			public void Border(GUIBrightness? brightness = null) {
				Current.pborder(GetColor(Current.m_currentParameters), brightness ?? Current.m_currentParameters.Brightness);
			}
		}

		public class ForegroundDrawTools : AreaDrawTools {
			protected override GUIColor GetColor(GUIDrawParameters parameters) => parameters.ForegroundColor;
		}

		public class BackgroundDrawTools : AreaDrawTools {
			protected override GUIColor GetColor(GUIDrawParameters parameters) => parameters.BackgroundColor;
		}
	}
}