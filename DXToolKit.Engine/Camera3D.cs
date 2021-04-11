using SharpDX;
using SharpDX.DirectInput;
using System.Linq;

namespace DXToolKit.Engine {
	/// <summary>
	/// Camera key binds used by Camera3D class
	/// </summary>
	public struct CameraKeyBinds {
		/// <summary>
		/// Keys used to move forward
		/// </summary>
		public Key[] ForwardKeys;

		/// <summary>
		/// Keys used to move backward
		/// </summary>
		public Key[] BackwardKeys;

		/// <summary>
		/// Keys used to translate left
		/// </summary>
		public Key[] LeftKeys;

		/// <summary>
		/// Keys used to translate right
		/// </summary>
		public Key[] RightKeys;

		/// <summary>
		/// Keys used to translate up
		/// </summary>
		public Key[] UpKeys;

		/// <summary>
		/// Keys used to translate down
		/// </summary>
		public Key[] DownKeys;

		/// <summary>
		/// Keys used to increase speed
		/// </summary>
		public Key[] SprintKeys;

		/// <summary>
		/// Mouse button used to rotate the camera
		/// </summary>
		public MouseButton[] RotateButtons;

		/// <summary>
		/// Gets some default keybindings for the camera (WASD / up down left right arrow etc)
		/// </summary>
		public static CameraKeyBinds Default => new CameraKeyBinds {
			ForwardKeys = new[] {Key.W, Key.Up},
			BackwardKeys = new[] {Key.S, Key.Down},
			LeftKeys = new[] {Key.A, Key.Left},
			RightKeys = new[] {Key.D, Key.Right},
			UpKeys = new[] {Key.Space},
			DownKeys = new[] {Key.C, Key.LeftControl},
			SprintKeys = new[] {Key.LeftShift, Key.RightShift},
			RotateButtons = new[] {MouseButton.Right},
		};

		/// <summary>
		/// Gets a value indicating if a Forward key is pressed
		/// </summary>
		public bool Forward() => ForwardKeys.Select(Input.KeyPressed).Any(pressed => pressed);

		/// <summary>
		/// Gets a value indicating if a Backward key is pressed
		/// </summary>
		public bool Backward() => BackwardKeys.Select(Input.KeyPressed).Any(pressed => pressed);

		/// <summary>
		/// Gets a value indicating if a Left key is pressed
		/// </summary>
		public bool Left() => LeftKeys.Select(Input.KeyPressed).Any(pressed => pressed);

		/// <summary>
		/// Gets a value indicating if a Right key is pressed
		/// </summary>
		public bool Right() => RightKeys.Select(Input.KeyPressed).Any(pressed => pressed);

		/// <summary>
		/// Gets a value indicating if a Up key is pressed
		/// </summary>
		public bool Up() => UpKeys.Select(Input.KeyPressed).Any(pressed => pressed);

		/// <summary>
		/// Gets a value indicating if a Down key is pressed
		/// </summary>
		public bool Down() => DownKeys.Select(Input.KeyPressed).Any(pressed => pressed);

		/// <summary>
		/// Gets a value indicating if a Sprint key is pressed
		/// </summary>
		public bool Sprint() => SprintKeys.Select(Input.KeyPressed).Any(pressed => pressed);

		/// <summary>
		/// Gets a value indicating if a Rotate mouse button is pressed
		/// </summary>
		public bool Rotate() => RotateButtons.Select(Input.MousePressed).Any(pressed => pressed);

		/// <summary>
		/// Gets a value indicating if a Rotate mouse button is released
		/// </summary>
		public bool RotateStop() => RotateButtons.Select(Input.MouseUp).Any(pressed => pressed);
	}

	/// <summary>
	/// Class used to calculate a Projection and View matrix
	/// </summary>
	public class Camera3D : DXCamera {
		private const float ROTATION_SLOWDOWN_FACTOR = 24.0F;
		private const float TARGET_LERP_SPEED_FACTOR = 10.0F;
		private const float TARGET_ZOOM_SPEED_FACTOR = 20.0F;

		/// <summary>
		/// Creates a new instance of a Camera3D used to calculate a Projection and View matrix
		/// </summary>
		public Camera3D() {
			Graphics.Device.OnResizeEnd += () => {
				AspectRatio = EngineConfig.ScreenWidth / (float) EngineConfig.ScreenHeight;
			};
			AspectRatio = EngineConfig.ScreenWidth / (float) EngineConfig.ScreenHeight;
		}

		/// <summary>
		/// Amount of units to move per second
		/// </summary>
		private float m_moveSpeed = 10.0F;

		public float MoveSpeed {
			get => m_moveSpeed;
			set => m_moveSpeed = value;
		}

