using System;
using DXToolKit.GUI;
using SharpDX;
using SharpDX.Direct2D1;

namespace DXToolKit.Engine {
	public class CloseButton : GraphicButton {
		public CloseButton() { }

		public CloseButton(Action<GUIMouseEventArgs> click) {
			this.Click += click;
		}

		protected override void CreateGraphics(RenderTarget renderTarget, RectangleF bounds, GUIColorPalette palette, GUIDrawTools drawTools, float recommendedStrokeWidth) {
			bounds.Inflate(-recommendedStrokeWidth, -recommendedStrokeWidth);

			var topLeft = new Vector2(bounds.X, bounds.Y);
			var topRight = new Vector2(bounds.X + bounds.Width, bounds.Y);
			var bottomLeft = new Vector2(bounds.X, bounds.Y + bounds.Height);
			var bottomRight = new Vector2(bounds.X + bounds.Width, bounds.Y + bounds.Height);
			var brush = palette.GetBrush(GUIColor.Text, TextBrightness);

			renderTarget.BeginDraw();
			renderTarget.DrawLine(topLeft, bottomRight, brush, recommendedStrokeWidth);
			renderTarget.DrawLine(topRight, bottomLeft, brush, recommendedStrokeWidth);
			renderTarget.EndDraw();
		}
	}
}