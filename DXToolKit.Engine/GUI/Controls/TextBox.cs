using System;
using System.Linq;
using System.Windows.Forms;
using DXToolKit.GUI;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectInput;
using SharpDX.DirectWrite;
using TextAlignment = SharpDX.DirectWrite.TextAlignment;

namespace DXToolKit.Engine {
	public class TextBox : GUIElement {
		private struct GUITextPadding {
			public float Left;
			public float Right;
			public float Top;
			public float Bottom;

			public GUITextPadding(float value) {
				Left = Right = Top = Bottom = value;
			}

			public void ScaleRectangleF(ref RectangleF target) {
				target.X += Left;
				target.Y += Top;
				target.Height -= Top + Bottom;
				target.Width -= Left + Right;
			}

			public RectangleF ScaleRectangleF(RectangleF target) {
				target.X += Left;
				target.Y += Top;
				target.Height -= Top + Bottom;
				target.Width -= Left + Right;
				return target;
			}
		}

		private const int m_borderWidth = 1;
		private const int m_scrollbarSize = 16;

		private int m_caretPosition = -1;
		private int m_selectionStart = -1;
		private int m_selectionEnd = -1;
		private bool m_isTextSelected => m_selectionStart != -1 && m_selectionEnd != -1;
		private bool m_skipMoveCaret;
		private bool m_isMultiline;
		private bool m_displayCaret = true;
		private float m_caretBlinkTimer;
		private float m_caretBlinkTime;
		private Vector2 m_textOffset;
		private GUITextPadding m_padding;
		private Layer m_textEditLayer;
		private LayerParameters m_layerParameters;
		private Scrollbar m_verticalScroll;
		private Scrollbar m_horizontalScroll;
		private GUIPanel m_scrollbarPadding;

		public TextBox(string text = "", bool multiline = false) {
			m_isMultiline = multiline;

			Text = text;
			TextAlignment = TextAlignment.Leading;
			ParagraphAlignment = ParagraphAlignment.Near;

			m_caretBlinkTime = SystemInformation.CaretBlinkTime / 1000.0F;
			m_textEditLayer = new Layer(Graphics.Device);
			m_layerParameters = new LayerParameters {Opacity = 1.0F};

			m_padding = new GUITextPadding(4);
			Draggable = true;

			m_verticalScroll = Append(new Scrollbar(GUIDirection.Vertical) {
				Enabled = false,
				GUIColor = GUIColor.Primary,
			});

			m_horizontalScroll = Append(new Scrollbar(GUIDirection.Horizontal) {
				Enabled = false,
				GUIColor = GUIColor.Primary,
			});

			m_scrollbarPadding = Append(new GUIPanel {
				Enabled = false,
				GUIColor = GUIColor.Primary
			});

			if (m_isMultiline == false) {
				m_verticalScroll.Visible = false;
				m_horizontalScroll.Visible = false;
				m_padding.Top = 0;
				m_padding.Bottom = 0;
				ParagraphAlignment = ParagraphAlignment.Center;
			}

			Height = multiline ? 128 : 24;
		}

		protected override void OnUpdate() {
			Debug.Log("Current caret: " + m_caretPosition);

			// Reset caret position of not focused.
			if (!ContainsFocus) {
				m_caretPosition = -1;
				m_selectionEnd = -1;
				m_selectionStart = -1;
			} else {
				if (Input.TextInputAvailable) {
					HandleTextInput(Input.TextInput);
				}

				HandleHotkeys();

				m_caretBlinkTimer += Time.DeltaTime;
				if (m_caretBlinkTimer > m_caretBlinkTime) {
					m_displayCaret = !m_displayCaret;
					m_caretBlinkTimer = 0;
					ToggleRedraw();
				}
			}

			if (MouseHovering) {
				Input.SetCursorStyle(CursorStyle.IBeam);
			}

			if (m_isMultiline == false) {
				if (Text.Contains("\r") || Text.Contains("\n")) {
					Text = Text.Replace('\r', ' ').Replace('\n', ' ');
				}
			}


			if (Mathf.Abs(Input.MouseWheelDelta) > 0) {
				if (m_verticalScroll.Enabled && ContainsMouse && !m_horizontalScroll.ContainsMouse) {
					m_verticalScroll.Value -= Input.MouseWheelDelta * 10;
				}
			}
		}

