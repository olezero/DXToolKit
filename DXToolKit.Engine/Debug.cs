using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace DXToolKit.Engine {
	/// <summary>
	/// Static debug class for logging / displaying debug objects
	/// </summary>
	public static class Debug {
		/// <summary>
		/// Keeps track of when a log was added and the log itself for multiframe logs	
		/// </summary>
		private class DebugLog {
			/// <summary>
			/// String to log
			/// </summary>
			public string LogStr;

			/// <summary>
			/// Time to live in ms
			/// </summary>
			public float TTL;
		}

		/// <summary>
		/// Primitive renderer used for rendering primitives
		/// </summary>
		private static PrimitiveRenderer m_primitiveRenderer;

		/// <summary>
		/// Camera set by user to use when rendering 3D objects
		/// </summary>
		private static DXCamera m_dxCamera;

		/// <summary>
		/// Transform used by all primitives
		/// </summary>
		private static Matrix m_primitiveTransform = Matrix.Identity;

		/// <summary>
		/// Line renderer used for 3D line rendering
		/// </summary>
		private static LineRenderer m_lineRenderer;

		/// <summary>
		/// List of debug logs to display each frame
		/// </summary>
		private static List<DebugLog> m_logs;

		/// <summary>
		/// Direct2D draw operations to run next frame
		/// </summary>
		private static List<Action<RenderTarget, SolidColorBrush>> m_d2dOperations;

		/// <summary>
		/// Text format used by debug logs
		/// </summary>
		private static TextFormat m_textFormat;

		/// <summary>
		/// Brush used to draw text
		/// </summary>
		private static SolidColorBrush m_brush;

		/// <summary>
		/// Trigger to only initialize once
		/// </summary>
		private static bool m_initialized;

		/// <summary>
		/// String buffer containing all debug logs to print in the given frame
		/// </summary>
		private static string m_toPrint = "";

		/// <summary>
		/// Initializes debug component
		/// </summary>
		/// <param name="device"></param>
		internal static void Initialize(GraphicsDevice device) {
			m_logs = new List<DebugLog>();
			m_d2dOperations = new List<Action<RenderTarget, SolidColorBrush>>();
			m_textFormat = new TextFormat(device.Factory, "Consolas", 13);
			m_brush = new SolidColorBrush(device, Color.White);
			m_initialized = true;
			m_primitiveRenderer = new PrimitiveRenderer(device);
			m_lineRenderer = new LineRenderer(device) {
				Resolution = 16
			};
		}

		/// <summary>
		/// Generates a log for a single frame if time is omitted, or for a given amount of time in milliseconds
		/// </summary>
		/// <param name="log">The log</param>
		/// <param name="time">Time to display (-1 or less for a single frame)</param>
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

		/// <summary>
		/// Runs 3D rendering
		/// </summary>
		internal static void Run3DRender() {
			if (!m_initialized) {
				return;
			}

			if (m_dxCamera != null) {
				m_primitiveRenderer.Render(m_dxCamera, m_primitiveTransform);
				m_lineRenderer.Render(m_dxCamera, m_primitiveTransform);
			}
		}

		/// <summary>
		/// Clears the per frame log
		/// </summary>
		internal static void ClearFrameLog() {
			lock (m_logs) {
				for (var i = 0; i < m_logs.Count; i++) {
					if (m_logs[i].TTL < 0) {
						m_logs.RemoveAt(i--);
					}
				}
			}

			lock (m_d2dOperations) {
				m_d2dOperations.Clear();
			}

			lock (m_primitiveRenderer) {
				m_primitiveRenderer.Clear();
			}
		}

		/// <summary>
		/// Renders debug logs to the screen
		/// </summary>
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

		/// <summary>
		/// Sets the camera the debug drawing should use
		/// </summary>
		/// <param name="dxCamera"></param>
		public static void SetD3DCamera(DXCamera dxCamera) {
			m_dxCamera = dxCamera;
		}

		/// <summary>
		/// Sets global primitive transform to all debug primitives
		/// </summary>
		/// <param name="transform">The world transform to use</param>
		public static void SetPrimitiveTransform(Matrix transform) {
			m_primitiveTransform = transform;
		}

		/// <summary>
		/// Draws a cube in 3D space.
		/// </summary>
		public static void Cube(Matrix transform, Color color) {
			CheckCameraSet();
			m_primitiveRenderer.Cube(ref transform, ref color);
		}

		/// <summary>
		/// Draws a sphere in 3D space.
		/// </summary>
		public static void Sphere(Matrix transform, Color color) {
			CheckCameraSet();
			m_primitiveRenderer.Sphere(ref transform, ref color);
		}

		/// <summary>
		/// Draws a plane in 3D space.
		/// </summary>
		public static void Plane(Matrix transform, Color color) {
			CheckCameraSet();
			m_primitiveRenderer.Plane(ref transform, ref color);
		}

		/// <summary>
		/// Draws a plane in 3D space.
		/// </summary>
		public static void Plane(Vector3 normal, Vector3 position, float scale, Color color, Matrix? transform = null) {
			CheckCameraSet();
			m_primitiveRenderer.Plane(normal, position, scale, color, transform ?? Matrix.Identity);
		}

		/// <summary>
		/// Draws a line box in 3D space.
		/// </summary>
		public static void Box(Vector3 min, Vector3 max, Color color, Matrix? transform = null) {
			CheckCameraSet();
			m_lineRenderer.Box(min, max, color, transform);
		}

		/// <summary>
		/// Draws a line arrow in 3D space.
		/// </summary>
		public static void Arrow(Vector3 origin, Vector3 direction, float size, Color color, Matrix? transform = null) {
			CheckCameraSet();
			m_lineRenderer.Arrow(origin, direction, size, color, transform);
		}

		/// <summary>
		/// Draws a line frustum in 3D space.
		/// </summary>
		public static void Frustum(BoundingFrustum frustum, Color color) {
			CheckCameraSet();
			m_lineRenderer.Frustum(frustum, color);
		}

		/// <summary>
		/// Draws a line circle in 3D space.
		/// </summary>
		public static void Circle(Vector3 center, float radius, Vector3 normal, Color color, Matrix? transform = null) {
			CheckCameraSet();
			m_lineRenderer.Circle(center, radius, normal, color, transform);
		}

		/// <summary>
		/// Draws a line ray in 3D space.
		/// </summary>
		public static void Ray(Ray ray, float length, Color color, Matrix? transform = null) {
			CheckCameraSet();
			m_lineRenderer.Ray(ray, length, color, transform);
		}

		/// <summary>
		/// Draws a line capsule in 3D space.
		/// </summary>
		public static void Capsule(Vector3 center, float radius, float innerHeight, Color color, Matrix? transform = null, bool heightLines = false) {
			CheckCameraSet();
			m_lineRenderer.Capsule(center, radius, innerHeight, color, transform, heightLines);
		}

		/// <summary>
		/// Draws a line bounding box in 3D space.
		/// </summary>
		public static void BoundingBox(BoundingBox bounds, Color color, Matrix? transform = null) {
			m_lineRenderer.BoundingBox(bounds, color, transform);
		}

		/// <summary>
		/// Draws a line box in 3D space.
		/// </summary>
		public static void Draw2D(Action<RenderTarget, SolidColorBrush> renderAction) {
			m_d2dOperations.Add(renderAction);
		}

		/// <summary>
		/// Draws a line transform in 3D space.
		/// Uses normal Red(X), Green(Y) and Blue(Z) if color is null
		/// </summary>
		public static void Transform(Matrix transformMatrix, Color? color = null) {
			CheckCameraSet();
			m_lineRenderer.Transform(transformMatrix, color);
		}

		/// <summary>
		/// Draws a line in 3D space.
		/// </summary>
		public static void Line(Vector3 p1, Vector3 p2, Color color) {
			CheckCameraSet();
			m_lineRenderer.Line(ref p1, ref p2, ref color);
		}

		/// <summary>
		/// Shorthand that checks if 3D camera is set
		/// </summary>
		private static void CheckCameraSet() {
			if (m_dxCamera == null) {
				throw new NullReferenceException("Camera is null, run SetD3DCamera first");
			}
		}

		/// <summary>
		/// Disposes of any unmanaged resources used by the Debug class
		/// </summary>
		internal static void Shutdown() {
			m_textFormat?.Dispose();
			m_brush?.Dispose();
			m_primitiveRenderer?.Dispose();
			m_lineRenderer?.Dispose();
			m_dxCamera = null;
		}
	}
}