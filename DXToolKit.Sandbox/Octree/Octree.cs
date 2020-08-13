using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DXToolKit;
using SharpDX;
using SharpDX.Mathematics.Interop;

// ReSharper disable SuspiciousTypeConversion.Global

namespace DXToolKit {
	// TODO - add update function to update the whole tree based on data "movement"
	// TODO - make this better and add it to DXToolKit as a standalone feature
	public class Octree<T> where T : IOctreeData {
		public static bool IGNORE_UPDATE_EXCEPTIONS = false;

		private Octree<T> m_parentNode;
		private Octree<T>[] m_childNodes;
		private List<T> m_data;
		private BoundingBox m_bounds;
		private int m_maxData;
		private int m_maxDepth;
		private int m_level;
		private List<T> m_allData;
		private bool m_isRootNode;
		private Octree<T> m_rootNode;

		public BoundingBox Bounds => m_bounds;
		public int Level => m_level;
		public List<T> Data => m_data;
		public List<T> AllData => m_allData;
		public bool HasChildNodes => m_childNodes != null;


		public Octree(BoundingBox bounds, int maxData = 4, int maxDepth = 10) {
			m_bounds = bounds;
			m_maxData = maxData;
			m_maxDepth = maxDepth;
			m_level = 0;
			m_data = new List<T>();
			m_rootNode = this;
			m_isRootNode = true;
			m_allData = new List<T>();
		}

		private Octree(BoundingBox bounds, Octree<T> parent) : this(bounds) {
			m_parentNode = parent;
			m_maxData = m_parentNode.m_maxData;
			m_maxDepth = m_parentNode.m_maxDepth;
			m_level = m_parentNode.m_level + 1;
			m_data = new List<T>();
			m_rootNode = parent.m_rootNode;
			m_isRootNode = false;
		}

		public bool AddData(T data) {
			var result = m_rootNode.Add(data);
			if (result) {
				m_rootNode.m_allData.Add(data);
			}

			return result;
		}

		private bool Add(T data) {
			// If bounds contains the input data 
			if (data is IOctreeBounds bData && m_bounds.Contains(bData.Bounds) == ContainmentType.Contains ||
			    data is IOctreePoint pData && m_bounds.Contains(pData.Point) == ContainmentType.Contains) {
				// Check if node has room for more data
				if (m_data.Count >= m_maxData && m_childNodes == null) {
					// If check if we can add to child nodes (split if we dont have children)
					if (m_childNodes == null) {
						// Check if we have reached maximum depth
						if (m_level >= m_maxDepth) {
							// Dont split, just add
							AddDataAware(data);
							return true;
						}

						// This moves data to child nodes as it can
						Split();
					}
				}

				// Try and add to all child nodes
				if (m_childNodes != null) {
					for (int i = 0; i < m_childNodes.Length; i++) {
						if (m_childNodes[i].Add(data)) {
							return true;
						}
					}
				}

				// If we're still here, add data directly
				// Since data is contained in this node, but not any child nodes, it must be too big for child nodes, but small enough to fit inside this node
				AddDataAware(data);
				return true;
			}

			// If its not contained, return false
			return false;
		}

		public bool RemoveData(IOctreeAware<T> data) {
			var node = data.ParentNode;
			if (data is T tD) {
				node.Remove(tD);
				return true;
			}

			return false;
		}

		public void Update() {
			if (!m_isRootNode) {
				throw new Exception("Update can only be called on root node");
			}

			// Remove
			var removedList = new List<T>();

			// Get and remove any data that is crossing some bounds
			GetAndRemoveIntersectingData(ref removedList);

			// Try to split root node back into child nodes
			/*
			var dataCopy = m_data.ToArray();
			for (int i = 0; i < m_data.Count; i++) {
				// Remove everything from this, not triggering merge
				Remove(m_data[i--], false);
			}

			// Add data back to allow splitting etc as normal
			for (int i = 0; i < dataCopy.Length; i++) {
				var added = AddData(dataCopy[i]);
				if (!added && !IGNORE_UPDATE_EXCEPTIONS) throw new Exception("Data was outside octree at the end of update");
			}
			*/

			// Add data back into tree
			for (int i = 0; i < removedList.Count; i++) {
				var added = m_rootNode.AddData(removedList[i]);
				if (!added && !IGNORE_UPDATE_EXCEPTIONS) throw new Exception("Data was outside octree at the end of update");
			}


			PushDataToLeafs();
			// Merge, this will recurse down the hierarchy
			Merge();
		}

