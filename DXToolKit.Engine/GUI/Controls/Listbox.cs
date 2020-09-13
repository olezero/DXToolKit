using System;
using System.Collections.Generic;
using System.Linq;
using DXToolKit;
using DXToolKit.Engine;
using DXToolKit.GUI;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectInput;
using SharpDX.DirectWrite;
using GUIElement = DXToolKit.Engine.GUIElement;
using TextAlignment = SharpDX.DirectWrite.TextAlignment;

namespace DXToolKit.Engine {
	/// <summary>
	/// Simple scrollable listbox
	/// </summary>
	public class Listbox<T> : GUIElement where T : GUIElement {
		private class OptionsPanel : Panel {
			protected override void OnRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) {
				tools.Background.Rectangle();
				tools.Shine(drawParameters.Bounds, true);
			}
		}

		private readonly Label m_nooptionsLabel;
		private readonly OptionsPanel m_optionsPanel;
		private readonly Scrollbar m_scrollbar;
		private readonly List<T> m_options;
		private readonly List<int> m_selectedOptions;
		private bool m_useDynamicHeight = true;
		private float m_maxDynamicHeight = 256.0F;
		private float m_minDynamicHeight = 20.0F;
		private float m_scrollbarWidth = 8.0F;
		private bool m_selectable = false;
		private bool m_multiselect = false;
		private bool m_highlightSelected = true;
		private bool m_highlightHoverOption = true;
		private bool m_allowKeyboardSelectiong = false;
		private int m_keyboardHighlightOption = -1;

		/// <summary>
		/// Event handler for when the user clicks on a option
		/// </summary>
		public delegate void OptionSelectEventHandler(T option);

		/// <summary>
		/// Event invoked when a option is selected
		/// </summary>
		public event OptionSelectEventHandler OptionSelected;

		/// <summary>
		/// Event invoked when a option is deselected
		/// </summary>
		public event OptionSelectEventHandler OptionDeselected;

		/// <summary>
		/// Event invoked when a option is clicked
		/// </summary>
		public event OptionSelectEventHandler OptionClicked;

		/// <summary>
		/// Event invoked when a option is double clicked
		/// </summary>
		public event OptionSelectEventHandler OptionDoubleClicked;

		/// <summary>
		/// Invoked when a option is selected
		/// </summary>
		/// <param name="option">The selected option</param>
		protected virtual void OnOptionSelected(T option) => OptionSelected?.Invoke(option);

		/// <summary>
		/// Invoked when a option is deselected
		/// </summary>
		/// <param name="option">The option</param>
		protected virtual void OnOptionDeselected(T option) => OptionDeselected?.Invoke(option);

		/// <summary>
		/// Invoked when a option is double clicked
		/// </summary>
		/// <param name="option">The option</param>
		protected virtual void OnOptionDoubleClicked(T option) => OptionDoubleClicked?.Invoke(option);

		/// <summary>
		/// Invoked when a option is clicked
		/// </summary>
		/// <param name="option"></param>
		protected virtual void OnOptionClicked(T option) => OptionClicked?.Invoke(option);

		/// <summary>
		/// Gets a list of all options in the list
		/// </summary>
		public List<T> Options => m_options;

		/// <summary>
		/// Gets a list over selected indices
		/// </summary>
		public List<int> SelectedIndices => m_selectedOptions;

		/// <summary>
		/// Gets a reference to the no options label that is displayed when there are no options in the listbox
		/// </summary>
		public Label NoOptionsLabel => m_nooptionsLabel;

		/// <summary>
		/// Gets a reference to the scrollbar used when options height exceed the total height of the listbox
		/// </summary>
		public Scrollbar Scrollbar => m_scrollbar;

		/// <summary>
		/// Gets all selected options
		/// </summary>
		/// <returns>List of all selected options</returns>
		public List<T> GetSelectedOptions() {
			var result = new List<T>();
			if (m_selectedOptions.Count > 0) {
				result.AddRange(m_selectedOptions.Select(t => m_options[t]));
			}

			return result;
		}

