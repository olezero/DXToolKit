using System;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace DXToolKit.Engine {
	/// <summary>
	/// Helpful toolset for drawing GUI elements
	/// </summary>
	public abstract class GUIDrawTools {
		/// <summary>
		/// Gets the current draw tools used by the GUI framework
		/// </summary>
		public static GUIDrawTools Current;

		/// <summary>
		/// Foreground draw tools, has foreground color already set for ease of use
		/// </summary>
		public readonly ForegroundDrawTools Foreground = new ForegroundDrawTools();

		/// <summary>
		/// Background draw tools, has background color already set for ease of use
		/// </summary>
		public readonly BackgroundDrawTools Background = new BackgroundDrawTools();

		/// <summary>
		/// Current draw params set by elements when they render
		/// </summary>
		private GUIDrawParameters m_currentParameters;

		/// <summary>
		/// Sets draw parameters for this element
		/// </summary>
		internal void SetParams(ref GUIDrawParameters drawParameters) {
			m_currentParameters = drawParameters;
		}

		/// <summary>
		/// Creates a new instance of the GUI draw tools
		/// </summary>
		protected GUIDrawTools() {
			if (Current == null) {
				Current = this;
			}
		}


		public GUIBrightness Brighten(GUIBrightness baseBrightness, int steps = 1) {
			var nextStep = (int) baseBrightness + steps;
			var brightnessValueCount = Enum.GetValues(typeof(GUIBrightness)).Length;
			return (GUIBrightness) Mathf.Clamp(nextStep, 0, brightnessValueCount - 1);
		}

		public GUIBrightness Darken(GUIBrightness baseBrightness, int steps = 1) {
			var nextStep = (int) baseBrightness - steps;
			var brightnessValueCount = Enum.GetValues(typeof(GUIBrightness)).Length;
			return (GUIBrightness) Mathf.Clamp(nextStep, 0, brightnessValueCount - 1);
		}


		/// <summary>
		/// Draws a rectangle
		/// </summary>
		/// <param name="renderTarget">Render target to use for drawing</param>
		/// <param name="bounds">Bounds of the rectangle</param>
		/// <param name="brush">Brush to use for drawing</param>
		protected abstract void Rectangle(RenderTarget renderTarget, RectangleF bounds, SolidColorBrush brush);

		/// <summary>
		/// Draws a border
		/// </summary>
		/// <param name="renderTarget">Render target to use for drawing</param>
		/// <param name="bounds">Bounds of the border</param>
		/// <param name="brush">Brush to use for drawing</param>
		protected abstract void Border(RenderTarget renderTarget, RectangleF bounds, SolidColorBrush brush);

		/// <summary>
		/// Draws some text
		/// </summary>
		/// <param name="renderTarget">Render target to use for drawing</param>
		/// <param name="offset">Text offset</param>
		/// <param name="textLayout">Text layout</param>
		/// <param name="brush">Brush to use for drawing</param>
		protected abstract void Text(RenderTarget renderTarget, Vector2 offset, TextLayout textLayout, SolidColorBrush brush);

		/// <summary>
		/// Draws a beveled border
		/// </summary>
		/// <param name="renderTarget">Render target to use for drawing</param>
		/// <param name="bounds">Bounds of the border</param>
		/// <param name="brush">Brush to use for drawing</param>
		/// <param name="borderWidth">Width of the border to draw</param>
		/// <param name="inverted">If it should be inverted</param>
		protected abstract void BevelBorder(RenderTarget renderTarget, RectangleF bounds, SolidColorBrush brush, float borderWidth, bool inverted);

		/// <summary>
		/// Draws some outer glow
		/// </summary>
		/// <param name="renderTarget">Render target to use for drawing</param>
		/// <param name="bounds">Bounds of the border</param>
		/// <param name="brush">Brush to use for drawing</param>
		/// <param name="size">The size of the glow</param>
		protected abstract void OuterGlow(RenderTarget renderTarget, RectangleF bounds, LinearGradientBrush brush, float size);

		/// <summary>
		/// Draws some inner glow
		/// </summary>
		/// <param name="renderTarget">Render target to use for drawing</param>
		/// <param name="bounds">Bounds of the border</param>
		/// <param name="brush">Brush to use for drawing</param>
		/// <param name="size">The size of the glow</param>
		protected abstract void InnerGlow(RenderTarget renderTarget, RectangleF bounds, LinearGradientBrush brush, float size);

		/// <summary>
		/// Shines the element
		/// </summary>
		/// <param name="renderTarget">Render target to use for drawing</param>
		/// <param name="bounds">Bounds of the border</param>
		/// <param name="brush">Brush to use for drawing</param>
		/// <param name="inverted">If gradient should be inverted</param>
		protected abstract void Shine(RenderTarget renderTarget, RectangleF bounds, LinearGradientBrush brush, bool inverted);


		public void Rectangle(GUIColor color, GUIBrightness brightness) {
			Rectangle(m_currentParameters.RenderTarget, m_currentParameters.Bounds, GUIColorPalette.Current, color, brightness);
		}

		public void Rectangle(RectangleF bounds, GUIColor color, GUIBrightness brightness) {
			Rectangle(m_currentParameters.RenderTarget, bounds, GUIColorPalette.Current, color, brightness);
		}

		public void Rectangle(GUIDrawParameters parameters, SolidColorBrush brush) {
			Rectangle(parameters.RenderTarget, parameters.Bounds, brush);
		}

		public void Rectangle(RenderTarget renderTarget, RectangleF bounds, GUIColorPalette palette, GUIColor color, GUIBrightness brightness, bool transparent = false) {
			Rectangle(renderTarget, bounds, palette[color, brightness, transparent]);
		}


		public void Border(GUIColor color, GUIBrightness brightness) {
			if (m_currentParameters.BorderWidth < 0.01F) return;
			Border(m_currentParameters.RenderTarget, m_currentParameters.Bounds, GUIColorPalette.Current, color, brightness);
		}

		public void Border(GUIDrawParameters parameters, SolidColorBrush brush) {
			if (parameters.BorderWidth < 0.01F) return;
			Border(parameters.RenderTarget, parameters.Bounds, brush);
		}

		public void Border(RectangleF bounds, GUIColor color, GUIBrightness brightness) {
			if (m_currentParameters.BorderWidth < 0.01F) return;
			Border(m_currentParameters.RenderTarget, bounds, GUIColorPalette.Current, color, brightness);
		}

		public void Border(RenderTarget renderTarget, RectangleF bounds, GUIColorPalette palette, GUIColor color, GUIBrightness brightness, bool transparent = false) {
			if (m_currentParameters.BorderWidth < 0.01F) return;
			Border(renderTarget, bounds, palette[color, brightness, transparent]);
		}


		public void Text(RenderTarget renderTarget, Vector2 offset, TextLayout textLayout, GUIColorPalette palette, GUIColor color, GUIBrightness brightness, bool transparent = false) {
			Text(renderTarget, offset, textLayout, palette[color, brightness, transparent]);
		}

		public void Text(GUIDrawParameters parameters, SolidColorBrush brush) {
			Text(parameters.RenderTarget, parameters.TextOffset, parameters.TextLayout, brush);
		}

		public void Text(Vector2? offset = null, GUIColor? color = null, GUIBrightness? brightness = null) {
			var textOffset = m_currentParameters.TextOffset;
			if (offset is Vector2 v2Offset) {
				textOffset += v2Offset;
			}

			Text(
				m_currentParameters.RenderTarget,
				textOffset,
				m_currentParameters.TextLayout,
				GUIColorPalette.Current, color ?? m_currentParameters.TextColor,
				brightness ?? m_currentParameters.TextBrightness
			);
		}


		public void BevelBorder(GUIBrightness? brightness = null, bool inverted = false) {
			if (m_currentParameters.BorderWidth < 0.01F) return;
			BevelBorder(m_currentParameters.RenderTarget, m_currentParameters.Bounds, GUIColorPalette.Current, m_currentParameters.BorderColor, brightness ?? m_currentParameters.Brightness, m_currentParameters.BorderWidth, inverted, false);
		}

		public void BevelBorder(GUIColor color, GUIBrightness brightness, bool inverted = false) {
			if (m_currentParameters.BorderWidth < 0.01F) return;
			BevelBorder(m_currentParameters.RenderTarget, m_currentParameters.Bounds, GUIColorPalette.Current, color, brightness, m_currentParameters.BorderWidth, inverted, false);
		}

		public void BevelBorder(RectangleF bounds, GUIColor color, GUIBrightness brightness, bool inverted = false) {
			if (m_currentParameters.BorderWidth < 0.01F) return;
			BevelBorder(m_currentParameters.RenderTarget, bounds, GUIColorPalette.Current, color, brightness, m_currentParameters.BorderWidth, inverted, false);
		}

		public void BevelBorder(GUIDrawParameters parameters, SolidColorBrush brush, bool inverted = false) {
			if (parameters.BorderWidth < 0.01F) return;
			BevelBorder(parameters.RenderTarget, parameters.Bounds, brush, parameters.BorderWidth, inverted);
		}

		public void BevelBorder(RenderTarget renderTarget, RectangleF bounds, GUIColorPalette palette, GUIColor color, GUIBrightness brightness, float borderWidth, bool inverted, bool transparent = false) {
			if (borderWidth < 0.01F) return;
			BevelBorder(renderTarget, bounds, palette[color, brightness, transparent], borderWidth, inverted);
		}


		public void InnerGlow(RenderTarget renderTarget, RectangleF bounds, GUIColor color, float size, float opacity) {
			if (size < 0.01F) return;
			if (opacity < 0.01F) return;

			var brush = GUIColorPalette.Current.GetGradientBrush(color);
			brush.Opacity = opacity;
			InnerGlow(renderTarget, bounds, brush, size);
		}

		public void InnerGlow(GUIColor color, float size, float opacity) {
			if (size < 0.01F) return;
			if (opacity < 0.01F) return;

			InnerGlow(m_currentParameters.RenderTarget, m_currentParameters.Bounds, color, size, opacity);
		}

		public void OuterGlow(RenderTarget renderTarget, RectangleF bounds, GUIColor color, float size, float opacity) {
			if (size < 0.01F) return;
			if (opacity < 0.01F) return;

			var brush = GUIColorPalette.Current.GetGradientBrush(color);
			brush.Opacity = opacity;
			OuterGlow(renderTarget, bounds, brush, size);
		}

		public void OuterGlow(GUIColor color, float size, float opacity) {
			if (size < 0.01F) return;
			if (opacity < 0.01F) return;

			OuterGlow(m_currentParameters.RenderTarget, m_currentParameters.Bounds, color, size, opacity);
		}


		public void OuterGlow(RectangleF bounds) {
			if (m_currentParameters.OuterGlow.Size < 0.01F) return;
			if (m_currentParameters.OuterGlow.Opacity < 0.01F) return;

			// Get brush from color palette
			var brush = GUIColorPalette.Current.GetGradientBrush(m_currentParameters.OuterGlow.Color);
			// Set opacity
			brush.Opacity = m_currentParameters.OuterGlow.Opacity;
			// Run abstract outer glow to render glow
			OuterGlow(m_currentParameters.RenderTarget,
				bounds,
				brush,
				m_currentParameters.OuterGlow.Size
			);
		}

		public void InnerGlow(RectangleF bounds) {
			if (m_currentParameters.InnerGlow.Size < 0.01F) return;
			if (m_currentParameters.InnerGlow.Opacity < 0.01F) return;

			// Get brush from color palette
			var brush = GUIColorPalette.Current.GetGradientBrush(m_currentParameters.InnerGlow.Color);
			// Set opacity
			brush.Opacity = m_currentParameters.InnerGlow.Opacity;
			// Run abstract outer glow to render glow
			InnerGlow(m_currentParameters.RenderTarget,
				bounds,
				brush,
				m_currentParameters.InnerGlow.Size
			);
		}


		public void TransparentText(Vector2? offset = null, GUIColor? color = null, GUIBrightness? brightness = null) {
			var textOffset = m_currentParameters.TextOffset;
			if (offset is Vector2 v2Offset) {
				textOffset += v2Offset;
			}

			Text(
				m_currentParameters.RenderTarget,
				textOffset,
				m_currentParameters.TextLayout,
				GUIColorPalette.Current, color ?? m_currentParameters.TextColor,
				brightness ?? m_currentParameters.TextBrightness,
				true
			);
		}

		public void TransparentRectangle(GUIColor color, GUIBrightness brightness) {
			Rectangle(m_currentParameters.RenderTarget, m_currentParameters.Bounds, GUIColorPalette.Current, color, brightness, true);
		}

		public void TransparentRectangle(RectangleF bounds, GUIColor color, GUIBrightness brightness) {
			Rectangle(m_currentParameters.RenderTarget, bounds, GUIColorPalette.Current, color, brightness, true);
		}

		public void TransparentBorder(GUIColor color, GUIBrightness brightness) {
			if (m_currentParameters.BorderWidth < 0.01F) return;
			Border(m_currentParameters.RenderTarget, m_currentParameters.Bounds, GUIColorPalette.Current, color, brightness, true);
		}

		public void TransparentBorder(RectangleF bounds, GUIColor color, GUIBrightness brightness) {
			if (m_currentParameters.BorderWidth < 0.01F) return;
			Border(m_currentParameters.RenderTarget, bounds, GUIColorPalette.Current, color, brightness, true);
		}

		public void Shine(bool inverted = false) {
			Shine(m_currentParameters.Bounds, inverted);
		}

		public void Shine(RectangleF bounds, bool inverted = false) {
			if (m_currentParameters.ShineOpacity < 0.01F) return;
			var brush = GUIColorPalette.Current.GetGradientBrush(GUIColor.Light);
			brush.Opacity = m_currentParameters.ShineOpacity;
			Shine(m_currentParameters.RenderTarget,
				bounds,
				brush,
				inverted
			);
		}

		#region Helper functions

		protected Geometry CreatePolygonGeometry(RenderTarget renderTarget, Vector2[] points, FigureBegin figureBegin = FigureBegin.Filled) {
			if (points.Length < 3) {
				throw new Exception("Need atleast 3 points to create a polygon");
			}

			var pathGeometry = new PathGeometry(renderTarget.Factory);
			using (var sink = pathGeometry.Open()) {
				sink.BeginFigure(points[0], figureBegin);
				for (int i = 1; i < points.Length; i++) {
					sink.AddLine(points[i]);
				}

				sink.EndFigure(FigureEnd.Closed);
				sink.Close();
				return pathGeometry;
			}
		}

		protected Vector2 GetClosestPointOnLineSegment(Vector2 A, Vector2 B, Vector2 P, bool constrain = false) {
			var AP = P - A; // Vector from A to P   
			var AB = B - A; // Vector from A to B  

			var magnitudeAB = AB.LengthSquared(); // Magnitude of AB vector (it's length squared)     
			var ABAPproduct = Vector2.Dot(AP, AB); // The DOT product of a_to_p and a_to_b     
			var distance = ABAPproduct / magnitudeAB; // The normalized "distance" from a to your closest point  

			if (constrain) {
				if (distance < 0) return A;
				if (distance > 1) return B;
				return A + AB * distance;
			}

			return A + AB * distance;
		}

		#endregion

		#region Classes

		public abstract class AreaDrawTools {
			protected abstract GUIColor GetColor(GUIDrawParameters parameters);

			public void Rectangle(GUIBrightness? brightness = null) {
				Current.Rectangle(GetColor(Current.m_currentParameters), brightness ?? Current.m_currentParameters.Brightness);
			}

			public void Rectangle(RectangleF bounds, GUIBrightness? brightness = null) {
				Current.Rectangle(bounds, GetColor(Current.m_currentParameters), brightness ?? Current.m_currentParameters.Brightness);
			}

			public void Border(GUIBrightness? brightness = null) {
				Current.Border(GetColor(Current.m_currentParameters), brightness ?? Current.m_currentParameters.Brightness);
			}

			public void Border(RectangleF bounds, GUIBrightness? brightness = null) {
				Current.Border(bounds, GetColor(Current.m_currentParameters), brightness ?? Current.m_currentParameters.Brightness);
			}

			public void TransparentRectangle(GUIBrightness? brightness = null) {
				Current.TransparentRectangle(GetColor(Current.m_currentParameters), brightness ?? Current.m_currentParameters.Brightness);
			}

			public void TransparentRectangle(RectangleF bounds, GUIBrightness? brightness = null) {
				Current.TransparentRectangle(bounds, GetColor(Current.m_currentParameters), brightness ?? Current.m_currentParameters.Brightness);
			}

			public void TransparentBorder(GUIBrightness? brightness = null) {
				Current.TransparentBorder(GetColor(Current.m_currentParameters), brightness ?? Current.m_currentParameters.Brightness);
			}

			public void TransparentBorder(RectangleF bounds, GUIBrightness? brightness = null) {
				Current.TransparentBorder(bounds, GetColor(Current.m_currentParameters), brightness ?? Current.m_currentParameters.Brightness);
			}

			public void BevelBorder(bool inverted = false, GUIBrightness? brightness = null) {
				Current.BevelBorder(GetColor(Current.m_currentParameters), brightness ?? Current.m_currentParameters.Brightness, inverted);
			}

			public void BevelBorder(RectangleF bounds, bool inverted = false, GUIBrightness? brightness = null) {
				Current.BevelBorder(bounds, GetColor(Current.m_currentParameters), brightness ?? Current.m_currentParameters.Brightness, inverted);
			}
		}

		public class ForegroundDrawTools : AreaDrawTools {
			protected override GUIColor GetColor(GUIDrawParameters parameters) => parameters.ForegroundColor;
		}

		public class BackgroundDrawTools : AreaDrawTools {
			protected override GUIColor GetColor(GUIDrawParameters parameters) => parameters.BackgroundColor;
		}

		#endregion
	}
}