		private void UpdateScrollBars() {
			m_textOffset.X = m_padding.Left;
			m_textOffset.Y = m_padding.Top;
			if (TextLayout == null) return;

			var metrics = TextLayout.Metrics;

			m_horizontalScroll.Enabled = metrics.WidthIncludingTrailingWhitespace > Width - m_padding.Left - m_padding.Right;
			m_verticalScroll.Enabled = (m_isMultiline) && (metrics.Height > Height - m_padding.Top - m_padding.Bottom);

			if (m_horizontalScroll.Enabled) {
				var extra = m_verticalScroll.Enabled ? m_verticalScroll.Width : 0;

				m_horizontalScroll.MinValue = 0;
				m_horizontalScroll.MaxValue = metrics.WidthIncludingTrailingWhitespace - (Width - m_padding.Left - m_padding.Right - extra);

				m_textOffset.X = -m_horizontalScroll.Value + m_padding.Left;
			}

			if (m_verticalScroll.Enabled) {
				var extra = m_horizontalScroll.Enabled ? m_horizontalScroll.Height : 0;

				m_verticalScroll.MinValue = 0;
				m_verticalScroll.MaxValue = metrics.Height - (Height - m_padding.Top - m_padding.Bottom - extra);
				m_textOffset.Y = -m_verticalScroll.Value + m_padding.Top;
			}

			// Only needed if multiline
			if (m_isMultiline) {
				if (m_verticalScroll.Enabled && m_horizontalScroll.Enabled) {
					m_scrollbarPadding.Enabled = true;
					m_scrollbarPadding.Width = m_scrollbarSize + m_borderWidth * 2;
					m_scrollbarPadding.Height = m_scrollbarSize + m_borderWidth * 2;

					m_scrollbarPadding.X = Width - (m_scrollbarSize + m_borderWidth);
					m_scrollbarPadding.Y = Height - (m_scrollbarSize + m_borderWidth);


					m_verticalScroll.Width = m_scrollbarSize;
					m_verticalScroll.Height = Height - m_borderWidth - m_borderWidth - m_scrollbarSize;

					m_horizontalScroll.Y = Height - m_scrollbarSize - m_borderWidth;
					m_horizontalScroll.Width = Width - m_borderWidth - m_borderWidth - m_scrollbarSize;
				} else if (m_verticalScroll.Enabled) {
					m_scrollbarPadding.Enabled = false;

					m_verticalScroll.Width = m_scrollbarSize;
					m_verticalScroll.Height = Height - m_borderWidth - m_borderWidth;
				} else if (m_horizontalScroll.Enabled) {
					m_scrollbarPadding.Enabled = false;

					m_horizontalScroll.Y = Height - m_scrollbarSize - m_borderWidth;
					m_horizontalScroll.Width = Width - m_borderWidth - m_borderWidth;
				} else {
					m_scrollbarPadding.Enabled = false;
				}
			}
		}

