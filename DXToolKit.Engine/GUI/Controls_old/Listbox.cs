using System;
using System.Collections.Generic;
using DXToolKit;
using DXToolKit.Engine;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using System.Linq;
using DXToolKit.GUI;
using SharpDX.DirectInput;
using TextAlignment = SharpDX.DirectWrite.TextAlignment;

namespace DXToolKit.Engine {
	/// <inheritdoc />
	public delegate void ListboxOptionEventhandler(ListboxOption option);

	/// <inheritdoc />
	public class ListboxOption : ActiveElement {
		private Listbox m_parentListbox;
		private bool m_selected = false;
		private bool m_isHighlighted = false;

		/// <summary>
		/// User data for the list box option
		/// </summary>
		public object Userdata;

		public ListboxOption(string text, object userdata = null) {
			Text = text;
			Height = 16;
			TextAlignment = TextAlignment.Leading;
			ParagraphAlignment = ParagraphAlignment.Center;
			Userdata = userdata;
		}

		public void Select() {
			m_parentListbox.Deselect();
			m_selected = true;
			ToggleRedraw();
		}

		public void Deselect() {
			m_selected = false;
			ToggleRedraw();
		}

		public void Highlight() {
			m_parentListbox.RemoveHighlight();
			m_isHighlighted = true;
			ToggleRedraw();
		}

		public void RemoveHighlight() {
			m_isHighlighted = false;
			ToggleRedraw();
		}

		protected override void OnParentSet(DXToolKit.GUI.GUIElement parent, DXToolKit.GUI.GUIElement child) {
			if (parent?.Parent != null && parent.Parent is Listbox listbox) {
				Font = parent.Parent.Font;
				FontSize = parent.Parent.FontSize;
				m_parentListbox = listbox;
			}

			base.OnParentSet(parent, child);
		}

		protected override void OnRender(RenderTarget renderTarget, RectangleF bounds, TextLayout textLayout, GUIColorPalette palette, GUIDrawTools drawTools) {
			if (MouseHovering) {
				drawTools.Rectangle(renderTarget, bounds, palette, ForegroundColor, GUIBrightness.Darkest);
			}

			if (m_selected) {
				drawTools.Rectangle(renderTarget, bounds, palette, ForegroundColor, GUIBrightness.Normal);
			}

			drawTools.Text(renderTarget, Vector2.Zero, textLayout, palette, TextColor, TextBrightness);

			if (m_isHighlighted) {
				renderTarget.DrawRectangle(bounds, palette.GetBrush(TextColor, GUIBrightness.Normal), 1, palette.StrokeStyles[DashStyle.Dash]);
			}
		}
	}

	public class Listbox : ActiveElement {
		private GUIPanel m_optionsPanel;
		private Scrollbar m_scrollbar;
		private float m_scrollbarWidth = 6.0F;
		private ListboxOption m_selectedOption;
		private bool m_dynamicHeight = false;
		private float m_minimumHeight = -1;
		private float m_maximumHeight = -1;
		private int m_highlightIndex = -1;

		/// <summary>
		/// Temporary variable to indicate if keyboard input on the whole application should move the highlight
		/// </summary>
		public bool AllowKeyboardSelect = false;

		private bool m_disableKeyboardSelect = true;


		public override GUIPadding Padding {
			get => m_optionsPanel.Padding;
			set => m_optionsPanel.Padding = value;
		}

		public bool DrawBorder = false;
		public bool DrawBackground = true;
		public bool AllowSelect = false;

		/// <summary>
		/// Maximum height of the control in pixels if UseDynamicHeight is set
		/// </summary>
		public float MaximumHeight {
			get => m_maximumHeight;
			set => m_maximumHeight = value;
		}

		/// <summary>
		/// Minimum height of the control in pixels if UseDynamicHeight is set
		/// </summary>
		public float MinimumHeight {
			get => m_minimumHeight;
			set => m_minimumHeight = value;
		}

