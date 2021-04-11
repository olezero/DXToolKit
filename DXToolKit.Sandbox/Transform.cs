using System;
using SharpDX;

namespace DXToolKit.Sandbox {
	public class Transform {
		private Transform m_parent;

		private Vector3 m_position;
		private Matrix m_rotation;
		private Vector3 m_scaling;

		private Matrix m_local;
		// private Matrix m_world;

		private bool m_hasLocalChanged = false;
		private bool m_hasWorldChanged = false;

		public Vector3 Position {
			get => m_position;
			set {
				m_position = value;
				m_hasLocalChanged = true;
			}
		}

		public Vector3 Forward => m_rotation.Forward;

		public Matrix Local {
			get {
				if (m_hasLocalChanged) {
					m_local = Matrix.Scaling(m_scaling) * m_rotation *
					          Matrix.Translation(m_position);
				}

				return m_local;
			}
			set { m_local = value; }
		}

		public Matrix World {
			get {
				// TODO: Save/Cache for multiple requests 
				if (m_parent != null) {
					return Local * m_parent.World;
				}

				return Local;
			}
			// TODO: Set
		}

		public Transform Parent {
			get => m_parent;
			set => SetParent(value);
		}

		public Transform() {
			m_local = Matrix.Identity;
			// m_world = Matrix.Identity;
			m_rotation = Matrix.Identity;
			m_position = Vector3.Zero;
			m_scaling = Vector3.One;
			m_hasLocalChanged = true;
			m_hasWorldChanged = true;
		}

		private void UnsetParent() {
			if (m_parent == null) return;
			m_parent = null;
		}

		private void SetParent(Transform parent) {
			UnsetParent();
			if (parent == null) return;
			
			// Need to update our position, rotation and scaling from local to world space
			// Then set parent
			// Then convert position, rotation and scaling to local
			
			m_parent = parent;
		}

		private Matrix ToWorld(Matrix localMatrix) {
			return localMatrix;
		}

		private Matrix ToLocal(Matrix worldMatrix) {
			return worldMatrix;
		}

		public void Translate(Vector3 translation) {
			m_position += translation;
			m_hasLocalChanged = true;
		}

		public void Translate(float x, float y, float z) {
			m_position.X += x;
			m_position.Y += y;
			m_position.Z += z;
			m_hasLocalChanged = true;
		}

		public void Rotate(float yaw, float pitch, float roll) {
			m_rotation *= Matrix.RotationYawPitchRoll(yaw, pitch, roll);
			m_hasLocalChanged = true;
		}

		public void SetRotation(float yaw = 0, float pitch = 0, float roll = 0) {
			m_rotation = Matrix.RotationYawPitchRoll(yaw, pitch, roll);
			m_hasLocalChanged = true;
		}

		public void Scale(Vector3 scaling) {
			m_scaling = scaling;
			m_hasLocalChanged = true;
		}

		public void Scale(float scale) {
			m_scaling.X = m_scaling.Y = m_scaling.Z = scale;
			m_hasLocalChanged = true;
		}

		public void Scale(float x, float y, float z) {
			m_scaling.X = x;
			m_scaling.Y = y;
			m_scaling.Z = z;
			m_hasLocalChanged = true;
		}

		// Rotate so transform "looks at" a given point in space, with "roll" set to where Up matches input up as closely as possible
		public void LookAt(Vector3 point, Vector3 Up) {
			m_rotation = Matrix.LookAtLH(m_position, point, Up);
			m_hasLocalChanged = true;
		}

		// Rotate around a given point given an angle in radians
		public void RotateAround(Vector3 point, float angle) { }
	}
}