		/// <summary>
		/// Gets or sets a value that controls the scroll bar width
		/// </summary>
		public float ScrollbarWidth {
			get => m_scrollbarWidth;
			set => m_scrollbarWidth = value;
		}

		/// <summary>
		/// Gets or sets a value indicating if the listbox should resize dynamically based on current option count 
		/// </summary>
		public bool UseDynamicHeight {
			get => m_useDynamicHeight;
			set => m_useDynamicHeight = value;
		}

		/// <summary>
		/// Gets or sets a value indicating the maximum height allowed when UseDynamicHeight is true
		/// </summary>
		public float MaxDynamicHeight {
			get => m_maxDynamicHeight;
			set => m_maxDynamicHeight = value;
		}

		/// <summary>
		/// Gets or sets a value indicating the minimum height allowed when UseDynamicHeight is true
		/// </summary>
		public float MinDynamicHeight {
			get => m_minDynamicHeight;
			set => m_minDynamicHeight = value;
		}

		/// <summary>
		/// Gets or sets a value indicating if options can be selected
		/// </summary>
		public bool Selectable {
			get => m_selectable;
			set => m_selectable = value;
		}

		/// <summary>
		/// Gets or sets a value if multiple options can be selected (clicking while holding the control key)
		/// </summary>
		public bool Multiselect {
			get => m_multiselect && m_selectable;
			set => m_multiselect = value;
		}

		/// <summary>
		/// Gets or sets a value indicating if selected elements should be highlighted
		/// </summary>
		public bool HighlightSelected {
			get => m_highlightSelected;
			set => m_highlightSelected = value;
		}

		/// <summary>
		/// Gets or sets a value indicating if options should be highlighted when the mouse hovers over them
		/// </summary>
		public bool HighlightHoverOption {
			get => m_highlightHoverOption;
			set => m_highlightHoverOption = value;
		}

		/// <summary>
		/// Gets or sets a value indicating if the user can use up down arrows to select options
		/// </summary>
		public bool AllowKeyboardSelectiong {
			get => m_allowKeyboardSelectiong;
			set => m_allowKeyboardSelectiong = value;
		}

		/// <summary>
		/// Gets or sets a value indicating if the current selection should be reset if focus is lost
		/// </summary>
		public bool ClearSelectionOnFocusLost = true;


		/// <inheritdoc />
		protected override float MinimumWidth => 24;

		/// <inheritdoc />
		protected override float MinimumHeight => 24;

		/// <summary>
		/// Creates a new listbox
		/// </summary>
		public Listbox() {
			m_options = new List<T>();
			m_selectedOptions = new List<int>();
			m_optionsPanel = Append(new OptionsPanel());
			m_nooptionsLabel = m_optionsPanel.Append(new Label("-") {
				TextAlignment = TextAlignment.Center,
				ParagraphAlignment = ParagraphAlignment.Center,
			});
			m_scrollbar = Append(new Scrollbar(GUIDirection.Vertical));
			m_scrollbar.ValueChanged += f => {
				m_optionsPanel.RenderOffset = new Vector2(0, Mathf.Round(-f));
			};
			m_scrollbar.Visible = false;
			PositionChildren();

			InnerGlow.Size = 1.0F;
			InnerGlow.Opacity = 0.2F;
		}

