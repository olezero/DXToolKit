using SharpDX;

namespace DXToolKit.GUI {
	public class GUIMouseEventArgs {
		public Vector2 MousePosition;
		public Vector2 MouseMove;
		
		public bool LeftMousePressed;
		public bool RightMousePressed;

		public bool LeftMouseDown;
		public bool LeftMouseUp;

		public bool RightMouseDown;
		public bool RightMouseUp;

		public bool LeftDoubleClick;
		public bool RightDoubleClick;
	}
}