		/// <summary>
		/// Tries to push all data out to leaf nodes if there is to much data in a single node
		/// </summary>
		private void PushDataToLeafs() {
			if (m_data.Count >= m_maxData * 4) {
				if (m_childNodes == null) {
					Split();
				}

				for (int i = 0; i < m_data.Count; i++) {
					var added = false;
					for (int j = 0; j < m_childNodes.Length; j++) {
						if (m_childNodes[j].Add(m_data[i])) {
							added = true;
							break;
						}
					}

					if (added) {
						// remove from this node, but not "alldata" since data has not been added back into "alldata" when moving to child node
						Remove(m_data[i--], false, false);
					}
				}
			}

			if (m_childNodes != null) {
				foreach (var node in m_childNodes) {
					node.PushDataToLeafs();
				}
			}
		}

		private void GetAndRemoveIntersectingData(ref List<T> result) {
			if (m_childNodes != null) {
				for (int i = 0; i < m_childNodes.Length; i++) {
					m_childNodes[i].GetAndRemoveIntersectingData(ref result);
				}
			}

			if (m_data != null && m_data.Count > 0) {
				for (int i = 0; i < m_data.Count; i++) {
					if (m_data[i] is IOctreeBounds bData && m_bounds.Contains(bData.Bounds) != ContainmentType.Contains ||
					    m_data[i] is IOctreePoint pData && m_bounds.Contains(pData.Point) != ContainmentType.Contains) {
						result.Add(m_data[i]);
						Remove(m_data[i--], false);
					}
				}
			}
		}


		public void Reconstruct() {
			var allData = m_allData.ToArray();
			Clear();
			for (int i = 0; i < allData.Length; i++) {
				AddData(allData[i]);
			}
		}

		private void Remove(T data, bool runMerge = true, bool removeFromAllData = true) {
			// Remove data from this node
			m_data.Remove(data);

			if (removeFromAllData) {
				m_rootNode.m_allData.Remove(data);
			}

			// Ask parent to try and merge since we now have less total data in the tree
			// If parent is null, its the root node
			if (runMerge) {
				m_parentNode?.Merge();
			}
		}

		private bool Merge() {
			// Check if all child data can fit in this node, if it can move it to this and delete children
			// What if children has children ? check that too
			if (m_childNodes == null) {
				// Dont have any children, return true as if we have merged
				return true;
			}

			// Recursively try and merge all nodes
			var canMerge = true;
			for (int i = 0; i < m_childNodes.Length; i++) {
				if (m_childNodes[i].Merge() == false) {
					canMerge = false;
				}
			}

			// If a single child down the hierarchy cannot merge, this node cannot merge
			if (canMerge == false) return false;

			// Now we should just have 8 children and they dont have children at this point
			// Get currently stored data count
			var totalData = Data.Count;
			foreach (var childNode in m_childNodes) {
				// Add all child data counts
				totalData += childNode.Data.Count;
			}

			// If more then max, return false
			if (totalData > m_maxData) {
				return false;
			}

			// Now we know that there is enough room for all data in this node
			foreach (var childNode in m_childNodes) {
				foreach (var data in childNode.Data) {
					// Move data from child to this
					AddDataAware(data);
				}

				// CLear data on child
				childNode.Data.Clear();
			}

			// Delete child nodes
			m_childNodes = null;

			// Return successful merge
			return true;
		}

		/// <summary>
		/// Just a common entry point for adding data.
		/// Makes sure to set IOctreeAware parent node
		/// </summary>
		/// <param name="data">Data to add</param>
		private void AddDataAware(T data) {
			if (data is IOctreeAware<T> octreeAware) {
				octreeAware.ParentNode = this;
			}

			m_data.Add(data);
		}

