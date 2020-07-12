using System.IO;
using SharpDX;

namespace DXToolKit {
	public class DXCamera {
		private Matrix m_uprotationMatrix = Matrix.Identity;

		/// <summary>
		/// Matrix controlling the rotation of the camera
		/// </summary>
		private Matrix m_rotationMatrix = Matrix.Identity;

		/// <summary>
		/// View matrix to be passed to a shader
		/// </summary>
		private Matrix m_viewMatrix = Matrix.Identity;

		/// <summary>
		/// Projection matrix to be passed to a shader
		/// </summary>
		private Matrix m_projectionMatrix = Matrix.Identity;

		/// <summary>
		/// Field of view in radians
		/// </summary>
		private float m_fieldOfView = Mathf.DegToRad(60.0F);

		/// <summary>
		/// Aspect ratio of the camera
		/// </summary>
		private float m_aspectRatio = 16.0F / 9.0F;

		/// <summary>
		/// Near clipping plane of the camera
		/// </summary>
		private float m_nearClippingPlane = 0.1F;

		/// <summary>
		/// Far clipping plane of the camera
		/// </summary>
		private float m_farClippingPlane = 1000.0F;

		/// <summary>
		/// Value indicating if the projection matrix has to be recreated
		/// Useful if multiple calls to projection matrix is run per frame, so instead of generating a whole new matrix each time, we generate significantly less per frame
		/// </summary>
		private bool m_hasProjectionChanged = true;

		/// <summary>
		/// Value indicating if the view matrix has to be recreated.
		/// Useful if multiple calls to view matrix is run per frame, so instead of generating a whole new matrix each time, we generate significantly less per frame
		/// </summary>
		private bool m_hasViewChanged = true;

		/// <summary>
		/// Position of the camera
		/// </summary>
		private Vector3 m_position = Vector3.Zero;

		/// <summary>
		/// Camera yaw
		/// </summary>
		private float m_yaw;

		/// <summary>
		/// Camera pitch
		/// </summary>
		private float m_pitch;

		/// <summary>
		/// Camera roll
		/// </summary>
		private float m_roll;

		/// <summary>
		/// Target when camera is in orbit mode
		/// </summary>
		private Vector3 m_target;

		/// <summary>
		/// Gets or sets a value if the pitch of the camera should be clamped between -89 and 89 degrees.
		/// Default: true
		/// </summary>
		public bool ClampPitch = true;

		/// <summary>
		/// Gets or sets the position of the camera
		/// Be aware that this may not work as intended if the camera is in orbit mode
		/// </summary>
		public Vector3 Position {
			get => m_position;
			set {
				m_position = value;
				m_hasViewChanged = true;
			}
		}

		/// <summary>
		/// Gets or sets the yaw of the camera in radians.
		/// </summary>
		public float Yaw {
			get => m_yaw;
			set => m_yaw = value;
		}

		/// <summary>
		/// Gets or sets the pitch of the camera in radians.
		/// </summary>
		public float Pitch {
			get => m_pitch;
			set => m_pitch = value;
		}

		/// <summary>
		/// Gets or sets the roll of the camera in radians.
		/// </summary>
		public float Roll {
			get => m_roll;
			set => m_roll = value;
		}

		/// <summary>
		/// Gets or sets the horizontal field of view of the camera in degrees
		/// </summary>
		public float FieldOfView {
			get => Mathf.RadToDeg(m_fieldOfView);
			set {
				m_fieldOfView = Mathf.DegToRad(value);
				m_hasProjectionChanged = true;
			}
		}

		/// <summary>
		/// Gets or sets the aspect ratio of the camera
		/// </summary>
		public float AspectRatio {
			get => m_aspectRatio;
			set {
				m_aspectRatio = value;
				m_hasProjectionChanged = true;
			}
		}

		/// <summary>
		/// Gets or sets the near clipping plane of the camera.
		/// </summary>
		public float NearClippingPlane {
			get => m_nearClippingPlane;
			set {
				m_nearClippingPlane = value;
				m_hasProjectionChanged = true;
			}
		}