		protected override void OnRender(RenderTarget renderTarget, RectangleF bounds, TextLayout textLayout, GUIColorPalette palette, GUIDrawTools tools) {
			UpdateScrollBars();

			tools.Rectangle(renderTarget, bounds, palette, GUIColor.Default, GUIBrightness.Darkest);
			tools.Border(renderTarget, bounds, palette, GUIColor.Primary, Brightness);

			var textBounds = bounds;
			if (m_isMultiline) {
				if (m_verticalScroll.Enabled) {
					textBounds.Width -= m_verticalScroll.Width;
				}

				if (m_horizontalScroll.Enabled) {
					textBounds.Height -= m_horizontalScroll.Height;
				}
			}

			m_layerParameters.ContentBounds = m_padding.ScaleRectangleF(textBounds);


			//rt.Transform = Matrix3x2.Scaling(2);
			renderTarget.PushLayer(ref m_layerParameters, m_textEditLayer);

			// Draw selection rectangles
			if (m_selectionStart != -1) {
				GetSelection(out var start, out var length);
				var metrics = TextLayout.HitTestTextRange(start, length, bounds.X + m_textOffset.X, bounds.Y + m_textOffset.Y);
				foreach (var test in metrics) {
					var rect = new RectangleF(
						(int) test.Left,
						(int) test.Top,
						(int) test.Width,
						(int) Math.Ceiling(test.Height)
					);
					if (Text.Length > test.TextPosition + test.Length) {
						if (Text.Substring(test.TextPosition, test.Length).Contains("\r")) {
							rect.Width = Math.Max(bounds.Width, TextLayout.Metrics.Width);
						}
					}

					if (m_isMultiline == false) {
						rect.Y = bounds.Y + m_borderWidth;
						rect.Height = bounds.Height - m_borderWidth - m_borderWidth;
					}

					renderTarget.FillRectangle(rect, palette.GetBrush(GUIColor.Primary, GUIBrightness.Darkest));
				}
			}

			tools.Text(renderTarget, m_textOffset, textLayout, palette, GUIColor.Text, TextBrightness);

			// Draw caret
			if (Focused && m_displayCaret) {
				var metrics = TextLayout.HitTestTextPosition(m_caretPosition, false, out var xRef, out var yRef);
				var top = new Vector2(bounds.X + xRef, bounds.Y + yRef) + m_textOffset;
				var bottom = new Vector2(bounds.X + xRef, bounds.Y + yRef + metrics.Height) + m_textOffset;
				top.X = (int) top.X;
				top.Y = (int) top.Y;
				bottom.X = (int) bottom.X;
				bottom.Y = (int) bottom.Y;
				renderTarget.DrawLine(top, bottom, palette.GetBrush(GUIColor.Text, GUIBrightness.Normal), 1.5F);
			}

			renderTarget.PopLayer();
		}

		protected override void OnResize() {
			base.OnResize();

			if (m_isMultiline) {
				m_verticalScroll.Y = m_borderWidth;
				m_verticalScroll.X = Width - m_scrollbarSize - m_borderWidth;
				m_verticalScroll.Width = m_scrollbarSize;
				m_verticalScroll.Height = Height - m_borderWidth - m_borderWidth - m_scrollbarSize;

				m_horizontalScroll.X = m_borderWidth;
				m_horizontalScroll.Y = Height - m_scrollbarSize - m_borderWidth;
				m_horizontalScroll.Width = Width - m_borderWidth - m_borderWidth - m_scrollbarSize;
				m_horizontalScroll.Height = m_scrollbarSize;

				UpdateScrollBars();
			}
		}

		protected override void OnFocusLost() {
			ToggleRedraw();
			base.OnFocusLost();
		}

		protected override void OnDragStart() {
			SetCaretPosition(LocalMousePosition - m_textOffset, false);
			base.OnDragStart();
		}

		protected override void OnDrag() {
			if (Input.MouseMove.LengthSquared() > 0) {
				SetCaretPosition(LocalMousePosition - m_textOffset);
			}

			base.OnDrag();
		}

		protected override void OnMouseDown(GUIMouseEventArgs args) {
			if (m_isMultiline) {
				SetCaretPosition(LocalMousePosition - m_textOffset);
			} else {
				if (!m_skipMoveCaret) {
					SetCaretPosition(LocalMousePosition - m_textOffset);
				} else {
					m_skipMoveCaret = false;
				}
			}

			base.OnMouseDown(args);
		}

