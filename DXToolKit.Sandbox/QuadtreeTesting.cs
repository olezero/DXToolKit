using System;
using DXToolKit.Engine;
using SharpDX;
using SharpDX.Direct2D1;

namespace DXToolKit.Sandbox {
	public class MyBoundsData : IQuadtreeBounds {
		public RectangleF Bounds { get; set; }
	}

	public class MyPointData : IQuadtreePoint {
		public Vector2 Point { get; set; }
	}


	public class QuadtreeTesting : Sketch {
		private Random m_random;
		private Quadtree<MyPointData> m_quadtree;
		private SolidColorBrush m_brush;
		private Camera3D m_camera;
		private float m_cameraZoom = 1.0F;

		protected override void OnLoad() {
			m_camera = new Camera3D {
				Position = new Vector3(ScreenWidth / 2.0F, ScreenHeight / 2.0F, -10),
				OrthoWidth = EngineConfig.ScreenWidth,
				OrthoHeight = EngineConfig.ScreenHeight,
			};
			m_random = new Random(0);
			m_quadtree = new Quadtree<MyPointData>(new RectangleF(100, 100, EngineConfig.ScreenWidth - 200, EngineConfig.ScreenWidth - 200));
			m_brush = new SolidColorBrush(m_device, Color.White);


			var minimum = new Vector2(100, 100);
			var maximum = new Vector2(ScreenWidth - 200, ScreenHeight - 200);

			for (int i = 0; i < 100; i++) {
				var data = new MyPointData {
					Point = m_random.NextVector2(minimum, maximum),
				};

				m_quadtree.Add(data);
			}
		}

		protected override void Update() {
			
			if (MousePressed(MouseButton.Right)) {
				m_camera.Translate(-MouseMove.X / m_cameraZoom, MouseMove.Y / m_cameraZoom, 0);
			}

			if (Math.Abs(NormalizedMouseWheel) > 0.0001F) {
				if (NormalizedMouseWheel > 0) {
					m_cameraZoom *= 1.1F;
				} else {
					m_cameraZoom /= 1.1F;
				}

				m_camera.OrthoScaling = m_cameraZoom;
			}


			if (MousePressed(MouseButton.Left)) {
				var world = m_camera.ScreenToWorld(MousePosition, ScreenWidth, ScreenHeight);

				var minimum = new Vector2(100, 100);
				var maximum = new Vector2(ScreenWidth - 100, ScreenHeight - 100);
				var pos = m_random.NextVector2(minimum, maximum);


				var worldPos = new Vector2(world.Position.X, world.Position.Y);
				for (int i = 0; i < 1; i++) {
					pos = worldPos + m_random.NextVector2(-Vector2.One, Vector2.One);
					m_quadtree.Add(new MyPointData {
						Point = pos,
					});
				}
			}
		}

		protected override void Render() {
			m_renderTarget.Transform = m_camera.D2DTransformMatrix;
			m_renderTarget.BeginDraw();

			var nodeCount = 0;
			var allNodes = m_quadtree.AllNodes();
			foreach (var node in allNodes) {
				m_renderTarget.DrawRectangle(node.Bounds, m_brush, 1.0F / m_cameraZoom);
				nodeCount++;
			}

			Debug.Log(nodeCount);

			foreach (var data in m_quadtree.AllData()) {
				m_renderTarget.FillEllipse(new Ellipse(data.Point, 2.0F / m_cameraZoom, 2.0F / m_cameraZoom), m_brush);
			}

			m_renderTarget.EndDraw();
		}

		protected override void OnUnload() { }
	}
}