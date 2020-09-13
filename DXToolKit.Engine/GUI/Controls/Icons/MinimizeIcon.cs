using SharpDX;
using SharpDX.Direct2D1;

namespace DXToolKit.Engine {
	/// <summary>
	/// Standard minimize line
	/// </summary>
	public class MinimizeIcon : IconElement {
		/// <inheritdoc />
		protected override void CreateIcon(RenderTarget renderTarget, RectangleF bounds, GUIColorPalette palette, GUIDrawTools drawTools, float recommendedStrokeWidth, SolidColorBrush iconBrush) {
			renderTarget.BeginDraw();
			renderTarget.DrawLine(bounds.BottomLeft, bounds.BottomRight, iconBrush, recommendedStrokeWidth);
			renderTarget.EndDraw();
		}
	}
}