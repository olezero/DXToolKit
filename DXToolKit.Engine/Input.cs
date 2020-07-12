using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SharpDX;
using SharpDX.DirectInput;
using SharpDX.Windows;

namespace DXToolKit {
	public enum MouseButton {
		Left = 0,
		Right = 1,
		Middle = 2,
		Mouse4 = 3,
		Mouse5 = 4,
	}

	public enum CursorStyle {
		Default,
		IBeam,
	}

	public static class Input {
		[DllImport("user32.dll")]
		static extern IntPtr GetForegroundWindow();

		private static DirectInput m_diretInput;
		private static Keyboard m_keyboard;
		private static Mouse m_mouse;

		private static CursorStyle m_cursorStyle;

		private static List<Key> m_keysUp = new List<Key>();
		private static List<Key> m_keysDown = new List<Key>();
		private static List<Key> m_pressedKeys = new List<Key>();

		public static List<Key> KeysUp => m_keysUp;
		public static List<Key> KeysDown => m_keysDown;
		public static List<Key> KeysPressed => m_pressedKeys;


		private static List<MouseButton> m_mouseButtonsUp = new List<MouseButton>();
		private static List<MouseButton> m_mouseButtonsDown = new List<MouseButton>();
		private static List<MouseButton> m_mouseButtonsPressed = new List<MouseButton>();
		private static List<MouseButton> m_mouseDoubleClick = new List<MouseButton>();
		private static float[] m_doubleClickTimers;

		public static bool KeyUp(Key key) => m_keysUp.Contains(key);
		public static bool KeyDown(Key key) => m_keysDown.Contains(key);
		public static bool KeyPressed(Key key) => m_pressedKeys.Contains(key);
		public static bool RepeatKey(Key key) => m_toggleRepeatKey && m_repeatKey == key;

		public static Key? RepeatingKey {
			get {
				if (m_toggleRepeatKey) {
					return m_repeatKey;
				}

				return null;
			}
		}

		public static bool MouseUp(MouseButton button) => m_mouseButtonsUp.Contains(button);
		public static bool MouseDown(MouseButton button) => m_mouseButtonsDown.Contains(button);
		public static bool MousePressed(MouseButton button) => m_mouseButtonsPressed.Contains(button);
		public static bool MouseDoubleClick(MouseButton button) => m_mouseDoubleClick.Contains(button);
		public static void SetCursorStyle(CursorStyle style) => m_cursorStyle = style;

		public static List<MouseButton> MouseButtonsUp => m_mouseButtonsUp;
		public static List<MouseButton> MouseButtonsDown => m_mouseButtonsDown;
		public static List<MouseButton> MouseButtonsPressed => m_mouseButtonsPressed;
		public static List<MouseButton> MouseButtonsDoubleClick => m_mouseDoubleClick;

		private static string m_textInputBuffer = "";
		private static string m_lastFrameInput = "";

		/// <summary>
		/// Gets the text buffered text input from the last frame.
		/// </summary>
		public static string TextInput => m_lastFrameInput;

		public static bool TextInputAvailable => m_lastFrameInput.Length > 0;

		public static int MouseWheelDelta { get; private set; }
		public static readonly int MouseWheelScrollLines;

		public static bool UseHardwareMouse { get; set; } = false;

		public static Vector2 MouseMove { get; private set; }
		//public static Vector2 MousePosition { get; private set; }

		public static Vector2 MousePosition {
			get {
				var pos = Cursor.Position;
				pos.X -= WindowsFormRectangle.Left;
				pos.Y -= WindowsFormRectangle.Top;
				return new Vector2(pos.X, pos.Y);
			}
		}

		private static System.Drawing.Rectangle WindowsFormRectangle;


		private static Vector2 m_lastFrameMousePosition;
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
				// Should only handle letters, numbers and whitespace (space and return)
				if (char.IsLetter(args.KeyChar) || char.IsNumber(args.KeyChar) || char.IsWhiteSpace(args.KeyChar)) {
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
			renderForm.LocationChanged += (sender, args) => { WindowsFormRectangle = renderForm.RectangleToScreen(renderForm.ClientRectangle); };
		}

		internal static void Frame(RenderForm renderForm) {
			/*
			if (GetForegroundWindow() != renderForm.Handle) {
				return;
			}
			*/
			if (renderForm.Focused == false) {
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

			/*
			switch (m_cursorStyle) {
				case CursorStyle.Default:
					renderForm.Cursor = Cursors.Default;
					break;
				case CursorStyle.IBeam:
					renderForm.Cursor = Cursors.IBeam;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			m_cursorStyle = CursorStyle.Default;
			*/
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
			for (var i = 0; i < mbuttons.Length; i++) {
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
				// TODO - fix this. After changing from a renderform.mousemove to cursor.position this broke, since we cant save mouseposition. Although "hardware" mouse seams more sluggish then a direct from windows mouse position
				throw new NotImplementedException();
				MouseMove = new Vector2(state.X, state.Y);
				//MousePosition += MouseMove;
			} else {
				//MousePosition = m_windowMousePosition;
				MouseMove = MousePosition - m_lastFrameMousePosition;
				m_lastFrameMousePosition = MousePosition;
			}

			// Clear double click list
			m_mouseDoubleClick.Clear();

			// Increment timers
			for (int i = 0; i < m_doubleClickTimers.Length; i++) {
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

		internal static void Shutdown() {
			m_mouse?.Dispose();
			m_keyboard?.Dispose();
			m_diretInput?.Dispose();
		}
	}
}