		/// <summary>
		/// Gets or sets the far clipping plane of the camera.
		/// </summary>
		public float FarClippingPlane {
			get => m_farClippingPlane;
			set {
				m_farClippingPlane = value;
				m_hasProjectionChanged = true;
			}
		}

		/// <summary>
		/// Gets a value indicating if the camera is in orbit mode
		/// </summary>
		public bool IsOrbitCamera { get; private set; }

		/// <summary>
		/// Gets or sets the target of the camera while in orbit mode
		/// </summary>
		public Vector3 Target {
			get => m_target;
			set {
				m_target = value;
				Orbit();
			}
		}

		/// <summary>
		/// Gets or sets the distance from the camera to the target while in orbit mode
		/// </summary>
		public float TargetDistance {
			get => Vector3.Distance(m_target, m_position);
			set => SetOrbit(m_yaw, m_pitch, value);
		}


		public Vector3 UpVector {
			get => m_uprotationMatrix.Up;
			set {
				m_uprotationMatrix.Up = value;
				m_uprotationMatrix.Forward = Vector3.Cross(value, m_uprotationMatrix.Right);
				m_uprotationMatrix.Left = Vector3.Cross(value, m_uprotationMatrix.Forward);
				Rotate(0, 0, 0);
				// m_hasViewChanged = true;
			}
		}

		/// <summary>
		/// Gets the view matrix of the camera
		/// </summary>
		public Matrix ViewMatrix {
			get {
				if (m_hasViewChanged) {
					// ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
					if (IsOrbitCamera) {
						m_viewMatrix = Matrix.LookAtLH(m_position, m_target, m_rotationMatrix.Up);
					} else {
						m_viewMatrix = Matrix.LookAtLH(m_position, m_position - m_rotationMatrix.Forward, m_rotationMatrix.Up);
					}

					m_hasViewChanged = false;
					m_hasFustrumChanged = true;
				}

				return m_viewMatrix;
			}
		}

		/// <summary>
		/// Gets the projection matrix of the camera
		/// </summary>
		public Matrix ProjectionMatrix {
			get {
				if (m_hasProjectionChanged) {
					if (m_isOrthographic) {
						m_projectionMatrix = Matrix.OrthoLH(m_orthoWidth / m_orthoScaling, m_orthoHeight / m_orthoScaling, m_nearClippingPlane, m_farClippingPlane);
					} else {
						m_projectionMatrix = Matrix.PerspectiveFovLH(m_fieldOfView, m_aspectRatio, m_nearClippingPlane, m_farClippingPlane);
					}

					m_hasProjectionChanged = false;
					m_hasFustrumChanged = true;
				}

				return m_projectionMatrix;
			}
		}

		public Matrix3x2 D2DTransformMatrix {
			get {
				var halfWidth = m_orthoWidth / 2.0F;
				var halfHeight = m_orthoHeight / 2.0F;
				// Start by flipping the whole matrix so that positive Y is up, and negative Y is down
				return Matrix3x2.Scaling(1, -1) *
				       Matrix3x2.Scaling(m_orthoScaling) *
				       // Translate based on position
				       Matrix3x2.Translation((-m_position.X * m_orthoScaling), (m_position.Y * m_orthoScaling)) *
				       // Rotate based on roll
				       Matrix3x2.Rotation(m_roll) *
				       // Translate to center screen
				       Matrix3x2.Translation(halfWidth, halfHeight);
			}
		}

		/// <summary>
		/// Gets a the view and projection matrix as one.
		/// </summary>
		public Matrix ViewProjection => ViewMatrix * ProjectionMatrix;

		/// <summary>
		/// Gets a transposed view and projection matrix
		/// </summary>
		public Matrix ViewProjectionTransposed => Matrix.Transpose(ViewProjection);

		public bool m_hasFustrumChanged = true;
		private BoundingFrustum m_boundingFrustum;

		public BoundingFrustum CameraFrustum {
			get {
				if (m_hasFustrumChanged) {
					m_boundingFrustum.Matrix = ViewProjection;
					m_hasFustrumChanged = false;
				}

				return m_boundingFrustum;
			}
		}


