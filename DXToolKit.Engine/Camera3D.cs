using SharpDX;
using SharpDX.DirectInput;
using System.Linq;
using SharpDX.Direct3D11;

namespace DXToolKit.Engine {
	public struct CameraKeyBinds {
		public Key[] ForwardKeys;
		public Key[] BackwardKeys;
		public Key[] LeftKeys;
		public Key[] RightKeys;
		public Key[] UpKeys;
		public Key[] DownKeys;
		public Key[] SprintKeys;

		public MouseButton[] RotateButtons;

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

		public bool Forward() => ForwardKeys.Select(Input.KeyPressed).Any(pressed => pressed);
		public bool Backward() => BackwardKeys.Select(Input.KeyPressed).Any(pressed => pressed);
		public bool Left() => LeftKeys.Select(Input.KeyPressed).Any(pressed => pressed);
		public bool Right() => RightKeys.Select(Input.KeyPressed).Any(pressed => pressed);
		public bool Up() => UpKeys.Select(Input.KeyPressed).Any(pressed => pressed);
		public bool Down() => DownKeys.Select(Input.KeyPressed).Any(pressed => pressed);
		public bool Sprint() => SprintKeys.Select(Input.KeyPressed).Any(pressed => pressed);
		public bool Rotate() => RotateButtons.Select(Input.MousePressed).Any(pressed => pressed);
		public bool RotateStop() => RotateButtons.Select(Input.MouseUp).Any(pressed => pressed);
	}

	public class Camera3D : DXCamera {
		private const float ROTATION_SLOWDOWN_FACTOR = 12.0F;
		private const float TARGET_LERP_SPEED_FACTOR = 10.0F;
		private const float TARGET_ZOOM_SPEED_FACTOR = 20.0F;

		public Camera3D() {
			Graphics.Device.OnResizeEnd += () => { AspectRatio = (float) EngineConfig.ScreenWidth / (float) EngineConfig.ScreenHeight; };
		}

		/// <summary>
		/// Amount of units to move per second
		/// </summary>
		private float m_moveSpeed = 10.0F;

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

		private float m_targetZoom;

		/// <summary>
		/// Controls key bindings used by the camera
		/// </summary>
		private CameraKeyBinds m_keybindings = CameraKeyBinds.Default;

		private Vector3 m_newTarget;

		public void SmoothLerpToTarget(Vector3 target) {
			m_targetZoom = TargetDistance;
			if (IsOrbitCamera) {
				// Get current target
				m_newTarget = target;

				// Toggle orbit camera with new target
				ToggleOrbitCamera(Target);
			}
			else {
				// If toggling orbit camera for the first time, snap directly to new target.
				m_newTarget = target;
				ToggleOrbitCamera(target);
			}

			m_smoothTransitionOrbitCam = true;
		}

		public void Update() {
			// Get transformed mouse movement
			var mouseMove = Input.MouseMove * (Mathf.PI / 180.0F) * m_mouseSensitivity;

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
					}
					else {
						m_rotationVelocity /= 1 + Time.DeltaTime * ROTATION_SLOWDOWN_FACTOR;
					}

					Rotate(m_rotationVelocity.X, m_rotationVelocity.Y, 0);
				}

				if (m_smoothTransitionOrbitCam) {
					Target = Vector3.Lerp(Target, m_newTarget, Time.DeltaTime * TARGET_LERP_SPEED_FACTOR);
				}

				if (Mathf.Abs(Input.MouseWheelDelta) > 0) {
					if (Input.MouseWheelDelta > 0) {
						m_targetZoom /= 1.2F;
					}
					else {
						m_targetZoom *= 1.2F;
					}
				}

				if (m_smoothOrbitZoom) {
					TargetDistance = Mathf.Lerp(TargetDistance, m_targetZoom, Time.DeltaTime * TARGET_ZOOM_SPEED_FACTOR);
				}
				else {
					TargetDistance = m_targetZoom;
				}
			}
			else {
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