using System;
using System.Globalization;
using SharpDX;

namespace DXToolKit.Engine {
	public sealed class GUIColorPaletteDescription {
		public Color Default;
		public Color Primary;
		public Color Success;
		public Color Danger;
		public Color Warning;
		public Color Info;
		public Color Text;
		public Color Dark;
		public Color Light;
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

		public static GUIColorPaletteDescription Cyborg => FromHex("#505050", "#2A9FD6", "#77B300", "#CC0000", "#FF8800", "#9933CC", "#F0F0F0");

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

		private static Color FromHex(string hex) {
			int intValue = int.Parse(hex.Trim('#') + "FF", NumberStyles.HexNumber);
			var clr = Color.FromAbgr(intValue);
			clr.A = byte.MaxValue;
			return clr;
		}

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