		public bool UseDynamicHeight {
			get => m_dynamicHeight;
			set {
				m_dynamicHeight = value;
				if (m_dynamicHeight) {
					if (m_minimumHeight < 0) {
						m_minimumHeight = Height;
					}

					if (m_maximumHeight < 0) {
						m_maximumHeight = Height;
					}
				}
			}
		}

		public float ScrollbarWidth {
			get => m_scrollbarWidth;
			set => m_scrollbarWidth = value;
		}

		public float DefaultOptionHeight = -1;
		public bool DisplayNoOptionsOnEmpty = true;
		public bool DeselectOnFocusLost = true;

		public ListboxOption SelectedOption => m_selectedOption;

		public event ListboxOptionEventhandler OptionClicked;
		public event ListboxOptionEventhandler OptionSelected;
		public event ListboxOptionEventhandler HighlightChanged;
		public event ListboxOptionEventhandler OptionDoubleClicked;

		protected virtual void OnOptionClicked(ListboxOption option) => OptionClicked?.Invoke(option);
		protected virtual void OnOptionSelected(ListboxOption option) => OptionSelected?.Invoke(option);
		protected virtual void OnHightlightOptionChanged(ListboxOption option) => HighlightChanged?.Invoke(option);
		protected virtual void OnOptionDoubleClick(ListboxOption option) => OptionDoubleClicked?.Invoke(option);

		public Listbox() {
			//m_scrollbarWidth = 6.0F;
			m_optionsPanel = Append(new GUIPanel {
				Padding = new GUIPadding(3, 3, 3, 3),
				CanReceiveMouseInput = false,
			});
			m_scrollbar = Append(new Scrollbar(GUIDirection.Vertical) {
				Enabled = false,
				//Padding = m_optionsPanel.Padding
			});
			m_scrollbar.ValueChanged += scroll => {
				// Floored value or else the text can look a bit weird since rendering text between 0 and 1 can be a bit wobbely
				m_optionsPanel.RenderOffset = new Vector2(0, -Mathf.Floor(scroll));
			};
			Text = "-";
			ParagraphAlignment = ParagraphAlignment.Center;
			TextAlignment = TextAlignment.Center;
		}

		protected override void OnBoundsChanged() {
			m_optionsPanel.Width = Width;
			m_optionsPanel.Height = Height;
			m_optionsPanel.X = 0;
			m_optionsPanel.Y = 0;

			m_scrollbar.Width = m_scrollbarWidth;
			m_scrollbar.Height = Height - Padding.Top - Padding.Bottom;
			m_scrollbar.X = Width - m_scrollbarWidth - Padding.Right;
			m_scrollbar.Y = Padding.Top;
			UpdateOptions();
			base.OnBoundsChanged();
		}

		protected override void OnPreUpdate() {
			if (AllowKeyboardSelect && !m_disableKeyboardSelect) {
				if (Input.RepeatKey(Key.Down)) {
					if (m_highlightIndex == -1 && m_selectedOption != null) {
						m_highlightIndex = OptionIndex(m_selectedOption);
					}

					m_highlightIndex++;
					m_highlightIndex = Mathf.Clamp(m_highlightIndex, 0, m_optionsPanel.ChildElements.Count - 1);
					var option = OptionFromIndex(m_highlightIndex);
					if (option != null) {
						option.Highlight();
						OnHightlightOptionChanged(option);
						ScrollToHighlight();
					}
				}

				if (Input.RepeatKey(Key.Up)) {
					if (m_highlightIndex == -1 && m_selectedOption != null) {
						m_highlightIndex = OptionIndex(m_selectedOption);
					}

					m_highlightIndex--;
					m_highlightIndex = Mathf.Clamp(m_highlightIndex, 0, m_optionsPanel.ChildElements.Count - 1);
					var option = OptionFromIndex(m_highlightIndex);
					if (option != null) {
						option.Highlight();
						OnHightlightOptionChanged(option);
						ScrollToHighlight();
					}
				}

				if (Input.KeyDown(Key.Return) || Input.KeyDown(Key.NumberPadEnter)) {
					var option = OptionFromIndex(m_highlightIndex);
					if (option != null) {
						option.Select();
						OnOptionClicked(option);
						OnOptionSelected(option);
					}
				}
			} else {
				m_highlightIndex = -1;
			}

			base.OnPreUpdate();
		}

