namespace DXToolKit.Engine {
	/// <summary>
	/// Standard set of colors used by the GUI.
	/// Based on config in GUIColorPaletteDescription
	/// </summary>
	public enum GUIColor {
		/// <summary>
		/// Default color usually applied to background
		/// </summary>
		Default,

		/// <summary>
		/// Primary color usually applied to foreground
		/// </summary>
		Primary,

		/// <summary>
		/// Success color usually applied to "confirm" actions
		/// </summary>
		Success,

		/// <summary>
		/// Danger color usually applied to "danger" actions
		/// </summary>
		Danger,

		/// <summary>
		/// Warning color usually applied to "warning" actions
		/// </summary>
		Warning,

		/// <summary>
		/// Info color usually applied to "info" actions
		/// </summary>
		Info,

		/// <summary>
		/// Text color used for all basic text rendering
		/// </summary>
		Text,

		/// <summary>
		/// Dark color defined by GUIColorPaletteDescription
		/// </summary>
		Dark,

		/// <summary>
		/// Light color defined by GUIColorPaletteDescription
		/// </summary>
		Light,
	}
}