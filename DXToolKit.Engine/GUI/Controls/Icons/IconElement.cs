using SharpDX;
using SharpDX.Direct2D1;

namespace DXToolKit.Engine {
	/// <summary>
	/// Element that represents a icon
	/// </summary>
	public abstract class IconElement : GraphicElement {
		/// <summary>
		/// GUIColor used for the icon.
		/// Defaults to Text
		/// </summary>
		private GUIColor m_iconColor = GUIColor.Text;

		/// <summary>
		/// Icon brightness when finding correct brush to render icon.
		/// Default to Normal
		/// </summary>
		private GUIBrightness m_iconBrightness = GUIBrightness.Normal;

		/// <summary>
		/// Bitmap opacity
		/// </summary>
		private float m_iconOpacity = 1.0F;

		/// <summary>
		/// Controller for drawing background
		/// </summary>
		private bool m_drawbackground;

		/// <summary>
		/// Gets or sets the icon color
		/// Default: GUIColor.Text
		/// </summary>
		public GUIColor IconColor {
			get => m_iconColor;
			set {
				if (m_iconColor != value) ToggleGraphicsRedraw();
				m_iconColor = value;
			}
		}

		/// <summary>
		/// Gets or sets the brightness of the icon
		/// Default: GUIBrightness.Normal
		/// </summary>
		public GUIBrightness IconBrightness {
			get => m_iconBrightness;
			set {
				if (m_iconBrightness != value) ToggleGraphicsRedraw();
				m_iconBrightness = value;
			}
		}

		/// <summary>
		/// Gets or sets the opacity of the icon
		/// Default: 1.0F
		/// </summary>
		public float IconOpacity {
			get => m_iconOpacity;
			set {
				if (MathUtil.NearEqual(value, m_iconOpacity) == false) {
					ToggleRedraw();
				}

				m_iconOpacity = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating if background should be rendered
		/// Default: false
		/// </summary>
		public bool Drawbackground {
			get => m_drawbackground;
			set {
				if (m_drawbackground != value) ToggleRedraw();
				m_drawbackground = value;
			}
		}

		/// <summary>
		/// Creates a new Icon element, has disabled mouse input and keyboard input and cannot receive focus by default
		/// </summary>
		protected IconElement(bool disableInput = true) {
			Width = 24.0F;
			Height = 24.0F;
			if (disableInput) {
				CanReceiveMouseInput = false;
				CanReceiveFocus = false;
				CanReceiveKeyboardInput = false;
			}
		}

		/// <inheritdoc />
		protected override void OnRender(GUIDrawTools tools, ref GUIDrawParameters parameters, Bitmap graphics) {
			if (m_drawbackground) {
				tools.Background.Rectangle();
				tools.Background.BevelBorder();
			}

			parameters.RenderTarget.DrawBitmap(graphics, parameters.Bounds, m_iconOpacity, BitmapInterpolationMode.Linear);
			if (m_drawbackground) {
				tools.Shine(parameters.Bounds, true);
			}
		}

		/// <inheritdoc />
		protected sealed override void CreateGraphics(RenderTarget renderTarget, RectangleF bounds, GUIColorPalette palette, GUIDrawTools drawTools, float recommendedStrokeWidth) {
			//bounds.Inflate(-Mathf.Floor(recommendedStrokeWidth), -Mathf.Floor(recommendedStrokeWidth));
			bounds.Inflate(-recommendedStrokeWidth, -recommendedStrokeWidth);
			CreateIcon(renderTarget, bounds, palette, drawTools, recommendedStrokeWidth, palette[m_iconColor, m_iconBrightness]);
		}

		/// <summary>
		/// Render the icon, use the whole bounds since padding is already included
		/// </summary>
		/// <param name="renderTarget">The rendertarget to use for drawing operations</param>
		/// <param name="bounds">Bounds of the graphics</param>
		/// <param name="palette">Color palette for retrieving brushes</param>
		/// <param name="drawTools">Drawing tools to help with drawing</param>
		/// <param name="recommendedStrokeWidth">A recommended stroke width to use while creating the graphics. Tries to fit 8 "strokes" within the smaller of width/height</param>
		/// <param name="iconBrush">Correct brush to use when drawing the icon</param>
		protected abstract void CreateIcon(RenderTarget renderTarget, RectangleF bounds, GUIColorPalette palette, GUIDrawTools drawTools, float recommendedStrokeWidth, SolidColorBrush iconBrush);
	}
}