		/// <summary>
		/// Splits the node into 8 child nodes, moving as much data as possible to child nodes
		/// </summary>
		private void Split() {
			// Create 8 sub nodes
			var childBounds = SplitBoundingBox(m_bounds);
			// Setup child nodes
			m_childNodes = new Octree<T>[8];
			// Create children with bounds
			for (int i = 0; i < childBounds.Length; i++) {
				m_childNodes[i] = new Octree<T>(childBounds[i], this);
			}

			// Try and move all data from this to children. Note, every data might not fit in child node
			for (int i = 0; i < m_data.Count; i++) {
				var data = m_data[i];
				for (int j = 0; j < m_childNodes.Length; j++) {
					var child = m_childNodes[j];
					// If child can take data, remove it from this node
					if (child.Add(data)) {
						// Remove
						m_data.RemoveAt(i);
						// Step back one index
						i--;
						// break the loop
						break;
					}
				}
			}
		}

		/// <summary>
		/// Quick splitting of a bounding box into 8 smaller ones
		/// </summary>
		/// <param name="box">Box to split</param>
		/// <returns>Array of 8 smaller boxes</returns>
		private BoundingBox[] SplitBoundingBox(BoundingBox box) {
			var result = new BoundingBox[8];
			var halfSize = box.Size / 2.0F;
			var index = 0;
			for (int x = 0; x < 2; x++) {
				for (int y = 0; y < 2; y++) {
					for (int z = 0; z < 2; z++) {
						var minimum = box.Minimum;
						minimum.X += x * halfSize.X;
						minimum.Y += y * halfSize.Y;
						minimum.Z += z * halfSize.Z;
						var maximum = minimum + halfSize;
						result[index++] = new BoundingBox(
							minimum,
							maximum
						);
					}
				}
			}

			return result;
		}


		/// <summary>
		/// Gets all nodes in the tree
		/// </summary>
		/// <param name="onlyWithData">If only nodes with data should be returned</param>
		/// <returns>Octree node</returns>
		public IEnumerable<Octree<T>> AllNodes(bool onlyWithData = false) {
			if (m_childNodes != null) {
				foreach (var childNode in m_childNodes) {
					foreach (var node in childNode.AllNodes()) {
						yield return node;
					}
				}
			}

			if (onlyWithData) {
				if (m_data.Count > 0) {
					yield return this;
				}
			} else {
				yield return this;
			}
		}

		/// <summary>
		/// Gets all nodes that intersects the given ray
		/// </summary>
		/// <param name="ray">The ray to check against</param>
		/// <param name="onlyData">If lookup should only return nodes with data</param>
		/// <returns>IEnumerable of nodes</returns>
		public IEnumerable<Octree<T>> NodeIntersect(Ray ray, bool onlyData = true) {
			if (m_bounds.Intersects(ref ray)) {
				if (m_childNodes != null) {
					foreach (var childNode in m_childNodes) {
						foreach (var intersect in childNode.NodeIntersect(ray, onlyData)) {
							yield return intersect;
						}
					}
				}

				if (onlyData) {
					if (m_data.Count > 0) {
						yield return this;
					}
				} else {
					yield return this;
				}
			}
		}

		public IEnumerable<T> IntersectsData(Ray ray) {
			foreach (var node in NodeIntersect(ray)) {
				foreach (var data in node.m_data) {
					if (data is IOctreeBounds bData) {
						if (bData.Bounds.Intersects(ref ray)) {
							yield return data;
						}
					}

					if (data is IOctreePoint pData) {
						var p = pData.Point;
						if (ray.Intersects(ref p)) {
							yield return data;
						}
					}
				}
			}
		}

		public T[] Intersects(ref Ray ray) {
			var result = new List<T>();
			collectIntersects(ref ray, ref result);
			return result.ToArray();
		}

		public T[] Intersects(ref BoundingBox bounds) {
			var result = new List<T>();
			collectIntersects(ref bounds, ref result);
			return result.ToArray();
		}

		public T[] Intersects(ref BoundingSphere bounds) {
			var result = new List<T>();
			collectIntersects(ref bounds, ref result);
			return result.ToArray();
		}

