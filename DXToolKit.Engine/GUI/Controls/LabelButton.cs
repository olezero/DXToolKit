using System;
using DXToolKit;
using DXToolKit.Engine;
using DXToolKit.GUI;
using SharpDX;
using SharpDX.DirectWrite;


namespace DXToolKit.Engine {
	/// <summary>
	/// A button element that is just a label but with a bit more "flash"
	/// </summary>
	public class LabelButton : ActiveElement {
		private bool m_autoResizeHeight = true;
		private bool m_autoResizeWidth = true;
		private bool m_runSizeRecalc = true;

		private bool m_useColorAnimation = true;
		private float m_colorLerpAmount = 0.0F;
		private float m_colorLerpTime = 50.0F;

		/// <summary>
		/// Gets or sets a value indicating if the element should auto size width to fit the stored text
		/// Default: True
		/// </summary>
		public bool AutoResizeWidth {
			get => m_autoResizeWidth;
			set => m_autoResizeWidth = value;
		}

		/// <summary>
		/// Gets or sets a value indicating if the element should auto size height to fit the stored text
		/// Default: True
		/// </summary>
		public bool AutoResizeHeight {
			get => m_autoResizeHeight;
			set => m_autoResizeHeight = value;
		}

		/// <summary>
		/// Gets or sets the amount of time in milliseconds the color change should take when the element is hovered over.
		/// This requires that UseColorAnimation is true
		/// Default: 50ms
		/// </summary>
		public float ColorLerpTime {
			get => m_colorLerpTime;
			set => m_colorLerpTime = value;
		}

		/// <summary>
		/// Gets or sets a value indicating if the color should lerp from TextColor to ForegroundColor when the mouse hovers over the element
		/// Default: True
		/// </summary>
		public bool UseColorAnimation {
			get => m_useColorAnimation;
			set => m_useColorAnimation = value;
		}

		/// <summary>
		/// Creates a new label button
		/// </summary>
		/// <param name="text">The text of the button</param>
		public LabelButton(string text) {
			Text = text;
			CanReceiveMouseInput = true;
			CanReceiveFocus = true;
			CanReceiveKeyboardInput = true;
			FontWeight = FontWeight.Bold;
			TextAlignment = TextAlignment.Center;
			ParagraphAlignment = ParagraphAlignment.Center;
			FontSize = 22;

			// Set size to something small
			Width = Height = 1;
			// Recalculate size to set width and height to font size
			RecalcSize();
		}

		/// <summary>
		/// Creates a new label button
		/// </summary>
		/// <param name="text">The text of the button</param>
		/// <param name="onClick">Click event handler</param>
		public LabelButton(string text, Action<GUIMouseEventArgs> onClick) : this(text) {
			if (onClick != null) Click += onClick;
		}

		/// <inheritdoc />
		protected override void OnTextChanged(string text) {
			m_runSizeRecalc = true;
			base.OnTextChanged(text);
		}

		private void RecalcSize() {
			if (m_autoResizeWidth) {
				Width = Mathf.Max(Width, Mathf.Ceiling(FontCalculator.CalculateTextWidth(this)));
			}

			if (m_autoResizeHeight) {
				Height = Mathf.Max(Height, Mathf.Ceiling(FontCalculator.CalculateTextHeight(this)));
			}

			m_runSizeRecalc = false;
			ToggleRedraw();
		}


		/// <inheritdoc />
		protected override void OnMouseEnter() {
			if (m_useColorAnimation) {
				Animation.AddAnimation(m_colorLerpAmount, 1, m_colorLerpTime, (from, to, amount) => {
					m_colorLerpAmount = Mathf.Lerp(from, to, amount);
					ToggleRedraw();
					return true;
				});
			}

			base.OnMouseEnter();
		}

		/// <inheritdoc />
		protected override void OnMouseLeave() {
			if (m_useColorAnimation) {
				Animation.AddAnimation(m_colorLerpAmount, 0, m_colorLerpTime, (from, to, amount) => {
					m_colorLerpAmount = Mathf.Lerp(from, to, amount);
					ToggleRedraw();
					return true;
				});
			}

			base.OnMouseLeave();
		}

		/// <inheritdoc />
		protected override void OnRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) {
			if (m_runSizeRecalc) RecalcSize();
			if (m_useColorAnimation) {
				var palette = GUIColorPalette.Current;
				var lerpBrush = palette.LerpedBrush(
					palette[drawParameters.TextColor, drawParameters.TextBrightness],
					palette[drawParameters.ForegroundColor, GUIBrightness.Brightest],
					m_colorLerpAmount
				);
				tools.Text(drawParameters, lerpBrush);
			} else {
				if (MouseHovering) {
					tools.Text(Vector2.Zero, drawParameters.ForegroundColor, IsMousePressed ? GUIBrightness.Brightest : GUIBrightness.Normal);
				} else {
					tools.Text(Vector2.Zero, drawParameters.TextColor, GUIBrightness.Normal);
				}
			}
		}
	}
}