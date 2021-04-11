using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using DXToolKit;
using DXToolKit.Engine;
using DXToolKit.GUI;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectInput;
using SharpDX.DirectWrite;

namespace DXToolKit.Engine {
	public class TextBox : ActiveElement {
		private int m_caretPosition;
		private Vector2 m_textOffset;
		private GUIPadding m_textpadding;

		private bool m_multiline = false;
		private bool m_caretBlinkHidden = false;
		private float m_caretBlinkTimer = 0;
		private bool m_caretVisible;
		private float m_systemCaretBlinkTime = 0.0F;
		private int m_selectionStart = -1;
		private int m_selectionEnd = -1;
		private LayerParameters m_textEditorLayerParameters;
		private Layer m_textEditorLayer;
		private bool m_zoomToCaret = true;

		private Scrollbar m_verticalScroll;
		private Scrollbar m_horizontalScroll;

		public int ScrollSize = 16;
		public bool DrawBorder = false;
		public bool OnlyNumbers = false;
		public bool IgnoreTab = true;
		public bool AllowEditing = true;


		public string SelectedText => GetSelectedText();

		public TextBox(string text = "", bool multiline = false) {
			Text = text;
			m_multiline = multiline;

			m_caretPosition = 0;
			Draggable = true;
			m_systemCaretBlinkTime = SystemInformation.CaretBlinkTime / 1000.0F;
			m_caretVisible = false;
			m_textEditorLayerParameters = new LayerParameters {
				Opacity = 1.0F,
				ContentBounds = new RectangleF(0, 0, Width, Height),
			};
			m_textEditorLayer = new Layer(Graphics.Device);
			m_verticalScroll = Append(new Scrollbar(GUIDirection.Vertical) {
				Enabled = false
			});
			m_horizontalScroll = Append(new Scrollbar(GUIDirection.Horizontal) {
				Enabled = false
			});
			m_textOffset = Vector2.Zero;
			if (m_multiline) {
				m_textpadding = new GUIPadding(2);
				ParagraphAlignment = ParagraphAlignment.Near;
			} else {
				m_textpadding = new GUIPadding(2, 0);
				ParagraphAlignment = ParagraphAlignment.Center;
			}

			m_verticalScroll.ValueChanged += value => {
				m_textOffset.Y = -value;
			};

			m_horizontalScroll.ValueChanged += value => {
				m_textOffset.X = -value;
			};
		}

		protected override void OnBoundsChanged() {
			m_verticalScroll.Width = ScrollSize;
			m_verticalScroll.Height = Height;
			m_verticalScroll.X = Width - ScrollSize;

			m_horizontalScroll.Width = Width;
			m_horizontalScroll.Height = ScrollSize;
			m_horizontalScroll.Bottom = Height;
			base.OnBoundsChanged();
		}

		protected override void OnDispose() {
			m_textEditorLayer?.Dispose();
			base.OnDispose();
		}

		protected override void OnUpdate() {
			if (ContainsFocus) {
				m_caretPosition = Mathf.Clamp(m_caretPosition, 0, Text.Length);
				m_caretBlinkTimer += Time.DeltaTime;
				if (m_caretBlinkTimer > m_systemCaretBlinkTime) {
					m_caretBlinkTimer = 0;
					m_caretBlinkHidden = !m_caretBlinkHidden;
					ToggleRedraw();
				}

				if (Input.RepeatingKey is Key repeatingKey) {
					HandleHotkey(repeatingKey);
				}

				if (Input.TextInputAvailable && AllowEditing) {
					HandleTextInput(Input.TextInput);
				}
			} else {
				m_caretVisible = false;
			}

			if (ContainsMouse && AllowEditing) {
				if (m_multiline && m_verticalScroll.Enabled) {
					if (m_verticalScroll.ContainsMouse == false && m_horizontalScroll.ContainsMouse == false) {
						if (Mathf.Abs(Input.MouseWheelDelta) > 0) {
							m_textOffset.Y += Input.MouseWheelDelta * FontSize * Input.MouseWheelScrollLines;
							// Bit silly, but the scrollbar clamps and maps values correctly, so why not just use it
							m_verticalScroll.Value = -m_textOffset.Y;
							m_textOffset.Y = -m_verticalScroll.Value;
							ToggleRedraw();
						}
					}
				}
			}

			base.OnUpdate();
		}

