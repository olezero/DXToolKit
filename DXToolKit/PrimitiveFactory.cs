using System.Collections.Generic;
using SharpDX;

namespace DXToolKit {
	public struct Primitive {
		public Vector3[] Positions;
		public int[] Indices;
		public Vector2[] UVs;
		public Vector3[] Normals;
	}

	public static class PrimitiveFactory {
		public static Primitive Cube(float length = 1.0F, float height = 1.0F, float width = 1.0F) {
			#region Vertices

			var p0 = new Vector3(-length * .5f, -width * .5f, height * .5f);
			var p1 = new Vector3(length * .5f, -width * .5f, height * .5f);
			var p2 = new Vector3(length * .5f, -width * .5f, -height * .5f);
			var p3 = new Vector3(-length * .5f, -width * .5f, -height * .5f);

			var p4 = new Vector3(-length * .5f, width * .5f, height * .5f);
			var p5 = new Vector3(length * .5f, width * .5f, height * .5f);
			var p6 = new Vector3(length * .5f, width * .5f, -height * .5f);
			var p7 = new Vector3(-length * .5f, width * .5f, -height * .5f);

			var vertices = new[] {
				// Bottom
				p0, p1, p2, p3,

				// Left
				p7, p4, p0, p3,

				// Front
				p4, p5, p1, p0,

				// Back
				p6, p7, p3, p2,

				// Right
				p5, p6, p2, p1,

				// Top
				p7, p6, p5, p4
			};

			#endregion

			#region Normales

			var up = Vector3.Up;
			var down = Vector3.Down;
			var front = Vector3.ForwardLH;
			var back = Vector3.BackwardLH;
			var left = Vector3.Left;
			var right = Vector3.Right;

			var normales = new[] {
				// Bottom
				down, down, down, down,

				// Left
				left, left, left, left,

				// Front
				front, front, front, front,

				// Back
				back, back, back, back,

				// Right
				right, right, right, right,

				// Top
				up, up, up, up
			};

			#endregion

			#region UVs

			var _00 = new Vector2(0f, 0f);
			var _10 = new Vector2(1f, 0f);
			var _01 = new Vector2(0f, 1f);
			var _11 = new Vector2(1f, 1f);

			var uvs = new[] {
				// Bottom
				_11, _01, _00, _10,

				// Left
				_11, _01, _00, _10,

				// Front
				_11, _01, _00, _10,

				// Back
				_11, _01, _00, _10,

				// Right
				_11, _01, _00, _10,

				// Top
				_11, _01, _00, _10,
			};

			#endregion

			#region Triangles

			var triangles = new[] {
				// Bottom
				3, 1, 0,
				3, 2, 1,

				// Left
				3 + 4 * 1, 1 + 4 * 1, 0 + 4 * 1,
				3 + 4 * 1, 2 + 4 * 1, 1 + 4 * 1,

				// Front
				3 + 4 * 2, 1 + 4 * 2, 0 + 4 * 2,
				3 + 4 * 2, 2 + 4 * 2, 1 + 4 * 2,

				// Back
				3 + 4 * 3, 1 + 4 * 3, 0 + 4 * 3,
				3 + 4 * 3, 2 + 4 * 3, 1 + 4 * 3,

				// Right
				3 + 4 * 4, 1 + 4 * 4, 0 + 4 * 4,
				3 + 4 * 4, 2 + 4 * 4, 1 + 4 * 4,

				// Top
				3 + 4 * 5, 1 + 4 * 5, 0 + 4 * 5,
				3 + 4 * 5, 2 + 4 * 5, 1 + 4 * 5,
			};

			#endregion

			return new Primitive {Positions = vertices, Normals = normales, UVs = uvs, Indices = triangles};
		}