		/// <inheritdoc />
		protected override void OnPreUpdate() {
			if (ContainsFocus && m_allowKeyboardSelectiong) {
				if (Input.RepeatKey(Key.Up)) {
					if (m_selectedOptions.Count > 0 && m_keyboardHighlightOption == -1) {
						// Start at lowest selected option index
						var first = m_selectedOptions.OrderBy(i => i).FirstOrDefault();
						m_keyboardHighlightOption = first - 1;
					} else {
						m_keyboardHighlightOption -= 1;
					}

					if (m_keyboardHighlightOption < 0) {
						m_keyboardHighlightOption = 0;
					}

					ScrollToKeyboardHighlight();
					ToggleRedraw();
				}

				if (Input.RepeatKey(Key.Down)) {
					if (m_selectedOptions.Count > 0 && m_keyboardHighlightOption == -1) {
						// Start at highest selected option index
						var last = m_selectedOptions.OrderBy(i => i).LastOrDefault();
						m_keyboardHighlightOption = last + 1;
					} else {
						m_keyboardHighlightOption += 1;
					}

					if (m_keyboardHighlightOption >= m_options.Count) {
						m_keyboardHighlightOption = m_options.Count - 1;
					}

					ScrollToKeyboardHighlight();
					ToggleRedraw();
				}

				if (Input.KeyDown(Key.NumberPadEnter) || Input.KeyDown(Key.Return)) {
					if (m_keyboardHighlightOption != -1 && m_options.Count >= m_keyboardHighlightOption - 1) {
						SelectOption(m_options[m_keyboardHighlightOption]);
					}
				}
			} else {
				m_keyboardHighlightOption = -1;
			}

			base.OnPreUpdate();
		}

		/// <summary>
		/// Sets RenderOffset so that the current option highlighted by the keyboard is in view
		/// </summary>
		private void ScrollToKeyboardHighlight() {
			if (m_keyboardHighlightOption >= 0 && m_keyboardHighlightOption < m_options.Count) {
				var opt = m_options[m_keyboardHighlightOption];
				var scrollValue = m_scrollbar.Value;
				// If we can move to render offset 0 and highlight is still in view, do it

				if (opt.Top < scrollValue) {
					m_scrollbar.Value = opt.Top;
				}

				if (opt.Bottom > scrollValue + m_optionsPanel.Height) {
					m_scrollbar.Value = opt.Bottom - m_optionsPanel.Height;
				}
			}
		}

		/// <summary>
		/// Adds a option to the listbox
		/// </summary>
		/// <param name="option">The option to add</param>
		/// <param name="enableOptionMouseInput">A toggle if the element should be forced to receive mouse input</param>
		/// <returns>The option</returns>
		public T AddOption(T option, bool enableOptionMouseInput = true) {
			if (enableOptionMouseInput) {
				option.CanReceiveMouseInput = true;
				option.MouseEnter += option.ToggleRedraw;
				option.MouseLeave += option.ToggleRedraw;
			}

			ClearSelection();
			m_optionsPanel.Append(option);
			m_options.Add(option);
			option.Click += args => {
				if (m_selectable) {
					SelectOption(option);
				}

				OnOptionClicked(option);
			};
			option.DoubleClick += args => {
				OnOptionDoubleClicked(option);
			};
			PositionChildren();
			return option;
		}

		/// <summary>
		/// Removes an option from the list
		/// </summary>
		/// <param name="option">The option to remove</param>
		/// <param name="dispose">If the option should be disposed when removed</param>
		/// <returns>The removed option</returns>
		public T RemoveOption(T option, bool dispose = true) {
			ClearSelection();
			m_optionsPanel.Remove(option, dispose);
			m_options.Remove(option);
			PositionChildren();
			return option;
		}

		/// <summary>
		/// Removes all options from the listbox
		/// </summary>
		/// <param name="dispose">If options that are removed should be disposed</param>
		public void RemoveAllOptions(bool dispose = true) {
			ClearSelection();
			m_optionsPanel.RemoveAllChildren();
			m_options.Clear();
			PositionChildren();
			ToggleRedraw();
		}

		/// <summary>
		/// Scrolls to the top of the listbox
		/// </summary>
		public void ScrollToTop() {
			m_optionsPanel.RenderOffset = new Vector2(0, 0);
			m_scrollbar.Value = 0;
		}

		/// <summary>
		/// Selects all options in the listbox.
		/// Note: Selectable and Multiselect must be true to allow this
		/// </summary>
		/// <param name="runEvents">If OnOptionSelected events should run</param>
		public void SelectAll(bool runEvents = true) {
			// Make sure that multiselect and select is set
			if (m_selectable && m_multiselect) {
				// Remove selection without raising events, since already selected options will be selected again
				ClearSelection(false);
				for (int i = 0; i < m_options.Count; i++) {
					// Add option to selection index
					m_selectedOptions.Add(i);
					// Run on select if requested
					if (runEvents) OnOptionSelected(m_options[i]);
				}
			}

			m_keyboardHighlightOption = -1;
		}

