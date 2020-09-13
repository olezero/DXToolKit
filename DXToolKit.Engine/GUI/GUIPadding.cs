using System;
using SharpDX;

namespace DXToolKit.Engine {
	public struct GUIPadding : IEquatable<GUIPadding> {
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

		public float this[int index] {
			get {
				switch (index) {
					case 0: return Left;
					case 1: return Top;
					case 2: return Right;
					case 3: return Bottom;
				}

				throw new IndexOutOfRangeException();
			}
			set {
				switch (index) {
					case 0:
						Left = value;
						break;
					case 1:
						Top = value;
						break;
					case 2:
						Right = value;
						break;
					case 3:
						Bottom = value;
						break;
				}

				throw new IndexOutOfRangeException();
			}
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


		public bool Equals(GUIPadding other) {
			return MathUtil.NearEqual(Left, other.Left) &&
			       MathUtil.NearEqual(Right, other.Right) &&
			       MathUtil.NearEqual(Top, other.Top) &&
			       MathUtil.NearEqual(Bottom, other.Bottom);
		}
		
		
	}
}