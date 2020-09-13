using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace DXToolKit.Engine {
	internal sealed class BasicGUIDrawTools : GUIDrawTools {
		/*
		public override void Rectangle(RenderTarget renderTarget, RectangleF bounds, GUIColorPalette palette, GUIColor color, GUIBrightness brightness, bool transparent = false) {
			renderTarget.FillRectangle(bounds, palette[color, brightness, transparent]);
		}

		public override void Border(RenderTarget renderTarget, RectangleF bounds, GUIColorPalette palette, GUIColor color, GUIBrightness brightness, bool transparent = false) {
			bounds.Inflate(-1F, -1F);
			renderTarget.DrawRectangle(bounds, palette[color, brightness, transparent], 2);
		}

		public override void Text(RenderTarget renderTarget, Vector2 offset, TextLayout textLayout, GUIColorPalette palette, GUIColor color, GUIBrightness brightness, bool transparent = false) {
			renderTarget.DrawTextLayout(offset, textLayout, palette[color, brightness, transparent]);
		}

		public override void BevelBorder(RenderTarget renderTarget, RectangleF bounds, GUIColorPalette palette, GUIColor color, GUIBrightness brightness, float borderWidth, bool inverted, bool transparent = false) {
			// If there is no border width, get out of here
			if (borderWidth < 0) {
				return;
			}

			// Clamp border width to the smallest of height or width divided by two (since one border can at maximum take up half the size of the smallest dimension)
			var smallest = Mathf.Min(bounds.Width / 2.0F, bounds.Height / 2.0F);
			if (borderWidth > smallest) {
				borderWidth = smallest;
			}

			var strokeStyle = new StrokeStyle(renderTarget.Factory, new StrokeStyleProperties {
				EndCap = CapStyle.Triangle,
				StartCap = CapStyle.Triangle,
			});

			// Offset with a little extra just to cover spaces between lines
			var offset = borderWidth - 0.25F;
			var strokeWidth = borderWidth * 2;

			var brush = palette[color, brightness, transparent];
			var baseColor = new Color(brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A);
			var lightColor = new Color((byte) 255, (byte) 255, (byte) 255, baseColor.A);
			var blackColor = new Color((byte) 0, (byte) 0, (byte) 0, baseColor.A);

			// Top
			brush.Color = Color.Lerp(baseColor, inverted ? blackColor : lightColor, 0.2F);
			renderTarget.DrawLine(bounds.TopLeft + new Vector2(offset, 0), bounds.TopRight + new Vector2(-offset, 0), brush, strokeWidth, strokeStyle);

			// Right
			brush.Color = Color.Lerp(baseColor, inverted ? lightColor : blackColor, 0.1F);
			renderTarget.DrawLine(bounds.TopRight + new Vector2(0, offset), bounds.BottomRight + new Vector2(0, -offset), brush, strokeWidth, strokeStyle);

			// Bottom
			brush.Color = Color.Lerp(baseColor, inverted ? lightColor : blackColor, 0.2F);
			renderTarget.DrawLine(bounds.BottomRight + new Vector2(-offset, 0), bounds.BottomLeft + new Vector2(offset, 0), brush, strokeWidth, strokeStyle);

			// Left
			brush.Color = Color.Lerp(baseColor, inverted ? blackColor : lightColor, 0.1F);
			renderTarget.DrawLine(bounds.BottomLeft + new Vector2(0, -offset), bounds.TopLeft + new Vector2(0, offset), brush, strokeWidth, strokeStyle);

			// Reset brush color to default since we "stole" the brush from the palette and bade direct changes to it
			brush.Color = baseColor;

			// No need, just.. nice
			brush = null;

			Utilities.Dispose(ref strokeStyle);
		}
		*/
		protected override void Rectangle(RenderTarget renderTarget, RectangleF bounds, SolidColorBrush brush) {
			renderTarget.FillRectangle(bounds, brush);
		}

		protected override void Border(RenderTarget renderTarget, RectangleF bounds, SolidColorBrush brush) {
			bounds.Inflate(-1F, -1F);
			renderTarget.DrawRectangle(bounds, brush, 2);
		}

		protected override void Text(RenderTarget renderTarget, Vector2 offset, TextLayout textLayout, SolidColorBrush brush) {
			renderTarget.DrawTextLayout(offset, textLayout, brush);
		}

		protected override void BevelBorder(RenderTarget renderTarget, RectangleF bounds, SolidColorBrush brush, float borderWidth, bool inverted) {
			// If there is no border width, get out of here
			if (borderWidth < 0) {
				return;
			}

			// Clamp border width to the smallest of height or width divided by two (since one border can at maximum take up half the size of the smallest dimension)
			var smallest = Mathf.Min(bounds.Width / 2.0F, bounds.Height / 2.0F);
			if (borderWidth > smallest) {
				borderWidth = smallest;
			}

			var strokeStyle = new StrokeStyle(renderTarget.Factory, new StrokeStyleProperties {
				EndCap = CapStyle.Triangle,
				StartCap = CapStyle.Triangle,
			});

			// Offset with a little extra just to cover spaces between lines
			var offset = borderWidth - 0.25F;
			var strokeWidth = borderWidth * 2;

			var baseColor = new Color(brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A);
			var lightColor = new Color((byte) 255, (byte) 255, (byte) 255, baseColor.A);
			var blackColor = new Color((byte) 0, (byte) 0, (byte) 0, baseColor.A);

			// Top
			brush.Color = Color.Lerp(baseColor, inverted ? blackColor : lightColor, 0.2F);
			renderTarget.DrawLine(bounds.TopLeft + new Vector2(offset, 0), bounds.TopRight + new Vector2(-offset, 0), brush, strokeWidth, strokeStyle);

			// Right
			brush.Color = Color.Lerp(baseColor, inverted ? lightColor : blackColor, 0.1F);
			renderTarget.DrawLine(bounds.TopRight + new Vector2(0, offset), bounds.BottomRight + new Vector2(0, -offset), brush, strokeWidth, strokeStyle);

			// Bottom
			brush.Color = Color.Lerp(baseColor, inverted ? lightColor : blackColor, 0.2F);
			renderTarget.DrawLine(bounds.BottomRight + new Vector2(-offset, 0), bounds.BottomLeft + new Vector2(offset, 0), brush, strokeWidth, strokeStyle);

			// Left
			brush.Color = Color.Lerp(baseColor, inverted ? blackColor : lightColor, 0.1F);
			renderTarget.DrawLine(bounds.BottomLeft + new Vector2(0, -offset), bounds.TopLeft + new Vector2(0, offset), brush, strokeWidth, strokeStyle);

			// Reset brush color to default since we "stole" the brush from the palette and bade direct changes to it
			brush.Color = baseColor;

			// No need, just.. nice
			brush = null;

			Utilities.Dispose(ref strokeStyle);
		}

		protected override void OuterGlow(RenderTarget renderTarget, RectangleF bounds, LinearGradientBrush brush, float size) {
			var highlightBounds = bounds;
			highlightBounds.Inflate(size, size);
			var m_bounds = bounds;
			var m_lBrushWhite = brush;
			var m_renderTarget = renderTarget;
			var m_highlightWidth = size;


			// Top
			var triangle = CreatePolygonGeometry(renderTarget, new[] {
				highlightBounds.TopLeft,
				highlightBounds.TopRight,
				m_bounds.TopRight,
				m_bounds.TopLeft
			});

			m_lBrushWhite.StartPoint = m_bounds.TopLeft;
			m_lBrushWhite.EndPoint = m_bounds.TopLeft + new Vector2(0, -m_highlightWidth);
			m_renderTarget.FillGeometry(triangle, m_lBrushWhite);

			Utilities.Dispose(ref triangle);

			// Right
			triangle = CreatePolygonGeometry(renderTarget, new[] {
				highlightBounds.TopRight,
				highlightBounds.BottomRight,
				m_bounds.BottomRight,
				m_bounds.TopRight,
			});

			m_lBrushWhite.StartPoint = new Vector2(m_bounds.Right, 0);
			m_lBrushWhite.EndPoint = new Vector2(m_bounds.Right + m_highlightWidth, 0);
			m_renderTarget.FillGeometry(triangle, m_lBrushWhite);

			Utilities.Dispose(ref triangle);

			// Bottom
			triangle = CreatePolygonGeometry(renderTarget, new[] {
				highlightBounds.BottomRight,
				highlightBounds.BottomLeft,
				m_bounds.BottomLeft,
				m_bounds.BottomRight,
			});

			m_lBrushWhite.StartPoint = new Vector2(0, m_bounds.Bottom);
			m_lBrushWhite.EndPoint = new Vector2(0, m_bounds.Bottom + m_highlightWidth);
			m_renderTarget.FillGeometry(triangle, m_lBrushWhite);

			Utilities.Dispose(ref triangle);

			// Left
			triangle = CreatePolygonGeometry(renderTarget, new[] {
				highlightBounds.BottomLeft,
				highlightBounds.TopLeft,
				m_bounds.TopLeft,
				m_bounds.BottomLeft
			});

			m_lBrushWhite.StartPoint = m_bounds.TopLeft;
			m_lBrushWhite.EndPoint = m_bounds.TopLeft + new Vector2(-m_highlightWidth, 0);
			m_renderTarget.FillGeometry(triangle, m_lBrushWhite);

			Utilities.Dispose(ref triangle);
		}

		protected override void InnerGlow(RenderTarget renderTarget, RectangleF bounds, LinearGradientBrush brush, float size) {
			var strokeStyle = new StrokeStyle(renderTarget.Factory, new StrokeStyleProperties {
				EndCap = CapStyle.Round,
				StartCap = CapStyle.Round
			});

			// Push clipping rect so the lines dont extend over the target bounds
			renderTarget.PushAxisAlignedClip(bounds, AntialiasMode.PerPrimitive);

			// Left side
			brush.StartPoint = bounds.TopLeft;
			brush.EndPoint = bounds.TopLeft + new Vector2(size, 0);
			renderTarget.DrawLine(bounds.TopLeft, bounds.BottomLeft, brush, size * 2, strokeStyle);

			// Right side
			brush.StartPoint = bounds.TopRight;
			brush.EndPoint = bounds.TopRight + new Vector2(-size, 0);
			renderTarget.DrawLine(bounds.TopRight, bounds.BottomRight, brush, size * 2, strokeStyle);

			// Top side
			brush.StartPoint = bounds.TopLeft;
			brush.EndPoint = bounds.TopLeft + new Vector2(0, size);
			renderTarget.DrawLine(bounds.TopLeft, bounds.TopRight, brush, size * 2, strokeStyle);

			// Bottom side
			brush.StartPoint = bounds.BottomLeft;
			brush.EndPoint = bounds.BottomLeft + new Vector2(0, -size);
			renderTarget.DrawLine(bounds.BottomLeft, bounds.BottomRight, brush, size * 2, strokeStyle);

			// Pop clipping rect
			renderTarget.PopAxisAlignedClip();

			// Release stroke style
			Utilities.Dispose(ref strokeStyle);
		}

		protected override void Shine(RenderTarget renderTarget, RectangleF bounds, LinearGradientBrush brush, bool inverted) {
			brush.StartPoint = bounds.TopLeft;

			// Draw a line from bottom left to top right, i want the position where a perpendicular line hits that line coming from top left
			brush.EndPoint = GetClosestPointOnLineSegment(bounds.BottomLeft, bounds.TopRight, bounds.TopLeft);

			// If inverted just flip start and endpoint
			if (inverted) {
				var tmp = brush.StartPoint;
				brush.StartPoint = brush.EndPoint;
				brush.EndPoint = tmp;
			}

			renderTarget.FillRectangle(bounds, brush);
		}
	}
}