		protected override void OnDrag() {
			if (Input.MouseMove.LengthSquared() > 0) {
				var currentIndex = TextAtPoint(LocalMousePosition);
				if (currentIndex != -1) {
					if (m_selectionStart == -1) {
						m_selectionStart = currentIndex;
					}

					m_selectionEnd = currentIndex;
					MoveCaret(m_selectionEnd);
					ToggleRedraw();
				}
			}

			Input.SetCursorStyle(CursorStyle.IBeam);
			base.OnDrag();
		}

		private void HandleTextInput(string text) {
			if (Input.KeyPressed(Key.LeftControl) || Input.KeyPressed(Key.RightControl)) {
				base.OnTextInput(text);
				return;
			}

			AddText(text);
			//ResetSelect();
		}

		protected override void OnMouseHover() {
			Input.SetCursorStyle(CursorStyle.IBeam);
			base.OnMouseHover();
		}

		protected override void OnContainFocusGained() {
			m_caretVisible = true;
			ResetCaretBlink();
			if (m_skipMoveCaret) {
				m_skipMoveCaret = false;
			} else {
				MoveCaret(Text.Length);
			}

			base.OnContainFocusGained();
		}

		protected override void OnContainFocusLost() {
			m_caretVisible = false;
			ResetSelect();
			ToggleRedraw();
			base.OnContainFocusLost();
		}

		private bool m_skipMoveCaret = false;

		protected override void OnMouseDown(GUIMouseEventArgs args) {
			if (args.LeftMouseDown) {
				m_skipMoveCaret = true;
				MoveCaret(LocalMousePosition);
				ResetSelect();
			}

			base.OnMouseDown(args);
		}

		protected override void OnDoubleClick(GUIMouseEventArgs args) {
			if (m_multiline == false) {
				SelectAll();
			} else {
				// Select word 
				SelectWord();
			}

			base.OnDoubleClick(args);
		}

		private void SelectWord() {
			var nextWordIndex = NextWordIndex();
			var prevWordIndex = PreviousWordIndex();
			SelectText(prevWordIndex, nextWordIndex - 1);
		}

		private void HandleHotkey(Key key) {
			if (key == Key.LeftControl || key == Key.RightControl || key == Key.LeftShift || key == Key.RightShift) {
				return;
			}

			var ctrl = Input.KeyPressed(Key.LeftControl) || Input.KeyPressed(Key.RightControl);
			var shift = Input.KeyPressed(Key.LeftShift) || Input.KeyPressed(Key.RightShift);

			if (ctrl) {
				if (key == Key.C) Copy();
				if (key == Key.X && AllowEditing) Cut();
				if (key == Key.V && AllowEditing) Paste();
				if (key == Key.A) SelectAll();
			}

			if (key == Key.Left || key == Key.Right || key == Key.Up || key == Key.Down || key == Key.Delete || key == Key.Back) {
				if (m_multiline) {
					var test1 = TextLayout.HitTestTextPosition(m_caretPosition, false, out var xRef, out var yRef);
					var test2 = TextLayout.HitTestTextPosition(m_selectionStart, false, out var xRef2, out var yRef2);

					// If selection start is set, make sure the xRef is set to that instead of the current caret position
					if (m_selectionStart != -1 && (Input.KeyPressed(Key.LeftShift) || Input.KeyPressed(Key.RightShift))) {
						xRef = xRef2;
					}

					// Need to figure out new caret position
					if (key == Key.Up) {
						MoveCaret(new Vector2(xRef, yRef - test1.Height) + m_textOffset, shift);
					}

					if (key == Key.Down) {
						MoveCaret(new Vector2(xRef, yRef + test1.Height) + m_textOffset, shift);
					}
				}


				if (key == Key.Right) {
					var targetIndex = ctrl ? NextWordIndex() : m_caretPosition + 1;
					MoveCaret(targetIndex, shift);
				}

				if (key == Key.Left) {
					var targetIndex = ctrl ? PreviousWordIndex() : m_caretPosition - 1;
					MoveCaret(targetIndex, shift);
				}

				if (key == Key.Back) {
					if (AllowEditing) {
						Backspace(ctrl);
						// Need to reset select if shift + back is pressed, which does not do anything but makes some wonky selections
						ResetSelect();
					}
				}

				if (key == Key.Delete) {
					if (AllowEditing) {
						Delete(ctrl);
						// Need to reset select if shift + delete is pressed, which does not do anything but makes some wonky selections
						ResetSelect();
					}
				}

				if (!shift) ResetSelect();
			}
		}

