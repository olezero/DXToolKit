using System;
using DXToolKit.GUI;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace DXToolKit.Engine {
	/// <summary>
	/// GUI element representing a checkbox control
	/// </summary>
	public class GUICheckbox : GraphicButton {
		/// <summary>
		/// Gets or sets a value indicating if the checkbox is checked
		/// </summary>
		public bool Checked = false;

		/// <summary>
		/// Gets or sets a value indicating if a border should be drawn on the checkbox
		/// </summary>
		public bool DrawBorder = true;

		/// <summary>
		/// Gets or sets a value if the width of the control should be constrained to be the same as its height
		/// </summary>
		public bool ConstrainWidthToHeight = true;

		/// <summary>
		/// Invoked when the checkbox is toggled.
		/// </summary>
		public event Action<bool> OnToggle;

		/// <inheritdoc />
		public GUICheckbox(float size = 16) {
			Width = size;
			Height = size;
		}

		/// <inheritdoc />
		public GUICheckbox(float size = 16, Action<bool> onToggle = null) : this(size) {
			if (onToggle != null) {
				OnToggle += onToggle;
			}
		}

		/// <inheritdoc />
		protected override void OnBoundsChangedDirect() {
			if (ConstrainWidthToHeight) {
				Width = Height;
			}

			base.OnBoundsChangedDirect();
		}

		/// <summary>
		/// Toggles to checkbox
		/// </summary>
		/// <param name="fireEvents">If this call should invoke events</param>
		public void Toggle(bool fireEvents = true) {
			Checked = !Checked;
			if (fireEvents) {
				OnToggle?.Invoke(Checked);
			}

			ToggleRedraw();
		}

		/// <inheritdoc />
		protected override void OnClick(GUIMouseEventArgs args) {
			Toggle();
			base.OnClick(args);
		}

		/// <inheritdoc />
		protected override void CreateGraphics(RenderTarget renderTarget, RectangleF bounds, GUIColorPalette palette, GUIDrawTools drawTools, float recommendedStrokeWidth) {
			bounds.Inflate(-recommendedStrokeWidth - 1, -recommendedStrokeWidth - 1);

			var topLeft = new Vector2(bounds.X, bounds.Y);
			var topRight = new Vector2(bounds.X + bounds.Width, bounds.Y);
			var bottomLeft = new Vector2(bounds.X, bounds.Y + bounds.Height);
			var bottomRight = new Vector2(bounds.X + bounds.Width, bounds.Y + bounds.Height);
			var brush = palette.GetBrush(GUIColor.Text, TextBrightness);

			renderTarget.BeginDraw();
			renderTarget.DrawLine(topLeft, bottomRight, brush, recommendedStrokeWidth);
			renderTarget.DrawLine(topRight, bottomLeft, brush, recommendedStrokeWidth);
			renderTarget.EndDraw();
		}

		/// <inheritdoc />
		protected override void OnRender(RenderTarget renderTarget, RectangleF bounds, TextLayout textLayout, GUIColorPalette palette, GUIDrawTools drawTools, Bitmap graphics) {
			if (DrawBorder) {
				drawTools.Rectangle(renderTarget, bounds, palette, ForegroundColor, GUIBrightness.Normal);
				bounds.Inflate(-2, -2);
			}

			drawTools.Rectangle(renderTarget, bounds, palette, BackgroundColor, GUIBrightness.Dark);

			if (Checked) {
				renderTarget.DrawBitmap(graphics, bounds, 1.0F, BitmapInterpolationMode.Linear);
			}
		}
	}
}