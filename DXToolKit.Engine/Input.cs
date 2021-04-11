using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SharpDX;
using SharpDX.DirectInput;
using SharpDX.Windows;

namespace DXToolKit.Engine {
	/// <summary>
	/// DirectInput wrapper for mouse and keyboard input
	/// </summary>
	public static class Input {
		[DllImport("user32.dll")]
		static extern IntPtr GetForegroundWindow();

		private static DirectInput m_diretInput;
		private static Keyboard m_keyboard;
		private static Mouse m_mouse;

		// TODO: remove this when cursor style is fixed
		// ReSharper disable once NotAccessedField.Local
		private static CursorStyle m_cursorStyle;

		private static List<Key> m_keysUp = new List<Key>();
		private static List<Key> m_keysDown = new List<Key>();
		private static List<Key> m_pressedKeys = new List<Key>();

		/// <summary>
		/// Gets a list of keys that were released during the previous frame
		/// </summary>
		public static List<Key> KeysUp => m_keysUp;

		/// <summary>
		/// Gets a list of keys that were pressed down during the previous frame
		/// </summary>
		public static List<Key> KeysDown => m_keysDown;

		/// <summary>
		/// Gets a list of keys that are currently pressed.
		/// </summary>
		public static List<Key> KeysPressed => m_pressedKeys;

		private static List<MouseButton> m_mouseButtonsUp = new List<MouseButton>();
		private static List<MouseButton> m_mouseButtonsDown = new List<MouseButton>();
		private static List<MouseButton> m_mouseButtonsPressed = new List<MouseButton>();
		private static List<MouseButton> m_mouseDoubleClick = new List<MouseButton>();
		private static float[] m_doubleClickTimers;

		/// <summary>
		/// Gets a value indicating if the input key was released during the last frame
		/// </summary>
		/// <param name="key">The key to check against</param>
		/// <returns>True on key up</returns>
		public static bool KeyUp(Key key) => m_keysUp.Contains(key);

		/// <summary>
		/// Gets a value indicating if the input key was pressed down during the last frame
		/// </summary>
		/// <param name="key">The key to check against</param>
		/// <returns>True on key down</returns>
		public static bool KeyDown(Key key) => m_keysDown.Contains(key);

		/// <summary>
		/// Gets a value indicating if the input key is currently pressed
		/// </summary>
		/// <param name="key">The key to check against</param>
		/// <returns>True on key pressed</returns>
		public static bool KeyPressed(Key key) => m_pressedKeys.Contains(key);

		/// <summary>
		/// Gets a value indicating if the input key is currently pressed and repeated based on Win32 repeat delay
		/// </summary>
		/// <param name="key">The key to check against</param>
		/// <returns>True on every tick the repeat delay triggers a keypress</returns>
		public static bool RepeatKey(Key key) => m_toggleRepeatKey && m_repeatKey == key;

		/// <summary>
		/// Gets the current key that is set as the "repeating key" as Win32 can only have a single key being repeated at any given time
		/// </summary>
		public static Key? RepeatingKey {
			get {
				if (m_toggleRepeatKey) {
					return m_repeatKey;
				}

				return null;
			}
		}

		/// <summary>
		/// Gets a value indicating if the input mouse button was released during the previous frame
		/// </summary>
		/// <param name="button">The button to check</param>
		/// <returns>True on mouse up</returns>
		public static bool MouseUp(MouseButton button) => m_mouseButtonsUp.Contains(button);

		/// <summary>
		/// Gets a value indicating if the input mouse button was pressed down during the previous frame
		/// </summary>
		/// <param name="button">The button to check</param>
		/// <returns>True on mouse down</returns>
		public static bool MouseDown(MouseButton button) => m_mouseButtonsDown.Contains(button);

		/// <summary>
		/// Gets a value indicating if the input mouse button is currently being pressed
		/// </summary>
		/// <param name="button">The button to check</param>
		/// <returns>True on mouse pressed</returns>
		public static bool MousePressed(MouseButton button) => m_mouseButtonsPressed.Contains(button);

