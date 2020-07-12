using SharpDX.DXGI;

namespace DXToolKit.Engine {
	public static class EngineConfig {
		// TODO - allow setting of values here
		// TODO - need to load from file most likely

		private static int m_screenWidth = 1920;
		private static int m_screenHeight = 1080;
		private static Rational m_refreshRate = new Rational(165000, 1000);
		private static bool m_fullscreen = false;

		public static int ScreenWidth => m_screenWidth;
		public static int ScreenHeight => m_screenHeight;
		public static bool Fullscreen => m_fullscreen;
		public static bool UseVsync;

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