		public static Primitive Plane(float length = 1.0F, float width = 1.0F, int resX = 2, int resZ = 2) {
			#region Vertices		

			var vertices = new Vector3[resX * resZ];
			for (int z = 0; z < resZ; z++) {
				// [ -length / 2, length / 2 ]
				float zPos = ((float) z / (resZ - 1) - .5f) * length;
				for (int x = 0; x < resX; x++) {
					// [ -width / 2, width / 2 ]
					float xPos = ((float) x / (resX - 1) - .5f) * width;
					vertices[x + z * resX] = new Vector3(xPos, 0f, zPos);
				}
			}

			#endregion

			#region Normales

			var normales = new Vector3[vertices.Length];
			for (int n = 0; n < normales.Length; n++)
				normales[n] = Vector3.Up;

			#endregion

			#region UVs		

			var uvs = new Vector2[vertices.Length];
			for (int v = 0; v < resZ; v++) {
				for (int u = 0; u < resX; u++) {
					uvs[u + v * resX] = new Vector2((float) u / (resX - 1), (float) v / (resZ - 1));
				}
			}

			#endregion

			#region Triangles

			int nbFaces = (resX - 1) * (resZ - 1);
			var triangles = new int[nbFaces * 6];
			int t = 0;
			for (int face = 0; face < nbFaces; face++) {
				// Retrieve lower left corner from face ind
				int i = face % (resX - 1) + (face / (resZ - 1) * resX);

				triangles[t++] = i + resX;
				triangles[t++] = i + 1;
				triangles[t++] = i;

				triangles[t++] = i + resX;
				triangles[t++] = i + resX + 1;
				triangles[t++] = i + 1;
			}

			#endregion

			return new Primitive {Positions = vertices, Normals = normales, UVs = uvs, Indices = triangles};
		}