		/// <summary>
		/// Gets a value indicating if the input mouse button was double clicked based on Win32 double click delay
		/// </summary>
		/// <param name="button">The button to check</param>
		/// <returns>True on mouse double click</returns>
		public static bool MouseDoubleClick(MouseButton button) => m_mouseDoubleClick.Contains(button);

		/// <summary>
		/// Sets the cursor style to use in the application
		/// TODO: Check if there is a way of setting Form.cursor = cursor style
		/// </summary>
		/// <param name="style"></param>
		public static void SetCursorStyle(CursorStyle style) => m_cursorStyle = style;

		/// <summary>
		/// Gets a list of mouse buttons that was released during the last frame
		/// </summary>
		public static List<MouseButton> MouseButtonsUp => m_mouseButtonsUp;

		/// <summary>
		/// Gets a list of mouse buttons that was pressed down during the last frame
		/// </summary>
		public static List<MouseButton> MouseButtonsDown => m_mouseButtonsDown;

		/// <summary>
		/// Gets a list of mouse buttons that are pressed down
		/// </summary>
		public static List<MouseButton> MouseButtonsPressed => m_mouseButtonsPressed;

		/// <summary>
		/// Gets a list of mouse buttons that was double clicked during the last frame
		/// </summary>
		public static List<MouseButton> MouseButtonsDoubleClick => m_mouseDoubleClick;

		/// <summary>
		/// Text input buffer to store each frames text input as a string
		/// </summary>
		private static string m_textInputBuffer = "";

		/// <summary>
		/// Last frame input buffer to return to the user
		/// </summary>
		private static string m_lastFrameInput = "";

		/// <summary>
		/// Gets the text buffered text input from the last frame.
		/// </summary>
		public static string TextInput => m_lastFrameInput;

		/// <summary>
		/// Gets a value indicating if there is text input available
		/// </summary>
		public static bool TextInputAvailable => m_lastFrameInput.Length > 0;

		/// <summary>
		/// Gets a value indicating how much the mouse wheel has moved during the previous frame
		/// </summary>
		public static int MouseWheelDelta { get; private set; }

		/// <summary>
		/// Gets the amount of lines the mouse scroll wheel is configured in Win32 to scroll
		/// </summary>
		public static readonly int MouseWheelScrollLines;

		/// <summary>
		/// Gets or sets a value indicating if mouse should be hardware controlled and not windows Form based
		/// </summary>
		public static bool UseHardwareMouse { get; set; } = false;

		/// <summary>
		/// Gets a value indicating the amount of pixels the mouse moved the previous frame
		/// </summary>
		public static Vector2 MouseMove { get; private set; }

		/// <summary>
		/// Gets the current mouse position based on the top left corner of the Win32 Form
		/// </summary>
		public static Vector2 MousePosition {
			get {
				var pos = Cursor.Position;
				pos.X -= WindowsFormRectangle.Left;
				pos.Y -= WindowsFormRectangle.Top;
				return new Vector2(pos.X, pos.Y);
			}
		}

		/// <summary>
		/// Value used for left mouse selection rectangle
		/// </summary>
		private static RectangleF m_leftMouseSelection;

		/// <summary>
		/// Value used for right mouse selection rectangle
		/// </summary>
		private static RectangleF m_rightMouseSelection;

		/// <summary>
		/// Gets a value indicating if the left selection is active (Triggered by holding the left mouse button down and dragging the mouse)
		/// </summary>
		public static bool LeftSelectActive = false;

		/// <summary>
		/// Gets a value indicating if the right selection is active (Triggered by holding the left mouse button down and dragging the mouse)
		/// </summary>
		public static bool RightSelectActive = false;

		/// <summary>
		/// Gets a rectangle representing a selection box
		/// </summary>
		public static RectangleF LeftMouseSelectionRectangle {
			get {
				var result = m_leftMouseSelection;
				AbsRectangleF(ref result);
				return result;
			}
		}

		/// <summary>
		/// Gets a rectangle representing a selection box
		/// </summary>
		public static RectangleF RightMouseSelectionRectangle {
			get {
				var result = m_rightMouseSelection;
				AbsRectangleF(ref result);
				return result;
			}
		}

