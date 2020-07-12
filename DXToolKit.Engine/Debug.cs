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

		private static List<DebugLog> m_logs;
		private static TextFormat m_textFormat;
		private static SolidColorBrush m_brush;
		private static bool m_initialized;

		internal static void Initialize(GraphicsDevice device) {
			m_logs = new List<DebugLog>();
			m_textFormat = new TextFormat(device.Factory, "Consolas", 13);
			m_brush = new SolidColorBrush(device, Color.White);
			m_initialized = true;
			m_primitiveRenderer = new PrimitiveRenderer(device);
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
			if (m_dxCamera != null) {
				lock (m_primitiveRenderer) {
					m_primitiveRenderer.Cube(ref transform, ref color);
				}
			} else {
				throw new NullReferenceException("Camera is null, run SetD3DCamera first");
			}
		}

		public static void Sphere(Matrix transform, Color color) {
			if (m_dxCamera != null) {
				m_primitiveRenderer.Sphere(ref transform, ref color);
			} else {
				throw new NullReferenceException("Camera is null, run SetD3DCamera first");
			}
		}

		public static void Plane(Matrix transform, Color color) {
			if (m_dxCamera != null) {
				m_primitiveRenderer.Plane(ref transform, ref color);
			} else {
				throw new NullReferenceException("Camera is null, run SetD3DCamera first");
			}
		}

		internal static void Shutdown() {
			m_textFormat?.Dispose();
			m_brush?.Dispose();
			m_primitiveRenderer?.Dispose();
			m_dxCamera = null;
		}
	}
}