		public T[] Intersects(ref BoundingFrustum frustum) {
			var result = new List<T>();
			collectIntersects(ref frustum, ref result);
			return result.ToArray();
		}


		public T[] Contains(ref BoundingBox bounds, ContainmentType containmentType = ContainmentType.Contains) {
			throw new NotImplementedException();
		}

		public T[] Contains(ref BoundingSphere bounds, ContainmentType containmentType = ContainmentType.Contains) {
			throw new NotImplementedException();
		}

		public T[] Contains(ref BoundingFrustum bounds, ContainmentType containmentType = ContainmentType.Contains) {
			throw new NotImplementedException();
		}

		/// <summary>
		/// Runs a intersection check against
		/// </summary>
		/// <param name="screenRectangle">Screen rectangle to check against</param>
		/// <param name="projectParams">Projection parameters to tell the lookup how to project world objects into screen space</param>
		/// <param name="contains">If the whole bounds of the world object needs to be inside the screen rectangle</param>
		/// <returns>Array of matching objects</returns>
		public T[] Intersects(ref RectangleF screenRectangle, ref WorldToScreenParams projectParams, bool contains = false) {
			var result = new List<T>();
			collectIntersects(ref screenRectangle, ref projectParams, ref result, contains);
			return result.ToArray();
		}

		private void collectIntersects(ref Ray ray, ref List<T> collection) {
			if (m_bounds.Intersects(ref ray)) {
				if (m_childNodes != null) {
					for (int i = 0; i < m_childNodes.Length; i++) {
						m_childNodes[i].collectIntersects(ref ray, ref collection);
					}
				}

				if (m_data.Count > 0) {
					for (int i = 0; i < m_data.Count; i++) {
						if (m_data[i] is IOctreeBounds bData) {
							if (bData.Bounds.Intersects(ref ray)) {
								collection.Add(m_data[i]);
							}
						} else if (m_data[i] is IOctreePoint pData) {
							var p = pData.Point;
							if (ray.Intersects(ref p)) {
								collection.Add(m_data[i]);
							}
						}
					}
				}
			}
		}

		private void collectIntersects(ref BoundingBox bounds, ref List<T> collection) {
			if (m_bounds.Intersects(ref bounds)) {
				if (m_childNodes != null) {
					for (int i = 0; i < m_childNodes.Length; i++) {
						m_childNodes[i].collectIntersects(ref bounds, ref collection);
					}
				}

				if (m_data.Count > 0) {
					for (int i = 0; i < m_data.Count; i++) {
						if (m_data[i] is IOctreeBounds bData) {
							if (bData.Bounds.Intersects(ref bounds)) {
								collection.Add(m_data[i]);
							}
						} else if (m_data[i] is IOctreePoint pData) {
							if (bounds.Contains(pData.Point) == ContainmentType.Contains) {
								collection.Add(m_data[i]);
							}
						}
					}
				}
			}
		}

		private void collectIntersects(ref BoundingSphere bounds, ref List<T> collection) {
			if (m_bounds.Intersects(ref bounds)) {
				if (m_childNodes != null) {
					for (int i = 0; i < m_childNodes.Length; i++) {
						m_childNodes[i].collectIntersects(ref bounds, ref collection);
					}
				}

				if (m_data.Count > 0) {
					for (int i = 0; i < m_data.Count; i++) {
						if (m_data[i] is IOctreeBounds bData) {
							if (bData.Bounds.Intersects(ref bounds)) {
								collection.Add(m_data[i]);
							}
						} else if (m_data[i] is IOctreePoint pData) {
							var p = pData.Point;
							if (bounds.Contains(ref p) == ContainmentType.Contains) {
								collection.Add(m_data[i]);
							}
						}
					}
				}
			}
		}

