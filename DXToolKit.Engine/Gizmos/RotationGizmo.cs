using System;
using SharpDX;
// ReSharper disable UnusedVariable

namespace DXToolKit.Engine {
	/// <summary>
	/// Gizmo used for rotation
	/// </summary>
	public class RotationGizmo : GizmoBase {
		private Primitive m_circle;
		private Primitive m_collider;

		private GizmoDirection m_direction;
		private float m_height = 0.01F;
		private float m_radius = 0.5F;
		private float m_width = 0.01F;
		private float m_colliderOuter = 0.65F;
		private float m_colliderInner = 0.35F;

		/// <summary>
		/// Creates a new instance of the rotation gizmo
		/// </summary>
		/// <param name="device">Device used to create resources</param>
		public RotationGizmo(GraphicsDevice device) : base(device) {
			m_circle = PrimitiveFactory.Tube(
				height: m_height,
				topOuterRadius: m_radius,
				bottomOuterRadius: m_radius,
				topInnerRadius: m_radius - m_width,
				bottomInnerRadius: m_radius - m_width,
				sides: 64
			);

			m_collider = PrimitiveFactory.Tube(
				height: m_height,
				topOuterRadius: m_colliderOuter,
				bottomOuterRadius: m_colliderOuter,
				topInnerRadius: m_colliderInner,
				bottomInnerRadius: m_colliderInner,
				sides: 8
			);
		}

		/// <inheritdoc />
		public override Matrix Render(DXCamera camera, PrimitiveRenderer renderer, Matrix world) {
			var (xMatrix, yMatrix, zMatrix) = ConstructMatrices(world);
			m_direction = HandleDrag(ref camera, ref m_collider, ref xMatrix, ref yMatrix, ref zMatrix, out var isDragging);
			world.Decompose(out var worldScale, out var worldRotation, out var worldTranslation);

			renderer.CustomPrimitive(m_circle, xMatrix, GetColor(GizmoDirection.X, m_direction == GizmoDirection.X));
			renderer.CustomPrimitive(m_circle, yMatrix, GetColor(GizmoDirection.Y, m_direction == GizmoDirection.Y));
			renderer.CustomPrimitive(m_circle, zMatrix, GetColor(GizmoDirection.Z, m_direction == GizmoDirection.Z));
			Vector3 rotation = Vector3.Zero;

			if (isDragging) {
				var tMatrix = xMatrix;
				if (m_direction == GizmoDirection.X) tMatrix = xMatrix;
				if (m_direction == GizmoDirection.Y) tMatrix = yMatrix;
				if (m_direction == GizmoDirection.Z) tMatrix = zMatrix;

				var planeNormal = Vector3.TransformNormal(Vector3.Up, tMatrix);
				var amount = 0.0F;
				var mouseMove = MouseMove(camera, planeNormal, worldTranslation, out var p1, out var p2);
				if (mouseMove) {
					p1 = worldTranslation - p1;
					p2 = worldTranslation - p2;

					p1.Normalize();
					p2.Normalize();

					if (!Vector3.NearEqual(p1, p2, Vector3.One * Mathf.ZeroTolerance)) {
						var invWorldRot = Matrix.Invert(Matrix.RotationQuaternion(worldRotation));
						var p1Inv = Vector3.TransformNormal(p1, invWorldRot);
						var p2Inv = Vector3.TransformNormal(p2, invWorldRot);
						var cross = Vector3.Cross(p1Inv, p2Inv);
						var rot = (float) Math.Acos(Vector3.Dot(p1, p2));
						if (!float.IsNaN(rot) && !float.IsInfinity(rot)) {
							if (cross[(int) m_direction] < 0) {
								amount += rot;
							} else {
								amount -= rot;
							}
						}
					}
				}

				if (m_direction == GizmoDirection.X) rotation.X += amount;
				if (m_direction == GizmoDirection.Y) rotation.Y += amount;
				if (m_direction == GizmoDirection.Z) rotation.Z += amount;

				rotation = Vector3.TransformNormal(rotation, world);
			}


			/*
			renderer.CustomPrimitive(m_collider, xMatrix, GetColor(GizmoDirection.X, m_direction == GizmoDirection.X));
			renderer.CustomPrimitive(m_collider, yMatrix, GetColor(GizmoDirection.Y, m_direction == GizmoDirection.Y));
			renderer.CustomPrimitive(m_collider, zMatrix, GetColor(GizmoDirection.Z, m_direction == GizmoDirection.Z));
			*/

			return Matrix.RotationYawPitchRoll(rotation.Y, rotation.X, rotation.Z);
		}

		/// <inheritdoc />
		protected override void OnDispose() { }
	}
}