		public static Primitive Cone(float height = 1.0F, float bottomRadius = 1.0F, float topRadius = 1.0F,
			int nbSides = 18) {
			int nbHeightSeg = 1; // Not implemented yet

			int nbVerticesCap = nbSides + 1;

			#region Vertices

			// bottom + top + sides
			var vertices = new Vector3[nbVerticesCap + nbVerticesCap + nbSides * nbHeightSeg * 2 + 2];
			int vert = 0;
			float _2pi = Mathf.PI * 2f;

			// Bottom cap
			vertices[vert++] = new Vector3(0f, 0f, 0f);
			while (vert <= nbSides) {
				float rad = (float) vert / nbSides * _2pi;
				vertices[vert] = new Vector3(Mathf.Cos(rad) * bottomRadius, 0f, Mathf.Sin(rad) * bottomRadius);
				vert++;
			}

			// Top cap
			vertices[vert++] = new Vector3(0f, height, 0f);
			while (vert <= nbSides * 2 + 1) {
				float rad = (float) (vert - nbSides - 1) / nbSides * _2pi;
				vertices[vert] = new Vector3(Mathf.Cos(rad) * topRadius, height, Mathf.Sin(rad) * topRadius);
				vert++;
			}

			// Sides
			int v = 0;
			while (vert <= vertices.Length - 4) {
				float rad = (float) v / nbSides * _2pi;
				vertices[vert] = new Vector3(Mathf.Cos(rad) * topRadius, height, Mathf.Sin(rad) * topRadius);
				vertices[vert + 1] = new Vector3(Mathf.Cos(rad) * bottomRadius, 0, Mathf.Sin(rad) * bottomRadius);
				vert += 2;
				v++;
			}

			vertices[vert] = vertices[nbSides * 2 + 2];
			vertices[vert + 1] = vertices[nbSides * 2 + 3];

			#endregion

			#region Normales

			// bottom + top + sides
			var normales = new Vector3[vertices.Length];
			vert = 0;

			// Bottom cap
			while (vert <= nbSides) {
				normales[vert++] = Vector3.Down;
			}

			// Top cap
			while (vert <= nbSides * 2 + 1) {
				normales[vert++] = Vector3.Up;
			}

			// Sides
			v = 0;
			while (vert <= vertices.Length - 4) {
				float rad = (float) v / nbSides * _2pi;
				float cos = Mathf.Cos(rad);
				float sin = Mathf.Sin(rad);

				normales[vert] = new Vector3(cos, 0f, sin);
				normales[vert + 1] = normales[vert];

				vert += 2;
				v++;
			}

			normales[vert] = normales[nbSides * 2 + 2];
			normales[vert + 1] = normales[nbSides * 2 + 3];

			#endregion

			#region UVs

			var uvs = new Vector2[vertices.Length];

			// Bottom cap
			int u = 0;
			uvs[u++] = new Vector2(0.5f, 0.5f);
			while (u <= nbSides) {
				float rad = (float) u / nbSides * _2pi;
				uvs[u] = new Vector2(Mathf.Cos(rad) * .5f + .5f, Mathf.Sin(rad) * .5f + .5f);
				u++;
			}

			// Top cap
			uvs[u++] = new Vector2(0.5f, 0.5f);
			while (u <= nbSides * 2 + 1) {
				float rad = (float) u / nbSides * _2pi;
				uvs[u] = new Vector2(Mathf.Cos(rad) * .5f + .5f, Mathf.Sin(rad) * .5f + .5f);
				u++;
			}

			// Sides
			int u_sides = 0;
			while (u <= uvs.Length - 4) {
				float t = (float) u_sides / nbSides;
				uvs[u] = new Vector2(t, 1f);
				uvs[u + 1] = new Vector2(t, 0f);
				u += 2;
				u_sides++;
			}

			uvs[u] = new Vector2(1f, 1f);
			uvs[u + 1] = new Vector2(1f, 0f);

			#endregion

			#region Triangles

			int nbTriangles = nbSides + nbSides + nbSides * 2;
			var triangles = new int[nbTriangles * 3 + 3];

			// Bottom cap
			int tri = 0;
			int i = 0;
			while (tri < nbSides - 1) {
				triangles[i] = 0;
				triangles[i + 1] = tri + 1;
				triangles[i + 2] = tri + 2;
				tri++;
				i += 3;
			}

			triangles[i] = 0;
			triangles[i + 1] = tri + 1;
			triangles[i + 2] = 1;
			tri++;
			i += 3;

			// Top cap
			//tri++;
			while (tri < nbSides * 2) {
				triangles[i] = tri + 2;
				triangles[i + 1] = tri + 1;
				triangles[i + 2] = nbVerticesCap;
				tri++;
				i += 3;
			}

			triangles[i] = nbVerticesCap + 1;
			triangles[i + 1] = tri + 1;
			triangles[i + 2] = nbVerticesCap;
			tri++;
			i += 3;
			tri++;

			// Sides
			while (tri <= nbTriangles) {
				triangles[i] = tri + 2;
				triangles[i + 1] = tri + 1;
				triangles[i + 2] = tri + 0;
				tri++;
				i += 3;

				triangles[i] = tri + 1;
				triangles[i + 1] = tri + 2;
				triangles[i + 2] = tri + 0;
				tri++;
				i += 3;
			}

			#endregion

			var result = new Primitive();
			result.Positions = vertices;
			result.Normals = normales;
			result.UVs = uvs;
			result.Indices = triangles;
			return result;
		}

