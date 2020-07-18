using System;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace DXToolKit.Engine {
	public class Scrollbar : ActiveElement {
		private GUIDirection m_orientation;
		private RectangleF m_scollElement;
		private ArrowButton m_increaseButton;
		private ArrowButton m_decreaseButton;
		public GUIColor BackgroundColor = GUIColor.Default;
		private float m_minValue = 0.0F;
		private float m_maxValue = 1.0F;
		private float m_scrollElementRadius = -1;

		public float ScrollElementRadius {
			get => m_scrollElementRadius;
			set {
				m_scrollElementRadius = value;
				RescaleAndPositionChildren();
			}
		}

		public event Action<float> ValueChanged;

		public float Value {
			get => Mathf.Map(m_value, 0, 1, m_minValue, m_maxValue);
			set => SetValue(Mathf.Map(value, m_minValue, m_maxValue, 0, 1), false, false);
		}

		public float MinValue {
			get => m_minValue;
			set {
				if (Math.Abs(m_minValue - value) < 0.001F) return;
				var curVal = Value;
				m_minValue = value;
				Value = curVal;
			}
		}

		public float MaxValue {
			get => m_maxValue;
			set {
				if (Math.Abs(m_maxValue - value) < 0.001F) return;
				var curVal = Value;
				m_maxValue = value;
				Value = curVal;
			}
		}

		protected virtual void OnValueChanged(float value) => ValueChanged?.Invoke(value);

		/// <summary>
		/// Value from 0 to 1
		/// </summary>
		private float m_value;


		public Scrollbar(GUIDirection orientation) {
			m_orientation = orientation;
			if (m_orientation == GUIDirection.Horizontal) {
				m_increaseButton = Append(new ArrowButton(90));
				m_decreaseButton = Append(new ArrowButton(270));
			} else {
				m_increaseButton = Append(new ArrowButton(180));
				m_decreaseButton = Append(new ArrowButton(0));
			}

			RescaleAndPositionChildren();
			GUIColor = GUIColor.Primary;
			Draggable = true;

			m_increaseButton.MousePressed += args => {
				SetValue(m_value + Time.DeltaTime, true);
			};

			m_decreaseButton.MousePressed += args => {
				SetValue(m_value - Time.DeltaTime, true);
			};
		}


		protected override void OnResize() {
			var max = Mathf.Max(Width, Height);
			m_scrollElementRadius = max / 10.0F;

			RescaleAndPositionChildren();
		}

		protected override void OnUpdate() {
			if (ContainsMouse) {
				if (Mathf.Abs(Input.MouseWheelDelta) > 0) {
					var mwDelta = (Input.MouseWheelDelta > 0 ? 1 : -1) / 10.0F;
					SetValue(m_orientation == GUIDirection.Horizontal ? m_value + mwDelta : m_value - mwDelta);
				}
			}

			base.OnUpdate();
		}

		private void RescaleAndPositionChildren() {
			var min = Mathf.Min(Width, Height);
			var max = Mathf.Max(Width, Height);
			m_scrollElementRadius = Mathf.Clamp(m_scrollElementRadius, min / 2.0F, (max - min - min) / 2.0F);
			m_scollElement = new RectangleF(0, 0, m_scrollElementRadius * 2, m_scrollElementRadius * 2);
			m_increaseButton.Width = min;
			m_increaseButton.Height = min;
			m_decreaseButton.Width = min;
			m_decreaseButton.Height = min;
			if (m_orientation == GUIDirection.Horizontal) {
				m_increaseButton.Left = Width - min;
				m_decreaseButton.Left = 0;
			} else {
				m_increaseButton.Top = Height - min;
				m_decreaseButton.Top = 0;
			}

			m_increaseButton.GUIColor = GUIColor;
			m_decreaseButton.GUIColor = GUIColor;

			// Force a update call, so that when initialized a OnValueChange event is invoked
			SetValue(m_value, true);
		}

		protected override void OnDrag() {
			var (min, max) = GetRange();
			var local = m_orientation == GUIDirection.Horizontal ? ScreenToLocal(Input.MousePosition).X : ScreenToLocal(Input.MousePosition).Y;
			var scrollValue = Mathf.Map(local - m_scrollElementRadius, min, max, 0, 1);
			SetValue(scrollValue);
			base.OnDrag();
		}

		private void SetValue(float value, bool forceUpdate = false, bool runEvents = true) {
			value = Mathf.Clamp(value, 0, 1);
			var (min, max) = GetRange();
			if (m_orientation == GUIDirection.Horizontal) {
				m_scollElement.X = Mathf.Map(value, 0, 1, min, max);
			} else {
				m_scollElement.Y = Mathf.Map(value, 0, 1, min, max);
			}


			if (forceUpdate || MathUtil.NearEqual(value, m_value) == false) {
				m_value = value;
				if (runEvents) {
					OnValueChanged(Mathf.Map(m_value, 0, 1, MinValue, MaxValue));
				}
			}


			ToggleRedraw();
		}

		private (float min, float max) GetRange() {
			var min = Mathf.Min(Width, Height);
			var max = Mathf.Max(Width, Height);
			var rangeMin = min;
			var rangeMax = max - m_scrollElementRadius * 2 - min;
			return (rangeMin, rangeMax);
		}

		protected override void OnRender(RenderTarget renderTarget, RectangleF bounds, TextLayout textLayout, GUIColorPalette palette, GUIDrawTools drawTools) {
			drawTools.Rectangle(renderTarget, bounds, palette, BackgroundColor, Brightness);
			drawTools.Rectangle(renderTarget, m_scollElement, palette, GUIColor, MouseHovering ? GUIBrightness.Bright : Brightness);
		}
	}
}