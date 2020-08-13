using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Mathematics.Interop;

// ReSharper disable LoopCanBeConvertedToQuery

namespace DXToolKit {
	/// <summary>
	/// Standard octree used for partitioning space into octets for faster intersection checking
	/// </summary>
	/// <typeparam name="T">The object type this octree should store</typeparam>
	public class Octree<T> where T : IOctreeData {
		/// <summary>
		/// Defines the maximum amount of data allowed on a single node
		/// This does not limit the amount forced by data that cannot fit into any child nodes
		/// </summary>
		private readonly int MAX_DATA_PER_NODE;

		/// <summary>
		/// Defines the maximum amount of subdivisions in the octree
		/// </summary>
		private readonly int MAX_DEPTH;

		/// <summary>
		/// Value indicating if some more heavy update functions should run on multiple threads
		/// </summary>
		private bool m_allowThreading = true;

		/// <summary>
		/// Local data in this node
		/// </summary>
		private List<T> m_data;

		/// <summary>
		/// Eight child nodes or null if there is no child nodes
		/// </summary>
		private Octree<T>[] m_childNodes;

		/// <summary>
		/// Bounds of this node
		/// </summary>
		private BoundingBox m_bounds;

		/// <summary>
		/// Level of this node (Where 0 is top level)
		/// </summary>
		private int m_level;

		/// <summary>
		/// Reference to the parent node of this node
		/// </summary>
		private Octree<T> m_parentNode;

		/// <summary>
		/// A container for nodes that intersect / are not longer contained in its parent node when a update is called
		/// </summary>
		private List<T> m_updateIntersectList;

		/// <summary>
		/// Bool controlling if the octree can be updated
		/// </summary>
		private bool m_allowUpdate;

		/// <summary>
		/// Controller to limit the amount of data that is pushed to children.
		/// </summary>
		private int m_lastCleanupCount = 0;

		/// <summary>
		/// Gets the data in the node
		/// </summary>
		public List<T> Data => m_data;

		/// <summary>
		/// Gets the bounds of the node
		/// </summary>
		public BoundingBox Bounds => m_bounds;

		/// <summary>
		/// Gets a value indicating if the node has child nodes
		/// </summary>
		public bool HasChildNodes => m_childNodes != null;

		/// <summary>
		/// Gets a value indicating if the node has any stored data
		/// </summary>
		public bool HasData => m_data.Count > 0;

		/// <summary>
		/// Gets or sets a value that controls if the octree is allowed to use multiple threads
		/// </summary>
		public bool AllowThreading {
			get => m_allowThreading;
			set => m_allowThreading = value;
		}


		/// <summary>
		/// Creates a new octree
		/// </summary>
		/// <param name="minimum">Bounds minimum</param>
		/// <param name="maximum">Bounds maximum</param>
		/// <param name="maxDataPerNode">Max data per octree node before it will subdivide into 8 child nodes (this is just a soft limit)</param>
		/// <param name="maxDepth">The maximum amount of subdivisions allowed in the octree</param>
		/// <param name="allowThreading">If the octree should perform multithreaded operations when updating. This is only used for a octree that actually runs "update"</param>
		public Octree(Vector3 minimum, Vector3 maximum, int maxDataPerNode = 8, int maxDepth = 16, bool allowThreading = true) {
			VerifyDataType();
			MAX_DEPTH = maxDepth;
			MAX_DATA_PER_NODE = maxDataPerNode;
			m_allowThreading = allowThreading;

			m_bounds = new BoundingBox(minimum, maximum);
			m_data = new List<T>();
			m_childNodes = null;
			m_level = 0;
			m_parentNode = null;
			m_updateIntersectList = new List<T>();
		}

