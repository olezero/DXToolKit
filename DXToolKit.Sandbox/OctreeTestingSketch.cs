using System;
using System.Collections.Generic;
using DXToolKit.Engine;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectInput;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DXToolKit.Sandbox {
	public class Octree {
		private List<object> m_data;
		private List<OctreeNode> m_nodes;
		private List<BoundingBox> m_boundCache;
		private Stack<int> m_freeBoundsIndices;
	}

	public struct OctreeNode {
		private int[] m_childNodes;
		private int m_boundsIndex;
	}


	public class OctreeDataThingy : IOctreePoint, IOctreeAware<OctreeDataThingy> {
		public Vector3 Point { get; }

		public OctreeDataThingy(Vector3 point) {
			Point = point;
		}

		public Octree<OctreeDataThingy> ParentNode { get; set; }
	}

	public class OctreeDataBounds : IOctreeBounds, IOctreeAware<OctreeDataBounds> {
		public Vector3 Velocity;
		public BoundingBox WorldBounds;

		public OctreeDataBounds(BoundingBox bounds) {
			Bounds = bounds;
		}

		public void Move() {
			var min = Bounds.Minimum;
			var max = Bounds.Maximum;
			min += Velocity * Time.DeltaTime * 200;
			max += Velocity * Time.DeltaTime * 200;
			Bounds = new BoundingBox(min, max);


			if (WorldBounds.Contains(Bounds) == ContainmentType.Intersects || WorldBounds.Contains(Bounds) == ContainmentType.Disjoint) {
				Velocity = -Velocity;
				min += Velocity * Time.DeltaTime * 200;
				max += Velocity * Time.DeltaTime * 200;
				Bounds = new BoundingBox(min, max);
			}
		}

		public void RunCollisionDetection(Octree<IOctreeData> octree) {
			var bounds = Bounds;
			bounds.Minimum -= Vector3.One * 10;
			bounds.Maximum += Vector3.One * 10;

			var possible = octree.Intersects(ref bounds);
			if (possible.Length > 1) {
				// Debug.Log(possible.Length);
			}
		}

		public BoundingBox Bounds { set; get; }
		public Octree<OctreeDataBounds> ParentNode { get; set; }
	}


	public class Projectile : IOctreePoint {
		public Vector3 Velocity;
		public BoundingBox WorldBounds;
		public Vector3 Point { get; set; }

		public void Move() {
			Point += Velocity * Time.DeltaTime * 200;
			if (WorldBounds.Contains(Point) == ContainmentType.Disjoint) {
				Velocity = -Velocity;
				Point += Velocity * Time.DeltaTime * 200;
			}
		}
	}

	public class OctreeTestingSketch : Sketch {
		private Octree<IOctreeData> m_octree;
		private Camera3D m_camera;
		private Random m_random;
		private BoundingBox m_octreeBounds;
		private BoundingBox m_spawnBounds;


		private List<OctreeDataBounds> m_data;

		private Octree<Projectile> m_projectileOctree;
		private List<Projectile> m_allProjectiles;

		protected override void OnLoad() {
			//EngineConfig.UseVsync = true;
			//Time.TargetFrameRate = -1;
			m_camera = new Camera3D();
			m_camera.FarClippingPlane = 10000;
			Debug.SetD3DCamera(m_camera);
			m_octreeBounds = new BoundingBox(new Vector3(-1000, -1000, -1000), new Vector3(1000, 1000, 1000));
			m_spawnBounds = new BoundingBox(new Vector3(-900, -900, -900), new Vector3(900, 900, 900));

			m_octree = new Octree<IOctreeData>(m_octreeBounds, 2, 12);
			m_projectileOctree = new Octree<Projectile>(m_octreeBounds, 4, 12);


			m_random = new Random(0);
			for (int i = 0; i < 0; i++) {
				var point = m_random.NextVector3(m_octree.Bounds.Minimum, m_octree.Bounds.Maximum);
				m_octree.AddData(new OctreeDataThingy(point));
			}


			m_data = new List<OctreeDataBounds>();


			for (int i = 0; i < 1000; i++) {
				var size = m_random.NextVector3(Vector3.One * 1.0F, Vector3.One * 5.02F);
				var boundsMinimum = m_random.NextVector3(m_spawnBounds.Minimum, m_spawnBounds.Maximum - size);
				var bounds = new BoundingBox(boundsMinimum, boundsMinimum + size);
				//m_octree.AddData(new OctreeDataBounds(bounds));

				//var worldBounds = m_octreeBounds;
				//worldBounds.Minimum += Vector3.One * 10;
				//worldBounds.Maximum -= Vector3.One * 10;

				m_data.Add(new OctreeDataBounds(bounds) {
					Velocity = m_random.NextVector3(Vector3.One * -1, Vector3.One) * 1.1F,
					WorldBounds = m_spawnBounds,
				});
			}

			m_allProjectiles = new List<Projectile>();
			for (int i = 0; i < 3000; i++) {
				var position = m_random.NextVector3(m_spawnBounds.Minimum, m_spawnBounds.Maximum);
				var proj = new Projectile() {
					Point = position,
					WorldBounds = m_spawnBounds,
					Velocity = m_random.NextVector3(Vector3.One * -1, Vector3.One) * 1.1F,
				};

				m_projectileOctree.AddData(proj);
				m_allProjectiles.Add(proj);
			}

			m_camera.LoadFromFile("camera");
			m_camera.SmoothLerpToTarget(Vector3.Zero);


			foreach (var dat in m_data) {
				m_octree.AddData(dat);
			}
		}

		protected override void Update() {
			m_camera.Update();
			if (KeyDown(Key.F1)) {
				m_camera.SaveToFile("camera");
				Debug.Log("Camera position saved", 1000);
			}
		}

		private bool m_updateCameraFrustum = true;
		private BoundingFrustum m_cameraFrustum;

		protected override void Render() {
			foreach (var data in m_octree.AllData) {
				if (data is OctreeDataBounds bData) {
					bData.Move();
				}
			}

			for (int i = 0; i < m_allProjectiles.Count; i++) {
				m_allProjectiles[i].Move();
			}

			Profiler.StartProfiler("OCTREE UPDATE");
			var octreeUpdate = Task.Factory.StartNew(() => {
				m_octree.Update();
			});
			var projectileUpdate = Task.Factory.StartNew(() => {
				m_projectileOctree.Update();
			});
			Task.WaitAll(octreeUpdate, projectileUpdate);
			Profiler.StopProfiler("OCTREE UPDATE", true);

			Profiler.Profile("PROPER ASYNC", () => {
				var allData = m_octree.AllData;
				Parallel.For(0, allData.Count, i => {
					var dt = allData[i];
					if (dt is OctreeDataBounds bdata) {
						for (int j = 0; j < 10; j++) {
							bdata.RunCollisionDetection(m_octree);
						}
					}
				});
			});

			Profiler.Profile("PROJECTILE ASYNC", () => {
				var allData = m_octree.AllData;
				Parallel.For(0, allData.Count, i => {
					var dt = allData[i];
					if (dt is OctreeDataBounds bdata) {
						var bounds = bdata.Bounds;
						bounds.Minimum -= Vector3.One * 10;
						bounds.Maximum += Vector3.One * 10;
						var possible = m_projectileOctree.Intersects(ref bounds);
						if (possible.Length > 0) {
							//Debug.Log(possible.Length);
						}
					}
				});
			});


			foreach (var data in m_octree.AllData) {
				if (data is OctreeDataBounds bdata) {
					var transform = Matrix.Scaling(bdata.Bounds.Size) * Matrix.Translation(bdata.Bounds.Center);
					Debug.Cube(transform, Color.White);
				}
			}


			/*
			foreach (var node in m_octree.AllNodes()) {
				if (node.Data.Count > 0) {
					Debug.BoundingBox(node.Bounds, Color.White);
					if (node.Data.Count > 10) {
						Debug.Log("Node data count: " + node.Data.Count + "\t HasChildren: " + node.HasChildNodes);
					}
				}
			}

			foreach (var data in m_octree.AllData) {
				if (data is IOctreeBounds b) {
					Debug.BoundingBox(b.Bounds, Color.Yellow);
				}
			}
			*/


			Debug.Log("OctreeDataCount: " + m_octree.AllData.Count);


			Debug.Draw2D((target, brush) => {
				brush.Color = new Color(1.0F, 1.0F, 1.0F, 0.8F);
				target.DrawRectangle(Input.LeftMouseSelectionRectangle, brush, 2);
				brush.Color = new Color(1.0F, 1.0F, 1.0F, 0.1F);
				target.FillRectangle(Input.LeftMouseSelectionRectangle, brush);
			});


			var worldToScreenParams = new WorldToScreenParams {
				x = 0,
				y = 0,
				width = EngineConfig.ScreenWidth,
				height = EngineConfig.ScreenHeight,
				minZ = 1,
				maxZ = 1000,
				ViewFrustum = new BoundingFrustum(m_camera.ViewProjection),
				worldViewProjection = m_camera.ViewProjection,
			};
			var screenSelect = Input.LeftMouseSelectionRectangle;

			Profiler.StartProfiler("SELECTION");
			var d = m_octree.Intersects(ref screenSelect, ref worldToScreenParams, true);
			Profiler.StopProfiler("SELECTION", true);

			foreach (var octData in d) {
				if (octData is OctreeDataThingy dataThingy) {
					var p = dataThingy.Point;
					var screenPoint = m_camera.WorldToScreen(p, EngineConfig.ScreenWidth, EngineConfig.ScreenHeight);
					Debug.Draw2D((target, brush) => {
						brush.Color = Color.Yellow;
						target.FillEllipse(new Ellipse(screenPoint, 10, 10), brush);
					});
				} else if (octData is OctreeDataBounds bData) {
					var transform = Matrix.Scaling(bData.Bounds.Size) * Matrix.Translation(bData.Bounds.Center);
					Debug.Cube(transform, Color.White);
				}
			}

			Debug.Log(d.Length);


			return;

			/*
			Profiler.Profile("RECONSTRUCT", () => {
				m_octree.Reconstruct();
			});

			Profiler.Profile("MANUAL RECONSTRUCT", () => {
				m_octree.Clear();
				foreach (var dat in m_data) {
					m_octree.AddData(dat);
				}
			});
			*/


			Debug.Log($"Updating camera frustum: {m_updateCameraFrustum}");
			if (KeyDown(Key.F2)) {
				m_updateCameraFrustum = !m_updateCameraFrustum;
			}

			if (m_updateCameraFrustum) {
				m_cameraFrustum = new BoundingFrustum(m_camera.ViewProjection);
			} else {
				Debug.Frustum(m_cameraFrustum, Color.White);
			}


			foreach (var data in m_octree.Intersects(ref m_cameraFrustum)) {
				if (data is OctreeDataThingy dataThingy) {
					var p = dataThingy.Point;
					var screenPoint = m_camera.WorldToScreen(p, EngineConfig.ScreenWidth, EngineConfig.ScreenHeight);
					Debug.Draw2D((target, brush) => {
						brush.Color = Color.White;
						target.FillEllipse(new Ellipse(screenPoint, 5, 5), brush);
					});
				} else if (data is OctreeDataBounds bData) {
					Debug.BoundingBox(bData.Bounds, Color.Yellow);
				}
			}
		}

		protected override void OnUnload() { }
	}
}