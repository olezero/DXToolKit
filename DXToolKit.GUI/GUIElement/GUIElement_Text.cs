using System;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DirectWrite;


namespace DXToolKit.GUI {
	public abstract partial class GUIElement {
		/// <summary>
		/// Gets or sets the default font used by the GUI, this is a global variable
		/// </summary>
		public static string DEFAULT_FONT = "Arial";

		/// <summary>
		/// Gets or sets the default font size used by the GUI, this is a global variable
		/// </summary>
		public static int DEFAULT_FONT_SIZE = 14;

		/// <summary>
		/// Gets or sets the default font weight of all new GUI Elements
		/// </summary>
		public static FontWeight DEFAULT_FONT_WEIGHT = FontWeight.Normal;

		/// <summary>
		/// Gets or sets the default font style of all new GUI elements
		/// </summary>
		public static FontStyle DEFAULT_FONT_STYLE = FontStyle.Normal;

		/// <summary>
		/// Gets or sets the default font stretch of all new GUI elements
		/// </summary>
		public static FontStretch DEFAULT_FONT_STRETCH = FontStretch.Normal;

		/// <summary>
		/// Stored text in the element
		/// </summary>
		private string m_text = "";

		/// <summary>
		/// Container for the gui text used for rendering
		/// </summary>
		private GUIText m_guiText = new GUIText(DEFAULT_FONT, DEFAULT_FONT_SIZE, DEFAULT_FONT_WEIGHT, DEFAULT_FONT_STYLE, DEFAULT_FONT_STRETCH);

		/// <summary>
		/// Controller to run text change events
		/// </summary>
		private bool m_hasTextChanged;

		/// <summary>
		/// Controller to run text property change events
		/// </summary>
		private bool m_hasTextPropsChanged;

		/// <summary>
		/// Gets or sets the text of the element
		/// </summary>
		public string Text {
			get => m_text;
			set {
				if (m_text != value) {
					m_text = value;
					m_hasTextChanged = true;
				}
			}
		}

