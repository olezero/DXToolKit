using System;
using DXToolKit.GUI;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace DXToolKit.Engine {
	public class Button : ActiveElement {
		public Button() : this(null, GUIColor.Default, null) { }
		public Button(string text) : this(text, GUIColor.Default, null) { }
		public Button(string text, Action<GUIMouseEventArgs> onclick) : this(text, GUIColor.Default, onclick) { }

		public Button(string text, GUIColor color) : this(text, color, null) { }

		public Button(string text, GUIColor color, Action<GUIMouseEventArgs> onclick) {
			if (text != null) Text = text;
			if (onclick != null) Click += onclick;
			GUIColor = color;
			TextAlignment = TextAlignment.Center;
			ParagraphAlignment = ParagraphAlignment.Center;
			WordWrapping = WordWrapping.NoWrap;
			Padding = new GUIPadding(0);
		}

		protected override void OnRender(RenderTarget renderTarget, RectangleF bounds, TextLayout textLayout, GUIColorPalette palette, GUIDrawTools drawTools) {
			base.OnRender(renderTarget, bounds, textLayout, palette, drawTools);
			var textOffset = Vector2.Zero;

			if (MouseHovering) {
				if (IsMousePressed) {
					drawTools.Rectangle(renderTarget, bounds, palette, GUIColor, GUIBrightness.Brightest);
					textOffset.X += 1;
					textOffset.Y += 1;
				} else {
					drawTools.Rectangle(renderTarget, bounds, palette, GUIColor, GUIBrightness.Bright);
				}
			} else {
				drawTools.Rectangle(renderTarget, bounds, palette, GUIColor, Brightness);
			}

			drawTools.Text(renderTarget, textOffset, textLayout, palette, GUIColor.Text, TextBrightness);
		}
	}
}