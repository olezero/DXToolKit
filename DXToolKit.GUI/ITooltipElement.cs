using SharpDX;

namespace DXToolKit.GUI {
	/// <summary>
	/// Interface needed for a tool tip element
	/// </summary>
	public interface ITooltipElement {
		/// <summary>
		/// Invoked once when the element is opened. Implementor must make the element visible aswell as position it correctly.
		/// The element will be appended to the base element inside the gui system
		/// </summary>
		void OnOpen(string text, Vector2 mousePosition);

		/// <summary>
		/// Invoked when the tooltip should close
		/// </summary>
		void OnClose();
	}
}