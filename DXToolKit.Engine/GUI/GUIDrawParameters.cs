using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace DXToolKit.Engine {
	/// <summary>
	/// GUI Draw parameters used when rendering a given GUI element
	/// This is unique per element
	/// </summary>
	public class GUIDrawParameters {
		/// <summary>
		/// Defines what styles should be inherited from parent element
		/// </summary>
		public StyleInheritance StyleInheritance = new StyleInheritance();

		/// <summary>
		/// Gets or sets the default foreground color.
		/// </summary>
		public static GUIColor DEFAULT_FOREGROUND_COLOR = GUIColor.Primary;

		/// <summary>
		/// Gets or sets the default background color.
		/// </summary>
		public static GUIColor DEFAULT_BACKGROUND_COLOR = GUIColor.Default;

		/// <summary>
		/// Gets or sets the default text color.
		/// </summary>
		public static GUIColor DEFAULT_TEXT_COLOR = GUIColor.Text;

		/// <summary>
		/// Gets or sets the default brightness.
		/// </summary>
		public static GUIBrightness DEFAULT_BRIGHTNESS = GUIBrightness.Normal;

		/// <summary>
		/// Gets or sets the default text brightness
		/// </summary>
		public static GUIBrightness DEFAULT_TEXT_BRIGHTNESS = GUIBrightness.Normal;

		/// <summary>
		/// Gets or sets the default text offset
		/// </summary>
		public static Vector2 DEFAULT_TEXT_OFFSET = new Vector2(0, 0);

		/// <summary>
		/// Gets or sets the default opacity of shine (setting to lower then 0.01F disables shine)
		/// </summary>
		public static float DEFAULT_SHINE_OPACITY = 0.1F;

		/// <summary>
		/// Gets or sets the default border width (setting to lower then 0.01F disables borders)
		/// </summary>
		public static float DEFAULT_BORDER_WIDTH = 1.0F;

		/// <summary>
		/// Gets or sets the default outer glow properties
		/// </summary>
		public static GlowProperties DEFAULT_OUTER_GLOW = new GlowProperties {
			Color = GUIColor.Light,
			Opacity = 0.4F,
			Size = 4.0F,
		};

		/// <summary>
		/// Gets or sets the default inner glow properties
		/// </summary>
		public static GlowProperties DEFAULT_INNER_GLOW = new GlowProperties {
			Color = GUIColor.Dark,
			Opacity = 0.75F,
			Size = 4.0F,
		};

		/// <summary>
		/// Gets or sets the foreground color of this element
		/// </summary>
		public GUIColor ForegroundColor = DEFAULT_FOREGROUND_COLOR;

		/// <summary>
		/// Gets or sets the background color of this element
		/// </summary>
		public GUIColor BackgroundColor = DEFAULT_BACKGROUND_COLOR;

		/// <summary>
		/// Gets or sets the text color of this element
		/// </summary>
		public GUIColor TextColor = DEFAULT_TEXT_COLOR;

		/// <summary>
		/// Gets or sets the brightness of this element
		/// </summary>
		public GUIBrightness Brightness = DEFAULT_BRIGHTNESS;

		/// <summary>
		/// Gets or sets the text brightness of this element
		/// </summary>
		public GUIBrightness TextBrightness = DEFAULT_TEXT_BRIGHTNESS;

		/// <summary>
		/// Gets or sets the text offset of this element
		/// </summary>
		public Vector2 TextOffset = DEFAULT_TEXT_OFFSET;

		/// <summary>
		/// Gets or sets the border width/size of this element (Both for normal borders and beveled borders)
		/// Disable if less then 0.01F
		/// </summary>
		public float BorderWidth = DEFAULT_BORDER_WIDTH;

		/// <summary>
		/// Gets or sets the opacity of the shine of this element.
		/// Disable if less then 0.01F
		/// </summary>
		public float ShineOpacity = DEFAULT_SHINE_OPACITY;

		/// <summary>
		/// Gets or sets the outer glow properties of this element
		/// </summary>
		public GlowProperties OuterGlow;

		/// <summary>
		/// Gets or sets the inner glow properties of this element
		/// </summary>
		public GlowProperties InnerGlow;

		/// <summary>
		/// Gets the text layout used by this element.
		/// </summary>
		public TextLayout TextLayout { get; internal set; }

		/// <summary>
		/// Gets the rendertarget used by this element
		/// </summary>
		public RenderTarget RenderTarget { get; internal set; }

		/// <summary>
		/// Gets the bounds this element should render to
		/// </summary>
		public RectangleF Bounds { get; internal set; }

		/// <summary>
		/// Controller if border color should be the same as foreground or a separate value
		/// </summary>
		private GUIColor m_borderColor;

		/// <summary>
		/// Controller to check if border color has been set through the property
		/// </summary>
		private bool m_isBorderColorSet = false;

		/// <summary>
		/// Creates a new instance of the draw parameters
		/// </summary>
		public GUIDrawParameters() {
			// Create copies of the default values
			OuterGlow = DEFAULT_OUTER_GLOW.Copy();
			InnerGlow = DEFAULT_INNER_GLOW.Copy();
		}

		/// <summary>
		/// Gets or sets the border color.
		/// Defaults to ForegroundColor
		/// </summary>
		public GUIColor BorderColor {
			get => m_isBorderColorSet ? m_borderColor : ForegroundColor;
			set {
				m_borderColor = value;
				m_isBorderColorSet = true;
			}
		}

		/// <summary>
		/// Copies the variables in this to the target based on stored propagate information
		/// </summary>
		/// <param name="target"></param>
		public void Copy(ref GUIDrawParameters target) {
			var targetProp = target.StyleInheritance;
			if (targetProp.ForegroundColor) target.ForegroundColor = ForegroundColor;
			if (targetProp.BackgroundColor) target.BackgroundColor = BackgroundColor;
			if (targetProp.TextColor) target.TextColor = TextColor;
			if (targetProp.Brightness) target.Brightness = Brightness;
			if (targetProp.TextBrightness) target.TextBrightness = TextBrightness;
			if (targetProp.TextOffset) target.TextOffset = TextOffset;
			if (targetProp.BorderWidth) target.BorderWidth = BorderWidth;
			if (targetProp.ShineOpacity) target.ShineOpacity = ShineOpacity;
			if (targetProp.OuterGlow) target.OuterGlow = OuterGlow.Copy();
			if (targetProp.InnerGlow) target.InnerGlow = InnerGlow.Copy();
			if (targetProp.BorderColor) target.BorderColor = BorderColor;
		}


		/// <summary>
		/// Copies the draw parameters. Does not account for style inheritance
		/// </summary>
		/// <param name="other">The parameters to copy from</param>
		/// <returns>A new instance of draw parameters with copied values</returns>
		public static GUIDrawParameters DeepCopy(GUIDrawParameters other) {
			var result = new GUIDrawParameters {
				ForegroundColor = other.ForegroundColor,
				BackgroundColor = other.BackgroundColor,
				TextColor = other.TextColor,
				Brightness = other.Brightness,
				TextBrightness = other.TextBrightness,
				TextOffset = other.TextOffset,
				BorderWidth = other.BorderWidth,
				ShineOpacity = other.ShineOpacity,
				OuterGlow = other.OuterGlow.Copy(),
				InnerGlow = other.InnerGlow.Copy(),
				StyleInheritance = other.StyleInheritance.Copy(),
			};
			if (other.m_isBorderColorSet) {
				result.m_isBorderColorSet = true;
				result.m_borderColor = other.m_borderColor;
			}

			return result;
		}
	}
}