		/// <summary>
		/// Converts a rectangle to absolute values where 0,0 is always the top left corner of the rectangle
		/// </summary>
		/// <param name="rectangle"></param>
		private static void AbsRectangleF(ref RectangleF rectangle) {
			// Invert width
			if (rectangle.Width < 0) {
				rectangle.Width = -rectangle.Width;
				rectangle.X -= rectangle.Width;
			}

			// Invert height
			if (rectangle.Height < 0) {
				rectangle.Height = -rectangle.Height;
				rectangle.Y -= rectangle.Height;
			}
		}

		/// <summary>
		/// Holder for the windows forms rectangle
		/// </summary>
		private static System.Drawing.Rectangle WindowsFormRectangle;

		/// <summary>
		/// Values used for mouse move calculation
		/// </summary>
		private static Vector2 m_lastFrameMousePosition;

		/// <summary>
		/// Values used for mouse move calculation
		/// </summary>
		private static Vector2 m_windowMousePosition;

		private static Key m_repeatKey;
		private static float m_repeatKeyDelayTimer;
		private static float m_repeatKeyRepeatTimer;
		private static bool m_toggleRepeatKey;

		private static KeyboardState m_keyboardState;
		private static MouseState m_mouseState;

		/// <summary>
		/// Time in seconds to allow for a double click based on system settings.
		/// </summary>
		private static readonly float m_doubleClickTime;

		/// <summary>
		/// Time in seconds between each repeating key event
		/// </summary>
		private static readonly float m_repeatSpeed;

		/// <summary>
		/// Delay before a repeating key starts to run
		/// </summary>
		private static readonly float m_repeatDelay;

		static Input() {
			// The keyboard repeat-delay setting, from 0 (approximately 250 millisecond delay) through 3 (approximately 1 second delay).
			var delay = SystemInformation.KeyboardDelay;

			// This is a value in the range from 0 (approximately 2.5 repetitions per second) through 31 (approximately 30 repetitions per second).
			var speed = SystemInformation.KeyboardSpeed;

			// Map repeat delay/speed based on Microsoft's documentation.
			m_repeatDelay = Mathf.Map(delay, 0, 3, 250, 1000) / 1000.0F;
			m_repeatSpeed = Mathf.Map(speed, 0, 31, 1000.0F / 2.5F, 1000.0F / 30.0F) / 1000.0F;
			m_doubleClickTime = SystemInformation.DoubleClickTime / 1000.0F;
			MouseWheelScrollLines = SystemInformation.MouseWheelScrollLines;
		}

		internal static void Initialize(RenderForm renderForm) {
			m_diretInput = new DirectInput();
			m_keyboard = new Keyboard(m_diretInput);
			m_mouse = new Mouse(m_diretInput);
			m_cursorStyle = CursorStyle.Default;

			m_keyboard.Acquire();
			m_mouse.Acquire();

			renderForm.MouseMove += (sender, args) => {
				m_windowMousePosition.X = args.X;
				m_windowMousePosition.Y = args.Y;
			};

			renderForm.KeyPress += (sender, args) => {
				// Should only handle letters, numbers and whitespace (space and return) and some more....
				if (char.IsLetter(args.KeyChar) || char.IsNumber(args.KeyChar) || char.IsWhiteSpace(args.KeyChar) || char.IsPunctuation(args.KeyChar) || char.IsSeparator(args.KeyChar) || char.IsSymbol(args.KeyChar)) {
					m_textInputBuffer += args.KeyChar;
					args.Handled = true;
				}
			};

			// Prevents a big spike in mouse move when focus is received
			renderForm.GotFocus += (sender, args) => {
				//MousePosition = m_windowMousePosition;
				m_lastFrameMousePosition = m_windowMousePosition;
			};


			// Create timers for double clicks
			m_doubleClickTimers = new float[Enum.GetValues(typeof(MouseButton)).Length];
			// Set all to 10
			for (int i = 0; i < m_doubleClickTimers.Length; i++) m_doubleClickTimers[i] = 10;

			m_keyboardState = new KeyboardState();
			m_mouseState = new MouseState();


			WindowsFormRectangle = renderForm.RectangleToScreen(renderForm.ClientRectangle);
			renderForm.LocationChanged += (sender, args) => {
				WindowsFormRectangle = renderForm.RectangleToScreen(renderForm.ClientRectangle);
			};
		}

