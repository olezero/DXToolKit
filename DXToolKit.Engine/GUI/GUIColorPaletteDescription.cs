using System;
using System.Globalization;
using SharpDX;

namespace DXToolKit.Engine {
	/// <summary>
	/// Class describing the colors used by the GUI
	/// </summary>
	public sealed class GUIColorPaletteDescription {
		/// <summary>
		/// Default color usually applied to background
		/// </summary>
		public Color Default { get; private set; }

		/// <summary>
		/// Primary color usually applied to foreground
		/// </summary>
		public Color Primary { get; private set; }

		/// <summary>
		/// Success color usually applied to "confirm" actions
		/// </summary>
		public Color Success { get; private set; }

		/// <summary>
		/// Danger color usually applied to "danger" actions
		/// </summary>
		public Color Danger { get; private set; }

		/// <summary>
		/// Warning color usually applied to "warning" actions
		/// </summary>
		public Color Warning { get; private set; }

		/// <summary>
		/// Info color usually applied to "info" actions
		/// </summary>
		public Color Info { get; private set; }

		/// <summary>
		/// Text color used for all basic text rendering
		/// </summary>
		public Color Text { get; private set; }

		/// <summary>
		/// Dark color
		/// </summary>
		public Color Dark { get; private set; }

		/// <summary>
		/// Light color
		/// </summary>
		public Color Light { get; private set; }

		/// <summary>
		/// Gets or sets a value (from 0 to 1) indicating the opacity of transparent colors
		/// Default: 0.2F
		/// </summary>
		public float TransparentAlpha = 0.2F;

		/// <summary>
		/// Gets or sets a value (from 0 to 1) indicating how much darker the GUIBrightness.Darkest should darken the input color
		/// Default: 0.2F
		/// </summary>
		public float DarkestStrength = 0.2F;

		/// <summary>
		/// Gets or sets a value (from 0 to 1) indicating how much darker the GUIBrightness.Dark should darken the input color
		/// Default: 0.1F
		/// </summary>
		public float DarkStrength = 0.1F;

		/// <summary>
		/// Gets or sets a value (from 0 to 1) indicating how much brighter the GUIBrightness.Bright should brighten the input color
		/// Default: 0.1F
		/// </summary>
		public float BrightStrength = 0.1F;

		/// <summary>
		/// Gets or sets a value (from 0 to 1) indicating how much brighter the GUIBrightness.Brightest should brighten the input color
		/// Default: 0.2F
		/// </summary>
		public float BrightestStrength = 0.2F;

		/// <summary>
		/// Gets a preset "Cyborg" color description
		/// </summary>
		public static GUIColorPaletteDescription Cyborg => FromHex("#505050", "#2A9FD6", "#77B300", "#CC0000", "#FF8800", "#9933CC", "#F0F0F0");

		/// <summary>
		/// Creates a color description based on hex values of colors (format: #FFFFFF) 
		/// </summary>
		/// <param name="defaultColor"></param>
		/// <param name="primaryColor"></param>
		/// <param name="successColor"></param>
		/// <param name="dangerColor"></param>
		/// <param name="warningColor"></param>
		/// <param name="infoColor"></param>
		/// <param name="textColor"></param>
		/// <returns></returns>
		public static GUIColorPaletteDescription FromHex(string defaultColor, string primaryColor, string successColor, string dangerColor, string warningColor, string infoColor, string textColor) {
			return new GUIColorPaletteDescription {
				Default = FromHex(defaultColor),
				Primary = FromHex(primaryColor),
				Success = FromHex(successColor),
				Danger = FromHex(dangerColor),
				Warning = FromHex(warningColor),
				Info = FromHex(infoColor),
				Text = FromHex(textColor),
				Dark = new Color(20, 20, 20),
				Light = new Color(200, 200, 200),
			};
		}

		/// <summary>
		/// Converts a hex value to a color
		/// </summary>
		private static Color FromHex(string hex) {
			int intValue = int.Parse(hex.Trim('#') + "FF", NumberStyles.HexNumber);
			var clr = Color.FromAbgr(intValue);
			clr.A = byte.MaxValue;
			return clr;
		}

		/// <summary>
		/// Returns a color based on GUIColor enum
		/// </summary>
		public Color this[GUIColor index] {
			get {
				switch (index) {
					case GUIColor.Default:
						return Default;
					case GUIColor.Primary:
						return Primary;
					case GUIColor.Success:
						return Success;
					case GUIColor.Danger:
						return Danger;
					case GUIColor.Warning:
						return Warning;
					case GUIColor.Info:
						return Info;
					case GUIColor.Text:
						return Text;
					case GUIColor.Dark:
						return Dark;
					case GUIColor.Light:
						return Light;
					default:
						throw new ArgumentOutOfRangeException(nameof(index), index, null);
				}
			}
		}
	}
}