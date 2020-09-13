using DXToolKit;
using DXToolKit.Engine;
using SharpDX;
using SharpDX.Direct2D1;

namespace DXToolKit.Engine {
	/// <summary>
	/// Basic arrow icon
	/// </summary>
	public sealed class ArrowIcon : IconElement {
		private float m_rotation;
		private ArrowType m_arrowType;

		/// <summary>
		/// Gets or sets the rotation of the arrow in radians
		/// </summary>
		public float Rotation {
			get => m_rotation;
			set {
				if (!MathUtil.NearEqual(value, m_rotation)) {
					m_rotation = value;
					ToggleGraphicsRedraw();
				}
			}
		}

		/// <summary>
		/// Gets or sets the type of arrow	
		/// </summary>
		public ArrowType ArrowType {
			get => m_arrowType;
			set {
				if (m_arrowType != value) {
					ToggleGraphicsRedraw();
				}

				m_arrowType = value;
			}
		}

		/// <summary>
		/// Creates a new arrow icon
		/// </summary>
		/// <param name="rotationDegrees">Rotation of the arrow icon in degrees</param>
		/// <param name="arrowType">Type of arrow</param>
		public ArrowIcon(float rotationDegrees = 0, ArrowType arrowType = ArrowType.Normal) {
			m_rotation = Mathf.DegToRad(rotationDegrees);
			m_arrowType = arrowType;
		}

		/// <inheritdoc />
		protected override void CreateIcon(RenderTarget renderTarget, RectangleF bounds, GUIColorPalette palette, GUIDrawTools drawTools, float recommendedStrokeWidth, SolidColorBrush iconBrush) {
			var brush = iconBrush;
			var center = bounds.Center;
			var bottomCenter = new Vector2(bounds.Center.X, bounds.Bottom);
			var topCenter = new Vector2(bounds.Center.X, bounds.Top);
			var centerLeft = new Vector2(bounds.Left, bounds.Center.Y);
			var centerRight = new Vector2(bounds.Right, bounds.Center.Y);
			var bottomLeft = bounds.BottomLeft;
			var bottomRight = bounds.BottomRight;

			renderTarget.Transform = Matrix3x2.Translation(-center) * Matrix3x2.Rotation(m_rotation) * Matrix3x2.Translation(center);

			if (m_arrowType == ArrowType.Normal) {
				var stokeStyle = new StrokeStyle(renderTarget.Factory, new StrokeStyleProperties {
					EndCap = CapStyle.Triangle,
					StartCap = CapStyle.Square,
				});
				renderTarget.BeginDraw();
				renderTarget.DrawLine(bottomCenter, topCenter, brush, recommendedStrokeWidth, stokeStyle);
				renderTarget.DrawLine(topCenter, centerLeft, brush, recommendedStrokeWidth, stokeStyle);
				renderTarget.DrawLine(topCenter, centerRight, brush, recommendedStrokeWidth, stokeStyle);
				renderTarget.EndDraw();

				Utilities.Dispose(ref stokeStyle);
			} else {
				// Triangle arrow
				var path = new PathGeometry(renderTarget.Factory);
				var sink = path.Open();
				var figureBegin = m_arrowType == ArrowType.Triangle ? FigureBegin.Hollow : FigureBegin.Filled;

				// Bottom left
				sink.BeginFigure(bottomLeft + new Vector2(recommendedStrokeWidth / 4.0F, 0), figureBegin);
				// Bottom right
				sink.AddLine(bottomRight + new Vector2(-recommendedStrokeWidth / 4.0F, 0));
				// Top center
				sink.AddLine(topCenter + new Vector2(0, recommendedStrokeWidth / 2.0F));
				sink.EndFigure(FigureEnd.Closed);
				sink.Close();

				renderTarget.BeginDraw();
				if (figureBegin == FigureBegin.Hollow) {
					renderTarget.DrawGeometry(path, brush, recommendedStrokeWidth);
				} else {
					// Fill and draw geometry since we want the extra outline to match size with the hollow one
					renderTarget.DrawGeometry(path, brush, recommendedStrokeWidth);
					renderTarget.FillGeometry(path, brush);
				}

				renderTarget.EndDraw();

				Utilities.Dispose(ref sink);
				Utilities.Dispose(ref path);
			}

			brush = null;
		}
	}
}