using System;
using DXToolKit;
using DXToolKit.Engine;
using SharpDX;
using SharpDX.DirectWrite;

namespace DXToolKit.Engine {
	/// <summary>
	/// Vertical/Horizontal scrollbar
	/// </summary>
	public class Scrollbar : ActiveElement {
		/// <summary>
		/// Value from 0 to 1
		/// </summary>
		private float m_value = 0;

		/// <summary>
		/// Min value controller
		/// </summary>
		private float m_minValue = 0;

		/// <summary>
		/// Max value controller
		/// </summary>
		private float m_maxValue = 100;

		/// <summary>
		/// Drag offset used to keep scroll element on mouse position if the user started dragging on top of the element
		/// </summary>
		private Vector2 m_dragoffset = Vector2.Zero;

		/// <summary>
		/// Amount of steps it takes to go from min to max on the scroll bar while using the mouse wheel
		/// </summary>
		private float m_scrollStepCount = 10.0F;

		/// <summary>
		/// Direction of the scroll bar
		/// </summary>
		private readonly GUIDirection m_direction;

		/// <summary>
		/// The bounds of the scroll element
		/// </summary>
		private RectangleF m_scrollbounds;

		/// <summary>
		/// Controller for rendering text on the scrollbar
		/// </summary>
		private bool m_renderValueText = false;

		/// <summary>
		/// Manual controller for highlighting the scroll element when dragged, since GUIElement.IsDragged seams one frame to slow
		/// </summary>
		private bool m_isDragged = false;

		/// <summary>
		/// Vertical label used to display value text in a vertical form
		/// </summary>
		private Label m_verticalLabel;

		/// <summary>
		/// Controller for the scroll element size, if less then 0 it wont be used
		/// </summary>
		private float m_scrollElementSize = -1.0F;

		/// <summary>
		/// Gets or sets a value indicating if the rendered value should render vertically on a vertical scrollbar
		/// </summary>
		public bool UseVerticalTextOnVerticalScroll = true;

		/// <summary>
		/// Gets or sets a value indicating how many digits the rendered text should round to when rendering
		/// </summary>
		public int DrawValueRoundingDigits = 2;

		/// <summary>
		/// Gets the direction of the scrollbar	
		/// </summary>
		public GUIDirection Direction => m_direction;

		/// <summary>
		/// Gets the bounds of the scroll element
		/// </summary>
		public RectangleF Scrollbounds => m_scrollbounds;

		/// <summary>
		/// Gets or sets a value indicating if the value of the scrollbar should be printed on the scrollbar
		/// </summary>
		public bool DrawValue {
			get => m_renderValueText;
			set {
				m_renderValueText = value;
				if (value) {
					SetTextFromValue();
				}
			}
		}

		/// <summary>
		/// Amount of steps from one end to the other when using the mouse wheel.
		/// Default: 10
		/// </summary>
		public float ScrollStepCount {
			get => m_scrollStepCount;
			set => m_scrollStepCount = value;
		}

		/// <summary>
		/// Gets or sets the min value of the scroll bar.
		/// Default: 0
		/// Note: If changed live, this will change the Value property, since that is just mapped from 0-1 to MinValue-MaxValue
		/// </summary>
		public float MinValue {
			get => m_minValue;
			set {
				// Get current interpretation of value based on stored min value
				var preValue = Value;
				// Update stored min value
				m_minValue = value;
				// Set value based on pre value based on new min value
				Value = preValue;
			}
		}

		/// <summary>
		/// Gets or sets the max value of the scroll bar.
		/// Default: 100
		/// Note: If changed live, this will change the Value property, since that is just mapped from 0-1 to MinValue-MaxValue
		/// </summary>
		public float MaxValue {
			get => m_maxValue;
			set {
				// Get current interpretation of value based on stored max value
				var preValue = Value;
				// Update stored max value
				m_maxValue = value;
				// Set value based on pre value based on new max value
				Value = preValue;
			}
		}

		/// <summary>
		/// Gets or sets the value of the scrollbar.
		/// </summary>
		public float Value {
			get => Mathf.Map(m_value, 0, 1, m_minValue, m_maxValue);
			set {
				m_value = Mathf.Clamp(Mathf.Map(value, m_minValue, m_maxValue, 0, 1), 0, 1);
				SetPositionFromValue();
				SetTextFromValue();
				OnValueChanged(Value);
			}
		}