		/// <summary>
		/// Selects the input option if its part of the listbox
		/// </summary>
		/// <param name="option">The option to select</param>
		/// <param name="runEvents">If normal events should run</param>
		public void SelectOption(T option, bool runEvents = true) {
			// No select, return
			if (!m_selectable) return;

			var index = m_options.IndexOf(option);
			if (index != -1) {
				if (Input.KeyPressed(Key.RightControl) || Input.KeyPressed(Key.LeftControl) && m_multiselect) {
					// Multi select
					if (m_selectedOptions.Contains(index)) {
						m_selectedOptions.Remove(index);
						if (runEvents) OnOptionDeselected(option);
					} else {
						m_selectedOptions.Add(index);
						if (runEvents) OnOptionSelected(option);
					}
				} else {
					// No shift, clear and add
					ClearSelection(runEvents);
					m_selectedOptions.Add(index);
					if (runEvents) OnOptionSelected(option);
				}

				ToggleRedraw();
			}

			m_keyboardHighlightOption = -1;
		}

		/// <summary>
		/// Clears any selected options
		/// </summary>
		/// <param name="runEvents">If OnOptionDeselected events should be invoked</param>
		public void ClearSelection(bool runEvents = true) {
			if (m_selectable && runEvents) {
				for (int i = 0; i < m_selectedOptions.Count; i++) {
					var optIndex = m_selectedOptions[i];
					if (m_options.Count >= optIndex - 1) {
						OnOptionDeselected(m_options[optIndex]);
					}
				}
			}

			m_selectedOptions.Clear();
			ToggleRedraw();
		}


		/// <inheritdoc />
		protected override void OnBoundsChangedDirect() {
			m_nooptionsLabel.Width = m_optionsPanel.Width;
			m_nooptionsLabel.Height = m_optionsPanel.Height;
			PositionChildren();
			base.OnBoundsChangedDirect();
		}

		/// <inheritdoc />
		protected override void OnMouseWheel(float delta) {
			if (m_scrollbar.Visible) m_scrollbar.Scroll(delta);
			base.OnMouseWheel(delta);
		}

