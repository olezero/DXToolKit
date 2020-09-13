using System.Collections.Generic;
using DXToolKit.GUI;
using SharpDX;
using SharpDX.DirectWrite;

namespace DXToolKit.Engine {
	/// <summary>
	/// Standard combobox.
	/// A combobox can be a bit iffy with z-indexing if left open
	/// </summary>
	public class Combobox<T> : GUIElement where T : GUIElement {
		private Listbox<T> m_comboList;
		private T m_selectedOption;
		private ArrowIcon m_downArrow;
		private ArrowIcon m_upArrow;

		/// <summary>
		/// Gets the listbox that represents the dropdown of the combobox
		/// </summary>
		public Listbox<T> ComboList => m_comboList;

		/// <summary>
		/// Gets a reference to the selected option
		/// </summary>
		public T SelectedOption => m_selectedOption;

		/// <summary>
		/// Gets a reference to the options stored in the underlying listbox
		/// </summary>
		public List<T> Options => m_comboList.Options;

		/// <summary>
		/// Gets or sets a value indicating if the combo box should close when it looses focus
		/// Default: True
		/// </summary>
		public bool CloseOnFocusLost = true;

		/// <summary>
		/// Gets or sets a value indicating if the dropdown list should fade in/out
		/// Default: True
		/// </summary>
		public bool FadeOpenClose = true;

		/// <summary>
		/// Gets or sets a value indicating the time the fade animation should take in milliseconds
		/// Default: 50.0F
		/// </summary>
		public float FadeOpenCloseTime = 50.0F;

		/// <summary>
		/// Sets a value indicating if the arrow icon should be visible
		/// </summary>
		public bool UseArrowIcon {
			set {
				m_downArrow.Enabled = value;
				m_upArrow.Enabled = value;
			}
		}

		/// <summary>
		/// Event handler for when the value in the combobox changes
		/// </summary>
		/// <param name="option"></param>
		public delegate void ComboboxValueChangeHandler(T option);

		/// <summary>
		/// Invoked when the user clicks on one of the options in the combobox and its not the same as the one already selected
		/// </summary>
		public event ComboboxValueChangeHandler ValueChanged;

		/// <summary>
		/// Invoked when the user clicks on one of the options in the combobox, even if its the same as the selected option
		/// </summary>
		public event ComboboxValueChangeHandler OptionClicked;

		/// <summary>
		/// Invoked when the user clicks on one of the options in the combobox and its not the same as the one already selected
		/// </summary>
		protected virtual void OnValueChanged(T option) => ValueChanged?.Invoke(option);

		/// <summary>
		/// Invoked when the user clicks on one of the options in the combobox, even if its the same as the selected option
		/// </summary>
		protected virtual void OnOptionClicked(T option) => OptionClicked?.Invoke(option);

		/// <inheritdoc />
		public Combobox() {
			m_comboList = new Listbox<T>();
			m_downArrow = Append(new ArrowIcon(180, ArrowType.FilledTriangle));
			m_upArrow = Append(new ArrowIcon(0, ArrowType.FilledTriangle) {Visible = false});

			Width = 12 * 12;
			Height = 12 * 2;
			m_comboList.UseDynamicHeight = true;
			TextAlignment = TextAlignment.Leading;
			ParagraphAlignment = ParagraphAlignment.Center;
			Text = "-";
			TextOffset = new Vector2(4, 0);

			m_comboList.OptionClicked += option => {
				SelectOption(option);
				Close();
			};
			m_comboList.Visible = false;

			m_upArrow.IconOpacity = m_downArrow.IconOpacity = 0.8F;

			// Add handler if combo list looses focus.
			// Edge case if the user has interacted with the scrollbar and clicks another element outside the scope of this, since the combo list is a floating element
			m_comboList.ContainFocusLost += () => {
				if (CloseOnFocusLost && !ContainsFocus && !m_comboList.ContainsFocus) Close();
			};

			Close();
		}

		/// <inheritdoc />
		protected override void OnLateUpdate() {
			if (m_comboList.Visible) {
				m_comboList.X = ScreenBounds.X;
				m_comboList.Y = ScreenBounds.Bottom;
				if (!CloseOnFocusLost) {
					m_comboList.MoveToFront();
				}
			}

			base.OnLateUpdate();
		}

		/// <inheritdoc />
		protected override void OnMouseEnter() {
			m_upArrow.IconOpacity = m_downArrow.IconOpacity = 1.0F;
			base.OnMouseEnter();
		}

