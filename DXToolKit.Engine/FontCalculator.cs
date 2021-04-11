using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.DirectWrite;

namespace DXToolKit.Engine {
	/// <summary>
	/// Font calculator used for "relatively" fast font metric calculations.
	/// This class is based upon the static Graphics and DXApp classes, and will not work without them.
	/// </summary>
	public static class FontCalculator {
		private static TextFormat m_cachedFormat;
		private static TextLayout m_cachedLayout;
		private static bool m_runningCachedCalculations = false;
		private static Dictionary<string, TextFormat> m_formatCache = new Dictionary<string, TextFormat>();
		private static string m_cachedText = null;

		/// <summary>
		/// Starts a cached font calculation runs.
		/// This is useful if more then one calculation should take place on the given input data
		/// </summary>
		/// <param name="text">The text to calculate around (This can be changed later with about half the time consequence of a direct calculation)</param>
		/// <param name="font">The font to use</param>
		/// <param name="fontSize">Font size to use</param>
		/// <param name="fontWeight">Font weight to use</param>
		/// <param name="fontStyle">Font style to use</param>
		/// <param name="fontStretch">Font stretch to use</param>
		/// <exception cref="Exception">Will throw exception if EndCachedCalculations() has not been executed before another call to this method</exception>
		public static void StartCachedCalculations(string text, string font, int fontSize, FontWeight fontWeight = FontWeight.Normal, FontStyle fontStyle = FontStyle.Normal, FontStretch fontStretch = FontStretch.Normal) {
			if (m_runningCachedCalculations) throw new Exception("Cannot start new cached calculations before EndCachedCalculations() is called");
			m_cachedFormat = GetTextFormat(font, fontSize, fontWeight, fontStyle, fontStretch);
			m_cachedLayout = new TextLayout(Graphics.Factory, text, m_cachedFormat, float.MaxValue, float.MaxValue);
			m_cachedText = text;
			m_runningCachedCalculations = true;
		}

		/// <summary>
		/// Cache calculation.
		/// Retrieves height of the cached font data
		/// </summary>
		public static float CachedCalculateHeight() {
			CheckCachedCalculations();
			return m_cachedLayout.Metrics.Height;
		}

		/// <summary>
		/// Cache calculation.
		/// Retrieves width of the cached font data
		/// </summary>
		public static float CachedCalculateWidth() {
			CheckCachedCalculations();
			return m_cachedLayout.Metrics.Width;
		}

		/// <summary>
		/// Cache calculation.
		/// Retrieves metrics of the cached font data
		/// </summary>
		public static TextMetrics CachedGetMetrics() {
			CheckCachedCalculations();
			return m_cachedLayout.Metrics;
		}

		/// <summary>
		/// Cache calculation with the option to change the text.
		/// Retrieves height of the cached font data
		/// </summary>
		public static float CachedCalculateHeight(string text) {
			CreateLayout(text);
			return m_cachedLayout.Metrics.Height;
		}

		/// <summary>
		/// Cache calculation with the option to change the text.
		/// Retrieves width of the cached font data
		/// </summary>
		public static float CachedCalculateWidth(string text) {
			CreateLayout(text);
			return m_cachedLayout.Metrics.Width;
		}

		/// <summary>
		/// Cache calculation with the option to change the text.
		/// Retrieves height of the cached font data
		/// </summary>
		public static TextMetrics CachedGetMetrics(string text) {
			CreateLayout(text);
			return m_cachedLayout.Metrics;
		}

		private static void CreateLayout(string text) {
			CheckCachedCalculations();
			// If text is the same, return
			if (m_cachedText == text) return;
			m_cachedText = text;
			Utilities.Dispose(ref m_cachedLayout);
			m_cachedLayout = new TextLayout(Graphics.Factory, text, m_cachedFormat, float.MaxValue, float.MaxValue);
		}

		private static void CheckCachedCalculations() {
			if (!m_runningCachedCalculations) throw new Exception("Cannot run cached calculations before StartCachedCalculations is called");
		}