		/// <summary>
		/// Gets or sets the scroll element size
		/// If less then 0 automatic scroll size will be used
		/// </summary>
		public float ScrollElementSize {
			get => m_scrollElementSize;
			set {
				if (Math.Abs(m_scrollElementSize - value) > 0.001F) {
					m_scrollElementSize = value;
					OnBoundsChangedDirect();
				}
			}
		}

		/// <inheritdoc />
		protected override float MinimumWidth => m_direction == GUIDirection.Vertical ? 4 : 12;

		/// <inheritdoc />
		protected override float MinimumHeight => m_direction == GUIDirection.Vertical ? 12 : 4;

		/// <summary>
		/// Invoked if value changes
		/// </summary>
		public event Action<float> ValueChanged;

		/// <summary>
		/// Invoked when value changes
		/// </summary>
		/// <param name="value">The new value</param>
		protected virtual void OnValueChanged(float value) => ValueChanged?.Invoke(value);

		/// <summary>
		/// Creates a new scrollbar
		/// </summary>
		/// <param name="direction">The direction of the scrollbar</param>
		public Scrollbar(GUIDirection direction) {
			// Store direction
			m_direction = direction;

			// Set some default sizes
			if (m_direction == GUIDirection.Vertical) {
				Width = 12;
			} else {
				Height = 12;
			}

			// Should be draggable
			Draggable = true;
			// Text alignment if user wants to draw text
			TextAlignment = TextAlignment.Center;
			ParagraphAlignment = ParagraphAlignment.Center;
			// Vertical label used to render value text vertically
			m_verticalLabel = Append(new Label("test") {
				X = 0,
				Y = 0,
				TextAlignment = TextAlignment.Center,
				ParagraphAlignment = ParagraphAlignment.Center,
				// Default to invisible
				Visible = false,
			});

			InnerGlow.Opacity = 0.2F;
			InnerGlow.Size = 1.0F;
		}

		/// <summary>
		/// Creates a new scrollbar
		/// </summary>
		/// <param name="direction">The direction of the scrollbar</param>
		/// <param name="onValueChanged">Invoked when the value of the scrollbar changes</param>
		public Scrollbar(GUIDirection direction, Action<float> onValueChanged) : this(direction) {
			if (onValueChanged != null) {
				ValueChanged += onValueChanged;
			}
		}


		/// <summary>
		/// Scrolls the scrollbar by delta where 1 / -1 is equal to one step based on ScrollStepCount
		/// </summary>
		/// <param name="delta">The amount where 1 / -1 is equal to one step based on ScrollStepCount</param>
		public void Scroll(float delta) {
			if (m_direction == GUIDirection.Vertical) {
				m_scrollbounds.Y -= (Height - m_scrollbounds.Height) / m_scrollStepCount * delta;
				m_scrollbounds.Y = Mathf.Round(m_scrollbounds.Y, 4);
			} else {
				m_scrollbounds.X += (Width - m_scrollbounds.Width) / m_scrollStepCount * delta;
				m_scrollbounds.X = Mathf.Round(m_scrollbounds.X, 4);
			}

			ClampScroll();
			SetValueFromPosition();
			ToggleRedraw();
		}

		/// <inheritdoc />
		protected override void OnMouseWheel(float delta) {
			// Simple map delta to -1 or 1 depending on which way the scroll wheel was moved
			delta = delta > 0 ? 1 : -1;
			Scroll(delta);
			base.OnMouseWheel(delta);
		}

		/// <inheritdoc />
		protected override void OnDragStart() {
			if (m_scrollbounds.Contains(LocalMousePosition)) {
				m_dragoffset = m_scrollbounds.Center - LocalMousePosition;
			} else {
				m_dragoffset.X = 0;
				m_dragoffset.Y = 0;
			}

			m_isDragged = true;
			base.OnDragStart();
		}

		/// <inheritdoc />
		protected override void OnDrag() {
			if (m_direction == GUIDirection.Vertical) {
				m_scrollbounds.Y = LocalMousePosition.Y - m_scrollbounds.Height / 2.0F + m_dragoffset.Y;
			} else {
				m_scrollbounds.X = LocalMousePosition.X - m_scrollbounds.Width / 2.0F + m_dragoffset.X;
			}

			ClampScroll();
			SetValueFromPosition();
			ToggleRedraw();
			base.OnDrag();
		}