		public T AddOption<T>(T option) where T : ListboxOption {
			AppendOption(option);
			UpdateOptions();
			return option;
		}

		public void ClearOptions() {
			m_optionsPanel.RemoveAllChildren();
			UpdateOptions();
		}

		public void AddOption<T>(ICollection<T> options) where T : ListboxOption {
			foreach (var option in options) {
				AppendOption(option);
			}

			UpdateOptions();
		}

		private void AppendOption(ListboxOption option) {
			m_optionsPanel.Append(option);
			if (DefaultOptionHeight > 0) {
				option.Height = DefaultOptionHeight;
			}

			option.Click += args => {
				if (AllowSelect) {
					SetSelected(option);
					OnOptionSelected(option);
				}

				OnOptionClicked(option);
			};
			option.DoubleClick += args => {
				OnOptionDoubleClick(option);
			};
		}

		public T RemoveOption<T>(T option, bool disposeOption = true) where T : ListboxOption {
			m_optionsPanel.Remove(option);
			if (disposeOption) {
				option?.Dispose();
			}

			if (m_selectedOption == option) {
				m_selectedOption = null;
			}

			UpdateOptions();
			ToggleRedraw();
			return option;
		}

		public ListboxOption OptionFromIndex(int index) {
			var i = 0;
			foreach (var el in m_optionsPanel.ChildElements) {
				if (el is ListboxOption option) {
					if (i == index) {
						return option;
					}

					i++;
				}
			}

			return null;
		}

		public int OptionIndex(ListboxOption option) {
			var i = 0;
			foreach (var el in m_optionsPanel.ChildElements) {
				if (el is ListboxOption target) {
					if (target == option) {
						return i;
					}

					i++;
				}
			}

			return -1;
		}

		public void DisableKeyboardSelect() {
			m_disableKeyboardSelect = true;
		}

		public void EnableKeyboardSelect() {
			m_disableKeyboardSelect = false;
		}

		protected override void OnContainFocusGained() {
			if (m_disableKeyboardSelect == false) {
				AllowKeyboardSelect = true;
			}

			base.OnContainFocusGained();
		}


		protected override void OnContainFocusLost() {
			if (DeselectOnFocusLost) {
				if (AllowSelect) Deselect();
				if (m_disableKeyboardSelect == false) {
					AllowKeyboardSelect = false;
					RemoveHighlight();
				}
			}

			base.OnContainFocusLost();
		}

		protected override void OnMouseWheel(float delta) {
			if (m_scrollbar.Enabled) {
				var scrollDelta = 12.0F;
				if (DefaultOptionHeight > -1) {
					scrollDelta = DefaultOptionHeight;
				}

				if (delta > 0) {
					m_scrollbar.Value -= scrollDelta * Input.MouseWheelScrollLines;
				} else {
					m_scrollbar.Value += scrollDelta * Input.MouseWheelScrollLines;
				}

				m_optionsPanel.RenderOffset = new Vector2(0, -m_scrollbar.Value);
			}

			base.OnMouseWheel(delta);
		}

		public void Deselect() {
			foreach (var guiElement in m_optionsPanel.ChildElements) {
				if (guiElement is ListboxOption option) {
					option.Deselect();
				}
			}

			m_selectedOption = null;
		}

		public void RemoveHighlight() {
			foreach (var guiElement in m_optionsPanel.ChildElements) {
				if (guiElement is ListboxOption option) {
					option.RemoveHighlight();
				}
			}
		}