		/// <summary>
		/// Gets or sets the paragraph alignment of the element text
		/// </summary>
		public virtual ParagraphAlignment ParagraphAlignment {
			get => m_guiText.ParagraphAlignment;
			set {
				if (m_guiText != null) {
					if (m_guiText.ParagraphAlignment != value) {
						m_hasTextPropsChanged = true;
					}

					m_guiText.ParagraphAlignment = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets the text alignment of the element text
		/// </summary>
		public virtual TextAlignment TextAlignment {
			get => m_guiText.TextAlignment;
			set {
				if (m_guiText != null) {
					if (m_guiText.TextAlignment != value) {
						m_hasTextPropsChanged = true;
					}

					m_guiText.TextAlignment = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets the word wrapping of the element text
		/// </summary>
		public virtual WordWrapping WordWrapping {
			get => m_guiText.WordWrapping;
			set {
				if (m_guiText != null) {
					if (m_guiText.WordWrapping != value) {
						m_hasTextPropsChanged = true;
					}

					m_guiText.WordWrapping = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets the font used of the element text
		/// </summary>
		public virtual string Font {
			get => m_guiText.Font;
			set {
				if (m_guiText != null) {
					if (m_guiText.Font != value) {
						m_hasTextPropsChanged = true;
					}

					m_guiText.Font = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets the font size of the element text
		/// </summary>
		public virtual int FontSize {
			get => m_guiText.FontSize;
			set {
				if (m_guiText != null) {
					if (m_guiText.FontSize != value) {
						m_hasTextPropsChanged = true;
					}

					m_guiText.FontSize = value;
				}
			}
		}


		/// <summary>
		/// Gets or sets the font weight
		/// </summary>
		public virtual FontWeight FontWeight {
			get => m_guiText.FontWeight;
			set {
				if (m_guiText != null) {
					if (m_guiText.FontWeight != value) {
						m_hasTextPropsChanged = true;
					}

					m_guiText.FontWeight = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets the font style
		/// </summary>
		public virtual FontStyle FontStyle {
			get => m_guiText.FontStyle;
			set {
				if (m_guiText != null) {
					if (m_guiText.FontStyle != value) {
						m_hasTextPropsChanged = true;
					}

					m_guiText.FontStyle = value;
				}
			}
		}

		/// <summary>
		/// Gets or sets the font stretch
		/// </summary>
		public virtual FontStretch FontStretch {
			get => m_guiText.FontStretch;
			set {
				if (m_guiText != null) {
					if (m_guiText.FontStretch != value) {
						m_hasTextPropsChanged = true;
					}

					m_guiText.FontStretch = value;
				}
			}
		}

		/// <summary>
		/// Gets a reference to the underlying text layout. This can be null if no redraws has been called on the element.
		/// </summary>
		public TextLayout TextLayout => m_guiText.GetCachedTextLayout();

		/// <summary>
		/// Gets a reference to the underlying text format. This can be null if no redraws has been called on the element.
		/// </summary>
		public TextFormat TextFormat => m_guiText.GetCachedTextFormat();

		/// <summary>
		/// Text class used for caching and updating native TextFormat and TextLayout objects
		/// </summary>
		private sealed class GUIText : IDisposable {
			public TextLayout m_textLayout;
			public TextFormat m_textFormat;

			private string m_font;
			private int m_fontSize;

			private string m_cachedText;
			private float m_cachedMaxWidth;
			private float m_cachedMaxHeight;
			private bool m_hasFontOrSizeChanged;
			private bool m_hasFontStyleChanged;

			private ParagraphAlignment m_cachedParagraphAlignment;
			private TextAlignment m_cachedTextAlignment;
			private WordWrapping m_cachedWordWrapping;
			private FontWeight m_cachedfontWeight;
			private FontStyle m_cachedfontStyle;
			private FontStretch m_cachedFontStretch;

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

			public FontWeight FontWeight {
				get => m_cachedfontWeight;
				set {
					if (m_cachedfontWeight != value) {
						m_cachedfontWeight = value;
						m_hasFontStyleChanged = true;
					}
				}
			}

			public FontStyle FontStyle {
				get => m_cachedfontStyle;
				set {
					if (m_cachedfontStyle != value) {
						m_cachedfontStyle = value;
						m_hasFontStyleChanged = true;
					}
				}
			}

			public FontStretch FontStretch {
				get => m_cachedFontStretch;
				set {
					if (m_cachedFontStretch != value) {
						m_cachedFontStretch = value;
						m_hasFontStyleChanged = true;
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

			public GUIText(string font, int fontSize, FontWeight fontWeight = FontWeight.Normal, FontStyle fontStyle = FontStyle.Normal, FontStretch fontStretch = FontStretch.Normal) {
				m_font = font;
				m_fontSize = fontSize;
				m_cachedParagraphAlignment = ParagraphAlignment.Near;
				m_cachedTextAlignment = TextAlignment.Leading;
				m_cachedWordWrapping = WordWrapping.NoWrap;
				m_cachedfontStyle = fontStyle;
				m_cachedfontWeight = fontWeight;
				m_cachedFontStretch = fontStretch;
			}

			private void LoadText(FactoryCollection factory, ref string text, ref float maxWidth, ref float maxHeigth) {
				DisposeText();


				// Try and fetch font from a static DXFontCollection by string, if it exists, use that and the accompanying font collection, else use standard

				m_textFormat = new TextFormat(factory, m_font, m_cachedfontWeight, m_cachedfontStyle, m_cachedFontStretch, m_fontSize) {
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
				             !MathUtil.NearEqual(m_cachedMaxHeight, maxHeight) ||
				             m_hasFontStyleChanged;

				if (result) {
					m_cachedMaxWidth = maxWidth;
					m_cachedMaxHeight = maxHeight;
					m_cachedText = text;
				}

				m_hasFontOrSizeChanged = false;
				m_hasFontStyleChanged = false;
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