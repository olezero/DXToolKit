using System;
using SharpDX;
using SharpDX.DirectInput;

namespace DXToolKit.Engine {
	/// <summary>
	/// Common library of handy functions
	/// </summary>
	public abstract class FunctionToolBox {
		/// <summary>
		/// Gets the position of the mouse, where (0, 0) is the top left of the screen.
		/// </summary>
		protected Vector2 MousePosition => Input.MousePosition;

		/// <summary>
		/// Gets the amount of movement of the mouse the last frame. Where -x is left, +x is right, -y is up, +y is down.
		/// </summary>
		protected Vector2 MouseMove => Input.MouseMove;

		/// <summary>
		/// Gets a value indicating if a key is being held down
		/// </summary>
		/// <param name="key">The key to check</param>
		/// <returns>A value indicating if the key is being held down</returns>
		protected bool KeyPressed(Key key) => Input.KeyPressed(key);

		/// <summary>
		/// Gets a value indicating if a key was pressed last frame
		/// </summary>
		/// <param name="key">The key to check if was pressed down</param>
		/// <returns>True if key was pressed down</returns>
		protected bool KeyDown(Key key) => Input.KeyDown(key);

		/// <summary>
		/// Gets a value indicating if a key was released the last frame
		/// </summary>
		/// <param name="key">The key to check</param>
		/// <returns>A value indicating if the key was released last frame</returns>
		protected bool KeyUp(Key key) => Input.KeyUp(key);

		/// <summary>
		/// Gets a value indicating if a mouse button is pressed.
		/// </summary>
		protected bool MousePressed(MouseButton button) => Input.MousePressed(button);

		/// <summary>
		/// Gets a value indicating if a mouse button was pressed down last frame.
		/// </summary>
		protected bool MouseDown(MouseButton button) => Input.MouseDown(button);

		/// <summary>
		/// Gets a value indicating if a mouse button was let go the last frame.
		/// </summary>
		protected bool MouseUp(MouseButton button) => Input.MouseUp(button);

		/// <summary>
		/// Gets a value of how much the mouse wheel has moved the last frame.
		/// </summary>
		protected float MouseWheelDelta => Input.MouseWheelDelta;

		/// <summary>
		/// Gets a value indicating if the mousewheel has moved the last frame, from a range of -1, 0, 1
		/// </summary>
		protected float NormalizedMouseWheel => Abs(Input.MouseWheelDelta) > 0 ? Input.MouseWheelDelta > 0 ? 1 : -1 : 0;

		/// <summary>
		/// Gets the width of the screen
		/// </summary>
		protected float ScreenWidth => EngineConfig.ScreenWidth;

		/// <summary>
		/// Gets the height of the screen
		/// </summary>
		protected float ScreenHeight => EngineConfig.ScreenHeight;

		/// <summary>
		/// Transforms an input range to an output range
		/// </summary>
		/// <param name="input">The input value</param>
		/// <param name="inputMin">The minimum value of the input</param>
		/// <param name="inputMax">The maximum value if the input</param>
		/// <param name="outMin">The minimum value of the output</param>
		/// <param name="outMax">The maximum value of the output</param>
		protected float Map(float input, float inputMin, float inputMax, float outMin, float outMax) => Mathf.Map(input, inputMin, inputMax, outMin, outMax);
		protected float Sin(float val) => Mathf.Sin(val);
		protected float Cos(float val) => Mathf.Cos(val);
		protected float Lerp(float from, float to, float amount) => Mathf.Lerp(from, to, amount);
		protected float Max(float a, float b) => Mathf.Max(a, b);
		protected float Min(float a, float b) => Mathf.Min(a, b);
		protected int Floor(float val) => (int) Math.Floor(val);
		protected int Ceiling(float val) => (int) Math.Ceiling(val);
		protected float DegToRad(float degrees) => Mathf.DegToRad(degrees);
		protected float RadToDeg(float radians) => Mathf.RadToDeg(radians);
		protected float Clamp(float value, float min, float max) => Mathf.Clamp(value, min, max);
		protected float Sqrt(float value) => Mathf.Sqrt(value);
		protected float Abs(float value) => Mathf.Abs(value);
	}
}