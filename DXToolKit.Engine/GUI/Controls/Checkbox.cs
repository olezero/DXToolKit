using System;
using DXToolKit.GUI;

namespace DXToolKit.Engine {
	/// <summary>
	/// Creates a simple checkbox
	/// </summary>
	public class Checkbox : ActiveElement {
		private IconElement m_checkIcon;
		private bool m_isChecked = false;

		/// <summary>
		/// Invoked when the value of the checkbox has changed
		/// </summary>
		public event Action<bool> ValueChanged;

		/// <summary>
		/// Invoked when the value of the checkbox has changed
		/// </summary>
		protected virtual void OnValueChanged(bool value) => ValueChanged?.Invoke(value);

		/// <summary>
		/// Gets or sets a value indicating if the checkbox is checked
		/// </summary>
		public bool Checked {
			get => m_isChecked;
			set {
				if (m_isChecked != value) {
					m_isChecked = value;
					m_checkIcon.Visible = m_isChecked;
					OnValueChanged(value);
					ToggleRedraw();
				}
			}
		}

		/// <summary>
		/// Gets or sets the color of the check icon
		/// </summary>
		public GUIColor IconColor {
			get => m_checkIcon.IconColor;
			set => m_checkIcon.IconColor = value;
		}

		/// <summary>
		/// Creates a new checkbox width a default icon (Cross)
		/// </summary>
		public Checkbox() : this(new CrossIcon()) { }

		/// <summary>
		/// Creates a new checkbox with a custom check icon
		/// </summary>
		public Checkbox(IconElement checkIcon) {
			m_checkIcon = Append(checkIcon);
			Width = 24;
			Height = 24;
			m_checkIcon.Width = Width;
			m_checkIcon.Height = Height;
			m_checkIcon.X = 0;
			m_checkIcon.Y = 0;
			m_checkIcon.Visible = m_isChecked;
		}

		/// <inheritdoc />
		protected override void OnBoundsChangedDirect() {
			m_checkIcon.Width = Width;
			m_checkIcon.Height = Height;
			m_checkIcon.X = 0;
			m_checkIcon.Y = 0;

			// Set border width based on icon recommended stroke width
			BorderWidth = Mathf.Max(Mathf.Floor(m_checkIcon.RecommendedStrokeWidth / 2.0F), 1.0F);
		}

		/// <inheritdoc />
		protected override void OnRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) {
			tools.Background.Rectangle(tools.Darken(drawParameters.Brightness));
			tools.Background.BevelBorder(true);
		}

		/// <inheritdoc />
		protected override void OnClick(GUIMouseEventArgs args) {
			m_isChecked = !m_isChecked;
			m_checkIcon.Visible = m_isChecked;
			OnValueChanged(m_isChecked);
			base.OnClick(args);
		}
	}
}