		private bool m_isOrthographic;
		private float m_orthoWidth = 1.0F;
		private float m_orthoHeight = 1.0F;
		private float m_orthoScaling = 1.0F;

		public bool IsOrthographic {
			get => m_isOrthographic;
			set {
				m_isOrthographic = value;
				m_hasProjectionChanged = true;
			}
		}

		public float OrthoWidth {
			get => m_orthoWidth;
			set {
				m_orthoWidth = value;
				m_hasProjectionChanged = true;
				IsOrthographic = true;
			}
		}

		public float OrthoHeight {
			get => m_orthoHeight;
			set {
				m_orthoHeight = value;
				m_hasProjectionChanged = true;
				IsOrthographic = true;
			}
		}

		public float OrthoScaling {
			get => m_orthoScaling;
			set {
				m_orthoScaling = value;
				m_hasProjectionChanged = true;
				IsOrthographic = true;
			}
		}

		public RectangleF OrthoCameraBounds {
			get {
				var halfWidth = m_orthoWidth / 2.0F / m_orthoScaling;
				var halfHeight = m_orthoHeight / 2.0F / m_orthoScaling;
				return new RectangleF(m_position.X - halfWidth, m_position.Y + halfHeight - (halfHeight * 2), halfWidth * 2, halfHeight * 2);
			}
		}

		/// <summary>
		/// Translates the camera by the input amount, based on its rotation.
		/// If in orbit mode, only Z +- translation is used as a "zoom" value.
		/// </summary>
		/// <param name="x">X translation</param>
		/// <param name="y">Y translation</param>
		/// <param name="z">Z translation</param>
		public void Translate(float x, float y, float z) {
			Translate(new Vector3(x, y, z));
		}

		/// <summary>
		/// Translates the camera by the input amount, based on its rotation.
		/// If in orbit mode, only Z +- translation is used as a "zoom" value.
		/// </summary>
		/// <param name="translation">Amount to translate by</param>
		public void Translate(Vector3 translation) {
			// If is orbit cam, translate based on that
			if (IsOrbitCamera) {
				// Add to position, to allow for z translation
				m_position += Vector3.TransformNormal(translation, m_rotationMatrix);
				// Calculate orbit
				Orbit();
			} else {
				// If not, add a transformed vector to current position
				m_position += Vector3.TransformNormal(translation, m_rotationMatrix);
			}

			// Update view
			m_hasViewChanged = true;
		}

		/// <summary>
		/// Rotates the camera by the input amount
		/// </summary>
		/// <param name="yaw">Amount of yaw in radians to rotate</param>
		/// <param name="pitch">Amount of pitch in radians to rotate</param>
		/// <param name="roll">Amount of roll in radians to rotate</param>
		public void Rotate(float yaw, float pitch, float roll) {
			// Add to yaw, pitch and roll
			m_yaw += yaw;
			m_pitch += pitch;
			m_roll += roll;

			// Set camera rotation to the new yaw pitch roll
			SetRotation(m_yaw, m_pitch, m_roll);
		}

		/// <summary>
		/// Sets the rotation of the camera to a given yaw, pitch and roll, in radians.
		/// </summary>
		/// <param name="yaw">Target yaw in radians</param>
		/// <param name="pitch">Target pitch in radians</param>
		/// <param name="roll">Target roll in radians</param>
		public void SetRotation(float yaw, float pitch, float roll) {
			// Save yaw, pitch and roll
			m_yaw = yaw;
			m_pitch = pitch;
			m_roll = roll;

			// Check if pitch should be clamped
			if (ClampPitch) {
				if (m_pitch > Mathf.DegToRad(89F)) {
					m_pitch = Mathf.DegToRad(89F);
				}

				if (m_pitch < Mathf.DegToRad(-89F)) {
					m_pitch = Mathf.DegToRad(-89F);
				}
			}

			// If orbiting, run orbit mode
			if (IsOrbitCamera) {
				Orbit();
			} else {
				// Else create a fresh rotation matrix.
				m_rotationMatrix = Matrix.RotationYawPitchRoll(m_yaw, m_pitch, m_roll) * m_uprotationMatrix;
			}

			// Update view matrix
			m_hasViewChanged = true;
		}

