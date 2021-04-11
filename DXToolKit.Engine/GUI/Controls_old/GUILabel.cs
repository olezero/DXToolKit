using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace DXToolKit.Engine {
	/// <inheritdoc />
	public class GUILabel : GUIElement {
		/// <inheritdoc />
		public GUILabel(string text) {
			Text = text;
			CanReceiveMouseInput = false;
		}

		/// <inheritdoc />
		protected override void OnRender(RenderTarget renderTarget, RectangleF bounds, TextLayout textLayout, GUIColorPalette palette, GUIDrawTools drawTools) {
			drawTools.Text(renderTarget, Vector2.Zero, textLayout, palette, GUIColor.Text, GUIBrightness.Normal);
		}
	}
}