		internal static void Frame(RenderForm renderForm) {
			/*
			if (GetForegroundWindow() != renderForm.Handle) {
				return;
			}
			*/
			if (renderForm.Focused == false) {
				// Clear all buffers if we dont have focus
				m_pressedKeys.Clear();
				m_keysUp.Clear();
				m_keysDown.Clear();
				m_mouseButtonsDown.Clear();
				m_mouseButtonsPressed.Clear();
				m_mouseButtonsUp.Clear();
				m_textInputBuffer = "";
				ResetMouseSelection();
				return;
			}

			// Copy text buffer
			m_lastFrameInput = m_textInputBuffer;

			// Clear text input buffer every frame
			if (m_textInputBuffer.Length > 0) {
				m_textInputBuffer = "";
			}

			m_keyboard.GetCurrentState(ref m_keyboardState);
			m_mouse.GetCurrentState(ref m_mouseState);

			HandleKeyboard(m_keyboardState);
			HandleMouse(m_mouseState);
			HandleMouseSelection();

			/*
			 this messes with normal resize handles for the window
			 TODO - this should maybe only be applied if the mouse is within thee window with some (10ish pixel) margin
			 
			switch (m_cursorStyle) {
				case CursorStyle.Default:
					renderForm.Cursor = Cursors.Default;
					break;
				case CursorStyle.IBeam:
					renderForm.Cursor = Cursors.IBeam;
					break;
				case CursorStyle.Cross:
					renderForm.Cursor = Cursors.Cross;
					break;
				case CursorStyle.VSplit:
					renderForm.Cursor = Cursors.VSplit;
					break;
				case CursorStyle.HSplit:
					renderForm.Cursor = Cursors.HSplit;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			m_cursorStyle = CursorStyle.Default;
			*/
		}

		private static void ResetMouseSelection() {
			m_rightMouseSelection.X = m_leftMouseSelection.X = float.NegativeInfinity;
			m_rightMouseSelection.Y = m_leftMouseSelection.Y = float.NegativeInfinity;
			m_rightMouseSelection.Width = m_leftMouseSelection.Width = 0;
			m_rightMouseSelection.Height = m_leftMouseSelection.Height = 0;
			LeftSelectActive = RightSelectActive = false;
		}

		private static void HandleMouseSelection() {
			LeftSelectActive = HandleSelectionRectangle(ref m_leftMouseSelection, MouseButton.Left);
			RightSelectActive = HandleSelectionRectangle(ref m_rightMouseSelection, MouseButton.Right);
		}

		private static bool HandleSelectionRectangle(ref RectangleF rectangle, MouseButton dragButton) {
			if (m_mouseButtonsPressed.Contains(dragButton)) {
				// Was clicked, start selection at this point
				if (m_mouseButtonsDown.Contains(dragButton)) {
					rectangle.X = MousePosition.X;
					rectangle.Y = MousePosition.Y;
				}

				// Dragging
				rectangle.Width = MousePosition.X - rectangle.X;
				rectangle.Height = MousePosition.Y - rectangle.Y;
				return true;
			}

			// Reset if not pressed
			rectangle.X = rectangle.Y = float.NegativeInfinity;
			rectangle.Width = rectangle.Height = 0;
			return false;
		}

		private static void HandleKeyboard(KeyboardState state) {
			// Key down == inside currently pressed, but not in last frames pressed.
			m_keysDown = state.PressedKeys.Except(m_pressedKeys).ToList();

			// Key Up == inside last frames pressed, but not in current.
			m_keysUp = m_pressedKeys.Except(state.PressedKeys).ToList();

			// Update pressed key buffer for next frame.
			m_pressedKeys = state.PressedKeys.ToList();

			// Handle repeating key
			HandleRepeatingKey();
		}

