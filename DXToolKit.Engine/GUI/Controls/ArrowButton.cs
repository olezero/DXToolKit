using System;
using SharpDX;
using SharpDX.Direct2D1;
using DXToolKit.GUI;

namespace DXToolKit.Engine {
	public class ArrowButton : GraphicButton {
		private float m_rotation;
		private bool m_filled;
		private bool m_open;

		public ArrowButton(float rotationDegrees, Action<GUIMouseEventArgs> onClick = null, bool open = false, bool filled = false)
			: this(rotationDegrees, GUIColor.Default, onClick, open, filled) { }

		public ArrowButton(float rotationDegrees, GUIColor color, Action<GUIMouseEventArgs> onClick = null, bool open = false, bool filled = false) {
			m_rotation = rotationDegrees;
			ForegroundColor = color;
			if (onClick != null) Click += onClick;
			m_open = open;
			m_filled = filled;
		}

		protected override void CreateGraphics(RenderTarget renderTarget, RectangleF bounds, GUIColorPalette palette, GUIDrawTools drawTools, float recommendedStrokeWidth) {
			// Set transform to rotate then translate to the center, before applying inflate so that the graphic gets drawn to the center of the button
			renderTarget.Transform = Matrix3x2.Rotation(Mathf.DegToRad(m_rotation)) * Matrix3x2.Translation(bounds.Width / 2.0F, bounds.Height / 2.0F);

			// Reduce size of the bounds, so that the arrow does not reach the edges
			bounds.Inflate(-recommendedStrokeWidth * 1.5F, -recommendedStrokeWidth);

			// Creates a triangle around 0, 0
			var top = new Vector2(0, -bounds.Height / 2.0F);
			var bottomLeft = new Vector2(-bounds.Width / 2.0F, bounds.Height / 2.0F);
			var bottomRight = new Vector2(bounds.Width / 2.0F, bounds.Height / 2.0F);

			// Get a path and geometry sink
			var pathGeometry = new PathGeometry(Graphics.Factory);
			var sink = pathGeometry.Open();

			sink.BeginFigure(bottomLeft, m_filled ? FigureBegin.Filled : FigureBegin.Hollow);
			sink.AddLine(top);
			sink.AddLine(bottomRight);
			sink.EndFigure(m_open ? FigureEnd.Open : FigureEnd.Closed);
			sink.Close();

			var strokeStyle = new StrokeStyle(Graphics.Factory, new StrokeStyleProperties() {
				// Looks best in my opinion
				LineJoin = LineJoin.Miter
			});

			renderTarget.BeginDraw();
			renderTarget.DrawGeometry(pathGeometry, palette.GetBrush(GUIColor.Text, TextBrightness), recommendedStrokeWidth, strokeStyle);

			if (m_filled) {
				renderTarget.FillGeometry(pathGeometry, palette.GetBrush(GUIColor.Text, TextBrightness));
			}

			renderTarget.EndDraw();

			// Dispose of any temp resources for drawing
			pathGeometry?.Dispose();
			strokeStyle?.Dispose();
			sink?.Dispose();
		}
	}
}