		/// <summary>
		/// Orbits the camera around m_target, using m_yaw and m_pitch
		/// </summary>
		private void Orbit() {
			if (IsOrbitCamera) {
				SetOrbit(m_yaw, m_pitch);
			}
		}

		/// <summary>
		/// Sets the orbit of the camera
		/// </summary>
		private void SetOrbit(float yaw, float pitch, float? distance = null) {
			if (IsOrbitCamera) {
				m_yaw = yaw;
				m_pitch = pitch;
				m_rotationMatrix = Matrix.RotationYawPitchRoll(m_yaw, m_pitch, 0);
				var dir = Vector3.TransformNormal(-Vector3.ForwardLH, m_rotationMatrix);

				// If distance is not input, generate it based on distance from position to target
				if (distance == null) {
					distance = Vector3.Distance(m_position, m_target);
				}

				m_position = m_target + (dir * (float) distance);
				m_hasViewChanged = true;
			}
		}

		/// <summary>
		/// Enables / Disables orbit camera.
		/// </summary>
		/// <param name="target">The target to orbit, if null orbit is disabled</param>
		public void ToggleOrbitCamera(Vector3? target = null) {
			if (target != null) {
				m_target = (Vector3) target;
				IsOrbitCamera = true;
				Orbit();
			} else {
				IsOrbitCamera = false;
			}

			m_hasViewChanged = true;
		}

		/// <summary>
		/// Creates a save string of the cameras position and rotation.
		/// </summary>
		/// <returns>A string containing the position and rotation of the camera</returns>
		public string SavePosition() {
			var position = string.Format("{0}|{1}|{2}|", m_position.X, m_position.Y, m_position.Z);

			var rotation = string.Format("{0}|{1}|{2}", m_yaw, m_pitch, m_roll);
			return position + rotation;
		}

		/// <summary>
		/// Sets the cameras position and rotation based on input string
		/// </summary>
		/// <param name="saveStr">The save string generated from SavePosition() function</param>
		public void LoadPosition(string saveStr) {
			var split = saveStr.Split('|');
			m_position.X = float.Parse(split[0]);
			m_position.Y = float.Parse(split[1]);
			m_position.Z = float.Parse(split[2]);
			m_yaw = float.Parse(split[3]);
			m_pitch = float.Parse(split[4]);
			m_roll = float.Parse(split[5]);
			SetRotation(m_yaw, m_pitch, m_roll);
			m_hasViewChanged = true;
		}

		/// <summary>
		/// Saves camera position information to a file
		/// </summary>
		/// <param name="filename">The file to save the position to</param>
		public void SaveToFile(string filename) {
			var pos = SavePosition();
			File.WriteAllText(filename, pos);
		}

		/// <summary>
		/// Loads the camera position from a file
		/// </summary>
		/// <param name="filename">The file to load position from</param>
		public void LoadFromFile(string filename) {
			if (File.Exists(filename)) {
				var pos = File.ReadAllText(filename);
				LoadPosition(pos);
			}
		}

		public virtual Ray ScreenToWorld(Vector2 screen, float screenWidth, float screenHeight) {
			return Ray.GetPickRay((int) screen.X, (int) screen.Y, new ViewportF(0, 0, screenWidth, screenHeight, 0, 1), ViewProjection);
		}

		public virtual Vector3 ScreenToWorld(Vector2 screen, float screenWidth, float screenHeight, float distance) {
			var ray = ScreenToWorld(screen, screenWidth, screenHeight);
			return ray.Position + ray.Direction * distance;
		}

		public Vector2 WorldToScreen(Vector3 world, float screenWidth, float screenHeight) {
			var result = Vector3.Project(world, 0, 0, screenWidth, screenHeight, 0, 1, ViewProjection);
			return new Vector2(result.X, result.Y);
		}
	}
}