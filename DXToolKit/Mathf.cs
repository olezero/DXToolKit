using System;
using SharpDX;

namespace DXToolKit {
	public static class Mathf {
		public static float PI = MathUtil.Pi;

		public static float Max(float a, float b) {
			return Math.Max(a, b);
		}

		public static float Min(float a, float b) {
			return Math.Min(a, b);
		}

		public static float Map(float input, float inputMin, float inputMax, float outputMin, float outputMax) {
			return (input - inputMin) * (outputMax - outputMin) / (inputMax - inputMin) + outputMin;
		}

		public static float Cos(float rad) {
			return (float) Math.Cos(rad);
		}

		public static float Sin(float rad) {
			return (float) Math.Sin(rad);
		}

		public static float Sqrt(float f) {
			return (float) Math.Sqrt(f);
		}

		public static float DegToRad(float degrees) {
			return MathUtil.DegreesToRadians(degrees);
		}

		public static float RadToDeg(float radians) {
			return MathUtil.RadiansToDegrees(radians);
		}

		public static float Clamp(float value, float min, float max) {
			return MathUtil.Clamp(value, min, max);
		}

		public static int Abs(int value) {
			return Math.Abs(value);
		}

		public static float Abs(float value) {
			return Math.Abs(value);
		}

		public static float Lerp(float from, float to, float amount) {
			return MathUtil.Lerp(from, to, amount);
		}

		private static Random m_random;

		public static float Rand(float min, float max) {
			if (m_random == null) {
				m_random = new Random(0);
			}

			return m_random.NextFloat(min, max);
		}
	}
}