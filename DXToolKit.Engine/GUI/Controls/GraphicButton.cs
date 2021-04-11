using System;
using DXToolKit.GUI;
using SharpDX;
using SharpDX.Direct2D1;

namespace DXToolKit.Engine {
	/// <summary>
	/// Base class for graphic buttons that extends from IconElement
	/// Pretty much obsolete since IconButton exists
	/// </summary>
	[Obsolete("Use IconButton")]
	public abstract class GraphicButton : IconElement {
		/// <summary>
		/// Creates a new graphic button
		/// </summary>
		protected GraphicButton() : base(false) {
			Width = 24;
			Height = 24;
		}

		/// <summary>
		/// Creates a new graphic button
		/// </summary>
		protected GraphicButton(Action<GUIMouseEventArgs> click) : this() {
			if (click != null) Click += click;
		}

		/// <summary>
		/// Creates a new graphic button
		/// </summary>
		protected GraphicButton(GUIColor foregroundColor, Action<GUIMouseEventArgs> click) : this(click) {
			ForegroundColor = foregroundColor;
		}

		/// <inheritdoc />
		protected override void OnRender(GUIDrawTools tools, ref GUIDrawParameters parameters, Bitmap graphics) {
			// Could just do a simple render here
			var targetBrightness = Brightness;
			var iconOffset = new Vector2();

			// Standard hover pressed
			if (MouseHovering) {
				if (IsMousePressed) {
					targetBrightness = GUIBrightness.Brightest;
					iconOffset.X += 0.2F;
					iconOffset.Y += 0.4F;
				} else {
					targetBrightness = GUIBrightness.Bright;
				}
			}

			// Draw rectangle to contain graphic
			tools.Background.Rectangle(targetBrightness);
			tools.Background.BevelBorder(IsMousePressed && MouseHovering);

			// Get bounds for the icon
			var iconBounds = parameters.Bounds;
			iconBounds.Location += iconOffset;

			// Render bitmap
			parameters.RenderTarget.DrawBitmap(graphics, iconBounds, 1.0F, BitmapInterpolationMode.Linear);
		}
	}
}