		private bool m_dontResetCaret = false;

		protected override void OnFocusGained() {
			//m_skipMoveCaret = true;
			//m_caretPosition = Text.Length;
			SetCaretPosition(LocalMousePosition, false);
			//m_dontResetCaret = true;
			base.OnFocusGained();
		}

		private void MoveCaret(int newLocation, bool allowSelection) {
			// Left / right shift or mouse pressed, but not if it has just been registered as a click.
			var selectModifier = Input.KeyPressed(Key.LeftShift) || Input.KeyPressed(Key.RightShift) ||
			                     (Input.MousePressed(MouseButton.Left) && !Input.MouseDown(MouseButton.Left));

			if (selectModifier && allowSelection) {
				if (m_selectionStart == -1) {
					m_selectionStart = m_caretPosition;
				}

				m_selectionEnd = newLocation;

				// Clamp selection to a range within the text. only do this in here since we rely on start and end being -1 for checking if something is selected.
				m_selectionEnd = MathUtil.Clamp(m_selectionEnd, 0, Text.Length);
				m_selectionStart = MathUtil.Clamp(m_selectionStart, 0, Text.Length);
			} else if (m_isTextSelected) {
				// If not allowed to select or no selection modifier is pressed, deselect.
				Deselect();
			}

			m_caretPosition = newLocation;
			m_caretPosition = MathUtil.Clamp(m_caretPosition, 0, Text.Length);
			m_caretBlinkTimer = 0;
			m_displayCaret = true;


			UpdateScrollBars();
			ScrollToCaret();
		}

		private void Deselect() {
			m_selectionStart = -1;
			m_selectionEnd = -1;
		}

		private void GetSelection(out int Start, out int Length) {
			Start = m_selectionStart > m_selectionEnd ? m_selectionEnd : m_selectionStart;
			Length = Math.Abs(m_selectionStart - m_selectionEnd);
			if (Length > Text.Length) {
				Length = Text.Length;
			}
		}

		private void SetCaretPosition(Vector2 localScreenPosition, bool allowSelection = true) {
			var hitTest = TextLayout.HitTestPoint(localScreenPosition.X, localScreenPosition.Y, out var isTrailingHit, out var isInside);
			if (isTrailingHit) {
				MoveCaret(hitTest.TextPosition + 1, allowSelection);
			} else {
				MoveCaret(hitTest.TextPosition, allowSelection);
			}
		}

		private void HandleHotkeys() {
			var ctrlModifier = Input.KeyPressed(Key.LeftControl) || Input.KeyPressed(Key.RightControl);

			if (Input.RepeatKey(Key.Left)) {
				if (ctrlModifier) {
					var index = PreviousWordIndex();
					MoveCaret(index, true);
				} else {
					MoveCaret(m_caretPosition - 1, true);
				}
			}

			if (Input.RepeatKey(Key.Right)) {
				if (ctrlModifier) {
					var index = NextWordIndex();
					MoveCaret(index, true);
				} else {
					MoveCaret(m_caretPosition + 1, true);
				}
			}


			if (m_isMultiline) {
				var test1 = TextLayout.HitTestTextPosition(m_caretPosition, false, out var xRef, out var yRef);
				var test2 = TextLayout.HitTestTextPosition(m_selectionStart, false, out var xRef2, out var yRef2);

				// If selection start is set, make sure the xRef is set to that instead of the current caret position
				if (m_selectionStart != -1 && (Input.KeyPressed(Key.LeftShift) || Input.KeyPressed(Key.RightShift))) {
					xRef = xRef2;
				}

				// Need to figure out new caret position
				if (Input.RepeatKey(Key.Up)) {
					SetCaretPosition(new Vector2(xRef, yRef - test1.Height));
				}

				if (Input.RepeatKey(Key.Down)) {
					SetCaretPosition(new Vector2(xRef, yRef + test1.Height));
				}
			}


			if (ctrlModifier) {
				if (Input.RepeatKey(Key.C)) {
					Copy();
				}

				if (Input.RepeatKey(Key.X)) {
					Cut();
				}

				if (Input.RepeatKey(Key.V)) {
					Paste();
				}

				if (Input.RepeatKey(Key.A)) {
					SelectAll();
				}

				if (Input.RepeatKey(Key.Back)) {
					Backspace(true);
				}

				if (Input.RepeatKey(Key.Delete)) {
					Delete(true);
				}
			} else {
				if (Input.RepeatKey(Key.Back)) {
					Backspace();
				}

				if (Input.RepeatKey(Key.Delete)) {
					Delete();
				}
			}

			ToggleRedraw();
		}