		private void collectIntersects(ref BoundingFrustum frustum, ref List<T> collection) {
			if (frustum.Intersects(ref m_bounds)) {
				if (m_childNodes != null) {
					for (int i = 0; i < m_childNodes.Length; i++) {
						m_childNodes[i].collectIntersects(ref frustum, ref collection);
					}
				}

				if (m_data.Count > 0) {
					for (int i = 0; i < m_data.Count; i++) {
						if (m_data[i] is IOctreeBounds bData) {
							var bBounds = bData.Bounds;
							if (frustum.Intersects(ref bBounds)) {
								collection.Add(m_data[i]);
							}
						} else if (m_data[i] is IOctreePoint pData) {
							var point = pData.Point;
							if (frustum.Contains(ref point) == ContainmentType.Contains) {
								collection.Add(m_data[i]);
							}
						}
					}
				}
			}
		}

		private void collectIntersects(ref RectangleF screenBounds, ref WorldToScreenParams param, ref List<T> collection, bool contains) {
			if (ProjectBoundsIntersection(ref m_bounds, ref param, ref screenBounds, false)) {
				if (m_childNodes != null) {
					for (int i = 0; i < m_childNodes.Length; i++) {
						m_childNodes[i].collectIntersects(ref screenBounds, ref param, ref collection, contains);
					}
				}

				if (m_data.Count > 0) {
					for (int i = 0; i < m_data.Count; i++) {
						if (m_data[i] is IOctreeBounds bData) {
							var bounds = bData.Bounds;
							if (ProjectBoundsIntersection(ref bounds, ref param, ref screenBounds, contains)) {
								collection.Add(m_data[i]);
							}
						} else if (m_data[i] is IOctreePoint pData) {
							var point = pData.Point;
							if (ProjectPointIntersection(ref point, ref param, ref screenBounds)) {
								collection.Add(m_data[i]);
							}
						}
					}
				}
			}
		}

		private bool ProjectPointIntersection(ref Vector3 point, ref WorldToScreenParams param, ref RectangleF screenBounds) {
			if (param.ViewFrustum.Contains(ref point) == ContainmentType.Disjoint) return false;
			Vector3.Project(
				vector: ref point,
				x: param.x,
				y: param.y,
				width: param.width,
				height: param.height,
				minZ: param.minZ,
				maxZ: param.maxZ,
				worldViewProjection: ref param.worldViewProjection,
				result: out var result
			);
			var screenPoint = new Vector2(result.X, result.Y);
			return screenBounds.Contains(screenPoint);
		}

		private bool ProjectBoundsIntersection(ref BoundingBox bounds, ref WorldToScreenParams param, ref RectangleF screenBounds, bool contains) {
			if (param.ViewFrustum.Intersects(ref bounds) == false) return false;

			var corners = bounds.GetCorners();
			for (int i = 0; i < corners.Length; i++) {
				Vector3.Project(
					vector: ref corners[i],
					x: param.x,
					y: param.y,
					width: param.width,
					height: param.height,
					minZ: param.minZ,
					maxZ: param.maxZ,
					worldViewProjection: ref param.worldViewProjection,
					result: out corners[i]
				);
			}

			//var rect = new RectangleF(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);
			var rect = new RawRectangleF(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);

			// Create a rectangle that contains all points and do a rect-rect intersect test
			for (int i = 0; i < corners.Length; i++) {
				if (corners[i].X < rect.Left) rect.Left = corners[i].X;
				if (corners[i].X > rect.Right) rect.Right = corners[i].X;
				if (corners[i].Y < rect.Top) rect.Top = corners[i].Y;
				if (corners[i].Y > rect.Bottom) rect.Bottom = corners[i].Y;
			}

			var rectF = new RectangleF(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);

			if (contains) {
				screenBounds.Contains(ref rectF, out var result);
				return result;
			}

			return rectF.Intersects(screenBounds);
		}

		/// <summary>
		/// Clears the whole tree of data
		/// </summary>
		public void Clear() {
			if (m_childNodes != null) {
				for (int i = 0; i < m_childNodes.Length; i++) {
					m_childNodes[i].Clear();
				}

				m_childNodes = null;
			}

			if (m_data != null) {
				for (int i = 0; i < m_data.Count; i++) {
					if (m_data[i] is IOctreeAware<T> aware) {
						aware.ParentNode = null;
					}
				}

				m_data.Clear();
			}

			m_allData.Clear();
		}
	}
}