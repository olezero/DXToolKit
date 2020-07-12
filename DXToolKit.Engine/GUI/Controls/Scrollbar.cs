using System;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace DXToolKit.Engine {
	public class Scrollbar : ActiveElement {
		private class ScrollElement : Button { }

		private ArrowButton m_minArrow;
		private ArrowButton m_maxArrow;
		private ScrollElement m_scrollElement;

		private GUIDirection m_direction;

		private float ButtonSize => m_direction == GUIDirection.Vertical ? Width : Height;

		public float PercentValue {
			get {
				var position = m_direction == GUIDirection.Vertical ? m_scrollElement.Y - m_scrollElement.Height : m_scrollElement.X - m_scrollElement.Width;
				var range = m_direction == GUIDirection.Vertical ? Height - (m_scrollElement.Height * 3) : Width - (m_scrollElement.Width * 3);
				return (position / range) * 100;
			}
			set {
				var minPosition = ButtonSize;
				var maxPosition = m_direction == GUIDirection.Vertical ? Height - ButtonSize - m_scrollElement.Height : Width - ButtonSize - m_scrollElement.Width;
				Mathf.Map(value, 0, 100, minPosition, maxPosition);
			}
		}

		/*
		private float m_minValue;
		private float m_maxValue;
		*/

		public float Value { get; set; }

		public Scrollbar(GUIDirection direction) {
			m_direction = direction;
			if (direction == GUIDirection.Vertical) {
				m_minArrow = Append(new ArrowButton(0));
				m_maxArrow = Append(new ArrowButton(180));
			} else {
				m_minArrow = Append(new ArrowButton(270));
				m_maxArrow = Append(new ArrowButton(90));
			}

			m_scrollElement = Append(new ScrollElement() {
				Draggable = true,
				Padding = new GUIPadding(0),
			});


			float dragStartOffset = 0;

			m_scrollElement.DragStart += () => {
				dragStartOffset = m_scrollElement.ScreenToLocal(Input.MousePosition).Y;
			};

			m_scrollElement.Drag += () => {
				// Need to offset by draw start position
				var newPosition = ScreenToLocal(Input.MousePosition).Y - dragStartOffset;
				newPosition = Mathf.Clamp(newPosition, m_scrollElement.Height, Height - m_scrollElement.Height - m_scrollElement.Height);
				m_scrollElement.Y = newPosition;
			};

			ResizeChildren();

			Brightness = GUIBrightness.Dark;
			m_scrollElement.GUIColor = GUIColor.Primary;
			m_minArrow.GUIColor = GUIColor.Primary;
			m_maxArrow.GUIColor = GUIColor.Primary;


			// m_scrollElement.BoundsChangedDirect += () => { Debug.Log("CHANEDNEHFBNAJHB!"); };
		}

		protected override void OnUpdate() {
			base.OnUpdate();
			Debug.Log("Value: " + PercentValue);
		}

		protected override void OnBoundsChangedDirect() {
			ResizeChildren();
			base.OnBoundsChangedDirect();
		}

		private void ResizeChildren() {
			var arrowSize = m_direction == GUIDirection.Vertical ? this.Width : this.Height;

			if (m_direction == GUIDirection.Vertical) {
				m_minArrow.Width = arrowSize;
				m_maxArrow.Width = arrowSize;
				m_minArrow.Height = arrowSize;
				m_maxArrow.Height = arrowSize;

				m_minArrow.X = 0;
				m_minArrow.Y = 0;

				m_maxArrow.X = 0;
				m_maxArrow.Y = Height - arrowSize;

				m_scrollElement.Width = arrowSize;
				m_scrollElement.Height = arrowSize;
			} else { }
		}

		protected override void OnRender(RenderTarget renderTarget, RectangleF bounds, TextLayout textLayout, GUIColorPalette palette, GUIDrawTools drawTools) {
			drawTools.Rectangle(renderTarget, bounds, palette, GUIColor, Brightness);
			drawTools.Border(renderTarget, bounds, palette, GUIColor, GUIBrightness.Darkest);
		}
	}
}