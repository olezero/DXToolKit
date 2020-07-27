using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace DXToolKit {
	/// <summary>
	/// Tool for rendering lines in 3D space
	/// </summary>
	public class LineRenderer : DeviceComponent {
		/// <summary>
		/// Vertex structure used by the GPU
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		private struct Vertex {
			/// <summary>
			/// Position of the vertex
			/// </summary>
			public Vector3 Position;

			/// <summary>
			/// Color of the vertex
			/// </summary>
			public Vector4 Color;

			/// <summary>
			/// Creates a new vertex with input position and color
			/// </summary>
			public Vertex(ref Vector3 pos, ref Color clr) {
				// Store position
				Position = pos;

				// Quick conversion from byte to float
				Color.X = clr.R / 255.0F;
				Color.Y = clr.G / 255.0F;
				Color.Z = clr.B / 255.0F;
				Color.W = clr.A / 255.0F;
			}

			/// <summary>
			/// Creates a new vertex with input position and color
			/// </summary>
			// ReSharper disable once UnusedMember.Local
			public Vertex(ref Vector3 pos, ref Vector4 clr) {
				Position = pos;
				Color = clr;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct MatrixBufferType {
			public Matrix WorldMatrix;
			public Matrix ViewMatrix;
			public Matrix ProjectionMatrix;
		}

		/// <summary>
		/// Saved identity matrix for faster usage. Maybe.
		/// </summary>
		private readonly Matrix IDENTITY_MATRIX = Matrix.Identity;

		/// <summary>
		/// List of vertices to render in a given frame
		/// </summary>
		private List<Vertex> m_vertices;

		/// <summary>
		/// Vertex buffer used for rendering
		/// </summary>
		private VertexBuffer<Vertex> m_vertexBuffer;

		/// <summary>
		/// World view projection buffer for the shader
		/// </summary>
		private ConstantBuffer<MatrixBufferType> m_matrixBuffer;

		/// <summary>
		/// Vertex shader input layout
		/// </summary>
		private InputLayout m_inputLayout;

		/// <summary>
		/// Vertex shader used to render lines
		/// </summary>
		private VertexShader m_vertexShader;

		/// <summary>
		/// Pixel shader used to render lines
		/// </summary>
		private PixelShader m_pixelShader;

		/// <summary>
		/// Holder for the world, view and projection matrices
		/// </summary>
		private MatrixBufferType m_matrices;

		/// <summary>
		/// Gets or sets the resolution of objects like Circles, Spheres and Capsules
		/// </summary>
		public int Resolution = 64;

		/// <summary>
		/// Creates a new instance of the line renderer
		/// </summary>
		/// <param name="device">Base device used for rendering</param>
		public LineRenderer(GraphicsDevice device) : base(device) {
			// Setup per frame vertex buffer
			m_vertices = new List<Vertex>();
			// Setup GPU vertex buffer
			m_vertexBuffer = new VertexBuffer<Vertex>(m_device, 1);
			// Setup world view projection buffer
			m_matrixBuffer = new ConstantBuffer<MatrixBufferType>(m_device);

			// Compile vertex and pixel shader
			var vsByteCode = ShaderBytecode.Compile(ShaderSource, "VS", "vs_5_0");
			var psByteCode = ShaderBytecode.Compile(ShaderSource, "PS", "ps_5_0");

			// Create shaders
			m_vertexShader = new VertexShader(m_device, vsByteCode);
			m_pixelShader = new PixelShader(m_device, psByteCode);

			// Create input layout
			m_inputLayout = new InputLayout(m_device, vsByteCode, new[] {
				new InputElement("POSITION", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0),
				new InputElement("COLOR", 0, Format.R32G32B32A32_Float, InputElement.AppendAligned, 0),
			});

			// Create empty matrix buffer
			m_matrices = new MatrixBufferType();
		}

		/// <summary>
		/// Disposes of the line renderer
		/// </summary>
		protected override void OnDispose() {
			Utilities.Dispose(ref m_vertexBuffer);
			Utilities.Dispose(ref m_matrixBuffer);
			Utilities.Dispose(ref m_inputLayout);
			Utilities.Dispose(ref m_vertexShader);
			Utilities.Dispose(ref m_pixelShader);

			m_vertices.Clear();
			m_vertices = null;
		}

		/// <summary>
		/// Renders the line renderer buffer
		/// </summary>
		/// <param name="camera">Camera used for view and projection</param>
		/// <param name="world">Matrix to transform all lines</param>
		public void Render(DXCamera camera, Matrix? world = null) {
			// Make sure there is vertices to draw
			if (m_vertices.Count > 0) {
				// Update constant buffer
				m_matrices.WorldMatrix = Matrix.Transpose(world ?? Matrix.Identity);
				m_matrices.ViewMatrix = Matrix.Transpose(camera.ViewMatrix);
				m_matrices.ProjectionMatrix = Matrix.Transpose(camera.ProjectionMatrix);
				m_matrixBuffer.Write(m_matrices);

				// Copy to buffer
				m_vertexBuffer.WriteRange(m_vertices.ToArray());

				// Set topology to line list
				m_context.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;

				// Set input layout and vertex buffer
				m_context.InputAssembler.InputLayout = m_inputLayout;
				m_context.InputAssembler.SetVertexBuffers(0, m_vertexBuffer);

				// Set vertex and pixel shader
				m_context.VertexShader.Set(m_vertexShader);
				m_context.VertexShader.SetConstantBuffer(0, m_matrixBuffer);
				m_context.PixelShader.Set(m_pixelShader);

				// Draw lines
				m_context.Draw(m_vertices.Count, 0);

				// Clear vertices
				m_vertices.Clear();
			}
		}

		/// <summary>
		/// Adds a line to the rendering queue
		/// </summary>
		/// <param name="start">Start of the line</param>
		/// <param name="end">End of the line</param>
		/// <param name="color">Color of the line</param>
		public void Line(Vector3 start, Vector3 end, Color color) {
			Line(ref start, ref end, ref color);
		}

		/// <summary>
		/// Adds a line to the rendering queue
		/// </summary>
		/// <param name="start">Start of the line</param>
		/// <param name="end">End of the line</param>
		/// <param name="color">Color of the line</param>
		public void Line(ref Vector3 start, ref Vector3 end, ref Color color) {
			m_vertices.Add(new Vertex(ref start, ref color));
			m_vertices.Add(new Vertex(ref end, ref color));
		}

		/// <summary>
		/// Adds a line to the rendering queue
		/// </summary>
		/// <param name="start">Start of the line</param>
		/// <param name="end">End of the line</param>
		/// <param name="color">Color of the line</param>
		/// <param name="transform">Transformation of the line</param>
		public void Line(ref Vector3 start, ref Vector3 end, ref Color color, ref Matrix transform) {
			if (transform.IsIdentity == false) {
				start = Vector3.TransformCoordinate(start, transform);
				end = Vector3.TransformCoordinate(end, transform);
			}

			m_vertices.Add(new Vertex(ref start, ref color));
			m_vertices.Add(new Vertex(ref end, ref color));
		}

		/// <summary>
		/// Adds a ray to the rendering queue
		/// </summary>
		/// <param name="ray">Ray to render</param>
		/// <param name="length">Length of the ray to render</param>
		/// <param name="color">Color of the ray</param>
		/// <param name="transform">Transformation of the ray</param>
		public void Ray(Ray ray, float length, Color color, Matrix? transform = null) {
			var tr = IDENTITY_MATRIX;
			if (transform != null) {
				tr = (Matrix) transform;
			}

			var v1 = ray.Position;
			var v2 = ray.Position + (ray.Direction * length);
			Line(ref v1, ref v2, ref color, ref tr);
		}

		/// <summary>
		/// Adds a box to the rendering queue
		/// </summary>
		/// <param name="min">Minimum extents of the box</param>
		/// <param name="max">Maximum extents of the box</param>
		/// <param name="color">Color of the box</param>
		/// <param name="transform">Transform of the box</param>
		public void Box(Vector3 min, Vector3 max, Color color, Matrix? transform = null) {
			Box(ref min, ref max, ref color, ref transform);
		}

		/// <summary>
		/// Adds a box to the rendering queue
		/// </summary>
		/// <param name="min">Minimum extents of the box</param>
		/// <param name="max">Maximum extents of the box</param>
		/// <param name="color">Color of the box</param>
		/// <param name="transform">Transform of the box</param>
		public void Box(ref Vector3 min, ref Vector3 max, ref Color color, ref Matrix? transform) {
			var w = max.X - min.X;
			var h = max.Y - min.Y;
			var d = max.Z - min.Z;

			var front_TL = new Vector3(min.X, min.Y + h, min.Z);
			var front_TR = new Vector3(min.X + w, min.Y + h, min.Z);
			var front_BL = new Vector3(min.X, min.Y, min.Z);
			var front_BR = new Vector3(min.X + w, min.Y, min.Z);

			var back_TL = new Vector3(min.X, min.Y + h, min.Z + d);
			var back_TR = new Vector3(min.X + w, min.Y + h, min.Z + d);
			var back_BL = new Vector3(min.X, min.Y, min.Z + d);
			var back_BR = new Vector3(min.X + w, min.Y, min.Z + d);

			if (transform != null) {
				var tr = (Matrix) transform;
				Vector3.TransformCoordinate(ref front_TL, ref tr, out front_TL);
				Vector3.TransformCoordinate(ref front_TR, ref tr, out front_TR);
				Vector3.TransformCoordinate(ref front_BL, ref tr, out front_BL);
				Vector3.TransformCoordinate(ref front_BR, ref tr, out front_BR);

				Vector3.TransformCoordinate(ref back_TL, ref tr, out back_TL);
				Vector3.TransformCoordinate(ref back_TR, ref tr, out back_TR);
				Vector3.TransformCoordinate(ref back_BL, ref tr, out back_BL);
				Vector3.TransformCoordinate(ref back_BR, ref tr, out back_BR);
			}

			Line(ref front_TL, ref front_TR, ref color);
			Line(ref front_TR, ref front_BR, ref color);
			Line(ref front_BR, ref front_BL, ref color);
			Line(ref front_BL, ref front_TL, ref color);

			Line(ref back_TL, ref back_TR, ref color);
			Line(ref back_TR, ref back_BR, ref color);
			Line(ref back_BR, ref back_BL, ref color);
			Line(ref back_BL, ref back_TL, ref color);

			Line(ref back_TL, ref front_TL, ref color);
			Line(ref back_TR, ref front_TR, ref color);
			Line(ref back_BL, ref front_BL, ref color);
			Line(ref back_BR, ref front_BR, ref color);
		}


		/// <summary>
		/// Adds a frustum to the rendering queue
		/// </summary>
		/// <param name="frustum">The frustum to add</param>
		/// <param name="color">Color of the frustum</param>
		public void Frustum(BoundingFrustum frustum, Color color) {
			var corners = frustum.GetCorners();

			// Front rectangle
			Line(ref corners[0], ref corners[1], ref color);
			Line(ref corners[1], ref corners[2], ref color);
			Line(ref corners[2], ref corners[3], ref color);
			Line(ref corners[3], ref corners[0], ref color);

			// Back rectangle
			Line(ref corners[4], ref corners[5], ref color);
			Line(ref corners[5], ref corners[6], ref color);
			Line(ref corners[6], ref corners[7], ref color);
			Line(ref corners[7], ref corners[4], ref color);

			// Connecting lines
			Line(ref corners[0], ref corners[4], ref color);
			Line(ref corners[1], ref corners[5], ref color);
			Line(ref corners[2], ref corners[6], ref color);
			Line(ref corners[3], ref corners[7], ref color);
		}

		/// <summary>
		/// Adds a frustum to the rendering queue
		/// </summary>
		/// <param name="frustum">The frustum to add</param>
		/// <param name="color">Color of the frustum</param>
		public void Frustum(ref BoundingFrustum frustum, ref Color color) {
			var corners = frustum.GetCorners();

			// Front rectangle
			Line(ref corners[0], ref corners[1], ref color);
			Line(ref corners[1], ref corners[2], ref color);
			Line(ref corners[2], ref corners[3], ref color);
			Line(ref corners[3], ref corners[0], ref color);

			// Back rectangle
			Line(ref corners[4], ref corners[5], ref color);
			Line(ref corners[5], ref corners[6], ref color);
			Line(ref corners[6], ref corners[7], ref color);
			Line(ref corners[7], ref corners[4], ref color);

			// Connecting lines
			Line(ref corners[0], ref corners[4], ref color);
			Line(ref corners[1], ref corners[5], ref color);
			Line(ref corners[2], ref corners[6], ref color);
			Line(ref corners[3], ref corners[7], ref color);
		}

		/// <summary>
		/// Adds a bounding box to the rendering queue
		/// </summary>
		/// <param name="box">Bounding box to add</param>
		/// <param name="color">Color of the box</param>
		/// <param name="transform">Transform of the box</param>
		public void BoundingBox(BoundingBox box, Color color, Matrix? transform = null) {
			Box(ref box.Minimum, ref box.Maximum, ref color, ref transform);
		}

		/// <summary>
		/// Adds a bounding box to the rendering queue
		/// </summary>
		/// <param name="box">Bounding box to add</param>
		/// <param name="color">Color of the box</param>
		/// <param name="transform">Transform of the box</param>
		public void BoundingBox(ref BoundingBox box, ref Color color, ref Matrix? transform) {
			Box(ref box.Minimum, ref box.Maximum, ref color, ref transform);
		}

		/// <summary>
		/// Adds a oriented bounding box to the rendering queue
		/// </summary>
		/// <param name="box">Bounding box to add</param>
		/// <param name="color">Color of the box</param>
		/// <param name="transform">Transform to apply after the oriented box's transform</param>
		public void OrientedBoundingBox(OrientedBoundingBox box, Color color, Matrix? transform = null) {
			var tr = box.Transformation;
			if (transform != null) {
				tr *= (Matrix) transform;
			}

			var extents = box.Extents;
			var min = -extents + box.Center;
			var max = extents + box.Center;

			Box(min, max, color, tr);
		}

		/// <summary>
		/// Adds a bounding sphere to the rendering queue
		/// </summary>
		/// <param name="sphere">The sphere to render</param>
		/// <param name="color">Color of the sphere</param>
		/// <param name="transform">Transform of the sphere</param>
		public void BoundingSphere(BoundingSphere sphere, Color color, Matrix? transform = null) {
			Sphere(sphere.Center, sphere.Radius, color, transform);
		}

		/// <summary>
		/// Adds a sphere to the rendering queue
		/// </summary>
		/// <param name="center">The center of the sphere</param>
		/// <param name="radius">The radius of the sphere</param>
		/// <param name="color">Color of the sphere</param>
		/// <param name="transform">Transform of the sphere</param>
		public void Sphere(Vector3 center, float radius, Color color, Matrix? transform = null) {
			Circle(center, radius, Vector3.Up, color, transform);
			Circle(center, radius, Vector3.Left, color, transform);
			Circle(center, radius, Vector3.ForwardLH, color, transform);
		}

		/// <summary>
		/// Adds a circle to the rendering queue
		/// </summary>
		/// <param name="center">Center of the circle</param>
		/// <param name="radius">Radius of the circle</param>
		/// <param name="normal">Facing direction of the circle</param>
		/// <param name="color">Color of the circle</param>
		/// <param name="transform">Transform of the circle</param>
		public void Circle(Vector3 center, float radius, Vector3 normal, Color color, Matrix? transform) {
			var tr = IDENTITY_MATRIX;
			if (transform != null) {
				tr = (Matrix) transform;
			}

			// Normalize normal
			var n1 = Vector3.Normalize(normal);
			// Generate a normal facing another way, to get some perpendicular vectors
			var temp = new Vector3(n1.Y, n1.Z, n1.X);
			// Get both perpendicular lines to the input normal.
			var p1 = Vector3.Normalize(Vector3.Cross(n1, temp));
			var p2 = Vector3.Normalize(Vector3.Cross(n1, p1));

			// Get sides
			int sides = Resolution;

			// Standard 3d circle calculation
			for (int i = 0; i < sides; i++) {
				var a1 = (Mathf.Pi * 2 / sides) * (i + 0);
				var a2 = (Mathf.Pi * 2 / sides) * (i + 1);

				var a1x = Mathf.Cos(a1) * p1.X + Mathf.Sin(a1) * p2.X;
				var a1y = Mathf.Cos(a1) * p1.Y + Mathf.Sin(a1) * p2.Y;
				var a1z = Mathf.Cos(a1) * p1.Z + Mathf.Sin(a1) * p2.Z;

				var a2x = Mathf.Cos(a2) * p1.X + Mathf.Sin(a2) * p2.X;
				var a2y = Mathf.Cos(a2) * p1.Y + Mathf.Sin(a2) * p2.Y;
				var a2z = Mathf.Cos(a2) * p1.Z + Mathf.Sin(a2) * p2.Z;

				var v1 = new Vector3(a1x, a1y, a1z) * radius + center;
				var v2 = new Vector3(a2x, a2y, a2z) * radius + center;

				Line(ref v1, ref v2, ref color, ref tr);
			}
		}

		/// <summary>
		/// Renders a sphere filled with quads
		/// </summary>
		/// <param name="center">The center of the sphere</param>
		/// <param name="radius">The radius of the sphere</param>
		/// <param name="color">Color of the sphere</param>
		/// <param name="transform">Transform of the sphere</param>
		public void QuadSphere(Vector3 center, float radius, Color color, Matrix? transform = null) {
			int len = Resolution;
			int lon = Resolution;

			var tr = IDENTITY_MATRIX;
			if (transform != null) {
				tr = (Matrix) transform;
			}

			// Vertical Lines
			for (int i = 0; i < len; i++) {
				for (int j = 0; j < lon; j++) {
					var points = new Vector3[2];
					for (int k = 0; k < 2; k++) {
						var theta = i / (float) len * Mathf.Pi * 2;
						var arpha = (j + k) / (float) lon * Mathf.Pi;

						var x = radius * Mathf.Cos(theta) * Mathf.Sin(arpha);
						var y = radius * Mathf.Sin(theta) * Mathf.Sin(arpha);
						var z = radius * Mathf.Cos(arpha);

						points[k] = new Vector3(x, z, y) + center;
					}


					Line(ref points[0], ref points[1], ref color, ref tr);
				}
			}

			// Horizontal Lines
			for (int i = 0; i < len; i++) {
				for (int j = 0; j < lon; j++) {
					var points = new Vector3[2];
					for (int k = 0; k < 2; k++) {
						var theta = i / (float) len * Mathf.Pi;
						var arpha = (j + k) / (float) lon * Mathf.Pi * 2;

						var x = radius * Mathf.Cos(arpha) * Mathf.Sin(theta);
						var y = radius * Mathf.Sin(arpha) * Mathf.Sin(theta);
						var z = radius * Mathf.Cos(theta);

						points[k] = new Vector3(x, z, y) + center;
					}

					Line(ref points[0], ref points[1], ref color, ref tr);
				}
			}
		}

		/// <summary>
		/// Adds a capsule to the rendering queue
		/// </summary>
		/// <param name="center">Center of the capsule</param>
		/// <param name="radius">Radius of the capsule</param>
		/// <param name="innerHeight">The height of the inner cylinder of the capsule</param>
		/// <param name="color">Color of the capsule</param>
		/// <param name="transform">Transform of the capsule</param>
		/// <param name="heightLines">If renderer should add height lines on the inner cylinder</param>
		public void Capsule(Vector3 center, float radius, float innerHeight, Color color, Matrix? transform = null, bool heightLines = false) {
			int len = Resolution;
			int lon = Resolution;
			var tr = IDENTITY_MATRIX;
			if (transform != null) {
				tr = (Matrix) transform;
			}

			var topCenter = center + new Vector3(0, innerHeight / 2.0F, 0);
			var bottomCenter = center + new Vector3(0, -innerHeight / 2.0F, 0);

			// Vertical Lines
			for (int i = 0; i < len; i++) {
				for (int j = 0; j < lon; j++) {
					var points = new Vector3[2];
					for (int k = 0; k < 2; k++) {
						var theta = i / (float) len * Mathf.Pi * 2;
						var arpha = (j + k) / (float) lon * Mathf.Pi;
						var x = radius * Mathf.Cos(theta) * Mathf.Sin(arpha);
						var y = radius * Mathf.Sin(theta) * Mathf.Sin(arpha);
						var z = radius * Mathf.Cos(arpha);
						points[k] = new Vector3(x, z, y);
					}


					// Top half
					if (j < lon / 2) {
						points[0] += topCenter;
						points[1] += topCenter;
						Line(ref points[0], ref points[1], ref color, ref tr);
					}

					// Bottom half
					if (j >= lon / 2) {
						points[0] += bottomCenter;
						points[1] += bottomCenter;
						Line(ref points[0], ref points[1], ref color, ref tr);
					}
				}
			}

			// Horizontal Lines
			for (int i = 0; i < len; i++) {
				for (int j = 0; j < lon; j++) {
					var points = new Vector3[2];
					for (int k = 0; k < 2; k++) {
						var theta = i / (float) len * Mathf.Pi;
						var arpha = (j + k) / (float) lon * Mathf.Pi * 2;
						var x = radius * Mathf.Cos(arpha) * Mathf.Sin(theta);
						var y = radius * Mathf.Sin(arpha) * Mathf.Sin(theta);
						var z = radius * Mathf.Cos(theta);
						points[k] = new Vector3(x, z, y);
					}

					// Top half
					if (i < len / 2) {
						points[0] += topCenter;
						points[1] += topCenter;
						Line(ref points[0], ref points[1], ref color, ref tr);
					}

					// Bottom half
					if (i > len / 2) {
						points[0] += bottomCenter;
						points[1] += bottomCenter;
						Line(ref points[0], ref points[1], ref color, ref tr);
					}
				}
			}

			// Cylinder
			var heightStart = new Vector3[len];
			var heightEnd = new Vector3[len];
			if (heightLines == false) {
				lon = 2;
			}

			// Height segments
			for (int j = 0; j < lon; j++) {
				var y = j * (innerHeight / (lon - 1)) - (innerHeight / 2.0F);
				// Circle segments
				for (int i = 0; i < len; i++) {
					var points = new Vector3[2];
					for (int k = 0; k < 2; k++) {
						var theta = (i + k) / (float) len * Mathf.Pi * 2;
						var x = radius * Mathf.Cos(theta);
						var z = radius * Mathf.Sin(theta);
						// var y = 0;
						points[k] = new Vector3(x, y, z) + center;
					}

					if (j == 0) {
						heightStart[i] = points[0];
					}

					if (j == lon - 1) {
						heightEnd[i] = points[0];
					}

					Line(ref points[0], ref points[1], ref color, ref tr);
				}
			}

			for (int i = 0; i < len; i++) {
				var v1 = heightStart[i];
				var v2 = heightEnd[i];
				Line(ref v1, ref v2, ref color, ref tr);
			}
		}

		/// <summary>
		/// Adds a plane to the rendering queue
		/// </summary>
		/// <param name="center">Center of the plane</param>
		/// <param name="normal">Facing direction of the plane</param>
		/// <param name="size">Size of the plane in both width and height</param>
		/// <param name="color">Color of the plane</param>
		/// <param name="transform">Transform of the plane</param>
		public void Plane(Vector3 center, Vector3 normal, float size, Color color, Matrix? transform = null) {
			var tr = IDENTITY_MATRIX;
			if (transform != null) {
				tr = (Matrix) transform;
			}

			var n1 = Vector3.Normalize(normal);
			var temp = new Vector3(n1.Y, n1.Z, n1.X);
			var p1 = Vector3.Normalize(Vector3.Cross(n1, temp)) * size * 0.5F;
			var p2 = Vector3.Normalize(Vector3.Cross(n1, p1)) * size * 0.5F;

			var v1 = p1 + p2 + center;
			var v2 = p1 - p2 + center;
			Line(ref v1, ref v2, ref color, ref tr);

			v1 = p1 + p2 + center;
			v2 = -p1 + p2 + center;
			Line(ref v1, ref v2, ref color, ref tr);

			v1 = -p1 + p2 + center;
			v2 = -p1 - p2 + center;
			Line(ref v1, ref v2, ref color, ref tr);

			v1 = -p1 - p2 + center;
			v2 = p1 - p2 + center;
			Line(ref v1, ref v2, ref color, ref tr);
		}

		/// <summary>
		/// Adds an arrow to the rendering queue
		/// </summary>
		/// <param name="origin">Starting point of the arrow</param>
		/// <param name="direction">Direction of the arrow</param>
		/// <param name="size">Size of the arrow</param>
		/// <param name="color">Color of the arrow</param>
		/// <param name="transform">Transform or the arrow</param>
		public void Arrow(Vector3 origin, Vector3 direction, float size, Color color, Matrix? transform = null) {
			Arrow(ref origin, ref direction, ref size, ref color, ref transform);
		}

		/// <summary>
		/// Adds an arrow to the rendering queue
		/// </summary>
		/// <param name="origin">Starting point of the arrow</param>
		/// <param name="direction">Direction of the arrow</param>
		/// <param name="size">Size of the arrow</param>
		/// <param name="color">Color of the arrow</param>
		/// <param name="transform">Transform or the arrow</param>
		public void Arrow(ref Vector3 origin, ref Vector3 direction, ref float size, ref Color color, ref Matrix? transform) {
			const float HEAD = 0.1F;

			var n1 = Vector3.Normalize(direction);
			// Not good at extremes
			// var temp = new Vector3(n1.Y, n1.Z, n1.X);

			// Try to cross against up
			var temp = Vector3.Up;
			if (n1 == Vector3.Up || n1 == Vector3.Down) {
				temp = Vector3.Left;
			} else {
				// Flip "up" normal if its facing is less then 90 degrees of
				if (Vector3.Dot(n1, temp) < 0F) {
					temp = Vector3.Down;
				}
			}


			var p1 = Vector3.Normalize(Vector3.Cross(n1, temp)) * HEAD * size;
			var p2 = Vector3.Normalize(Vector3.Cross(n1, p1)) * HEAD * size;

			var start = origin;
			var end = origin + direction * size;

			var a1 = end + p1 - direction * HEAD * 2 * size;
			var a2 = end - p1 - direction * HEAD * 2 * size;
			var a3 = end + p2 - direction * HEAD * 2 * size;
			var a4 = end - p2 - direction * HEAD * 2 * size;

			if (transform != null) {
				var tr = (Matrix) transform;
				Vector3.TransformCoordinate(ref start, ref tr, out start);
				Vector3.TransformCoordinate(ref end, ref tr, out end);

				Vector3.TransformCoordinate(ref a1, ref tr, out a1);
				Vector3.TransformCoordinate(ref a2, ref tr, out a2);
				Vector3.TransformCoordinate(ref a3, ref tr, out a3);
				Vector3.TransformCoordinate(ref a4, ref tr, out a4);
			}

			Line(ref start, ref end, ref color);
			Line(ref end, ref a1, ref color);
			Line(ref end, ref a2, ref color);
			Line(ref end, ref a3, ref color);
			Line(ref end, ref a4, ref color);
		}

		/// <summary>
		/// Adds a transformation matrix to the rendering queue
		/// </summary>
		/// <param name="transformMatrix">The matrix to render</param>
		/// <param name="color">Color of the matrix, if null default colors of Red for X, Green for Y and Blue for Z is used</param>
		public void Transform(Matrix transformMatrix, Color? color = null) {
			Color r, g, b;
			if (color == null) {
				r = Color.Red;
				g = Color.Green;
				b = Color.Blue;
			} else {
				r = (Color) color;
				g = (Color) color;
				b = (Color) color;
			}

			// Unpack scaling and reset matrix scaling to 1
			var scaling = transformMatrix.ScaleVector;
			transformMatrix.ScaleVector = new Vector3(1, 1, 1);

			Arrow(Vector3.Zero, Vector3.Right, scaling.X, r, transformMatrix);
			Arrow(Vector3.Zero, Vector3.Up, scaling.Y, g, transformMatrix);
			Arrow(Vector3.Zero, Vector3.ForwardLH, scaling.Z, b, transformMatrix);
		}

		/// <summary>
		/// Simple rendering of lines
		/// </summary>
		private const string ShaderSource = @"
cbuffer MatrixBuffer {
	matrix worldMatrix;
	matrix viewMatrix;
	matrix projectionMatrix;
};
struct VSInn {
	float3 pos : POSITION;
	float4 clr : COLOR;
};
struct PSInn {
	float4 pos : SV_POSITION;
	float4 clr : COLOR;
};
PSInn VS(VSInn input) {
	PSInn output;
	output.pos = mul(float4(input.pos, 1.0F), worldMatrix);
	output.pos = mul(output.pos, viewMatrix);
	output.pos = mul(output.pos, projectionMatrix);

	output.clr = input.clr;
	return output;
};
float4 PS(PSInn input) : SV_Target {
	return input.clr * input.clr.a;
};
";
	}
}