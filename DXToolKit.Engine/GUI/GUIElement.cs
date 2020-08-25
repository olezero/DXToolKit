using System;
using System.Collections.Generic;
using DXToolKit.GUI;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace DXToolKit.Engine {
	public class GUIDrawParameters {
		public GUIColor ForegroundColor = GUIColor.Primary;
		public GUIColor BackgroundColor = GUIColor.Default;
		public GUIColor TextColor = GUIColor.Text;
		public GUIBrightness Brightness = GUIBrightness.Normal;
		public GUIBrightness TextBrightness = GUIBrightness.Normal;
		public Vector2 TextOffset = Vector2.Zero;
		public float BorderSize = 1.0F;
		public TextLayout TextLayout;
		public RenderTarget RenderTarget;
		public RectangleF Bounds;

		private GUIColor m_borderColor;
		private bool m_isBorderColorSet = false;

		public GUIColor BorderColor {
			get => m_isBorderColorSet ? m_borderColor : ForegroundColor;
			set {
				m_borderColor = value;
				m_isBorderColorSet = true;
			}
		}
	}

	/// <summary>
	/// Overriding and hiding base GUIElement with a bit more handy dandy stuff
	/// </summary>
	public abstract class GUIElement : DXToolKit.GUI.GUIElement, IGUIGriddable {
		private GUIDrawParameters m_drawParameters = new GUIDrawParameters();

		public GUIColor ForegroundColor {
			get => m_drawParameters.ForegroundColor;
			set {
				if (m_drawParameters.ForegroundColor != value) ToggleRedraw();
				m_drawParameters.ForegroundColor = value;
			}
		}

		public GUIColor BackgroundColor {
			get => m_drawParameters.BackgroundColor;
			set {
				if (m_drawParameters.BackgroundColor != value) ToggleRedraw();
				m_drawParameters.BackgroundColor = value;
			}
		}

		public GUIColor BorderColor {
			get => m_drawParameters.BorderColor;
			set {
				if (m_drawParameters.BorderColor != value) ToggleRedraw();
				m_drawParameters.BorderColor = value;
			}
		}

		public GUIColor TextColor {
			get => m_drawParameters.TextColor;
			set {
				if (m_drawParameters.TextColor != value) ToggleRedraw();
				m_drawParameters.TextColor = value;
			}
		}

		public GUIBrightness Brightness {
			get => m_drawParameters.Brightness;
			set {
				if (m_drawParameters.Brightness != value) ToggleRedraw();
				m_drawParameters.Brightness = value;
			}
		}

		public GUIBrightness TextBrightness {
			get => m_drawParameters.TextBrightness;
			set {
				if (m_drawParameters.TextBrightness != value) ToggleRedraw();
				m_drawParameters.TextBrightness = value;
			}
		}

		public float BorderSize {
			get => m_drawParameters.BorderSize;
			set {
				if (MathUtil.NearEqual(m_drawParameters.BorderSize, value) == false) {
					m_drawParameters.BorderSize = value;
					ToggleRedraw();
				}
			}
		}

		public Vector2 TextOffset {
			get => m_drawParameters.TextOffset;
			set {
				if (m_drawParameters.TextOffset != value) {
					m_drawParameters.TextOffset = value;
					ToggleRedraw();
				}
			}
		}


		public Vector2 LocalMousePosition => ScreenToLocal(Input.MousePosition);

		private GUIPadding m_padding = new GUIPadding(0);

		public virtual GUIPadding Padding {
			get => m_padding;
			set => m_padding = value;
		}
		/*

		public bool MouseIgnoresPadding = true;
		// TODO - add a screen bounds offset type thing on base GUIElement to allow for padding to set child element.X and Y to 0
		// TODO - at the moment padding does not position child elements correctly at 0, 0 etc.

		protected sealed override RectangleF MouseScreenBounds {
			get {
				if (MouseIgnoresPadding == false) return ScreenBounds;
				if (m_padding.Top > 0 || m_padding.Bottom > 0 || m_padding.Left > 0 || m_padding.Right > 0) {
					return m_padding.ResizeRectangle(ScreenBounds);
				}

				return ScreenBounds;
			}
		}

		protected sealed override bool UseClippingBounds => true;

		protected sealed override RectangleF ClippedRenderBounds {
			get {
				var clipbounds = m_padding.ResizeRectangle(Bounds);
				return clipbounds;
			}
		}
		*/


		protected sealed override void OnRender(RenderTarget renderTarget, RectangleF bounds, TextLayout textLayout) {
			// Might not be needed, since clipping bounds will clip away padding
			// Padding.ResizeRectangle(ref bounds);
			// OnRender(renderTarget, bounds, textLayout, GUIColorPalette.Current, GUIDrawTools.Current);
			m_drawParameters.TextLayout = textLayout;
			m_drawParameters.RenderTarget = renderTarget;
			m_drawParameters.Bounds = bounds;
			GUIDrawTools.Current.SetParams(ref m_drawParameters);
			OnRender(GUIDrawTools.Current, ref m_drawParameters);
			OnRender(m_drawParameters.RenderTarget, m_drawParameters.Bounds, m_drawParameters.TextLayout, GUIColorPalette.Current, GUIDrawTools.Current);
		}

		protected virtual void OnRender(RenderTarget renderTarget, RectangleF bounds, TextLayout textLayout, GUIColorPalette palette, GUIDrawTools drawTools) { }
		protected virtual void OnRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) { }
	}
}