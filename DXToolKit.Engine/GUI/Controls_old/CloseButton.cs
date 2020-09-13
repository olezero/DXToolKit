using System;
using DXToolKit.GUI;
using SharpDX;
using SharpDX.Direct2D1;

namespace DXToolKit.Engine {
	/// <inheritdoc />
	public class CloseButton : GraphicButton {
		/// <inheritdoc />
		public CloseButton() { }

		/// <inheritdoc />
		public CloseButton(Action<GUIMouseEventArgs> click) {
			Click += click;
		}

		/// <inheritdoc />
		protected override void CreateGraphics(RenderTarget renderTarget, RectangleF bounds, GUIColorPalette palette, GUIDrawTools drawTools, float recommendedStrokeWidth) {
			bounds.Inflate(-recommendedStrokeWidth, -recommendedStrokeWidth);

			var topLeft = new Vector2(bounds.X + 1, bounds.Y + 1);
			var topRight = new Vector2(bounds.X + bounds.Width - 1, bounds.Y + 1);
			var bottomLeft = new Vector2(bounds.X + 1, bounds.Y + bounds.Height - 1);
			var bottomRight = new Vector2(bounds.X + bounds.Width - 1, bounds.Y + bounds.Height - 1);
			var brush = palette.GetBrush(GUIColor.Text, TextBrightness);

			renderTarget.BeginDraw();
			renderTarget.DrawLine(topLeft, bottomRight, brush, recommendedStrokeWidth);
			renderTarget.DrawLine(topRight, bottomLeft, brush, recommendedStrokeWidth);
			renderTarget.EndDraw();
		}
	}
}