		/// <summary>
		/// Gets the text index of the position after the next word compared to the current caret position.
		/// </summary>
		private int NextWordIndex() {
			// If caret position is a newline, return m_caretPosition + 1
			if (m_caretPosition < Text.Length - 1) {
				if (Text[m_caretPosition] == '\r' || Text[m_caretPosition] == '\n') {
					return m_caretPosition + 1;
				}
			}

			bool whitespaceFound = false;
			var result = Text.Substring(m_caretPosition).TakeWhile((c, i) => {
				// If first whitespace is a newline, return false. Since it should always stop at a newline
				if (c == '\r' || c == '\n') return false;
				// Looking for first space
				if (char.IsWhiteSpace(c) == false && whitespaceFound == false) return true;
				// First space found
				whitespaceFound = true;
				// Keep returning true until a char is not a whitespace char
				return char.IsWhiteSpace(c) && whitespaceFound;
			}).Count();
			return m_caretPosition + result;
		}

		/// <summary>
		/// Gets the text index of the position before the previous word.
		/// </summary>
		private int PreviousWordIndex() {
			// If caret position is a newline, return m_caretPosition - 1
			if (m_caretPosition > 0) {
				if (Text[m_caretPosition - 1] == '\r' || Text[m_caretPosition - 1] == '\n') {
					return m_caretPosition - 1;
				}
			}

			bool firstCharFound = false;
			var result = Text.Substring(0, m_caretPosition).Reverse().TakeWhile((c, i) => {
				// Always stop at a newline
				if (c == '\r' || c == '\n') return false;
				// First scan to first actual char
				if (char.IsWhiteSpace(c) && firstCharFound == false) return true;
				// First char found
				firstCharFound = true;
				// Keep scanning until we hit a whitespace
				return char.IsWhiteSpace(c) == false && firstCharFound;
			}).Count();
			return m_caretPosition - result;
		}

		private void HandleTextInput(string textInput) {
			// Dont handle keys if control is pressed.
			// This would be a separate handler for editing etc
			if (Input.KeyPressed(Key.LeftControl) || Input.KeyPressed(Key.RightControl)) return;

			foreach (var ch in textInput) {
				if (ch == (char) 8) {
					Backspace();
				} else {
					// Skip enter if text box is not multiline
					if (ch == (char) 13 && m_isMultiline == false) {
						return;
					}

					// Make sure caret position is inside text area. This is not needed, but just making sure.
					if (m_caretPosition > Text.Length) {
						m_caretPosition = Text.Length;
					}

					if (m_caretPosition < 0) {
						m_caretPosition = 0;
					}

					// Remove selection
					RemoveSelectedText();

					// insert at caret position
					Text = Text.Insert(m_caretPosition, ch.ToString());

					// Move caret to new position
					MoveCaret(m_caretPosition + 1, false);
				}
			}
		}


		private void RemoveSelectedText() {
			// Check if there is any selected text
			if (m_isTextSelected) {
				// Remove selected text
				GetSelection(out var start, out var length);
				Text = Text.Remove(start, length);

				// Caret position needs to be set at start
				MoveCaret(start, false);
				Deselect();
			}
		}

