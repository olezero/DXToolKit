using System.Collections.Generic;
using SharpDX;

// ReSharper disable LoopCanBeConvertedToQuery

// ReSharper disable SuspiciousTypeConversion.Global

namespace DXToolKit.Sandbox {
	public interface IQuadtreeData { }

	public interface IQuadtreeBounds : IQuadtreeData {
		RectangleF Bounds { get; }
	}

	public interface IQuadtreePoint : IQuadtreeData {
		Vector2 Point { get; }
	}

	public interface IQuadtreeAware<T> where T : IQuadtreeData {
		Quadtree<T> ParentNode { get; set; }
	}

	public class Quadtree<T> where T : IQuadtreeData {
		private readonly int MAX_DEPTH;
		private readonly int MAX_DATA_PER_NODE;

		private List<T> m_data;
		private Quadtree<T>[] m_childNodes;
		private Quadtree<T> m_parentNode;
		private RectangleF m_bounds;
		private int m_level;

		public RectangleF Bounds => m_bounds;

		public Quadtree(RectangleF bounds, int maxDataPerNode = 4, int maxDepth = 12) {
			m_bounds = bounds;
			m_data = new List<T>();
			m_childNodes = null;
			m_parentNode = null;
			MAX_DATA_PER_NODE = maxDataPerNode;
			MAX_DEPTH = maxDepth;
			m_level = 0;
		}

		private Quadtree(Quadtree<T> parent, ref RectangleF bounds) {
			m_parentNode = parent;
			m_bounds = bounds;
			m_childNodes = null;
			m_data = new List<T>();
			m_level = m_parentNode.m_level + 1;
			MAX_DEPTH = m_parentNode.MAX_DEPTH;
			MAX_DATA_PER_NODE = m_parentNode.MAX_DATA_PER_NODE;
		}

		public List<Quadtree<T>> AllNodes() {
			var result = new List<Quadtree<T>>();
			CollectAllNodes(ref result);
			return result;
		}

		public List<T> AllData() {
			var result = new List<T>();
			CollactAllData(ref result);
			return result;
		}

		private void CollectAllNodes(ref List<Quadtree<T>> result) {
			if (m_childNodes != null) {
				for (int i = 0; i < m_childNodes.Length; i++) {
					m_childNodes[i].CollectAllNodes(ref result);
				}
			}

			result.Add(this);
		}

		private void CollactAllData(ref List<T> result) {
			if (m_childNodes != null) {
				for (int i = 0; i < m_childNodes.Length; i++) {
					m_childNodes[i].CollactAllData(ref result);
				}
			}

			if (m_data.Count > 0) {
				result.AddRange(m_data);
			}
		}

		public bool Add(T data) {
			bool contains = false;
			if (data is IQuadtreeBounds bData) {
				var b = bData.Bounds;
				m_bounds.Contains(ref b, out contains);
			}

			if (data is IQuadtreePoint pData) {
				var p = pData.Point;
				m_bounds.Contains(ref p, out contains);
			}

			if (contains) {
				if (m_childNodes == null && m_data.Count >= MAX_DATA_PER_NODE && m_level < MAX_DEPTH) {
					Split();
				}

				if (m_childNodes != null) {
					for (int i = 0; i < m_childNodes.Length; i++) {
						if (m_childNodes[i].Add(data)) {
							return true;
						}
					}
				}

				m_data.Add(data);
				if (data is IQuadtreeAware<T> aware) aware.ParentNode = this;
				return true;
			}

			return false;
		}

		private bool Split() {
			if (m_childNodes == null) {
				var newBounds = SplitRectangle(ref m_bounds);
				m_childNodes = new Quadtree<T>[4];
				for (int i = 0; i < newBounds.Length; i++) {
					m_childNodes[i] = new Quadtree<T>(this, ref newBounds[i]);
					for (int j = 0; j < m_data.Count; j++) {
						if (m_childNodes[i].Add(m_data[j])) {
							m_data.RemoveAt(j--);
						}
					}
				}

				return true;
			}

			return false;
		}


		private RectangleF[] SplitRectangle(ref RectangleF rectangle) {
			var result = new RectangleF[4];
			var halfWidth = rectangle.Width / 2.0F;
			var halfHeight = rectangle.Height / 2.0F;
			var index = 0;
			for (int x = 0; x < 2; x++) {
				for (int y = 0; y < 2; y++) {
					result[index++] = new RectangleF(
						x: rectangle.X + x * halfWidth,
						y: rectangle.Y + y * halfHeight,
						width: halfWidth,
						height: halfHeight
					);
				}
			}

			return result;
		}
	}
}