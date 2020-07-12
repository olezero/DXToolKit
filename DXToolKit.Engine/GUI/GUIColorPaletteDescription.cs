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

		public static GUIColorPaletteDescription Cyborg => FromHex("#505050", "#2A9FD6", "#77B300", "#CC0000", "#FF8800", "#9933CC", "#F0F0F0");

		public static GUIColorPaletteDescription FromHex(string defaultColor, string primaryColor, string successColor, string dangerColor, string warningColor, string infoColor, string textColor) {
			return new GUIColorPaletteDescription() {
				Default = FromHex(defaultColor),
				Primary = FromHex(primaryColor),
				Success = FromHex(successColor),
				Danger = FromHex(dangerColor),
				Warning = FromHex(warningColor),
				Info = FromHex(infoColor),
				Text = FromHex(textColor),
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
					default:
						throw new ArgumentOutOfRangeException(nameof(index), index, null);
				}
			}
		}
	}
}