		public static Primitive Tube(float height = 1.0F, float bottomRadius1 = 1.0F,
			float bottomRadius2 = 0.5F, float topRadius1 = 1.0F, float topRadius2 = 0.5F, int nbSides = 24) {
			// Outer shell is at radius1 + radius2 / 2, inner shell at radius1 - radius2 / 2
			int nbVerticesCap = nbSides * 2 + 2;
			int nbVerticesSides = nbSides * 2 + 2;

			#region Vertices

			// bottom + top + sides
			var vertices = new Vector3[nbVerticesCap * 2 + nbVerticesSides * 2];
			int vert = 0;
			float _2pi = Mathf.PI * 2f;

			// Bottom cap
			int sideCounter = 0;
			while (vert < nbVerticesCap) {
				sideCounter = sideCounter == nbSides ? 0 : sideCounter;

				float r1 = (float) (sideCounter++) / nbSides * _2pi;
				float cos = Mathf.Cos(r1);
				float sin = Mathf.Sin(r1);
				vertices[vert] = new Vector3(cos * (bottomRadius1 - bottomRadius2 * .5f), -height / 2.0F,
					sin * (bottomRadius1 - bottomRadius2 * .5f));
				vertices[vert + 1] = new Vector3(cos * (bottomRadius1 + bottomRadius2 * .5f), -height / 2.0F,
					sin * (bottomRadius1 + bottomRadius2 * .5f));
				vert += 2;
			}

			// Top cap
			sideCounter = 0;
			while (vert < nbVerticesCap * 2) {
				sideCounter = sideCounter == nbSides ? 0 : sideCounter;

				float r1 = (float) (sideCounter++) / nbSides * _2pi;
				float cos = Mathf.Cos(r1);
				float sin = Mathf.Sin(r1);
				vertices[vert] = new Vector3(cos * (topRadius1 - topRadius2 * .5f), height / 2.0F,
					sin * (topRadius1 - topRadius2 * .5f));
				vertices[vert + 1] = new Vector3(cos * (topRadius1 + topRadius2 * .5f), height / 2.0F,
					sin * (topRadius1 + topRadius2 * .5f));
				vert += 2;
			}

			// Sides (out)
			sideCounter = 0;
			while (vert < nbVerticesCap * 2 + nbVerticesSides) {
				sideCounter = sideCounter == nbSides ? 0 : sideCounter;

				float r1 = (float) (sideCounter++) / nbSides * _2pi;
				float cos = Mathf.Cos(r1);
				float sin = Mathf.Sin(r1);

				vertices[vert] = new Vector3(cos * (topRadius1 + topRadius2 * .5f), height / 2.0F,
					sin * (topRadius1 + topRadius2 * .5f));
				vertices[vert + 1] = new Vector3(cos * (bottomRadius1 + bottomRadius2 * .5f), -height / 2.0F,
					sin * (bottomRadius1 + bottomRadius2 * .5f));
				vert += 2;
			}

			// Sides (in)
			sideCounter = 0;
			while (vert < vertices.Length) {
				sideCounter = sideCounter == nbSides ? 0 : sideCounter;

				float r1 = (float) (sideCounter++) / nbSides * _2pi;
				float cos = Mathf.Cos(r1);
				float sin = Mathf.Sin(r1);

				vertices[vert] = new Vector3(cos * (topRadius1 - topRadius2 * .5f), height / 2.0F,
					sin * (topRadius1 - topRadius2 * .5f));
				vertices[vert + 1] = new Vector3(cos * (bottomRadius1 - bottomRadius2 * .5f), -height / 2.0F,
					sin * (bottomRadius1 - bottomRadius2 * .5f));
				vert += 2;
			}

			#endregion

			#region Normales

			// bottom + top + sides
			var normales = new Vector3[vertices.Length];
			vert = 0;

			// Bottom cap
			while (vert < nbVerticesCap) {
				normales[vert++] = Vector3.Down;
			}

			// Top cap
			while (vert < nbVerticesCap * 2) {
				normales[vert++] = Vector3.Up;
			}

			// Sides (out)
			sideCounter = 0;
			while (vert < nbVerticesCap * 2 + nbVerticesSides) {
				sideCounter = sideCounter == nbSides ? 0 : sideCounter;

				float r1 = (float) (sideCounter++) / nbSides * _2pi;

				normales[vert] = new Vector3(Mathf.Cos(r1), 0f, Mathf.Sin(r1));
				normales[vert + 1] = normales[vert];
				vert += 2;
			}

			// Sides (in)
			sideCounter = 0;
			while (vert < vertices.Length) {
				sideCounter = sideCounter == nbSides ? 0 : sideCounter;

				float r1 = (float) (sideCounter++) / nbSides * _2pi;

				normales[vert] = -(new Vector3(Mathf.Cos(r1), 0f, Mathf.Sin(r1)));
				normales[vert + 1] = normales[vert];
				vert += 2;
			}

			#endregion

			#region UVs

			var uvs = new Vector2[vertices.Length];

			vert = 0;
			// Bottom cap
			sideCounter = 0;
			while (vert < nbVerticesCap) {
				float t = (float) (sideCounter++) / nbSides;
				uvs[vert++] = new Vector2(0f, t);
				uvs[vert++] = new Vector2(1f, t);
			}

			// Top cap
			sideCounter = 0;
			while (vert < nbVerticesCap * 2) {
				float t = (float) (sideCounter++) / nbSides;
				uvs[vert++] = new Vector2(0f, t);
				uvs[vert++] = new Vector2(1f, t);
			}

			// Sides (out)
			sideCounter = 0;
			while (vert < nbVerticesCap * 2 + nbVerticesSides) {
				float t = (float) (sideCounter++) / nbSides;
				uvs[vert++] = new Vector2(t, 0f);
				uvs[vert++] = new Vector2(t, 1f);
			}

			// Sides (in)
			sideCounter = 0;
			while (vert < vertices.Length) {
				float t = (float) (sideCounter++) / nbSides;
				uvs[vert++] = new Vector2(t, 0f);
				uvs[vert++] = new Vector2(t, 1f);
			}

			#endregion

			#region Triangles

			int nbFace = nbSides * 4;
			int nbTriangles = nbFace * 2;
			int nbIndexes = nbTriangles * 3;
			var triangles = new int[nbIndexes];

			// Bottom cap
			int i = 0;
			sideCounter = 0;
			while (sideCounter < nbSides) {
				int current = sideCounter * 2;
				int next = sideCounter * 2 + 2;

				triangles[i++] = next + 1;
				triangles[i++] = next;
				triangles[i++] = current;

				triangles[i++] = current + 1;
				triangles[i++] = next + 1;
				triangles[i++] = current;

				sideCounter++;
			}

			// Top cap
			while (sideCounter < nbSides * 2) {
				int current = sideCounter * 2 + 2;
				int next = sideCounter * 2 + 4;

				triangles[i++] = current;
				triangles[i++] = next;
				triangles[i++] = next + 1;

				triangles[i++] = current;
				triangles[i++] = next + 1;
				triangles[i++] = current + 1;

				sideCounter++;
			}

			// Sides (out)
			while (sideCounter < nbSides * 3) {
				int current = sideCounter * 2 + 4;
				int next = sideCounter * 2 + 6;

				triangles[i++] = current;
				triangles[i++] = next;
				triangles[i++] = next + 1;

				triangles[i++] = current;
				triangles[i++] = next + 1;
				triangles[i++] = current + 1;

				sideCounter++;
			}


			// Sides (in)
			while (sideCounter < nbSides * 4) {
				int current = sideCounter * 2 + 6;
				int next = sideCounter * 2 + 8;

				triangles[i++] = next + 1;
				triangles[i++] = next;
				triangles[i++] = current;

				triangles[i++] = current + 1;
				triangles[i++] = next + 1;
				triangles[i++] = current;

				sideCounter++;
			}

			#endregion

			return new Primitive {Positions = vertices, Normals = normales, UVs = uvs, Indices = triangles};
		}