		/// <inheritdoc />
		protected override void OnDragStop() {
			m_isDragged = false;
			ToggleRedraw();
			base.OnDragStop();
		}

		/// <inheritdoc />
		protected override void OnBoundsChangedDirect() {
			var maxSize = Mathf.Max(Width, Height);
			if (m_scrollElementSize > 0) {
				maxSize = m_scrollElementSize * 4;
			}

			m_scrollbounds.Width = Mathf.Floor(maxSize / 4.0F);
			m_scrollbounds.Height = Mathf.Floor(maxSize / 4.0F);
			if (m_direction == GUIDirection.Vertical) {
				m_scrollbounds.Width = Width;
			} else {
				m_scrollbounds.Height = Height;
			}

			if (m_verticalLabel != null) {
				m_verticalLabel.Width = Width;
				m_verticalLabel.Height = Height;
				// Bit of a hodgepodge rotation translation to make the text the center of the scrollbar
				m_verticalLabel.RenderTransform = Matrix3x2.Rotation(Mathf.PiOverTwo) * Matrix3x2.Translation(Height / 2.0F + Width / 2.0F, Height / 2.0F - Width / 2.0F);
			}

			SetPositionFromValue();
			base.OnBoundsChangedDirect();
		}

		/// <inheritdoc />
		protected override void OnRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) {
			m_verticalLabel.Visible = m_renderValueText && UseVerticalTextOnVerticalScroll && m_direction == GUIDirection.Vertical;
			SetPositionFromValue();
			tools.Background.Rectangle();
			tools.InnerGlow(drawParameters.Bounds);
			if (MouseHovering) {
				tools.Foreground.Rectangle(m_scrollbounds, m_isDragged ? GUIBrightness.Brightest : GUIBrightness.Bright);
			} else {
				tools.Foreground.Rectangle(m_scrollbounds);
			}

			RenderText(tools);
			tools.Foreground.BevelBorder(m_scrollbounds);
		}

		/// <summary>
		/// Renders the text. Handles rendering vertical if UseVerticalTextOnVerticalScroll is true
		/// </summary>
		protected void RenderText(GUIDrawTools tools) {
			if (m_renderValueText) {
				if (m_direction == GUIDirection.Vertical) {
					if (!UseVerticalTextOnVerticalScroll) {
						tools.Text();
					}
				} else {
					tools.Text();
				}
			}
		}

		/// <summary>
		/// Clamps the position of the scroll element to the scroll bar
		/// </summary>
		private void ClampScroll() {
			m_scrollbounds.Y = Mathf.Clamp(m_scrollbounds.Y, 0, Height - m_scrollbounds.Height);
			m_scrollbounds.X = Mathf.Clamp(m_scrollbounds.X, 0, Width - m_scrollbounds.Width);
		}


		/// <summary>
		/// Sets the position of the scroll element based on stored m_value
		/// </summary>
		private void SetPositionFromValue() {
			// Make sure value is between 0 and 1
			m_value = Mathf.Clamp(m_value, 0, 1);
			// Set the scroll position based on value
			if (m_direction == GUIDirection.Vertical) {
				m_scrollbounds.Y = Mathf.Map(m_value, 0, 1, 0, Height - m_scrollbounds.Height);
			} else {
				m_scrollbounds.X = Mathf.Map(m_value, 0, 1, 0, Width - m_scrollbounds.Width);
			}

			// Toggle redraw since scroll bounds have been updated
			ToggleRedraw();
		}

		/// <summary>
		/// Sets the stored m_value based on scroll position
		/// </summary>
		private void SetValueFromPosition() {
			// Set the value based on position
			m_value = m_direction == GUIDirection.Vertical
				? Mathf.Map(m_scrollbounds.Y, 0, Height - m_scrollbounds.Height, 0, 1)
				: Mathf.Map(m_scrollbounds.X, 0, Width - m_scrollbounds.Width, 0, 1);

			OnValueChanged(Value);
			SetTextFromValue();
		}

		/// <summary>
		/// Updates text based on Value and DrawValueRoundingDigits
		/// </summary>
		private void SetTextFromValue() {
			// Dont need to set text if user dont want to draw it
			if (m_renderValueText) {
				Text = Mathf.Round(Value, DrawValueRoundingDigits).ToString("0." + new string('0', DrawValueRoundingDigits));
				m_verticalLabel.Text = Text;
			}
		}
	}
}