		private static void HandleMouse(MouseState state) {
			var mbuttons = state.Buttons;

			var current = new List<MouseButton>();
			for (var i = 0;
				i < mbuttons.Length;
				i++) {
				if (mbuttons[i]) {
					current.Add((MouseButton) i);
				}
			}

			// Same as keyboard.
			m_mouseButtonsDown = current.Except(m_mouseButtonsPressed).ToList();
			m_mouseButtonsUp = m_mouseButtonsPressed.Except(current).ToList();
			m_mouseButtonsPressed = current.ToList();
			if (Math.Abs(state.Z) > 0) {
				if (state.Z > 0) {
					MouseWheelDelta = 1;
				}

				if (state.Z < 0) {
					MouseWheelDelta = -1;
				}
			} else {
				MouseWheelDelta = 0;
			}

			if (UseHardwareMouse) {
				// TODO - fix this. After changing from a renderform.mousemove to cursor.position this broke, since we cant save mouse position. Although "hardware" mouse seams more sluggish then a direct from windows mouse position
				throw new NotImplementedException();
				//MouseMove = new Vector2(state.X, state.Y);
				//MousePosition += MouseMove;
			} else {
				//MousePosition = m_windowMousePosition;
				MouseMove = MousePosition - m_lastFrameMousePosition;
				m_lastFrameMousePosition = MousePosition;
			}

			// Clear double click list
			m_mouseDoubleClick.Clear();

			// Increment timers
			for (int i = 0;
				i < m_doubleClickTimers.Length;
				i++) {
				m_doubleClickTimers[i] += Time.DeltaTime;
				if (m_doubleClickTimers[i] > 10) {
					m_doubleClickTimers[i] = 10;
				}
			}

			foreach (var button in m_mouseButtonsDown) {
				// If a timer is less then double click time, run double click
				if (m_doubleClickTimers[(int) button] < m_doubleClickTime) {
					m_mouseDoubleClick.Add(button);
					m_doubleClickTimers[(int) button] = 10;
				} else {
					// reset timer when 0
					m_doubleClickTimers[(int) button] = 0;
				}
			}
		}


		private static void HandleRepeatingKey() {
			// Toggle to control of the repeat key should be sent out as a keypress. Reset every frame.
			m_toggleRepeatKey = false;

			// If any key is "down" that key is the current repeating key
			if (m_keysDown.Count > 0) {
				// Set repeat key to the last key that has been pressed by the user.
				m_repeatKey = m_keysDown[0];

				// Reset delay and repeat timer since key has changed.
				m_repeatKeyDelayTimer = 0;
				m_repeatKeyRepeatTimer = 0;

				// Need to toggle once when its pressed the first time
				m_toggleRepeatKey = true;
			}

			// If the target repeat key is currently being pressed, increment delay timer.
			if (KeyPressed(m_repeatKey)) {
				m_repeatKeyDelayTimer += Time.DeltaTime;

				// If its being pressed while the delay timer is greater then target delay time, increment repeat time.
				if (m_repeatKeyDelayTimer > m_repeatDelay) {
					// Increment repeat timer
					m_repeatKeyRepeatTimer += Time.DeltaTime;
				}

				// If repeat timer is greater then target repeat time, toggle keypress
				if (m_repeatKeyRepeatTimer > m_repeatSpeed) {
					m_repeatKeyRepeatTimer = 0;
					m_toggleRepeatKey = true;
				}
			} else {
				m_repeatKeyDelayTimer = 0;
				m_repeatKeyRepeatTimer = 0;
				m_toggleRepeatKey = false;
			}
		}

		/// <summary>
		/// Simulates a key press
		/// TODO: Move to seperate sub class. Such as Input.Fake.Keypress and Input.Fake.MousePress(x,y,button)
		/// </summary>
		/// <param name="key"></param>
		public static void SimulateKeyPress(Key key) {
			m_keysDown.Add(key);
			m_pressedKeys.Add(key);
		}

		internal static void Shutdown() {
			m_mouse?.Dispose();
			m_keyboard?.Dispose();
			m_diretInput?.Dispose();
		}

		/// <summary>
		/// TODO: Something like this for simulating
		/// </summary>
		public static class Faker {
			public static void Sim(Key key) {
				m_keysDown.Add(key);
			}
		}
	}
}