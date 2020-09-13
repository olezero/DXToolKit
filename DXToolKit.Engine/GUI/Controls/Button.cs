using System;
using DXToolKit.Engine;
using DXToolKit.GUI;
using SharpDX;
using SharpDX.DirectWrite;

namespace DXToolKit.Engine {
	/// <summary>
	/// Classic button element
	/// </summary>
	public class Button : ActiveElement {
		/// <summary>
		/// Creates a new button
		/// </summary>
		public Button() {
			Height = 24.0F;
			Width = 12.0F * 8.0F;
			Text = "Button";
			TextAlignment = TextAlignment.Center;
			ParagraphAlignment = ParagraphAlignment.Center;
			WordWrapping = WordWrapping.WholeWord;
			FontWeight = FontWeight.Bold;
		}

		/// <summary>
		/// Creates a new button with specified text
		/// </summary>
		public Button(string text) : this() {
			Text = text;
		}

		/// <summary>
		/// Creates a new button with text and background color
		/// </summary>
		public Button(string text, GUIColor backgroundColor) : this(text) {
			BackgroundColor = backgroundColor;
		}

		/// <summary>
		/// Creates a new button with text, background color and a click event handler
		/// </summary>
		public Button(string text, GUIColor backgroundColor, Action<GUIMouseEventArgs> click) : this(text, backgroundColor) {
			if (click != null) Click += click;
		}

		/// <summary>
		/// Creates a new button with text and a click event handler
		/// </summary>
		public Button(string text, Action<GUIMouseEventArgs> click) : this(text) {
			if (click != null) Click += click;
		}

		/// <inheritdoc />
		protected override void OnRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) {
			var textOffset = Vector2.Zero;
			if (MouseHovering) {
				if (IsMousePressed) {
					tools.Background.Rectangle(tools.Brighten(drawParameters.Brightness, 2));
					tools.Background.BevelBorder(true, drawParameters.Brightness);
					textOffset.X += 0.5F;
					textOffset.Y += 1.0F;
				} else {
					tools.Background.Rectangle(tools.Brighten(drawParameters.Brightness, 1));
					tools.Background.BevelBorder(false, drawParameters.Brightness);
				}
			} else {
				tools.Background.Rectangle();
				tools.Background.BevelBorder(false, drawParameters.Brightness);
			}

			tools.Text(textOffset);
			tools.Shine(drawParameters.Bounds);
		}
	}
}