using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace DXToolKit.Engine {
	public abstract class GUIDrawTools {
		public static GUIDrawTools Current;

		protected GUIDrawTools() {
			if (Current == null) {
				Current = this;
			}
		}

		public abstract void Rectangle(RenderTarget renderTarget, RectangleF bounds, GUIColorPalette palette, GUIColor color, GUIBrightness brightness);
		public abstract void Border(RenderTarget renderTarget, RectangleF bounds, GUIColorPalette palette, GUIColor color, GUIBrightness brightness);
		public abstract void Text(RenderTarget renderTarget, Vector2 offset, TextLayout textLayout, GUIColorPalette palette, GUIColor color, GUIBrightness brightness);
	}
}