		/// <summary>
		/// Child octree constructor
		/// </summary>
		/// <param name="parentNode">Parent node</param>
		/// <param name="bounds">Target bounds</param>
		private Octree(Octree<T> parentNode, ref BoundingBox bounds) {
			m_bounds = bounds;
			m_data = new List<T>();
			m_childNodes = null;
			m_parentNode = parentNode;
			m_level = parentNode.m_level + 1;
			MAX_DATA_PER_NODE = parentNode.MAX_DATA_PER_NODE;
			MAX_DEPTH = parentNode.MAX_DEPTH;
		}

		/// <summary>
		/// Adds data to the octree.
		/// </summary>
		/// <param name="data">The data to add</param>
		/// <returns>A value indicating if the data was successfully added. (Can fail if data is outside the bounds of the octree)</returns>
		public bool Add(T data) {
			if (data is IOctreeBounds bData && m_bounds.Contains(bData.Bounds) == ContainmentType.Contains ||
			    data is IOctreePoint pData && m_bounds.Contains(pData.Point) == ContainmentType.Contains) {
				if (m_childNodes == null) {
					if (m_data.Count >= MAX_DATA_PER_NODE && m_level < MAX_DEPTH) {
						Split();
					}
				}

				if (m_childNodes != null) {
					for (int i = 0; i < m_childNodes.Length; i++) {
						if (m_childNodes[i].Add(data)) {
							return true;
						}
					}
				}

				m_data.Add(data);
				if (data is IOctreeAware<T> aware) aware.ParentNode = this;

				return true;
			}

			return false;
		}


		/// <summary>
		/// Removes data from the octree. Must be of type IOctreeAware so the octree knows what node to remove the data from
		/// </summary>
		/// <param name="data">The data to remove</param>
		/// <param name="merge">If the octree should try to merge the node after removing data</param>
		/// <returns>A value indicating if the removal was successful</returns>
		public bool Remove(IOctreeAware<T> data, bool merge = true) {
			if (data.ParentNode != null) {
				// Remove date from node
				data.ParentNode.m_data.Remove((T) data);
				if (merge) {
					if (data.ParentNode.m_parentNode == null) {
						// If root node, run merge directly
						Merge();
					} else {
						// Else get nodes parent to try and merge
						data.ParentNode.m_parentNode.Merge();
					}
				}

				// Unset node property
				data.ParentNode = null;
				return true;
			}

			return false;
		}


		/// <summary>
		/// Updates the octree to reflect changes in positions of all contained data.
		/// Can only be called on the root node of the octree
		/// </summary>
		/// <exception cref="Exception">Will throw exceptions if update is not called on the root node, or if the data in the octree does not implement IOctreeAware</exception>
		public void Update() {
			if (m_parentNode != null) {
				throw new Exception("Update must be called on root node of the octree");
			}

			if (m_allowUpdate == false) {
				throw new Exception("Cannot update a octree that consists of data not implementing IOctreeAware<T>");
			}

			// Empty collection list
			m_updateIntersectList.Clear();

			// Gather all data points that are on the edge of a node
			GetOutOfBoundsData(ref m_updateIntersectList);

			// Remove and add back those data points
			for (int i = 0; i < m_updateIntersectList.Count; i++) {
				if (m_updateIntersectList[i] is IOctreeAware<T> aware) {
					// Remove without merging
					Remove(aware, false);
				}
			}

			for (int i = 0; i < m_updateIntersectList.Count; i++) {
				var success = Add(m_updateIntersectList[i]);
				if (!success) {
					// TODO dynamic resize?
					throw new Exception("Node could not be added when updating, must be outside the bounds of the octree");
				}
			}

			// Run a PushDataToLeafs to push all data to leaf nodes if possible
			TryPushDataToChildren();

			// Run merge to clean up any node that can merge
			Merge();
		}


