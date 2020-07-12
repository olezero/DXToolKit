using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace DXToolKit.Engine {
	public class Window : GUIElement {
		public class WinHeader : GUIElement {
			private Window m_parentWindow;
			private CloseButton m_closeButton;

			public WinHeader(Window parent) {
				m_parentWindow = parent;
				Draggable = true;

				m_closeButton = Append(new CloseButton {
					GUIColor = m_parentWindow.HeaderColor,
				});
				ResizeChildren();
			}

			protected override void OnRender(RenderTarget renderTarget, RectangleF bounds, TextLayout textLayout, GUIColorPalette palette, GUIDrawTools drawTools) {
				drawTools.Rectangle(renderTarget, bounds, palette, m_parentWindow.HeaderColor, m_parentWindow.Brightness);

				if (string.IsNullOrEmpty(Text) == false) {
					drawTools.Text(renderTarget, Vector2.Zero, textLayout, palette, GUIColor.Text, TextBrightness);
				}

				drawTools.Border(renderTarget, bounds, palette, m_parentWindow.BodyColor, GUIBrightness.Darkest);
			}

			protected override void OnBoundsChanged() {
				ResizeChildren();
				base.OnBoundsChanged();
			}

			private void ResizeChildren() {
				// Position close button on the right side of the header, with some padding
				m_closeButton.X = this.Width - this.Height + 2;
				m_closeButton.Y = 2;

				// Scale the close button to match this.height with some padding
				m_closeButton.Width = this.Height - 4;
				m_closeButton.Height = this.Height - 4;
			}
		}

		public class WinBody : GUIElement {
			private Window m_parentWindow;

			public WinBody(Window parent) {
				m_parentWindow = parent;
				Draggable = false;
			}

			protected override void OnRender(RenderTarget renderTarget, RectangleF bounds, TextLayout textLayout, GUIColorPalette palette, GUIDrawTools drawTools) {
				drawTools.Rectangle(renderTarget, bounds, palette, m_parentWindow.BodyColor, m_parentWindow.Brightness);
				drawTools.Border(renderTarget, bounds, palette, m_parentWindow.BodyColor, GUIBrightness.Darkest);
			}
		}

		public WinHeader Header;
		public WinBody Body;

		public GUIColor HeaderColor = GUIColor.Primary;
		public GUIColor BodyColor = GUIColor.Default;

		public Window() {
			// Append elements for header and body
			Header = Append(new WinHeader(this));
			Body = Append(new WinBody(this));
			// Set default drag to true
			Draggable = true;
			// Set some default width and height
			Width = 12 * 32;
			Height = 12 * 24;
			// Set some default paragraph and text alignments
			ParagraphAlignment = ParagraphAlignment.Center;
			TextAlignment = TextAlignment.Center;
			// Setup header dragging
			Header.Drag += () => {
				if (Draggable) {
					X += Input.MouseMove.X;
					Y += Input.MouseMove.Y;
				}
			};
			// Default text
			Text = "Window";
		}

		protected sealed override void OnBoundsChangedDirect() {
			Header.Width = this.Width;
			Header.Height = 20;
			Header.X = 0;
			Header.Y = 0;

			Body.Width = this.Width;
			Body.Height = this.Height - Header.Height;
			Body.Y = Header.Height;
			Body.X = 0;

			base.OnBoundsChangedDirect();
		}

		protected override void OnTextChanged() {
			Header.Text = Text;
			Header.TextAlignment = TextAlignment;
			Header.ParagraphAlignment = ParagraphAlignment;

			base.OnTextChanged();
		}

		protected override void OnContainFocusGained() {
			MoveToFront();
			base.OnContainFocusGained();
		}
	}
}