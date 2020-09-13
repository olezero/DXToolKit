using SharpDX;

namespace DXToolKit.GUI {
	/// <summary>
	/// Mouse args args passed to the GUI
	/// </summary>
	public class GUIMouseEventArgs {
		/// <summary>
		/// Current mouse position
		/// </summary>
		public Vector2 MousePosition;

		/// <summary>
		/// Mouse movement since last frame
		/// </summary>
		public Vector2 MouseMove;

		/// <summary>
		/// If left mouse button is pressed
		/// </summary>
		public bool LeftMousePressed;

		/// <summary>
		/// If right mouse button is pressed
		/// </summary>
		public bool RightMousePressed;

		/// <summary>
		/// If left mouse was pressed down this frame
		/// </summary>
		public bool LeftMouseDown;

		/// <summary>
		/// If left mouse was released this frame
		/// </summary>
		public bool LeftMouseUp;

		/// <summary>
		/// If right mouse was pressed down this frame
		/// </summary>
		public bool RightMouseDown;

		/// <summary>
		/// If right mouse was released this frame
		/// </summary>
		public bool RightMouseUp;

		/// <summary>
		/// If left mouse was double clicked
		/// </summary>
		public bool LeftDoubleClick;

		/// <summary>
		/// If right mouse was double clicked
		/// </summary>
		public bool RightDoubleClick;

		/// <summary>
		/// Mouse wheel delta this frame
		/// </summary>
		public float MouseWheelDelta;
	}
}