		protected override void OnRender(RenderTarget renderTarget, RectangleF bounds, TextLayout textLayout, GUIColorPalette palette, GUIDrawTools drawTools) {
			drawTools.Rectangle(renderTarget, bounds, palette, BackgroundColor, GUIBrightness.Dark);

			var textMetrics = textLayout.Metrics;
			var textBounds = bounds;
			textBounds.Width -= m_textpadding.Right;
			textBounds.Height -= m_textpadding.Bottom;

			if (m_multiline) {
				if (textMetrics.Width + m_textpadding.Left + m_textpadding.Right > Width) {
					m_horizontalScroll.Enabled = true;
					m_horizontalScroll.MinValue = 0;
					m_horizontalScroll.MaxValue = textMetrics.Width + m_textpadding.Left + m_textpadding.Right + Width / 2.0F - Width;
					textBounds.Height -= m_horizontalScroll.Height;
				} else {
					m_horizontalScroll.Enabled = false;
				}

				if (textMetrics.Height + m_textpadding.Top + m_textpadding.Bottom > Height) {
					m_verticalScroll.Enabled = true;
					m_verticalScroll.MinValue = 0;
					m_verticalScroll.MaxValue = textMetrics.Height + m_textpadding.Top + m_textpadding.Bottom + Height / 2.0F - Height;
					textBounds.Width -= m_verticalScroll.Width;
				} else {
					m_verticalScroll.Enabled = false;
				}

				m_horizontalScroll.Width = m_verticalScroll.Enabled ? Width - ScrollSize : Width;
				m_verticalScroll.Height = m_horizontalScroll.Enabled ? Height - ScrollSize : Height;
			}


			var textPaddingVector = new Vector2(m_textpadding.Left, m_textpadding.Top);

			// Need to do it this way or else the hit test will return an invalid position since text layout has not been updated before render is called
			// But its fine to delay until render, since its just a visual thing
			// This way we can also prevent "zoom to caret" when scrollbars are used to move the text offset
			if (m_zoomToCaret) {
				ZoomToCaret();
			}

			m_textEditorLayerParameters.ContentBounds = textBounds;
			renderTarget.PushLayer(ref m_textEditorLayerParameters, m_textEditorLayer);

			if (m_selectionStart >= 0 && m_selectionEnd >= 0) {
				var (start, length) = GetSelectionLength();
				var result = textLayout.HitTestTextRange(start, length, 0, 0);
				for (int i = 0; i < result.Length; i++) {
					var hit = result[i];
					renderTarget.FillRectangle(
						new RectangleF(
							hit.Left + m_textOffset.X + textPaddingVector.X,
							hit.Top + m_textOffset.Y + textPaddingVector.Y,
							hit.Width,
							hit.Height
						),
						palette.GetBrush(ForegroundColor, GUIBrightness.Brightest)
					);
				}
			}

			drawTools.Text(renderTarget, m_textOffset + textPaddingVector, textLayout, palette, GUIColor.Text, TextBrightness);
			if (m_caretVisible) {
				DrawCaret(renderTarget, palette.GetBrush(GUIColor.Text, TextBrightness));
			}

			renderTarget.PopLayer();

			if (DrawBorder) {
				drawTools.Border(renderTarget, bounds, palette, ForegroundColor, GUIBrightness.Normal);
			}
		}

