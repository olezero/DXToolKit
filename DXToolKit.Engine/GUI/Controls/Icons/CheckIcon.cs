using SharpDX;
using SharpDX.Direct2D1;

namespace DXToolKit.Engine {
	/// <inheritdoc />
	public class CheckIcon : IconElement {
		/// <inheritdoc />
		protected override void CreateIcon(RenderTarget renderTarget, RectangleF bounds, GUIColorPalette palette, GUIDrawTools drawTools, float recommendedStrokeWidth, SolidColorBrush iconBrush) {
			var hookOffset = recommendedStrokeWidth * 2;

			var topRight = bounds.TopRight;
			var bottomLeft = bounds.BottomLeft + new Vector2(hookOffset, 0);
			var hookEnd = bounds.BottomLeft + new Vector2(0, -hookOffset);

			var strokeStyle = new StrokeStyle(renderTarget.Factory, new StrokeStyleProperties() {
				EndCap = CapStyle.Round,
				StartCap = CapStyle.Round,
				LineJoin = LineJoin.Round,
			});
			var pathGeometry = new PathGeometry(renderTarget.Factory);
			var pathSink = pathGeometry.Open();
			pathSink.BeginFigure(topRight, FigureBegin.Hollow);
			pathSink.AddLine(bottomLeft);
			pathSink.AddLine(hookEnd);
			pathSink.EndFigure(FigureEnd.Open);
			pathSink.Close();

			renderTarget.BeginDraw();
			renderTarget.DrawGeometry(pathGeometry, iconBrush, recommendedStrokeWidth, strokeStyle);
			renderTarget.EndDraw();

			Utilities.Dispose(ref strokeStyle);
			Utilities.Dispose(ref pathGeometry);
			Utilities.Dispose(ref pathSink);
		}
	}
}