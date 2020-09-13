using SharpDX;
using SharpDX.Direct2D1;

namespace DXToolKit.Engine {
	/// <summary>
	/// Cross icon
	/// </summary>
	public sealed class CrossIcon : IconElement {
		/// <inheritdoc />
		protected override void CreateIcon(RenderTarget renderTarget, RectangleF bounds, GUIColorPalette palette, GUIDrawTools drawTools, float recommendedStrokeWidth, SolidColorBrush iconBrush) {
			var strokeStyle = new StrokeStyle(Graphics.Factory, new StrokeStyleProperties {
				EndCap = CapStyle.Triangle,
				StartCap = CapStyle.Triangle,
			});
			renderTarget.BeginDraw();
			renderTarget.DrawLine(bounds.TopLeft, bounds.BottomRight, iconBrush, recommendedStrokeWidth, strokeStyle);
			renderTarget.DrawLine(bounds.TopRight, bounds.BottomLeft, iconBrush, recommendedStrokeWidth, strokeStyle);
			renderTarget.EndDraw();

			Utilities.Dispose(ref strokeStyle);
		}
	}
}