using System;
using SharpDX;
using DXToolKit.GUI;

namespace DXToolKit.Engine {
	/// <summary>
	/// Basic button that uses a icon instead of text
	/// Good practice to keep width and height the same 
	/// </summary>
	public class IconButton : ActiveElement {
		private IconElement m_icon;
		public bool SinkOnPress = true;

		/// <summary>
		/// Creates a new icon button
		/// </summary>
		/// <param name="icon">The icon element to use for the button</param>
		public IconButton(IconElement icon) {
			m_icon = Append(icon);
			m_icon.X = 0;
			m_icon.Y = 0;
			Height = m_icon.Width;
			Width = m_icon.Height;
		}

		/// <summary>
		/// Creates a new icon button
		/// </summary>
		/// <param name="icon">The icon element to use for the button</param>
		/// <param name="onClick">Invoked when the button is clicked</param>
		public IconButton(IconElement icon, Action<GUIMouseEventArgs> onClick) : this(icon) {
			if (onClick != null) Click += onClick;
		}

		/// <inheritdoc />
		protected override void OnBoundsChangedDirect() {
			m_icon.Width = Width;
			m_icon.Height = Height;
			m_icon.X = 0;
			m_icon.Y = 0;
			base.OnBoundsChangedDirect();
		}

		/// <inheritdoc />
		protected override void OnLateUpdate() {
			// Set render offset based on if mouse is pressed while over the button, so the icon gets offset
			if (SinkOnPress) {
				RenderOffset = IsMousePressed ? new Vector2(1F, 1F) : Vector2.Zero;
			}

			base.OnLateUpdate();
		}

		/// <inheritdoc />
		protected override void OnRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) {
			// Could just do a simple render here
			var targetBrightness = MouseHovering ? IsMousePressed ? tools.Brighten(Brightness, 2) : tools.Brighten(Brightness) : Brightness;
			// Draw rectangle to contain graphic
			tools.Background.Rectangle(targetBrightness);
			tools.Background.BevelBorder(SinkOnPress && IsMousePressed && MouseHovering, Brightness);
			tools.Shine();
		}
	}
}