		public static Primitive Sphere(float radius = 1.0F, int nbLong = 24, int nbLat = 16) {
			#region Vertices

			var vertices = new Vector3[(nbLong + 1) * nbLat + 2];
			float _pi = Mathf.PI;
			float _2pi = _pi * 2f;

			vertices[0] = Vector3.Up * radius;
			for (int lat = 0; lat < nbLat; lat++) {
				float a1 = _pi * (lat + 1) / (nbLat + 1);
				float sin1 = Mathf.Sin(a1);
				float cos1 = Mathf.Cos(a1);

				for (int lon = 0; lon <= nbLong; lon++) {
					float a2 = _2pi * (lon == nbLong ? 0 : lon) / nbLong;
					float sin2 = Mathf.Sin(a2);
					float cos2 = Mathf.Cos(a2);

					vertices[lon + lat * (nbLong + 1) + 1] = new Vector3(sin1 * cos2, cos1, sin1 * sin2) * radius;
				}
			}

			vertices[vertices.Length - 1] = Vector3.Up * -radius;

			#endregion

			#region Normales		

			var normales = new Vector3[vertices.Length];
			for (int n = 0; n < vertices.Length; n++)
				normales[n] = Vector3.Normalize(vertices[n]);

			#endregion

			#region UVs

			var uvs = new Vector2[vertices.Length];
			uvs[0] = Vector2.UnitY;
			uvs[uvs.Length - 1] = Vector2.Zero;
			for (int lat = 0; lat < nbLat; lat++)
			for (int lon = 0; lon <= nbLong; lon++)
				uvs[lon + lat * (nbLong + 1) + 1] =
					new Vector2((float) lon / nbLong, 1f - (float) (lat + 1) / (nbLat + 1));

			#endregion

			#region Triangles

			int nbFaces = vertices.Length;
			int nbTriangles = nbFaces * 2;
			int nbIndexes = nbTriangles * 3;
			var triangles = new int[nbIndexes];

			//Top Cap
			int i = 0;
			for (int lon = 0; lon < nbLong; lon++) {
				triangles[i++] = lon + 2;
				triangles[i++] = lon + 1;
				triangles[i++] = 0;
			}

			//Middle
			for (int lat = 0; lat < nbLat - 1; lat++) {
				for (int lon = 0; lon < nbLong; lon++) {
					int current = lon + lat * (nbLong + 1) + 1;
					int next = current + nbLong + 1;

					triangles[i++] = current;
					triangles[i++] = current + 1;
					triangles[i++] = next + 1;

					triangles[i++] = current;
					triangles[i++] = next + 1;
					triangles[i++] = next;
				}
			}

			//Bottom Cap
			for (int lon = 0; lon < nbLong; lon++) {
				triangles[i++] = vertices.Length - 1;
				triangles[i++] = vertices.Length - (lon + 2) - 1;
				triangles[i++] = vertices.Length - (lon + 1) - 1;
			}

			#endregion

			return new Primitive {Positions = vertices, Normals = normales, UVs = uvs, Indices = triangles};
		}