		/// <summary>
		/// Gets all data that are no longer completely contained within its parent node
		/// </summary>
		/// <param name="resultList">The resulting data that is out of bounds</param>
		private void GetOutOfBoundsData(ref List<T> resultList) {
			if (HasData) {
				for (int i = 0; i < m_data.Count; i++) {
					var oob = (m_data[i] is IOctreeBounds bData && m_bounds.Contains(bData.Bounds) != ContainmentType.Contains ||
					           m_data[i] is IOctreePoint pData && m_bounds.Contains(pData.Point) != ContainmentType.Contains);
					if (oob) {
						resultList.Add(m_data[i]);
					}
				}
			}

			if (HasChildNodes) {
				if (m_level == 0 && m_allowThreading) {
					var dataLists = new List<T>[8];

					Parallel.For(0, 8, i => {
						dataLists[i] = new List<T>();
						m_childNodes[i].GetOutOfBoundsData(ref dataLists[i]);
					});

					for (int i = 0; i < 8; i++) {
						resultList.AddRange(dataLists[i]);
					}
				} else {
					for (int i = 0; i < m_childNodes.Length; i++) {
						m_childNodes[i].GetOutOfBoundsData(ref resultList);
					}
				}
			}
		}


		/// <summary>
		/// Tries to push all data into child nodes.
		/// <remarks>
		/// This is not 100% accurate. Will only run "push" on nodes where the amount of data has actually changed since the last update
		/// But since this is more of a optimization function, it should take as little time as possible to accomplish. If not, it would defeat the purpose.
		/// Reason for this function is to fix issues with nodes further up the hierarchy (especially the root node) getting to much data because data is moved to the root when that data is intersecting on one of the 6 midpoint lines after moving
		/// </remarks>
		/// </summary>
		private void TryPushDataToChildren() {
			if (m_lastCleanupCount != m_data.Count) {
				if (m_data.Count > MAX_DATA_PER_NODE * 1 && m_level < MAX_DEPTH) {
					for (int i = 0; i < m_data.Count; i++) {
						if (m_data.Count < MAX_DATA_PER_NODE) {
							break;
						}

						if (!HasChildNodes) {
							Split();
						}

						for (int j = 0; j < m_childNodes.Length; j++) {
							if (m_childNodes[j].Add(m_data[i])) {
								m_data.RemoveAt(i--);
								break;
							}
						}
					}
				}
			}

			if (HasChildNodes) {
				if (m_level == 0 && m_allowThreading) {
					Parallel.For(0, 8, i => {
						m_childNodes[i].TryPushDataToChildren();
					});
				} else {
					for (int i = 0; i < m_childNodes.Length; i++) {
						m_childNodes[i].TryPushDataToChildren();
					}
				}
			}

			m_lastCleanupCount = m_data.Count;
		}

		/// <summary>
		/// Merges this node and all child nodes of this node if possible.
		/// </summary>
		/// <returns>True on successful merge. Even if false, child nodes down the hierarchy might still have been merged</returns>
		/// <exception cref="Exception">
		/// Throws exception if there for some odd reason is a merge request on a node where one or more of its children has its own children.
		/// Should never happen, but this is just for safety to make sure data does not end up floating around the abyss
		/// </exception>
		private bool Merge() {
			if (m_childNodes != null) {
				// Recurse down the hierarchy to check if ALL child nodes can merge
				var canMerge = true;

				// Want to try and run merge on all child nodes
				if (m_level == 0 && m_allowThreading) {
					Parallel.For(0, 8, i => {
						if (!m_childNodes[i].Merge()) {
							canMerge = false;
						}
					});
				} else {
					for (int i = 0; i < m_childNodes.Length; i++) {
						if (!m_childNodes[i].Merge()) {
							canMerge = false;
						}
					}
				}

				// If child is preventing merge, get out of here
				if (!canMerge) {
					return false;
				}

				// Count up all data that would end up in this node
				var totalData = m_data.Count;
				for (int i = 0; i < m_childNodes.Length; i++) {
					totalData += m_childNodes[i].m_data.Count;
				}

				// Cannot merge, since total data would exceed capacity
				if (totalData > MAX_DATA_PER_NODE) {
					return false;
				}

				// If we are here, we can merge child nodes
				for (int i = 0; i < m_childNodes.Length; i++) {
					if (m_childNodes[i].HasChildNodes) {
						throw new Exception("This should never happen. We should never get to a point where a child node to be merged has children");
					}

					for (int j = 0; j < m_childNodes[i].m_data.Count; j++) {
						// Add data into this
						m_data.Add(m_childNodes[i].m_data[j]);
						// Update aware node
						if (m_childNodes[i].m_data[j] is IOctreeAware<T> aware) {
							aware.ParentNode = this;
						}
					}

					// Not really needed, but just to make sure there are no references lurking about
					m_childNodes[i].m_parentNode = null;
					m_childNodes[i].m_data = null;
					m_childNodes[i].m_childNodes = null;
				}

				// Remove all child nodes
				m_childNodes = null;

				// Merge successful
				return true;
			}

			// No children, report success
			return true;
		}

