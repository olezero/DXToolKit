using SharpDX;

namespace DXToolKit.Engine {
	/// <summary>
	/// Gizmo used for translation calculations
	/// </summary>
	public class TranslationGizmo : GizmoBase {
		private Primitive m_line;
		private Primitive m_cap;
		private Primitive m_collider;
		private GizmoDirection m_direction;

		private float m_arrowLength = 0.5F;
		private float m_capLength = 0.1F;
		private float m_lineRadius = 0.005F;
		private float m_capRadius = 0.02F;

		/// <summary>
		/// Creates a new instance of the gizmo used for translation calculations
		/// </summary>
		public TranslationGizmo(GraphicsDevice device) : base(device) {
			m_line = PrimitiveFactory.Cone(m_arrowLength - m_capLength, m_lineRadius, m_lineRadius, 8);
			m_cap = PrimitiveFactory.Cone(m_capLength, m_capRadius, 0.0F, 8);
			m_collider = PrimitiveFactory.Cone(m_arrowLength, 0.05F, 0.05F, 8);
		}

		/// <inheritdoc />
		public override Matrix Render(DXCamera camera, PrimitiveRenderer renderer, Matrix world) {
			var (xMatrix, yMatrix, zMatrix) = ConstructMatrices(world);

			xMatrix = FaceCamera(camera, ref xMatrix);
			yMatrix = FaceCamera(camera, ref yMatrix);
			zMatrix = FaceCamera(camera, ref zMatrix);

			m_direction = HandleDrag(ref camera, ref m_collider, ref xMatrix, ref yMatrix, ref zMatrix, out var isDragging);
			world.Decompose(out var worldScale, out var worldRotation, out var worldTranslation);


			/*
			var cameraNormal = Vector3.Normalize(camera.Position);
			var xDirection = Vector3.TransformNormal(Vector3.Up, xMatrix);
			cameraNormal.Y = 0;
			cameraNormal.Normalize();
			
			Debug.Arrow(Vector3.Zero, xDirection, 1, Color.Yellow);
			Debug.Arrow(Vector3.Zero, cameraNormal, 1, Color.Yellow);
			

			var cross = Vector3.Cross(xDirection, cameraNormal);
			cross.Normalize();

			Debug.Arrow(Vector3.Zero, cross, 1, Color.White);
			
			
			Debug.Plane(cross, Vector3.Zero, 1, Color.White);
			Debug.Plane(cameraNormal, Vector3.Zero, 1, Color.White);
			*/

			/*
			var cameraNormal = Vector3.Normalize(camera.Position);
			cameraNormal.Normalize();

			if (m_direction == GizmoDirection.X) {
				Debug.Arrow(Vector3.Zero, xMatrix.Up, 1, Color.Blue);
				Debug.Arrow(Vector3.Zero, xMatrix.Right, 1, Color.Red);
				Debug.Arrow(Vector3.Zero, xMatrix.Backward, 1, Color.Green);
				Debug.Arrow(Vector3.Zero, cameraNormal, 1, Color.Yellow);

				var cross = Vector3.Cross(xMatrix.Up, cameraNormal);
				cross.Normalize();
				Debug.Arrow(Vector3.Zero, cross, 1, Color.Yellow);

				cross = Vector3.Cross(cross, xMatrix.Up);
				cross.Normalize();
				Debug.Arrow(Vector3.Zero, cross, 1, Color.White);
				Debug.Plane(cross, Vector3.Zero, 1, Color.White);
			}


			if (m_direction == GizmoDirection.Y) {
				var cross = Vector3.Cross(yMatrix.Up, cameraNormal);
				cross = Vector3.Normalize(Vector3.Cross(cross, yMatrix.Up));
				Debug.Plane(cross, Vector3.Zero, 1, Color.White);
			}

			if (m_direction == GizmoDirection.Z) {
				var cross = Vector3.Cross(zMatrix.Up, cameraNormal);
				cross = Vector3.Normalize(Vector3.Cross(cross, zMatrix.Up));
				Debug.Plane(cross, Vector3.Zero, 1, Color.White);
			}
			*/
			/*
			if (m_direction == GizmoDirection.X) {
				var cross = Vector3.Cross(xMatrix.Up, cameraNormal);
				planeNormal = Vector3.Normalize(Vector3.Cross(cross, xMatrix.Up));
			}

			if (m_direction == GizmoDirection.Y) {
				var cross = Vector3.Cross(yMatrix.Up, cameraNormal);
				planeNormal = Vector3.Normalize(Vector3.Cross(cross, yMatrix.Up));
			}

			if (m_direction == GizmoDirection.Z) {
				var cross = Vector3.Cross(zMatrix.Up, cameraNormal);
				planeNormal = Vector3.Normalize(Vector3.Cross(cross, zMatrix.Up));
			}
			*/

			var movement = Vector3.Zero;
			if (isDragging) {
				var tMatrix = xMatrix;
				if (m_direction == GizmoDirection.X) tMatrix = xMatrix;
				if (m_direction == GizmoDirection.Y) tMatrix = yMatrix;
				if (m_direction == GizmoDirection.Z) tMatrix = zMatrix;
				var cameraNormal = Vector3.Normalize(worldTranslation - camera.Position);
				var cross = Vector3.Cross(tMatrix.Up, cameraNormal);
				var planeNormal = Vector3.Normalize(Vector3.Cross(cross, tMatrix.Up));

				var invRot = Matrix.Invert(Matrix.RotationQuaternion(worldRotation));
				var rot = Matrix.RotationQuaternion(worldRotation);

				if (Input.MousePressed(MouseButton.Left)) {
					var move = MouseMove(camera, planeNormal, worldTranslation);
					move = Vector3.TransformCoordinate(move, invRot);

					if (m_direction == GizmoDirection.X) {
						move.Y = 0;
						move.Z = 0;
					}

					if (m_direction == GizmoDirection.Y) {
						move.X = 0;
						move.Z = 0;
					}

					if (m_direction == GizmoDirection.Z) {
						move.Y = 0;
						move.X = 0;
					}

					move = Vector3.TransformCoordinate(move, rot);

					movement += move;
				}
			}


			DrawArrow(renderer, ref xMatrix, GizmoDirection.X);
			DrawArrow(renderer, ref yMatrix, GizmoDirection.Y);
			DrawArrow(renderer, ref zMatrix, GizmoDirection.Z);
			return Matrix.Translation(movement);
		}

		// ReSharper disable once MemberCanBeMadeStatic.Local
		private Matrix FaceCamera(DXCamera camera, ref Matrix matrix) {
			var camLook = Vector3.Normalize(camera.Position - matrix.TranslationVector);
			if (Vector3.Dot(camLook, matrix.Up) < 0) {
				return Matrix.RotationZ(Mathf.Pi) * matrix;
			}

			return matrix;
		}

		private void DrawArrow(PrimitiveRenderer renderer, ref Matrix world, GizmoDirection direction) {
			var clr = GetColor(direction, direction == m_direction);
			renderer.CustomPrimitive(m_line, Matrix.Translation(0, m_lineRadius, 0) * world, clr);
			renderer.CustomPrimitive(m_cap, Matrix.Translation(0, m_arrowLength - m_capLength, 0) * world, clr);
		}

		/// <inheritdoc />
		protected override void OnDispose() { }
	}
}