		private void ZoomToCaret() {
			// If caret is out of bounds, we need to move
			var boundingRect = new RectangleF {
				Location = -m_textOffset,
				Width = m_textEditorLayerParameters.ContentBounds.Right,
				Height = m_textEditorLayerParameters.ContentBounds.Bottom
			};
			m_textpadding.ResizeRectangle(ref boundingRect);
			var (cTop, cBottom) = GetCaretPosition();
			if (cBottom.Y > boundingRect.Bottom) {
				m_textOffset.Y -= cBottom.Y - boundingRect.Bottom;
			}

			if (cTop.Y < boundingRect.Top) {
				m_textOffset.Y -= cTop.Y - boundingRect.Top;
			}

			if (cTop.X > boundingRect.Right) {
				m_textOffset.X -= cTop.X - boundingRect.Right;
			}

			if (cTop.X < boundingRect.Left) {
				// Offset by half the bounding rect width when scrolling back through the text
				m_textOffset.X -= cTop.X - boundingRect.Left - boundingRect.Width / 2.0F;
			}

			// Little handle to both make sure we dont end up in -x aswell as snapping back to 0 when we can
			boundingRect.X = m_textpadding.Left;
			if (cTop.X < boundingRect.Right) {
				m_textOffset.X = 0;
			}

			// Small guard
			if (m_textOffset.Y > 0) {
				m_textOffset.Y = 0;
			}

			// Update scrollbar values
			m_verticalScroll.Value = -m_textOffset.Y;
			m_horizontalScroll.Value = -m_textOffset.X;
			m_zoomToCaret = false;
		}

		private void DrawCaret(RenderTarget renderTarget, SolidColorBrush brush) {
			if (TextLayout != null && m_caretPosition >= 0 && m_caretPosition <= Text.Length) {
				if (!m_caretBlinkHidden) {
					var (top, bottom) = GetCaretPosition(true);
					renderTarget.DrawLine(top, bottom, brush, 2);
					//var metrics = TextLayout.HitTestTextPosition(m_caretPosition, false, out var xRef, out var yRef);
					//renderTarget.DrawLine(new Vector2(xRef, yRef) + m_textOffset, new Vector2(xRef, yRef + metrics.Height) + m_textOffset, brush, 2);
				}
			}
		}

		private (Vector2 top, Vector2 bottom) GetCaretPosition(bool useOffset = false) {
			if (TextLayout != null && m_caretPosition >= 0 && m_caretPosition <= Text.Length) {
				var textPaddingVector = new Vector2(m_textpadding.Left, m_textpadding.Top);
				var metrics = TextLayout.HitTestTextPosition(m_caretPosition, false, out var xRef, out var yRef);
				if (useOffset) {
					return (
						new Vector2(xRef, yRef) + m_textOffset + textPaddingVector,
						new Vector2(xRef, yRef + metrics.Height) + m_textOffset + textPaddingVector
					);
				}

				return (
					new Vector2(xRef, yRef) + textPaddingVector,
					new Vector2(xRef, yRef + metrics.Height) + textPaddingVector
				);
			}

			return default;
		}


		private void AddText(string text) {
			if (IgnoreTab && text.Length == 1 && text[0] == '\t') {
				return;
			}

			HandleRemoveSelection();
			// Remove any newlines if not multiline
			if (!m_multiline) {
				text = text.Replace("\r", "");
				text = text.Replace("\n", "");
			}


			// Try parse number, if it fails dont update text
			if (OnlyNumbers) {
				var newText = Text.Insert(m_caretPosition, text);
				if (newText.Length == 1) {
					if (newText[0] == '.' || newText[0] == ',') {
						newText = "0.";
					}
				}


				if (newText.Length == 1 && newText[0] == '-') {
					Text = "-";
					MoveCaret(1);
				} else {
					if (float.TryParse(newText, out var r)) {
						Text = Text.Insert(m_caretPosition, text);
						MoveCaret(m_caretPosition + text.Length);
					}
				}
			} else {
				Text = Text.Insert(m_caretPosition, text);
				MoveCaret(m_caretPosition + text.Length);
			}

			ResetCaretBlink();
			ResetSelect();
		}

		private void RemoveTextAtPosition(int index, int length) {
			if (index < 0) {
				return;
			}

			if (index + length > Text.Length) {
				length = Text.Length - index;
			}

			var newText = Text.Remove(index, length);
			ResetCaretBlink();
			if (newText != Text) {
				Text = newText;
			}
		}