		/// <summary>
		/// Ends a cached calculation
		/// </summary>
		public static void EndCachedCalculations() {
			Utilities.Dispose(ref m_cachedLayout);
			m_runningCachedCalculations = false;
			m_cachedText = null;
		}

		/// <summary>
		/// Calculates text height based on input font information
		/// </summary>
		public static float CalculateTextHeight(string text, string font, int fontSize, FontWeight fontWeight = FontWeight.Normal, FontStyle fontStyle = FontStyle.Normal, FontStretch fontStretch = FontStretch.Normal) {
			var textFormat = GetTextFormat(font, fontSize, fontWeight, fontStyle, fontStretch);
			var textLayout = new TextLayout(Graphics.Factory, text, textFormat, float.MaxValue, float.MaxValue);
			var height = textLayout.Metrics.Height;
			Utilities.Dispose(ref textLayout);
			return height;
		}

		/// <summary>
		/// Calculates text width based on input font information
		/// </summary>
		public static float CalculateTextWidth(string text, string font, int fontSize, FontWeight fontWeight = FontWeight.Normal, FontStyle fontStyle = FontStyle.Normal, FontStretch fontStretch = FontStretch.Normal) {
			var textFormat = GetTextFormat(font, fontSize, fontWeight, fontStyle, fontStretch);
			var textLayout = new TextLayout(Graphics.Factory, text, textFormat, float.MaxValue, float.MaxValue);
			var width = textLayout.Metrics.Width;
			Utilities.Dispose(ref textLayout);
			return width;
		}

		/// <summary>
		/// Gets text metrics based on input font information
		/// </summary>
		public static TextMetrics GetMetrics(string text, string font, int fontSize, FontWeight fontWeight = FontWeight.Normal, FontStyle fontStyle = FontStyle.Normal, FontStretch fontStretch = FontStretch.Normal) {
			var textFormat = GetTextFormat(font, fontSize, fontWeight, fontStyle, fontStretch);
			var textLayout = new TextLayout(Graphics.Factory, text, textFormat, float.MaxValue, float.MaxValue);
			var metrics = textLayout.Metrics;
			Utilities.Dispose(ref textLayout);
			return metrics;
		}

		/// <summary>
		/// Gets text height based on a GUIElements stored text information.
		/// This can be handy since GUIElements dont have text layouts created before the first render pass.
		/// </summary>
		public static float CalculateTextHeight(GUIElement element) {
			return CalculateTextHeight(element.Text, element.Font, element.FontSize, element.FontWeight, element.FontStyle, element.FontStretch);
		}

		/// <summary>
		/// Gets text width based on a GUIElements stored text information.
		/// This can be handy since GUIElements dont have text layouts created before the first render pass.
		/// </summary>
		public static float CalculateTextWidth(GUIElement element) {
			return CalculateTextWidth(element.Text, element.Font, element.FontSize, element.FontWeight, element.FontStyle, element.FontStretch);
		}

		/// <summary>
		/// Gets text metrics based on input font information
		/// </summary>
		public static TextMetrics GetMetrics(GUIElement element) {
			return GetMetrics(element.Text, element.Font, element.FontSize, element.FontWeight, element.FontStyle, element.FontStretch);
		}


		private static TextFormat GetTextFormat(string font, int fontSize, FontWeight fontWeight = FontWeight.Normal, FontStyle fontStyle = FontStyle.Normal, FontStretch fontStretch = FontStretch.Normal) {
			var cacheKey = font + fontWeight + fontStyle + fontStretch + fontSize;
			if (m_formatCache.ContainsKey(cacheKey) == false) {
				var format = new TextFormat(Graphics.Factory, font, fontWeight, fontStyle, fontStretch, fontSize);
				m_formatCache.Add(cacheKey, format);
				DXApp.Current.AddDisposable(format);
			}

			return m_formatCache[cacheKey];
		}
	}
}