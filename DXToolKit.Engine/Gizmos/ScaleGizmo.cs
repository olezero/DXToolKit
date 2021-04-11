using SharpDX;

// ReSharper disable UnusedVariable

namespace DXToolKit.Engine {
	/// <summary>
	/// Gizmo used for scaling
	/// </summary>
	public class ScaleGizmo : GizmoBase {
		private Primitive m_line;
		private Primitive m_cube;
		private Primitive m_collider;
		private GizmoDirection m_direction;
		private Vector3 m_scaling = Vector3.One;

		private float m_arrowLength = 0.5F;
		private float m_capLength = 0.1F;
		private float m_lineRadius = 0.005F;
		private float m_capRadius = 0.02F;

		/// <summary>
		/// Creates a the gizmo used for scaling
		/// </summary>
		public ScaleGizmo(GraphicsDevice device) : base(device) {
			m_line = PrimitiveFactory.Cone(m_arrowLength - m_capLength, m_lineRadius, m_lineRadius, 8);
			m_cube = PrimitiveFactory.Cube(m_capRadius, m_capRadius, m_capRadius);
			m_collider = PrimitiveFactory.Cone(m_arrowLength, 0.05F, 0.05F, 8);
		}

		/// <inheritdoc />
		public override Matrix Render(DXCamera camera, PrimitiveRenderer renderer, Matrix world) {
			var (xMatrix, yMatrix, zMatrix) = ConstructMatrices(world);
			xMatrix = FaceCamera(camera, ref xMatrix, out var xInverted);
			yMatrix = FaceCamera(camera, ref yMatrix, out var yInverted);
			zMatrix = FaceCamera(camera, ref zMatrix, out var zInverted);

			m_direction = HandleDrag(ref camera, ref m_collider, ref xMatrix, ref yMatrix, ref zMatrix, out var isDragging);
			world.Decompose(out var worldScale, out var worldRotation, out var worldTranslation);


			if (isDragging) {
				var tMatrix = xMatrix;
				if (m_direction == GizmoDirection.X) tMatrix = xMatrix;
				if (m_direction == GizmoDirection.Y) tMatrix = yMatrix;
				if (m_direction == GizmoDirection.Z) tMatrix = zMatrix;
				var cameraNormal = Vector3.Normalize(worldTranslation - camera.Position);
				var cross = Vector3.Cross(tMatrix.Up, cameraNormal);
				var planeNormal = Vector3.Normalize(Vector3.Cross(cross, tMatrix.Up));
				var invRot = Matrix.Invert(Matrix.RotationQuaternion(worldRotation));
				if (Input.MousePressed(MouseButton.Left)) {
					var move = MouseMove(camera, planeNormal, worldTranslation);
					move = Vector3.TransformCoordinate(move, invRot);

					if (m_direction == GizmoDirection.X) {
						move.Y = 0;
						move.Z = 0;
						if (!xInverted) {
							move.X = -move.X;
						}
					}

					if (m_direction == GizmoDirection.Y) {
						move.X = 0;
						move.Z = 0;
						if (yInverted) {
							move.Y = -move.Y;
						}
					}

					if (m_direction == GizmoDirection.Z) {
						move.Y = 0;
						move.X = 0;
						if (zInverted) {
							move.Z = -move.Z;
						}
					}

					m_scaling += move * 2;
				}
			}


			DrawArrow(renderer, ref xMatrix, GizmoDirection.X);
			DrawArrow(renderer, ref yMatrix, GizmoDirection.Y);
			DrawArrow(renderer, ref zMatrix, GizmoDirection.Z);

			m_scaling = Vector3.Clamp(m_scaling, Vector3.Zero, m_scaling);
			return Matrix.Scaling(m_scaling);
		}

		// ReSharper disable once MemberCanBeMadeStatic.Local
		private Matrix FaceCamera(DXCamera camera, ref Matrix matrix, out bool iverted) {
			var camLook = Vector3.Normalize(camera.Position - matrix.TranslationVector);
			if (Vector3.Dot(camLook, matrix.Up) < 0) {
				iverted = true;
				return Matrix.RotationZ(Mathf.Pi) * matrix;
			}

			iverted = false;
			return matrix;
		}

		private void DrawArrow(PrimitiveRenderer renderer, ref Matrix world, GizmoDirection direction) {
			var clr = GetColor(direction, direction == m_direction);
			renderer.CustomPrimitive(m_line, Matrix.Translation(0, m_lineRadius, 0) * world, clr);
			renderer.CustomPrimitive(m_cube, Matrix.Translation(0, m_arrowLength - m_capLength, 0) * world, clr);
		}

		/// <inheritdoc />
		protected override void OnDispose() { }
	}
}