		private void Backspace(bool wholeWord = false) {
			if (wholeWord && m_isTextSelected == false) {
				m_selectionEnd = m_caretPosition;
				m_selectionStart = PreviousWordIndex();
			}

			if (m_isTextSelected) {
				RemoveSelectedText();
				return;
			}

			// Remove at caret position
			if (m_caretPosition > 0 && Text.Length > 0) {
				Text = Text.Remove(m_caretPosition - 1, 1);
				MoveCaret(m_caretPosition - 1, false);
			}
		}

		private void Delete(bool wholeWord = false) {
			if (wholeWord && m_isTextSelected == false) {
				m_selectionEnd = NextWordIndex();
				m_selectionStart = m_caretPosition;
			}

			if (m_isTextSelected) {
				RemoveSelectedText();
				return;
			}

			if (m_caretPosition < Text.Length) {
				Text = Text.Remove(m_caretPosition, 1);
			}
		}

		private void Paste() {
			// Remove selected text
			RemoveSelectedText();

			// Get text from clipboard
			var text = ClipboardHandler.GetText();

			// Insert text
			Text = Text.Insert(m_caretPosition, text);

			// Move caret to end of newly pasted string.
			MoveCaret(m_caretPosition + text.Length, false);
		}

		private void Cut() {
			GetSelection(out var start, out var length);
			if (length != 0) {
				var toClipboard = Text.Substring(start, length);
				if (toClipboard.Length > 0) {
					ClipboardHandler.SetText(toClipboard);
				}

				RemoveSelectedText();
			}
		}

		private void Copy() {
			GetSelection(out var start, out var length);
			if (length != 0) {
				var toClipboard = Text.Substring(start, length);
				if (toClipboard.Length > 0) {
					ClipboardHandler.SetText(toClipboard);
				}
			}
		}

		private void SelectAll() {
			m_selectionStart = 0;
			m_selectionEnd = Text.Length;
		}

		private void ScrollToCaret() {
			// Check if outside bounds
			var caretposition = TextLayout.HitTestTextPosition(m_caretPosition, true, out var xRef, out var yRef);

			var bounds = Bounds;
			bounds.X = 0;
			bounds.Y = 0;

			if (m_horizontalScroll.Enabled) {
				bounds.X += m_horizontalScroll.Value;
			}

			if (m_verticalScroll.Enabled) {
				bounds.Y += m_verticalScroll.Value;
			}

			bounds.Width -= m_padding.Left + m_padding.Right;
			bounds.Height -= m_padding.Top + m_padding.Bottom;

			if (m_horizontalScroll.Enabled) {
				bounds.Height -= m_horizontalScroll.Height;
			}

			if (m_verticalScroll.Enabled) {
				bounds.Width -= m_verticalScroll.Width;
			}

			var verticalOffset = 0.0F;
			var horizontalOffset = 0.0F;

			// Above the rect
			if (caretposition.Top < bounds.Y) {
				verticalOffset = caretposition.Top - bounds.Y;
			}

			// Below the rect
			if (caretposition.Top + caretposition.Height > bounds.Y + bounds.Height) {
				verticalOffset = caretposition.Top + caretposition.Height - (bounds.Y + bounds.Height);
			}

			// To the left
			if (caretposition.Left < bounds.X) {
				horizontalOffset = caretposition.Left - bounds.X;
			}

			// To the right
			if (caretposition.Left > bounds.Width + bounds.X) {
				horizontalOffset = caretposition.Left - (bounds.Width + bounds.X);
			}

			if (Mathf.Abs(verticalOffset) > 0 && m_verticalScroll.Enabled) {
				m_verticalScroll.Value += verticalOffset;
			}

			if (Mathf.Abs(horizontalOffset) > 0 && m_horizontalScroll.Enabled) {
				m_horizontalScroll.Value += horizontalOffset;
			}
		}
	}
}