		/// <summary>	
		/// Rotation sensitivity in degrees per pixel mouse has moved
		/// </summary>
		private float m_mouseSensitivity = 0.1F;

		/// <summary>
		/// Value controlling if the zoom (Z axis) of a orbit camera should have a smooth transition (Lerp)
		/// </summary>
		private bool m_smoothOrbitZoom = true;


		private bool m_smoothTransitionOrbitCam;

		/// <summary>
		/// Value controlling if the rotation should keep going after the mouse is let go when orbiting a target, with a slowdown
		/// </summary>
		private bool m_keepOrbitRotationVelocity = true;

		private Vector2 m_rotationVelocity = Vector2.Zero;
		private bool m_flipHorizontalRotation = false;
		private bool m_flipVerticalRotation = false;
		private float m_targetZoom = 10;
		private Vector3 m_newTarget;

		/// <summary>
		/// Controls key bindings used by the camera
		/// </summary>
		private CameraKeyBinds m_keybindings = CameraKeyBinds.Default;

		/// <summary>
		/// Call once to make the camera lerp to focus on a new target
		/// </summary>
		/// <param name="target"></param>
		public void SmoothLerpToTarget(Vector3 target) {
			m_targetZoom = TargetDistance;
			if (IsOrbitCamera) {
				// Get current target
				m_newTarget = target;

				// Toggle orbit camera with new target
				ToggleOrbitCamera(Target);
			} else {
				// If toggling orbit camera for the first time, snap directly to new target.
				m_newTarget = target;
				ToggleOrbitCamera(target);
			}

			m_smoothTransitionOrbitCam = true;
		}

		/// <summary>
		/// Updates the camera, gathering inputs and moving it accordingly
		/// </summary>
		public void Update() {
			// Get transformed mouse movement
			var mouseMove = Input.MouseMove * (Mathf.Pi / 180.0F) * m_mouseSensitivity;

			if (m_keybindings.Rotate()) {
				Rotate(m_flipHorizontalRotation ? -mouseMove.X : mouseMove.X,
					m_flipVerticalRotation ? -mouseMove.Y : mouseMove.Y, 0);

				if (m_keepOrbitRotationVelocity) {
					m_rotationVelocity = Vector2.Zero;
				}
			}

			if (IsOrbitCamera) {
				if (m_keepOrbitRotationVelocity) {
					if (m_keybindings.RotateStop()) {
						m_rotationVelocity = new Vector2(m_flipHorizontalRotation ? -mouseMove.X : mouseMove.X,
							m_flipVerticalRotation ? -mouseMove.Y : mouseMove.Y);
					}

					if (m_rotationVelocity.LengthSquared() < 0.000000001F) {
						m_rotationVelocity.X = 0;
						m_rotationVelocity.Y = 0;
					} else {
						m_rotationVelocity /= 1 + Time.DeltaTime * ROTATION_SLOWDOWN_FACTOR;
					}

					Rotate(m_rotationVelocity.X, m_rotationVelocity.Y, 0);
				}

				if (m_smoothTransitionOrbitCam) {
					// ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
					if (Vector3.NearEqual(Target, m_newTarget, new Vector3(Mathf.ZeroTolerance))) {
						Target = m_newTarget;
					} else {
						Target = Vector3.Lerp(Target, m_newTarget, Time.DeltaTime * TARGET_LERP_SPEED_FACTOR);
					}
				}

				if (Mathf.Abs(Input.MouseWheelDelta) > 0) {
					if (Input.MouseWheelDelta > 0) {
						m_targetZoom -= 0.1F * m_targetZoom;
					} else {
						m_targetZoom += 0.1F * m_targetZoom;
					}
				}

				if (m_smoothOrbitZoom) {
					TargetDistance = Mathf.Lerp(TargetDistance, m_targetZoom, Time.DeltaTime * TARGET_ZOOM_SPEED_FACTOR);
				} else {
					TargetDistance = m_targetZoom;
				}
			} else {
				var moveAmount = m_moveSpeed * Time.DeltaTime;
				if (m_keybindings.Sprint()) {
					moveAmount *= 10.0F;
				}

				if (m_keybindings.Forward()) {
					Translate(0, 0, moveAmount);
				}

				if (m_keybindings.Backward()) {
					Translate(0, 0, -moveAmount);
				}

				if (m_keybindings.Left()) {
					Translate(-moveAmount, 0, 0);
				}

				if (m_keybindings.Right()) {
					Translate(moveAmount, 0, 0);
				}

				if (m_keybindings.Up()) {
					Translate(0, moveAmount, 0);
				}

				if (m_keybindings.Down()) {
					Translate(0, -moveAmount, 0);
				}
			}
		}
	}
}