		/// <inheritdoc />
		protected override void OnRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) {
			m_nooptionsLabel.Enabled = m_options.Count == 0;
			tools.Background.Rectangle(GUIBrightness.Darkest);
		}

		/// <inheritdoc />
		protected override void OnContainFocusGained() {
			base.OnContainFocusGained();
			if (ClearSelectionOnFocusLost) {
				ClearSelection(false);
			}
		}

		/// <inheritdoc />
		protected override void OnContainFocusLost() {
			base.OnContainFocusLost();
			if (ClearSelectionOnFocusLost) {
				ClearSelection(false);
			}
		}

		/// <inheritdoc />
		protected override void PostRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) {
			// If no highlights, just return 
			if (m_highlightSelected == false && m_highlightHoverOption == false && m_allowKeyboardSelectiong == false) return;
			var thisScreenBounds = m_optionsPanel.ScreenBounds;
			for (var i = 0; i < m_options.Count; i++) {
				var b = m_options[i].ScreenBounds;
				b.Location = ScreenToLocal(b.Location);

				// Simple culling to only handle option that are actually displayed
				if (b.Bottom < 0 || b.Top > thisScreenBounds.Bottom) {
					continue;
				}

				// Quick controller for disabling mouse hover highlight if selected
				var selected = false;

				// Highlight selected
				if (m_highlightSelected) {
					if (m_selectedOptions.Contains(i)) {
						tools.TransparentRectangle(b, GUIColor.Text, GUIBrightness.Normal);
						selected = true;
					}
				}

				// Highlight mouse over
				if (m_highlightHoverOption) {
					if (m_options[i].MouseHovering && !selected) {
						tools.TransparentRectangle(b, GUIColor.Text, GUIBrightness.Darkest);
					}
				}

				if (i == m_keyboardHighlightOption && m_allowKeyboardSelectiong) {
					var strokeStyle = GUIColorPalette.Current.StrokeStyles[DashStyle.Dash];
					var brush = GUIColorPalette.Current[GUIColor.Light, GUIBrightness.Normal];
					b.Inflate(-1, -1);
					drawParameters.RenderTarget.DrawRectangle(b, brush, 1, strokeStyle);
					strokeStyle = null;
					brush = null;
				}
			}
		}

		/// <summary>
		/// Custom enumerator for the options so that the user can run filters / OrderBy etc on options list
		/// </summary>
		private Func<List<T>, IEnumerable<T>> m_optionsEnumerable;

		/// <summary>
		/// Sets a custom enumerator function on the list allowing for usage of all Linq functionality to filter/order the list
		/// </summary>
		/// <param name="func">Function that should be run when listbox positions all its child options</param>
		public void SetEnumerator(Func<List<T>, IEnumerable<T>> func) {
			m_optionsEnumerable = func;
			PositionChildren();
		}

		/// <summary>
		/// Gets the current IEnumerable of the options
		/// </summary>
		private IEnumerable<T> GetOrderedCollection() {
			return m_optionsEnumerable != null ? m_optionsEnumerable(m_options) : m_options;
		}

		private void PositionChildren() {
			var totalOptionsHeight = m_options.Sum(option => option.Height);

			if (m_nooptionsLabel != null) {
				m_nooptionsLabel.Width = Width;
				m_nooptionsLabel.Height = Height;
				m_nooptionsLabel.X = 0;
				m_nooptionsLabel.Y = 0;
				m_nooptionsLabel.TextAlignment = TextAlignment.Center;
				m_nooptionsLabel.ParagraphAlignment = ParagraphAlignment.Center;
			}


			m_scrollbar.Visible = false;
			m_scrollbar.Width = m_scrollbarWidth;
			m_scrollbar.Height = Height - 4;
			m_scrollbar.Left = Width - m_scrollbar.Width - 2;
			m_scrollbar.Top = 2;

			m_optionsPanel.Width = Width - 4;
			m_optionsPanel.Height = Height - 4;
			m_optionsPanel.X = 2;
			m_optionsPanel.Y = 2;

			if (m_useDynamicHeight) {
				if (totalOptionsHeight < m_maxDynamicHeight) {
					if (totalOptionsHeight > 0) {
						m_optionsPanel.Height = totalOptionsHeight < m_minDynamicHeight ? m_minDynamicHeight : totalOptionsHeight;
					} else {
						m_optionsPanel.Height = m_minDynamicHeight;
					}
				} else {
					m_optionsPanel.Height = m_maxDynamicHeight;
				}
			}

			if (totalOptionsHeight > m_optionsPanel.Height) {
				m_scrollbar.Visible = true;
				m_scrollbar.MaxValue = totalOptionsHeight - m_optionsPanel.Height;
				m_scrollbar.ScrollStepCount = Mathf.Max(10, m_options.Count / 2.0F);
				m_scrollbar.ToggleRedraw();
				m_optionsPanel.Width = Width - m_scrollbar.Width - 6;
			}


			var yOffset = 0.0F;
			foreach (var option in GetOrderedCollection()) {
				option.X = 0;
				option.Y = yOffset;
				option.Width = m_optionsPanel.Width;
				yOffset += option.Height;
			}

			// need to scroll up so the lowest element is visible
			var maxOffset = -(yOffset - Height + 4);
			if (maxOffset < 0) {
				if (m_optionsPanel.RenderOffset.Y < maxOffset) {
					m_optionsPanel.RenderOffset = new Vector2(0, maxOffset);
				}
			} else {
				m_optionsPanel.RenderOffset = new Vector2(0, 0);
			}

			Height = m_optionsPanel.Height + 4;
		}
	}
}