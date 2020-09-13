// Dont need comments on variables here, since they are pretty self explanatory 

#pragma warning disable 1591

namespace DXToolKit.Engine {
	/// <summary>
	/// Configuration for what properties should be passed on / inherited from parent
	/// </summary>
	public class StyleInheritance {
		public bool ForegroundColor = true;
		public bool BackgroundColor = true;
		public bool TextColor = true;
		public bool Brightness = true;
		public bool TextBrightness = true;

		public bool TextOffset = false;
		public bool BorderWidth = false;
		public bool ShineOpacity = false;
		public bool OuterGlow = false;
		public bool InnerGlow = false;
		public bool BorderColor = false;

		public bool ParagraphAlignment = true;
		public bool TextAlignment = true;
		public bool WordWrapping = true;
		public bool Font = true;
		public bool FontSize = true;
		public bool FontStyle = true;
		public bool FontWeight = true;
		public bool FontStretch = true;

		public StyleInheritance Copy() {
			return new StyleInheritance {
				ForegroundColor = ForegroundColor,
				BackgroundColor = BackgroundColor,
				TextColor = TextColor,
				Brightness = Brightness,
				TextBrightness = TextBrightness,
				TextOffset = TextOffset,
				BorderWidth = BorderWidth,
				ShineOpacity = ShineOpacity,
				OuterGlow = OuterGlow,
				InnerGlow = InnerGlow,
				BorderColor = BorderColor,
				ParagraphAlignment = ParagraphAlignment,
				TextAlignment = TextAlignment,
				WordWrapping = WordWrapping,
				Font = Font,
				FontSize = FontSize,
				FontStyle = FontStyle,
				FontWeight = FontWeight,
				FontStretch = FontStretch,
			};
		}
	}
}