		/// <inheritdoc />
		protected override void OnMouseLeave() {
			m_upArrow.IconOpacity = m_downArrow.IconOpacity = 0.8F;
			base.OnMouseLeave();
		}

		/// <inheritdoc />
		protected override void OnParentSet(DXToolKit.GUI.GUIElement parent, DXToolKit.GUI.GUIElement child) {
			base.OnParentSet(parent, child);
			BaseElement?.Append(m_comboList);
		}

		/// <inheritdoc />
		protected override void OnParentUnset(DXToolKit.GUI.GUIElement parent, DXToolKit.GUI.GUIElement child) {
			base.OnParentUnset(parent, child);
			// Can end up here when disposing of everything.
			// Make sure nothing is disposing before trying to append back
			if (IsDisposing == false && m_comboList != null && m_comboList.IsDisposing == false) {
				Append(m_comboList);
			}
		}

		/// <inheritdoc />
		protected override void OnDispose() {
			base.OnDispose();
			Utilities.Dispose(ref m_comboList);
		}

		/// <inheritdoc />
		protected override void OnBoundsChangedDirect() {
			m_comboList.X = ScreenBounds.X;
			m_comboList.Y = ScreenBounds.Y + Height;
			m_comboList.Width = Width;
			m_comboList.Height = Height;

			var iconSize = Mathf.Floor(Height / 2.0F);
			m_downArrow.X = Width - iconSize - 4.0F;
			m_downArrow.Y = Height / 2.0F - iconSize / 2.0F;
			m_downArrow.Width = iconSize * 1.2F;
			m_downArrow.Height = iconSize;
			m_upArrow.Bounds = m_downArrow.Bounds;
			base.OnBoundsChangedDirect();
		}

		/// <inheritdoc />
		protected override void OnRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) {
			tools.Background.Rectangle();
			tools.Background.Border();
			tools.Shine();
			tools.Text();
		}

		/// <inheritdoc />
		protected override void OnClick(GUIMouseEventArgs args) {
			if (m_comboList.Visible) {
				Close();
			} else {
				Open();
			}

			base.OnClick(args);
		}

		private void SelectOption(T option) {
			if (m_comboList.Options.Contains(option)) {
				if (option != m_selectedOption) {
					OnValueChanged(option);
				}

				OnOptionClicked(option);
				m_selectedOption = option;
				Text = option.Text;
			}
		}

		/// <summary>
		/// Opens the combobox
		/// </summary>
		public void Open() {
			// When opening make sure root is the correct root
			if (m_comboList.Parent != BaseElement) {
				BaseElement?.Append(m_comboList);
			}

			if (m_comboList.Visible == false) {
				m_comboList.Visible = true;
				m_comboList.MoveToFront();
				MoveToFront();
				m_comboList.Opacity = 1.0F;
				m_upArrow.Visible = m_comboList.Visible;
				m_downArrow.Visible = !m_comboList.Visible;

				if (FadeOpenClose) {
					Animation.AddAnimation(0, 1, FadeOpenCloseTime, (from, to, amount) => {
							m_comboList.Opacity = Mathf.Lerp(from, to, amount);
							return true;
						}
					);
				}
			}
		}

		/// <summary>
		/// Closes the combobox
		/// </summary>
		public void Close() {
			// When opening make sure root is the correct root
			if (m_comboList.Parent != BaseElement) {
				BaseElement?.Append(m_comboList);
			}

			if (m_comboList.Visible) {
				if (FadeOpenClose) {
					Animation.AddAnimation(1, 0, FadeOpenCloseTime, (from, to, amount) => {
							m_comboList.Opacity = Mathf.Lerp(from, to, amount);
							return true;
						}, () => {
							m_comboList.Visible = false;
							ToggleRedraw();
							m_upArrow.Visible = m_comboList.Visible;
							m_downArrow.Visible = !m_comboList.Visible;
						}
					);
				} else {
					m_comboList.Visible = false;
					m_upArrow.Visible = m_comboList.Visible;
					m_downArrow.Visible = !m_comboList.Visible;
				}
			}
		}

		/// <inheritdoc />
		protected override void OnContainFocusLost() {
			if (CloseOnFocusLost) {
				if (!m_comboList.ContainsFocus && !ContainsFocus) Close();
			}

			base.OnContainFocusLost();
		}
	}
}