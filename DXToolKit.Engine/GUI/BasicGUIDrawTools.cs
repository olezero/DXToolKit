using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace DXToolKit.Engine {
	internal sealed class BasicGUIDrawTools : GUIDrawTools {
		public override void Rectangle(RenderTarget renderTarget, RectangleF bounds, GUIColorPalette palette, GUIColor color, GUIBrightness brightness) {
			renderTarget.FillRectangle(bounds, palette[color, brightness]);
		}

		public override void Border(RenderTarget renderTarget, RectangleF bounds, GUIColorPalette palette, GUIColor color, GUIBrightness brightness) {
			bounds.Inflate(-1F, -1F);
			renderTarget.DrawRectangle(bounds, palette[color, brightness], 2);
		}

		public override void Text(RenderTarget renderTarget, Vector2 offset, TextLayout textLayout, GUIColorPalette palette, GUIColor color, GUIBrightness brightness) {
			renderTarget.DrawTextLayout(offset, textLayout, palette[color, brightness]);
		}
	}
}