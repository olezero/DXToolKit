using System;
using System.Collections.Generic;
using DXToolKit.Engine;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace DXToolKit {
	public static class Debug {
		private class DebugLog {
			public string LogStr;
			public float TTL;
		}


		private static PrimitiveRenderer m_primitiveRenderer;
		private static DXCamera m_dxCamera;
		private static Matrix m_primitiveTransform = Matrix.Identity;
		private static LineRenderer m_lineRenderer;

		private static List<DebugLog> m_logs;
		private static List<Action<RenderTarget, SolidColorBrush>> m_d2dOperations;
		private static TextFormat m_textFormat;
		private static SolidColorBrush m_brush;
		private static bool m_initialized;

		internal static void Initialize(GraphicsDevice device) {
			m_logs = new List<DebugLog>();
			m_d2dOperations = new List<Action<RenderTarget, SolidColorBrush>>();
			m_textFormat = new TextFormat(device.Factory, "Consolas", 13);
			m_brush = new SolidColorBrush(device, Color.White);
			m_initialized = true;
			m_primitiveRenderer = new PrimitiveRenderer(device);
			m_lineRenderer = new LineRenderer(device);
			m_lineRenderer.Resolution = 16;
		}

		public static void Log(object log, float time = -1) {
			if (!m_initialized) {
				return;
			}

			lock (m_logs) {
				m_logs.Add(new DebugLog {
					LogStr = log?.ToString() ?? "",
					TTL = time,
				});
			}
		}

		internal static void Run3DRender(GraphicsDevice device) {
			if (!m_initialized) {
				return;
			}

			if (m_dxCamera != null) {
				m_primitiveRenderer.Render(m_dxCamera, m_primitiveTransform);
				m_lineRenderer.Render(m_dxCamera, m_primitiveTransform);
			}
		}

		internal static void ClearFrameLog() {
			lock (m_logs) {
				for (var i = 0; i < m_logs.Count; i++) {
					if (m_logs[i].TTL < 0) {
						m_logs.RemoveAt(i--);
					}
				}
			}
		}

		private static string m_toPrint = "";

		internal static void Render(GraphicsDevice device) {
			if (!m_initialized) {
				return;
			}

			m_toPrint = "";

			lock (m_logs) {
				// First print all the "per frame" logs
				for (var i = 0; i < m_logs.Count; i++) {
					if (m_logs[i].TTL < 0) {
						m_toPrint += m_logs[i].LogStr + "\n";
						m_logs.RemoveAt(i--);
					}
				}

				// Then print all the timed logs
				for (int i = 0; i < m_logs.Count; i++) {
					m_toPrint += m_logs[i].LogStr + "\n";
					m_logs[i].TTL -= Time.DeltaTime * 1000;
					if (m_logs[i].TTL <= 0) {
						m_logs.RemoveAt(i);
						i--;
					}
				}
			}

			device.RenderTarget.Transform = Matrix3x2.Identity;
			device.RenderTarget.BeginDraw();
			foreach (var d2dOp in m_d2dOperations) {
				d2dOp?.Invoke(device.RenderTarget, m_brush);
			}

			m_d2dOperations.Clear();

			m_brush.Color = Color.White;

			device.RenderTarget.DrawText(m_toPrint, m_textFormat, new RectangleF(2, 2, EngineConfig.ScreenWidth - 2, EngineConfig.ScreenHeight - 2), m_brush);
			device.RenderTarget.EndDraw();
		}

		public static void SetD3DCamera(DXCamera dxCamera) {
			m_dxCamera = dxCamera;
		}

		public static void SetPrimitiveTransform(Matrix transform) {
			m_primitiveTransform = transform;
		}

		public static void Cube(Matrix transform, Color color) {
			CheckCameraSet();
			m_primitiveRenderer.Cube(ref transform, ref color);
		}

		public static void Sphere(Matrix transform, Color color) {
			CheckCameraSet();
			m_primitiveRenderer.Sphere(ref transform, ref color);
		}

		public static void Plane(Matrix transform, Color color) {
			CheckCameraSet();
			m_primitiveRenderer.Plane(ref transform, ref color);
		}
		
		public static void Plane(Vector3 normal, Vector3 position, float scale, Color color, Matrix? transform = null) {
			CheckCameraSet();
			m_primitiveRenderer.Plane(normal, position, scale, color, transform ?? Matrix.Identity);
		}

		public static void Box(Vector3 min, Vector3 max, Color color, Matrix? transform = null) {
			CheckCameraSet();
			m_lineRenderer.Box(min, max, color, transform);
		}

		public static void Arrow(Vector3 origin, Vector3 direction, float size, Color color, Matrix? transform = null) {
			CheckCameraSet();
			m_lineRenderer.Arrow(origin, direction, size, color, transform);
		}

		public static void Frustum(BoundingFrustum frustum, Color color) {
			CheckCameraSet();
			m_lineRenderer.Frustum(frustum, color);
		}

		public static void Circle(Vector3 center, float radius, Vector3 normal, Color color, Matrix? transform = null) {
			CheckCameraSet();
			m_lineRenderer.Circle(center, radius, normal, color, transform);
		}

		public static void Ray(Ray ray, float length, Color color, Matrix? transform = null) {
			CheckCameraSet();
			m_lineRenderer.Ray(ray, length, color, transform);
		}

		public static void Capsule(Vector3 center, float radius, float innerHeight, Color color, Matrix? transform = null, bool heightLines = false) {
			CheckCameraSet();
			m_lineRenderer.Capsule(center, radius, innerHeight, color, transform, heightLines);
		}

		public static void BoundingBox(BoundingBox bounds, Color color, Matrix? transform = null) {
			m_lineRenderer.BoundingBox(bounds, color, transform);
		}

		public static void Draw2D(Action<RenderTarget, SolidColorBrush> renderAction) {
			m_d2dOperations.Add(renderAction);
		}

		public static void Transform(Matrix transformMatrix, Color? color = null) {
			CheckCameraSet();
			m_lineRenderer.Transform(transformMatrix, color);
		}

		public static void Line(Vector3 p1, Vector3 p2, Color color) {
			CheckCameraSet();
			m_lineRenderer.Line(ref p1, ref p2, ref color);
		}


		private static void CheckCameraSet() {
			if (m_dxCamera == null) {
				throw new NullReferenceException("Camera is null, run SetD3DCamera first");
			}
		}


		internal static void Shutdown() {
			m_textFormat?.Dispose();
			m_brush?.Dispose();
			m_primitiveRenderer?.Dispose();
			m_lineRenderer?.Dispose();
			m_dxCamera = null;
		}
	}
}