		private void MoveCaret(int index, bool select = false) {
			var prevPosition = m_caretPosition;
			m_caretPosition = Mathf.Clamp(index, 0, Text.Length);
			if (select) {
				if (m_selectionStart != -1) {
					SelectText(m_selectionStart, m_caretPosition);
				} else {
					SelectText(prevPosition, m_caretPosition);
				}
			}

			ResetCaretBlink();
			m_zoomToCaret = true;
		}

		private void MoveCaret(Vector2 localPosition, bool select = false) {
			var position = TextAtPoint(localPosition);
			if (position != -1) {
				MoveCaret(position, select);
			}
		}

		private int TextAtPoint(Vector2 point) {
			if (TextLayout != null) {
				point -= m_textOffset;
				var result = TextLayout.HitTestPoint(point.X, point.Y, out var isTrailingHit, out var isInside);
				return isTrailingHit ? result.TextPosition + 1 : result.TextPosition;
			}

			return -1;
		}

		private void ResetCaretBlink() {
			m_caretBlinkHidden = false;
			m_caretBlinkTimer = 0;
		}

		public void Backspace(bool wholeWord = false) {
			if (HandleRemoveSelection() != 0) return;
			if (wholeWord) {
				var prevWorldIndex = PreviousWordIndex();
				var length = m_caretPosition - prevWorldIndex;
				RemoveTextAtPosition(m_caretPosition - length, length);
				MoveCaret(m_caretPosition - length);
			} else {
				RemoveTextAtPosition(m_caretPosition - 1, 1);
				MoveCaret(m_caretPosition - 1);
			}
		}

		public void Delete(bool wholeWord = false) {
			if (HandleRemoveSelection() != 0) return;
			if (wholeWord) {
				var nextWorldIndex = NextWordIndex();
				var length = nextWorldIndex - m_caretPosition;
				RemoveTextAtPosition(m_caretPosition, length);
			} else {
				RemoveTextAtPosition(m_caretPosition, 1);
			}
		}

		public void Copy() {
			var selection = GetSelectedText();
			if (selection != "") {
				ClipboardHandler.SetText(selection);
			}
		}

		public void Cut() {
			var selection = GetSelectedText();
			if (selection != "") {
				HandleRemoveSelection();
				ClipboardHandler.SetText(selection);
				ResetSelect();
			}
		}

		public void Paste() {
			var clipboardText = ClipboardHandler.GetText();
			if (clipboardText != "") {
				AddText(clipboardText);
				ResetSelect();
			}
		}

		public void SelectAll() {
			SelectText(0, Text.Length);
		}

		/// <summary>
		/// Handles deletion of current selected text
		/// </summary>
		/// <returns>The length of the deleted text</returns>
		private int HandleRemoveSelection() {
			if (SelectedText != "") {
				var (start, length) = GetSelectionLength();
				RemoveTextAtPosition(start, length);
				MoveCaret(start);
				return length;
			}

			return 0;
		}


		public void SelectText(int fromIndex, int toIndex) {
			m_selectionStart = fromIndex;
			m_selectionEnd = toIndex;
		}

		public void ResetSelect() {
			m_selectionStart = -1;
			m_selectionEnd = -1;
		}

		public string GetSelectedText() {
			var (start, length) = GetSelectionLength();
			if (start >= 0 && length > 0) {
				return Text.Substring(start, length);
			}

			return "";
		}

		public (int start, int length) GetSelectionLength() {
			var (selectStart, selectEnd) = GetSelectionStartEnd();
			var length = selectEnd - selectStart;
			if (selectStart > Text.Length) return (-1, -1);
			if (selectStart + length > Text.Length) {
				length = Text.Length - selectStart;
			}

			return (selectStart, length);
		}

		public (int start, int end) GetSelectionStartEnd() {
			var selectEnd = m_selectionEnd;
			var selectStart = m_selectionStart;
			if (selectEnd < selectStart) {
				var temp = selectStart;
				selectStart = selectEnd;
				selectEnd = temp;
			}

			return (selectStart, selectEnd);
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
	}
}