		public void SetSelected(ListboxOption option) {
			if (AllowSelect) {
				if (option != null) {
					option.Select();
					m_selectedOption = option;
				}
			}

			m_highlightIndex = -1;
		}

		protected override void OnRender(RenderTarget renderTarget, RectangleF bounds, TextLayout textLayout, GUIColorPalette palette, GUIDrawTools drawTools) {
			if (DrawBackground) {
				drawTools.Rectangle(renderTarget, bounds, palette, BackgroundColor, GUIBrightness.Normal);
			}

			if (DrawBorder) {
				drawTools.Border(renderTarget, bounds, palette, ForegroundColor, GUIBrightness.Bright);
			}

			if (DisplayNoOptionsOnEmpty && m_optionsPanel.ChildElements.Count(element => element.Visible) == 0) {
				drawTools.Text(renderTarget, Vector2.Zero, textLayout, palette, TextColor, TextBrightness);
			}
		}

		private void ScrollToHighlight() {
			if (m_highlightIndex != -1 && m_scrollbar.Enabled) {
				var option = OptionFromIndex(m_highlightIndex);
				if (option != null) {
					var panelBounds = m_optionsPanel.Bounds;
					m_optionsPanel.Padding.ResizeRectangle(ref panelBounds);

					var scrollTo = option.Bottom - panelBounds.Height;
					var scrollTop = option.Top - panelBounds.Top;

					if (scrollTo > m_scrollbar.Value) {
						m_scrollbar.Value = scrollTo;
						m_scrollbar.InvokeValueChanged();
					}

					if (scrollTop < m_scrollbar.Value) {
						m_scrollbar.Value = scrollTop;
						m_scrollbar.InvokeValueChanged();
					}
				}
			}
		}

		public void FilterOptions(Predicate<ListboxOption> predicate) {
			foreach (var option in m_optionsPanel.GetChildrenOfType<ListboxOption>()) {
				option.Visible = predicate.Invoke(option);
			}

			UpdateOptions();
		}

		private void UpdateOptions() {
			var panelBounds = m_optionsPanel.Bounds;
			m_optionsPanel.Padding.ResizeRectangle(ref panelBounds);

			// If scroll enabled, downsize padded panel width to compensate for it
			var totalOptionHeight = m_optionsPanel.ChildElements.Sum(element => element.Visible ? element.Height : 0);


			if (m_dynamicHeight) {
				var target = totalOptionHeight + m_optionsPanel.Padding.Top + m_optionsPanel.Padding.Bottom;
				target = Mathf.Clamp(target, m_minimumHeight, m_maximumHeight);
				Height = target;
				m_optionsPanel.Height = target;
			}


			// Enable scroll if option height exceeds panel height
			if (totalOptionHeight > panelBounds.Height) {
				m_scrollbar.Enabled = true;
				m_scrollbar.MaxValue = totalOptionHeight - panelBounds.Height;
				// Clamp render offset to total height
				if (-m_optionsPanel.RenderOffset.Y > totalOptionHeight - panelBounds.Height && m_scrollbar.Enabled) {
					//m_scrollbar.InvokeValueChanged();
					m_optionsPanel.RenderOffset = new Vector2(0, -(totalOptionHeight - panelBounds.Height));
				}

				// Resize bounds to fit in scrollbar
				panelBounds.Width -= m_scrollbar.Width + Padding.Right;
			} else {
				// All options fits, disable scroll and reset render offset
				m_scrollbar.Enabled = false;
				m_optionsPanel.RenderOffset = Vector2.Zero;
			}


			var yOffset = panelBounds.Top;
			foreach (var option in m_optionsPanel.ChildElements) {
				if (!option.Visible) continue;
				option.Width = panelBounds.Width;
				option.X = panelBounds.X;
				option.Y = yOffset;
				yOffset += option.Height;
			}
		}
	}
}