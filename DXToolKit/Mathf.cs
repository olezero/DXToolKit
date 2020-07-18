using System;
using SharpDX;

namespace DXToolKit {
	/// <summary>
	/// Helper class for floating point math operations
	/// </summary>
	public static class Mathf {
		/// <summary>
		/// A value specifying the approximation of π which is 180 degrees.
		/// </summary>
		public const float Pi = 3.141593f;

		/// <summary>
		/// A value specifying the approximation of 2π which is 360 degrees.
		/// </summary>
		public const float TwoPi = 6.283185f;

		/// <summary>
		/// A value specifying the approximation of π/2 which is 90 degrees.
		/// </summary>
		public const float PiOverTwo = 1.570796f;

		/// <summary>
		/// A value specifying the approximation of π/4 which is 45 degrees.
		/// </summary>
		public const float PiOverFour = 0.7853982f;

		/// <summary>
		/// Euler's number constant as a float
		/// </summary>
		public const float E = 2.7182818F;

		/// <summary>
		/// The value for which all absolute numbers smaller than are considered equal to zero.
		/// </summary>
		public const float ZeroTolerance = 1E-06f;


		/// <summary>Returns the larger of two single-precision floating-point numbers.</summary>
		/// <param name="a">The first of two single-precision floating-point numbers to compare.</param>
		/// <param name="b">The second of two single-precision floating-point numbers to compare.</param>
		/// <returns>Parameter <paramref name="a" /> or <paramref name="b" />, whichever is larger. If <paramref name="a" />, or <paramref name="b" />, or both <paramref name="a" /> and <paramref name="b" /> are equal to <see cref="F:System.Single.NaN" />, <see cref="F:System.Single.NaN" /> is returned.</returns>
		public static float Max(float a, float b) {
			return a > b || float.IsNaN(a) ? a : b;
		}

		/// <summary>Returns the smaller of two single-precision floating-point numbers.</summary>
		/// <param name="a">The first of two single-precision floating-point numbers to compare.</param>
		/// <param name="b">The second of two single-precision floating-point numbers to compare.</param>
		/// <returns>Parameter <paramref name="a" /> or <paramref name="b" />, whichever is smaller. If <paramref name="a" />, <paramref name="b" />, or both <paramref name="a" /> and <paramref name="b" /> are equal to <see cref="F:System.Single.NaN" />, <see cref="F:System.Single.NaN" /> is returned.</returns>
		public static float Min(float a, float b) {
			return a < b || float.IsNaN(a) ? a : b;
		}


		/// <summary>
		/// Maps a given value from a given input range, to a given output range
		/// For instance input value is 5, with input min 0 and max 10, and output min is 50, output max is 100, result would be 75
		/// </summary>
		/// <param name="input">Value to map</param>
		/// <param name="inputMin">Minimum known value of input</param>
		/// <param name="inputMax">Maximum known value of input</param>
		/// <param name="outputMin">Minimum desired output</param>
		/// <param name="outputMax">Maximum desired output</param>
		/// <returns>The value mapped to the new range</returns>
		public static float Map(float input, float inputMin, float inputMax, float outputMin, float outputMax) {
			return (input - inputMin) * (outputMax - outputMin) / (inputMax - inputMin) + outputMin;
		}

		/// <summary>Returns the cosine of the specified angle.</summary>
		/// <param name="d">An angle, measured in radians.</param>
		/// <returns>The cosine of <paramref name="d" />. If <paramref name="d" /> is equal to <see cref="F:System.Single.NaN" />, <see cref="F:System.Single.NegativeInfinity" />, or <see cref="F:System.Single.PositiveInfinity" />, this method returns <see cref="F:System.Single.NaN" />.</returns>
		public static float Cos(float d) {
			return (float) Math.Cos(d);
		}

		/// <summary>Returns the sine of the specified angle.</summary>
		/// <param name="a">An angle, measured in radians.</param>
		/// <returns>The sine of <paramref name="a" />. If <paramref name="a" /> is equal to <see cref="F:System.Single.NaN" />, <see cref="F:System.Single.NegativeInfinity" />, or <see cref="F:System.Single.PositiveInfinity" />, this method returns <see cref="F:System.Single.NaN" />.</returns>
		public static float Sin(float a) {
			return (float) Math.Sin(a);
		}


