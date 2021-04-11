using System;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace DXToolKit.Engine {
	public class Slider : ActiveElement {
		private Vector2 m_nobPosition;
		private float m_nobRadius = 5.0F;
		private float m_value;
		private float m_minValue = 0.0F;
		private float m_maxValue = 1.0F;
		private float m_nobMin = 0.0F;
		private float m_nobMax = 0.0F;
		private RectangleF m_grooveBounds;
		public event Action<float> OnValueChanged;

		public float MinValue {
			get => m_minValue;
			set => m_minValue = value;
		}

		public float MaxValue {
			get => m_maxValue;
			set => m_maxValue = value;
		}

		public float Value {
			get => Mathf.Map(m_value, 0, 1, m_minValue, m_maxValue);
			set => SetValue(Mathf.Map(value, m_minValue, m_maxValue, 0, 1), false);
		}

		public Slider() {
			Draggable = true;
			Drag += HandleDrag;
			ForegroundColor = GUIColor.Primary;
			Height = 24;
		}

		private void HandleDrag() {
			var local = ScreenToLocal(Input.MousePosition);
			var targetX = local.X;
			var clamped = Mathf.Clamp(targetX, m_nobMin, m_nobMax);
			// +1 and -1 are just for some padding to not draw the nob outside bounds
			var mapped = Mathf.Map(clamped, m_nobMin + m_nobRadius + 1, m_nobMax - m_nobRadius - 1, 0, 1);
			SetValue(mapped);
		}

		public Slider(float min, float max) : this() {
			m_minValue = min;
			m_maxValue = max;
		}

		public Slider(float min, float max, float current) : this() {
			m_minValue = min;
			m_maxValue = max;
			Value = current;
		}

		public Slider(Action<float> onValueChanged) : this() {
			OnValueChanged += onValueChanged;
		}

		/// <inheritdoc />
		protected override void OnBoundsChangedDirect() {
			base.OnBoundsChangedDirect();
			m_nobMin = 0;
			m_nobMax = Width;
		}

		/// <inheritdoc />
		protected override void OnBoundsChanged() {
			base.OnBoundsChanged();
			m_grooveBounds = Bounds;
			m_grooveBounds.Top = Height / 2 - 2;
			m_grooveBounds.Height = 4;
			m_grooveBounds.Left = m_nobMin;
			m_grooveBounds.Width = m_nobMax;
		}

		/// <summary>
		/// Sets value from 0.0 to 1.0
		/// </summary>
		/// <param name="value">Float from 0 to 1 (percent along the way)</param>
		private void SetValue(float value, bool invokeEvents = true) {
			value = Mathf.Clamp(value, 0, 1);
			if (MathUtil.NearEqual(m_value, value) == false) {
				m_value = value;
				if (invokeEvents) {
					OnValueChanged?.Invoke(Value);
				}

				ToggleRedraw();
			}
		}

		/// <inheritdoc />
		protected override void OnRender(RenderTarget renderTarget, RectangleF bounds, TextLayout textLayout, GUIColorPalette palette, GUIDrawTools drawTools) {
			// +1 and -1 are just for some padding to not draw the nob outside bounds
			m_nobPosition.X = Mathf.Map(m_value, 0, 1, m_nobMin + m_nobRadius + 1, m_nobMax - m_nobRadius - 1);
			m_nobPosition.Y = Height / 2.0F;

			drawTools.Rectangle(renderTarget, m_grooveBounds, palette, GUIColor.Default, GUIBrightness.Darkest);
			var brightness = MouseHovering ? GUIBrightness.Brightest : GUIBrightness.Normal;
			var ellipse = new Ellipse(m_nobPosition, m_nobRadius, m_nobRadius);
			renderTarget.FillEllipse(ellipse, palette.GetBrush(ForegroundColor, brightness));
			renderTarget.DrawEllipse(ellipse, palette.GetBrush(BackgroundColor, GUIBrightness.Darkest), 2);
		}

		public void InvokeValueChanged() {
			OnValueChanged?.Invoke(Value);
		}
	}
}