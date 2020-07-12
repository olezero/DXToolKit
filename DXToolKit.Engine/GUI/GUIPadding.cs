using SharpDX;

namespace DXToolKit.Engine {
	public struct GUIPadding {
		public float Left;
		public float Right;
		public float Top;
		public float Bottom;

		public GUIPadding(float values) {
			Left = Right = Top = Bottom = values;
		}

		public GUIPadding(float leftright, float topbottom) {
			Left = Right = leftright;
			Top = Bottom = topbottom;
		}

		public GUIPadding(float left, float right, float top, float bottom) {
			Left = left;
			Right = right;
			Top = top;
			Bottom = bottom;
		}

		public void ResizeRectangle(ref RectangleF rectangle) {
			rectangle.X += Left;
			rectangle.Y += Top;

			rectangle.Width -= Left + Right;
			rectangle.Height -= Top + Bottom;
		}

		public RectangleF ResizeRectangle(RectangleF rectangle) {
			rectangle.X += Left;
			rectangle.Y += Top;
			rectangle.Width -= Left + Right;
			rectangle.Height -= Top + Bottom;
			return rectangle;
		}
	}
}