		public static Primitive IcoSphere(float radius = 1.0F, int recursionLevel = 3) {
			return IcoSphereGenerator.Create(radius, recursionLevel);
		}

		private static class IcoSphereGenerator {
			private struct TriangleIndices {
				public int v1;
				public int v2;
				public int v3;

				public TriangleIndices(int v1, int v2, int v3) {
					this.v1 = v1;
					this.v2 = v2;
					this.v3 = v3;
				}
			}

			// return index of point in the middle of p1 and p2
			private static int getMiddlePoint(int p1, int p2, ref List<Vector3> vertices,
				ref Dictionary<long, int> cache, float radius) {
				// first check if we have it already
				bool firstIsSmaller = p1 < p2;
				long smallerIndex = firstIsSmaller ? p1 : p2;
				long greaterIndex = firstIsSmaller ? p2 : p1;
				long key = (smallerIndex << 32) + greaterIndex;

				int ret;
				if (cache.TryGetValue(key, out ret)) {
					return ret;
				}

				// not in cache, calculate it
				var point1 = vertices[p1];
				var point2 = vertices[p2];
				var middle = new Vector3
				(
					(point1.X + point2.X) / 2f,
					(point1.Y + point2.Y) / 2f,
					(point1.Z + point2.Z) / 2f
				);

				// add vertex makes sure point is on unit sphere
				int i = vertices.Count;
				vertices.Add(Vector3.Normalize(middle) * radius);

				// store it, return index
				cache.Add(key, i);

				return i;
			}