		/// <summary>
		/// Splits a node into 8 child nodes.
		/// This function does not respect MAX_DEPTH and will subdivide even if it should not be allowed to split
		/// </summary>
		/// <returns>True on success (will fail if the node has child nodes)</returns>
		private bool Split() {
			if (m_childNodes == null) {
				var newBounds = SplitBoundingBox(ref m_bounds);
				m_childNodes = new Octree<T>[8];
				for (int i = 0; i < 8; i++) {
					m_childNodes[i] = new Octree<T>(this, ref newBounds[i]);

					for (int j = 0; j < m_data.Count; j++) {
						if (m_childNodes[i].Add(m_data[j])) {
							m_data.RemoveAt(j);
							j--;
						}
					}
				}

				return true;
			}

			return false;
		}

		/// <summary>
		/// Gets every node in the octree
		/// </summary>
		/// <returns>A list of all nodes of the octree</returns>
		public List<Octree<T>> AllNodes() {
			var result = new List<Octree<T>>();
			GetAllNodes(ref result);
			return result;
		}

		/// <summary>
		/// Gets all the data stored in the octree.
		/// NOTE! This is "slower" then just having your own collection of the data aside the octree and polling that for information.
		/// </summary>
		/// <returns>A list of all data stored in the octree</returns>
		public List<T> AllData() {
			var result = new List<T>();
			GetAllData(ref result);
			return result;
		}

		/// <summary>
		/// Recurse get all nodes
		/// </summary>
		/// <param name="resultList">Reference result list nodes are added to</param>
		private void GetAllNodes(ref List<Octree<T>> resultList) {
			if (m_childNodes != null) {
				for (int i = 0; i < m_childNodes.Length; i++) {
					m_childNodes[i].GetAllNodes(ref resultList);
				}
			}

			resultList.Add(this);
		}

		/// <summary>
		/// Recurse get all data
		/// </summary>
		/// <param name="resultList">Reference result list data is added to</param>
		private void GetAllData(ref List<T> resultList) {
			if (m_childNodes != null) {
				for (int i = 0; i < m_childNodes.Length; i++) {
					m_childNodes[i].GetAllData(ref resultList);
				}
			}

			resultList.AddRange(m_data);
		}


