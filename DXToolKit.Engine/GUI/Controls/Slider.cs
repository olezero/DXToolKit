using System;
using DXToolKit.Engine;
using SharpDX;
using SharpDX.Direct2D1;

namespace DXToolKit.Engine {
	/// <summary>
	/// Simple slider based on a scroll bar with a different rendering setup
	/// </summary>
	public class Slider : Scrollbar {
		/// <summary>
		/// Creates a new slider element
		/// </summary>
		/// <param name="direction">The direction of the slider</param>
		public Slider(GUIDirection direction) : base(direction) {
			ShineOpacity = 0.3F;
		}

		/// <summary>
		/// Creates a new slider element
		/// </summary>
		/// <param name="direction">The direction of the slider</param>
		/// <param name="onValueChanged">Event callback when value is changed</param>
		public Slider(GUIDirection direction, Action<float> onValueChanged) : base(direction, onValueChanged) {
			ShineOpacity = 0.3F;
		}

		/// <inheritdoc />
		protected override void OnBoundsChangedDirect() {
			ScrollElementSize = Direction == GUIDirection.Vertical ? Width : Height;
			base.OnBoundsChangedDirect();
		}

		/// <inheritdoc />
		protected override void OnRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) {
			var infBounds = drawParameters.Bounds;
			if (Direction == GUIDirection.Vertical) {
				infBounds.Inflate(-Width / 4.0F, 0);
			} else {
				infBounds.Inflate(0, -Height / 4.0F);
			}

			drawParameters.RenderTarget.PushAxisAlignedClip(infBounds, AntialiasMode.Aliased);
			tools.Background.Rectangle(infBounds);
			tools.Background.BevelBorder(infBounds, true);
			drawParameters.RenderTarget.PopAxisAlignedClip();

			var ellipse = new Ellipse(Scrollbounds.Center, Scrollbounds.Width / 2.0F, Scrollbounds.Height / 2.0F);
			var targetBrightness = drawParameters.Brightness;
			if (MouseHovering) {
				targetBrightness = GUIBrightness.Bright;
			}

			var brush = GUIColorPalette.Current[drawParameters.ForegroundColor, targetBrightness];
			drawParameters.RenderTarget.FillEllipse(ellipse, brush);
			brush = GUIColorPalette.Current[drawParameters.ForegroundColor, GUIBrightness.Darkest];
			drawParameters.RenderTarget.DrawEllipse(ellipse, brush, drawParameters.BorderWidth);

			if (drawParameters.ShineOpacity > 0.01F) {
				var radialBrush = GUIColorPalette.Current.GetRadialGradientBrush(GUIColor.Light);
				radialBrush.Center = ellipse.Point;
				radialBrush.RadiusX = ellipse.RadiusX * 2;
				radialBrush.RadiusY = ellipse.RadiusY * 2;
				radialBrush.GradientOriginOffset = new Vector2(ellipse.RadiusX / 2.0F, -ellipse.RadiusY / 2.0F);
				radialBrush.Opacity = drawParameters.ShineOpacity;
				drawParameters.RenderTarget.FillEllipse(ellipse, radialBrush);
			}

			RenderText(tools);
		}
	}
}