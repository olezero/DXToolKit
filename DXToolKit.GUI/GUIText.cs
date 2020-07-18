using System;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DirectWrite;


namespace DXToolKit.GUI {
	public abstract partial class GUIElement {
		private sealed class GUIText : IDisposable {
			public TextLayout m_textLayout;
			public TextFormat m_textFormat;

			private string m_font;
			private int m_fontSize;

			private string m_cachedText;
			private float m_cachedMaxWidth;
			private float m_cachedMaxHeight;
			private bool m_hasFontOrSizeChanged;

			private ParagraphAlignment m_cachedParagraphAlignment;
			private TextAlignment m_cachedTextAlignment;
			private WordWrapping m_cachedWordWrapping;


			public ParagraphAlignment ParagraphAlignment {
				get => m_cachedParagraphAlignment;
				set {
					if (m_cachedParagraphAlignment != value) {
						// Only set them directly if they have been created.
						if (m_textFormat != null) m_textFormat.ParagraphAlignment = value;
						if (m_textLayout != null) m_textLayout.ParagraphAlignment = value;
						m_cachedParagraphAlignment = value;
					}
				}
			}

			public TextAlignment TextAlignment {
				get => m_cachedTextAlignment;
				set {
					if (m_cachedTextAlignment != value) {
						// Only set them directly if they have been created.
						if (m_textFormat != null) m_textFormat.TextAlignment = value;
						if (m_textLayout != null) m_textLayout.TextAlignment = value;
						m_cachedTextAlignment = value;
					}
				}
			}

			public WordWrapping WordWrapping {
				get => m_cachedWordWrapping;
				set {
					if (m_cachedWordWrapping != value) {
						// Only set them directly if they have been created.
						if (m_textFormat != null) m_textFormat.WordWrapping = value;
						if (m_textLayout != null) m_textLayout.WordWrapping = value;
						m_cachedWordWrapping = value;
					}
				}
			}

			public string Font {
				get => m_font;
				set {
					if (m_font != value) {
						m_font = value;
						m_hasFontOrSizeChanged = true;
					}
				}
			}

			public int FontSize {
				get => m_fontSize;
				set {
					if (m_fontSize != value) {
						m_fontSize = value;
						m_hasFontOrSizeChanged = true;
					}
				}
			}

			public GUIText(string font, int fontSize) {
				m_font = font;
				m_fontSize = fontSize;
				m_cachedParagraphAlignment = ParagraphAlignment.Near;
				m_cachedTextAlignment = TextAlignment.Leading;
				m_cachedWordWrapping = WordWrapping.NoWrap;
			}

			private void LoadText(FactoryCollection factory, ref string text, ref float maxWidth, ref float maxHeigth) {
				DisposeText();
				m_textFormat = new TextFormat(factory, m_font, m_fontSize) {
					ParagraphAlignment = m_cachedParagraphAlignment,
					TextAlignment = m_cachedTextAlignment,
					WordWrapping = m_cachedWordWrapping,
					IncrementalTabStop = m_fontSize,
				};

				m_textLayout = new TextLayout(factory, text, m_textFormat, maxWidth, maxHeigth) {
					ParagraphAlignment = m_cachedParagraphAlignment,
					TextAlignment = m_cachedTextAlignment,
					WordWrapping = m_cachedWordWrapping,
					IncrementalTabStop = m_fontSize,
				};
			}

			public TextLayout GetTextLayout(FactoryCollection factory, string text, float maxWidth, float maxHeight) {
				// Check if anything has changed, if so, load text again
				if (HasChanged(ref text, ref maxWidth, ref maxHeight) || m_textLayout == null) {
					LoadText(factory, ref text, ref maxWidth, ref maxHeight);
				}

				// Check if anything has changed, and if text is different, if so call LoadText
				return m_textLayout;
			}

			public TextFormat GetTextFormat(FactoryCollection factory, string text, float maxWidth, float maxHeight) {
				// Check if anything has changed, if so, load text again
				if (HasChanged(ref text, ref maxWidth, ref maxHeight) || m_textFormat == null) {
					LoadText(factory, ref text, ref maxWidth, ref maxHeight);
				}

				// Check if anything has changed, and if text is different, if so call LoadText
				return m_textFormat;
			}

			private bool HasChanged(ref string text, ref float maxWidth, ref float maxHeight) {
				var result = m_hasFontOrSizeChanged ||
				             text != m_cachedText ||
				             !MathUtil.NearEqual(m_cachedMaxWidth, maxWidth) ||
				             !MathUtil.NearEqual(m_cachedMaxHeight, maxHeight);

				if (result) {
					m_cachedMaxWidth = maxWidth;
					m_cachedMaxHeight = maxHeight;
					m_cachedText = text;
				}

				m_hasFontOrSizeChanged = false;
				return result;
			}

			public TextLayout GetCachedTextLayout() {
				return m_textLayout;
			}

			public TextFormat GetCachedTextFormat() {
				return m_textFormat;
			}

			private void DisposeText() {
				Utilities.Dispose(ref m_textFormat);
				Utilities.Dispose(ref m_textLayout);
			}

			public void Dispose() {
				m_textLayout?.Dispose();
				m_textFormat?.Dispose();
			}
		}
	}
}