		/// <summary>
		/// Splits a bounding box into eight smaller bounding boxes that fits perfectly inside the parent bounds
		/// </summary>
		/// <param name="box">The bounds to split</param>
		/// <returns>Array of eight new bounding boxes</returns>
		private BoundingBox[] SplitBoundingBox(ref BoundingBox box) {
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
		/// Verifies that the T data type
		/// </summary>
		private void VerifyDataType() {
			var interfaces = typeof(T).GetInterfaces();
			var boundsInterface = false;
			var pointInterface = false;
			var awareInterface = false;

			for (int i = 0; i < interfaces.Length; i++) {
				if (interfaces[i] == typeof(IOctreeBounds)) {
					boundsInterface = true;
				}

				if (interfaces[i] == typeof(IOctreePoint)) {
					pointInterface = true;
				}

				if (interfaces[i] == typeof(IOctreeAware<T>)) {
					awareInterface = true;
				}
			}

			if (boundsInterface && pointInterface) {
				throw new Exception("T cannot implement both IOctreeBoundsData and IOctreePointData");
			}

			if (!boundsInterface && !pointInterface) {
				throw new Exception("T must implement one of IOctreeBoundsData or IOctreePointData");
			}

			if (awareInterface) {
				m_allowUpdate = true;
			}
		}


		#region Intersection

		/// <summary>
		/// Gets data that intersects with a given ray.
		/// NOTE: This does not work with Point data (because of floating point imprecision)
		/// </summary>
		/// <param name="ray">The ray to check against</param>
		/// <returns>A list of intersecting data</returns>
		public List<T> Intersects(Ray ray) {
			var result = new List<T>();
			CollectIntersect(ref ray, ref result);
			return result;
		}

		/// <summary>
		/// Gets data that intersects with a given point in space.
		/// NOTE: This does not work with Point data (because of floating point imprecision)
		/// </summary>
		/// <param name="point">The point to check against</param>
		/// <returns>A list of intersecting data</returns>
		public List<T> Intersects(Vector3 point) {
			var result = new List<T>();
			CollectIntersect(ref point, ref result);
			return result;
		}

		/// <summary>
		/// Gets data that intersects with a given bounding box.
		/// </summary>
		/// <param name="box">The bounds to check against</param>
		/// <returns>A list of intersecting data</returns>
		public List<T> Intersects(BoundingBox box) {
			var result = new List<T>();
			CollectIntersect(ref box, ref result);
			return result;
		}

		/// <summary>
		/// Gets data that intersects with a given bounding sphere.
		/// </summary>
		/// <param name="sphere">The bounds to check against</param>
		/// <returns>A list of intersecting data</returns>
		public List<T> Intersects(BoundingSphere sphere) {
			var result = new List<T>();
			CollectIntersect(ref sphere, ref result);
			return result;
		}

		/// <summary>
		/// Gets data that intersects with a given bounding frustum.
		/// </summary>
		/// <param name="frustum">The bounds to check against</param>
		/// <returns>A list of intersecting data</returns>
		public List<T> Intersects(BoundingFrustum frustum) {
			var result = new List<T>();
			CollectIntersect(ref frustum, ref result);
			return result;
		}

		/// <summary>
		/// Gets data that intersects with a given screen rectangle.
		/// </summary>
		/// <param name="screenRectangle">The bounds to check against</param>
		/// <param name="worldToScreenParams">Information on how to project from world space into screen space</param>
		/// <param name="contains">If data must be contained within the screen rectangle</param>
		/// <returns>A list of intersecting data</returns>
		public List<T> Intersects(RectangleF screenRectangle, WorldToScreenParams worldToScreenParams, bool contains = false) {
			var result = new List<T>();
			CollectIntersect(ref screenRectangle, ref worldToScreenParams, ref result, contains);
			return result;
		}

		private void CollectIntersect(ref Ray ray, ref List<T> result) {
			if (m_bounds.Intersects(ref ray)) {
				for (int i = 0; i < m_data.Count; i++) {
					if (m_data[i] is IOctreeBounds bData && bData.Bounds.Intersects(ref ray)) {
						result.Add(m_data[i]);
					}
				}

				if (m_childNodes != null) {
					for (int i = 0; i < m_childNodes.Length; i++) {
						m_childNodes[i].CollectIntersect(ref ray, ref result);
					}
				}
			}
		}

		private void CollectIntersect(ref Vector3 point, ref List<T> result) {
			if (m_bounds.Contains(ref point) == ContainmentType.Contains) {
				for (int i = 0; i < m_data.Count; i++) {
					if (m_data[i] is IOctreeBounds bData && bData.Bounds.Contains(ref point) == ContainmentType.Contains) {
						result.Add(m_data[i]);
					}
				}

				if (m_childNodes != null) {
					for (int i = 0; i < m_childNodes.Length; i++) {
						m_childNodes[i].CollectIntersect(ref point, ref result);
					}
				}
			}
		}

		private void CollectIntersect(ref BoundingBox bounds, ref List<T> result) {
			if (m_bounds.Intersects(ref bounds)) {
				for (int i = 0; i < m_data.Count; i++) {
					if (m_data[i] is IOctreeBounds bData && bData.Bounds.Intersects(ref bounds) ||
					    m_data[i] is IOctreePoint pData && bounds.Contains(pData.Point) == ContainmentType.Contains) {
						result.Add(m_data[i]);
					}
				}

				if (m_childNodes != null) {
					for (int i = 0; i < m_childNodes.Length; i++) {
						m_childNodes[i].CollectIntersect(ref bounds, ref result);
					}
				}
			}
		}

		private void CollectIntersect(ref BoundingSphere bounds, ref List<T> result) {
			if (m_bounds.Intersects(ref bounds)) {
				for (int i = 0; i < m_data.Count; i++) {
					if (m_data[i] is IOctreeBounds bData && bData.Bounds.Intersects(ref bounds)) {
						result.Add(m_data[i]);
					} else if (m_data[i] is IOctreePoint pData) {
						var p = pData.Point;
						if (bounds.Contains(ref p) == ContainmentType.Contains) {
							result.Add(m_data[i]);
						}
					}
				}

				if (m_childNodes != null) {
					for (int i = 0; i < m_childNodes.Length; i++) {
						m_childNodes[i].CollectIntersect(ref bounds, ref result);
					}
				}
			}
		}

		private void CollectIntersect(ref BoundingFrustum frustum, ref List<T> result) {
			if (frustum.Intersects(ref m_bounds)) {
				for (int i = 0; i < m_data.Count; i++) {
					if (m_data[i] is IOctreeBounds bData) {
						var bounds = bData.Bounds;
						if (frustum.Intersects(ref bounds)) {
							result.Add(m_data[i]);
						}
					} else if (m_data[i] is IOctreePoint pData) {
						var p = pData.Point;
						if (frustum.Contains(ref p) == ContainmentType.Contains) {
							result.Add(m_data[i]);
						}
					}
				}

				if (m_childNodes != null) {
					for (int i = 0; i < m_childNodes.Length; i++) {
						m_childNodes[i].CollectIntersect(ref frustum, ref result);
					}
				}
			}
		}

		private void CollectIntersect(ref RectangleF screenBounds, ref WorldToScreenParams param, ref List<T> collection, bool contains) {
			if (ProjectBoundsIntersection(ref m_bounds, ref param, ref screenBounds, false)) {
				if (m_childNodes != null) {
					for (int i = 0; i < m_childNodes.Length; i++) {
						m_childNodes[i].CollectIntersect(ref screenBounds, ref param, ref collection, contains);
					}
				}

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

		private bool ProjectPointIntersection(ref Vector3 point, ref WorldToScreenParams param, ref RectangleF screenBounds) {
			if (param.ViewFrustum.Contains(ref point) == ContainmentType.Disjoint) return false;
			Vector3.Project(
				vector: ref point,
				x: param.X,
				y: param.Y,
				width: param.Width,
				height: param.Height,
				minZ: param.MinZ,
				maxZ: param.MaxZ,
				worldViewProjection: ref param.WorldViewProjection,
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
					x: param.X,
					y: param.Y,
					width: param.Width,
					height: param.Height,
					minZ: param.MinZ,
					maxZ: param.MaxZ,
					worldViewProjection: ref param.WorldViewProjection,
					result: out corners[i]
				);
			}

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

		#endregion

		#region Contains

		/// <summary>
		/// Runs a containment check against data in the octree.
		/// </summary>
		/// <param name="box">The bounds to check against</param>
		/// <param name="containmentType">What containment type to look for</param>
		/// <returns>A list of intersecting data</returns>
		public List<T> Contains(BoundingBox box, ContainmentType containmentType = ContainmentType.Contains) {
			var result = new List<T>();
			CollectContains(ref box, ref containmentType, ref result);
			return result;
		}

		/// <summary>
		/// Runs a containment check against data in the octree.
		/// </summary>
		/// <param name="sphere">The bounds to check against</param>
		/// <param name="containmentType">What containment type to look for</param>
		/// <returns>A list of intersecting data</returns>
		public List<T> Contains(BoundingSphere sphere, ContainmentType containmentType = ContainmentType.Contains) {
			var result = new List<T>();
			CollectContains(ref sphere, ref containmentType, ref result);
			return result;
		}

		/// <summary>
		/// Runs a containment check against data in the octree.
		/// </summary>
		/// <param name="frustum">The bounds to check against</param>
		/// <param name="containmentType">What containment type to look for</param>
		/// <returns>A list of intersecting data</returns>
		public List<T> Contains(BoundingFrustum frustum, ContainmentType containmentType = ContainmentType.Contains) {
			var result = new List<T>();
			CollectContains(ref frustum, ref containmentType, ref result);
			return result;
		}

		private void CollectContains(ref BoundingBox bounds, ref ContainmentType containmentType, ref List<T> result) {
			// Special rule if containment type is disjoint. In that case we need only nodes that are partially or totally outside of input bounds
			if (containmentType == ContainmentType.Disjoint && m_bounds.Contains(ref bounds) != ContainmentType.Contains || m_bounds.Intersects(ref bounds)) {
				for (int i = 0; i < m_data.Count; i++) {
					if (m_data[i] is IOctreeBounds bData) {
						if (bounds.Contains(bData.Bounds) == containmentType) {
							result.Add(m_data[i]);
						}
					} else if (m_data[i] is IOctreePoint pData) {
						if (bounds.Contains(pData.Point) == containmentType) {
							result.Add(m_data[i]);
						}
					}
				}

				if (m_childNodes != null) {
					for (int i = 0; i < m_childNodes.Length; i++) {
						m_childNodes[i].CollectContains(ref bounds, ref containmentType, ref result);
					}
				}
			}
		}

		private void CollectContains(ref BoundingSphere sphere, ref ContainmentType containmentType, ref List<T> result) {
			if (containmentType == ContainmentType.Disjoint && m_bounds.Contains(ref sphere) != ContainmentType.Contains || m_bounds.Intersects(ref sphere)) {
				for (int i = 0; i < m_data.Count; i++) {
					if (m_data[i] is IOctreeBounds bData) {
						var b = bData.Bounds;
						if (sphere.Contains(ref b) == containmentType) {
							result.Add(m_data[i]);
						}
					} else if (m_data[i] is IOctreePoint pData) {
						var p = pData.Point;
						if (sphere.Contains(ref p) == containmentType) {
							result.Add(m_data[i]);
						}
					}
				}

				if (m_childNodes != null) {
					for (int i = 0; i < m_childNodes.Length; i++) {
						m_childNodes[i].CollectContains(ref sphere, ref containmentType, ref result);
					}
				}
			}
		}

		private void CollectContains(ref BoundingFrustum frustum, ref ContainmentType containmentType, ref List<T> result) {
			if (containmentType == ContainmentType.Disjoint && frustum.Contains(ref m_bounds) != ContainmentType.Contains || frustum.Intersects(ref m_bounds)) {
				for (int i = 0; i < m_data.Count; i++) {
					if (m_data[i] is IOctreeBounds bData) {
						if (frustum.Contains(bData.Bounds) == containmentType) {
							result.Add(m_data[i]);
						}
					} else if (m_data[i] is IOctreePoint pData) {
						if (frustum.Contains(pData.Point) == containmentType) {
							result.Add(m_data[i]);
						}
					}
				}

				if (m_childNodes != null) {
					for (int i = 0; i < m_childNodes.Length; i++) {
						m_childNodes[i].CollectContains(ref frustum, ref containmentType, ref result);
					}
				}
			}
		}

		#endregion
	}
}