using SharpDX.DXGI;

namespace DXToolKit.Engine {
	/// <summary>
	/// Static used to handle engine config like screen size, refresh rate, fullscreen etc
	/// </summary>
	public static class EngineConfig {
		// TODO - allow setting of values here
		// TODO - need to load from file most likely

		private static int m_screenWidth = 1920;
		private static int m_screenHeight = 1080;
		private static Rational m_refreshRate = new Rational(165000, 1000);
		private static bool m_fullscreen = false;

		/// <summary>
		/// Gets the current screen width
		/// </summary>
		public static int ScreenWidth => m_screenWidth;

		/// <summary>
		/// Gets the current screen height
		/// </summary>
		public static int ScreenHeight => m_screenHeight;

		/// <summary>
		/// Gets a value indicating if the screen is currently in fullscreen mode
		/// </summary>
		public static bool Fullscreen => m_fullscreen;

		/// <summary>
		/// Gets or sets a value if the engine should use vsync
		/// </summary>
		public static bool UseVsync;

		/// <summary>
		/// Gets the engine config as a mode description
		/// </summary>
		/// <returns></returns>
		public static ModeDescription GetModedescription() {
			return new ModeDescription(m_screenWidth, m_screenHeight, m_refreshRate, Format.R8G8B8A8_UNorm);
		}

		internal static void SetConfig(ModeDescription modeDescription, Rational refresh, bool fullscreen, bool vsync) {
			m_screenWidth = modeDescription.Width;
			m_screenHeight = modeDescription.Height;
			m_refreshRate = refresh;
			m_fullscreen = fullscreen;
			UseVsync = vsync;
		}

		internal static void SetConfig(ModeDescription modeDescription) {
			m_screenWidth = modeDescription.Width;
			m_screenHeight = modeDescription.Height;
			m_refreshRate = modeDescription.RefreshRate;
		}
	}
}