			public static Primitive Create(float radius = 1.0F, int recursionLevel = 3) {
				var vertList = new List<Vector3>();
				var middlePointIndexCache = new Dictionary<long, int>();

				// create 12 vertices of a icosahedron
				float t = (1f + Mathf.Sqrt(5f)) / 2f;

				vertList.Add(Vector3.Normalize(new Vector3(-1f, t, 0f)) * radius);
				vertList.Add(Vector3.Normalize(new Vector3(1f, t, 0f)) * radius);
				vertList.Add(Vector3.Normalize(new Vector3(-1f, -t, 0f)) * radius);
				vertList.Add(Vector3.Normalize(new Vector3(1f, -t, 0f)) * radius);

				vertList.Add(Vector3.Normalize(new Vector3(0f, -1f, t)) * radius);
				vertList.Add(Vector3.Normalize(new Vector3(0f, 1f, t)) * radius);
				vertList.Add(Vector3.Normalize(new Vector3(0f, -1f, -t)) * radius);
				vertList.Add(Vector3.Normalize(new Vector3(0f, 1f, -t)) * radius);

				vertList.Add(Vector3.Normalize(new Vector3(t, 0f, -1f)) * radius);
				vertList.Add(Vector3.Normalize(new Vector3(t, 0f, 1f)) * radius);
				vertList.Add(Vector3.Normalize(new Vector3(-t, 0f, -1f)) * radius);
				vertList.Add(Vector3.Normalize(new Vector3(-t, 0f, 1f)) * radius);


				// create 20 triangles of the icosahedron
				var faces = new List<TriangleIndices>();

				// 5 faces around point 0
				faces.Add(new TriangleIndices(0, 11, 5));
				faces.Add(new TriangleIndices(0, 5, 1));
				faces.Add(new TriangleIndices(0, 1, 7));
				faces.Add(new TriangleIndices(0, 7, 10));
				faces.Add(new TriangleIndices(0, 10, 11));

				// 5 adjacent faces 
				faces.Add(new TriangleIndices(1, 5, 9));
				faces.Add(new TriangleIndices(5, 11, 4));
				faces.Add(new TriangleIndices(11, 10, 2));
				faces.Add(new TriangleIndices(10, 7, 6));
				faces.Add(new TriangleIndices(7, 1, 8));

				// 5 faces around point 3
				faces.Add(new TriangleIndices(3, 9, 4));
				faces.Add(new TriangleIndices(3, 4, 2));
				faces.Add(new TriangleIndices(3, 2, 6));
				faces.Add(new TriangleIndices(3, 6, 8));
				faces.Add(new TriangleIndices(3, 8, 9));

				// 5 adjacent faces 
				faces.Add(new TriangleIndices(4, 9, 5));
				faces.Add(new TriangleIndices(2, 4, 11));
				faces.Add(new TriangleIndices(6, 2, 10));
				faces.Add(new TriangleIndices(8, 6, 7));
				faces.Add(new TriangleIndices(9, 8, 1));


				// refine triangles
				for (int i = 0; i < recursionLevel; i++) {
					var faces2 = new List<TriangleIndices>();
					foreach (var tri in faces) {
						// replace triangle by 4 triangles
						int a = getMiddlePoint(tri.v1, tri.v2, ref vertList, ref middlePointIndexCache, radius);
						int b = getMiddlePoint(tri.v2, tri.v3, ref vertList, ref middlePointIndexCache, radius);
						int c = getMiddlePoint(tri.v3, tri.v1, ref vertList, ref middlePointIndexCache, radius);

						faces2.Add(new TriangleIndices(tri.v1, a, c));
						faces2.Add(new TriangleIndices(tri.v2, b, a));
						faces2.Add(new TriangleIndices(tri.v3, c, b));
						faces2.Add(new TriangleIndices(a, b, c));
					}

					faces = faces2;
				}


				var result = new Primitive {
					Positions = vertList.ToArray()
				};

				var triList = new List<int>();
				for (int i = 0; i < faces.Count; i++) {
					triList.Add(faces[i].v1);
					triList.Add(faces[i].v2);
					triList.Add(faces[i].v3);
				}

				result.Indices = triList.ToArray();
				result.UVs = new Vector2[result.Positions.Length];

				var normales = new Vector3[vertList.Count];
				for (int i = 0; i < normales.Length; i++)
					normales[i] = Vector3.Normalize(vertList[i]);


				result.Normals = normales;

				return result;
			}
		}
	}
}