		/// <summary>Returns the square root of a specified number.</summary>
		/// <param name="d">The number whose square root is to be found.</param>
		/// <returns>One of the values in the following table.
		/// <paramref name="d" /> parameter
		/// 
		///  Return value
		/// 
		///  Zero or positive
		/// 
		///  The positive square root of <paramref name="d" />.
		/// 
		///  Negative
		/// 
		/// <see cref="F:System.Single.NaN" /> Equals <see cref="F:System.Single.NaN" /><see cref="F:System.Single.NaN" /> Equals <see cref="F:System.Single.PositiveInfinity" /><see cref="F:System.Single.PositiveInfinity" /></returns>
		public static float Sqrt(float d) {
			return (float) Math.Sqrt(d);
		}

		/// <summary>Converts degrees to radians.</summary>
		/// <param name="degree">The value to convert.</param>
		/// <returns>The converted value.</returns>
		public static float DegToRad(float degree) {
			return MathUtil.DegreesToRadians(degree);
		}

		/// <summary>Converts radians to degrees.</summary>
		/// <param name="radian">The value to convert.</param>
		/// <returns>The converted value.</returns>
		public static float RadToDeg(float radian) {
			return MathUtil.RadiansToDegrees(radian);
		}

		/// <summary>Clamps the specified value.</summary>
		/// <param name="value">The value.</param>
		/// <param name="min">The min.</param>
		/// <param name="max">The max.</param>
		/// <returns>The result of clamping a value between min and max</returns>
		public static int Clamp(int value, int min, int max) {
			return MathUtil.Clamp(value, min, max);
		}
		
		/// <summary>Clamps the specified value.</summary>
		/// <param name="value">The value.</param>
		/// <param name="min">The min.</param>
		/// <param name="max">The max.</param>
		/// <returns>The result of clamping a value between min and max</returns>
		public static float Clamp(float value, float min, float max) {
			return MathUtil.Clamp(value, min, max);
		}

		/// <summary>Returns the absolute value of a 32-bit signed integer.</summary>
		/// <param name="value">A number that is greater than <see cref="F:System.Int32.MinValue" />, but less than or equal to <see cref="F:System.Int32.MaxValue" />.</param>
		/// <returns>A 32-bit signed integer, x, such that 0 ≤ x ≤<see cref="F:System.Int32.MaxValue" />.</returns>
		/// <exception cref="T:System.OverflowException">
		/// <paramref name="value" /> equals <see cref="F:System.Int32.MinValue" />.</exception>
		public static int Abs(int value) {
			return Math.Abs(value);
		}

		/// <summary>Returns the absolute value of a single-precision floating-point number.</summary>
		/// <param name="value">A number that is greater than or equal to <see cref="F:System.Single.MinValue" />, but less than or equal to <see cref="F:System.Single.MaxValue" />.</param>
		/// <returns>A single-precision floating-point number, x, such that 0 ≤ x ≤<see cref="F:System.Single.MaxValue" />.</returns>
		public static float Abs(float value) {
			return Math.Abs(value);
		}

		/// <summary>
		/// Interpolates between two values using a linear function by a given amount.
		/// </summary>
		/// <remarks>
		/// See http://www.encyclopediaofmath.org/index.php/Linear_interpolation and
		/// http://fgiesen.wordpress.com/2012/08/15/linear-interpolation-past-present-and-future/
		/// </remarks>
		/// <param name="from">Value to interpolate from.</param>
		/// <param name="to">Value to interpolate to.</param>
		/// <param name="amount">Interpolation amount.</param>
		/// <returns>The result of linear interpolation of values based on the amount.</returns>
		public static float Lerp(float from, float to, float amount) {
			return MathUtil.Lerp(from, to, amount);
		}

		/// <summary>
		/// Used for random generation
		/// </summary>
		private static Random m_random;

		/// <summary>
		/// Gets random <c>float</c> number within range.
		/// </summary>
		/// <param name="min">Minimum.</param>
		/// <param name="max">Maximum.</param>
		/// <returns>Random <c>float</c> number.</returns>
		public static float Rand(float min, float max) {
			if (m_random == null) {
				m_random = new Random(0);
			}

			return m_random.NextFloat(min, max);
		}
	}
}