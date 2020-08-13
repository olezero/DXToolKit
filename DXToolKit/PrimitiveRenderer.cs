using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System.Runtime.InteropServices;
using System.Collections.Generic;

#pragma warning disable 649

namespace DXToolKit {
	public class PrimitiveRenderer : DeviceComponent {
		/// <summary>
		/// Primitive batch used to gather up multiple instances of a primitive mesh and render it.
		/// </summary>
		private class PrimitiveBatch : DeviceComponent {
			/// <summary>
			/// Structure for each vertex in a primitive
			/// </summary>
			[StructLayout(LayoutKind.Sequential)]
			private struct VertexBufferType {
				public Vector3 Position;
				public Vector3 Normal;
				public Vector2 UV;
			}

			/// <summary>
			/// Structure for each instance in the render batch
			/// </summary>
			[StructLayout(LayoutKind.Sequential)]
			private struct InstanceBufferType {
				public Matrix WorldMatrix;
				public Vector4 Color;
			}

			/// <summary>
			/// Vertex buffer containing the vertices of a primitive
			/// </summary>
			private VertexBuffer<VertexBufferType> m_vertexBuffer;

			/// <summary>
			/// Index buffer containing the indices of the primitive
			/// </summary>
			private IndexBuffer m_indexBuffer;

			/// <summary>
			/// Instance buffer containing the data of each instance to render
			/// </summary>
			private VertexBuffer<InstanceBufferType> m_instanceBuffer;

			/// <summary>
			/// Dynamic list containing every instance to render
			/// </summary>
			private List<InstanceBufferType> m_instanceList;

			/// <summary>
			/// Controller for if the instance list has changed
			/// </summary>
			private bool m_hasInstancesChanged = false;

			/// <summary>
			/// Creates a new primitive batch used for organizing and rendering a primitive.
			/// </summary>
			/// <param name="device">Base device used for rendering</param>
			/// <param name="primitive">Primitive data used by the batch</param>
			public PrimitiveBatch(GraphicsDevice device, Primitive primitive) : base(device) {
				// Copy vertices from the input primitive
				var vertices = new VertexBufferType[primitive.Positions.Length];
				for (int i = 0; i < vertices.Length; i++) {
					vertices[i].Position = primitive.Positions[i];
					vertices[i].Normal = primitive.Normals[i];
					vertices[i].UV = primitive.UVs[i];
				}

				// Setup vertex and index buffer for the primitive
				m_vertexBuffer = new VertexBuffer<VertexBufferType>(m_device, vertices);
				m_indexBuffer = new IndexBuffer(m_device, primitive.Indices);

				// Create instance buffer with 1 element, since it needs atleast to allocate some data.
				m_instanceBuffer = new VertexBuffer<InstanceBufferType>(m_device, 1);

				// Create a list to dynamically add instances
				m_instanceList = new List<InstanceBufferType>();
			}

			/// <summary>
			/// Adds a new instance of the primitive to the rendering batch
			/// </summary>
			/// <param name="transform">The primitives transform</param>
			/// <param name="color">The primitives color</param>
			public void AddInstance(ref Matrix transform, ref Color color) {
				// Transpose the matrix for use in the shader
				Matrix.Transpose(ref transform, out transform);

				// Add a new instance to the instance list
				m_instanceList.Add(new InstanceBufferType() {
					WorldMatrix = transform,
					Color = color.ToVector4(),
				});

				// Trigger upload to GPU
				m_hasInstancesChanged = true;
			}

			/// <summary>
			/// Clears the batch of instances
			/// </summary>
			public void Clear() {
				m_instanceList.Clear();
				m_hasInstancesChanged = true;
			}

			/// <summary>
			/// Renders and clears the primitive batch
			/// </summary>
			public void RenderAndClear() {
				// Check if there is any instances to render
				if (m_instanceList.Count > 0) {
					// Copy from the dynamic instance list to the GPU buffer
					m_instanceBuffer.WriteRange(m_instanceList.ToArray());
					// Set vertex buffers to the input assembler
					m_context.InputAssembler.SetVertexBuffers(0, m_vertexBuffer, m_instanceBuffer);
					// Set index buffer to the input assembler
					m_context.InputAssembler.SetIndexBuffer(m_indexBuffer, Format.R32_UInt, 0);
					// Draw all the instances
					m_context.DrawIndexedInstanced(m_indexBuffer.ElementCount, m_instanceList.Count, 0, 0, 0);
				}

				// Clear instance list
				m_instanceList.Clear();
			}


			public void Render() {
				// Check if there is any instances to render
				if (m_instanceList.Count > 0) {
					if (m_hasInstancesChanged) {
						// Copy from the dynamic instance list to the GPU buffer
						m_instanceBuffer.WriteRange(m_instanceList.ToArray());
						// Trigger upload
						m_hasInstancesChanged = false;
					}

					// Set vertex buffers to the input assembler
					m_context.InputAssembler.SetVertexBuffers(0, m_vertexBuffer, m_instanceBuffer);
					// Set index buffer to the input assembler
					m_context.InputAssembler.SetIndexBuffer(m_indexBuffer, Format.R32_UInt, 0);
					// Draw all the instances
					m_context.DrawIndexedInstanced(m_indexBuffer.ElementCount, m_instanceList.Count, 0, 0, 0);
				}
			}

			protected override void OnDispose() {
				m_vertexBuffer?.Dispose();
				m_indexBuffer?.Dispose();
				m_instanceBuffer?.Dispose();
			}
		}

		/// <summary>
		/// Used for world view projection matrices
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		private struct MatrixBufferType {
			public Matrix WorldMatrix;
			public Matrix ViewMatrix;
			public Matrix ProjectionMatrix;
		}

		/// <summary>
		/// Used for a single light
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		private struct LightBufferType {
			public Vector4 AmbientColor;
			public Vector4 DiffuseColor;
			public Vector3 LightDirection;
			private float Padding;
		}

		/// <summary>
		/// Input layout for the shader / vertex buffers
		/// </summary>
		private InputLayout m_inputLayout;

		/// <summary>
		/// Vertex shader used for all primitive rendering
		/// </summary>
		private VertexShader m_vertexShader;

		/// <summary>
		/// Pixel shader used for all primitive rendering
		/// </summary>
		private PixelShader m_pixelShader;

		/// <summary>
		/// Constant buffer to send world, view and projection matrices to the GPU
		/// </summary>
		private ConstantBuffer<MatrixBufferType> m_matrixBuffer;

		/// <summary>
		/// Container for the world, view and projection matrices
		/// </summary>
		private MatrixBufferType m_matrices;

		/// <summary>
		/// Constant buffer to send light data to the GPU
		/// </summary>
		private ConstantBuffer<LightBufferType> m_lightBuffer;

		/// <summary>
		/// Primitive batch for cubes
		/// </summary>
		private PrimitiveBatch m_cubeBatch;

		/// <summary>
		/// Primitive batch for spheres
		/// </summary>
		private PrimitiveBatch m_sphereBatch;

		/// <summary>
		/// Primitive batch for planes
		/// </summary>
		private PrimitiveBatch m_planeBatch;

		/// <summary>
		/// Container for all custom primitives to render
		/// </summary>
		private Dictionary<Primitive, PrimitiveBatch> m_customBatches;


		/// <summary>
		/// Creates a new instance of a primitive renderer
		/// </summary>
		/// <param name="device">Base device used for rendering</param>
		public PrimitiveRenderer(GraphicsDevice device) : base(device) {
			// Load shader code
			var vsByteCode = ShaderBytecode.Compile(SHADER_SOURCE, "VS", "vs_5_0");
			var psByteCode = ShaderBytecode.Compile(SHADER_SOURCE, "PS", "ps_5_0");

			// Create input layout with vertex and instance information
			m_inputLayout = new InputLayout(m_device, vsByteCode, new[] {
				// Per vertex data
				new InputElement("POSITION", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
				new InputElement("NORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),
				new InputElement("TEXCOORD", 0, Format.R32G32_Float, InputElement.AppendAligned, 0, InputClassification.PerVertexData, 0),

				// Per instance data
				// Matrix needs four rows with four columns. Since there is no format to corresponds to that, we need to create 4 sequential input elements
				new InputElement("INSTANCE_TRANSFORM", 0, Format.R32G32B32A32_Float, InputElement.AppendAligned, 1, InputClassification.PerInstanceData, 1),
				new InputElement("INSTANCE_TRANSFORM", 1, Format.R32G32B32A32_Float, InputElement.AppendAligned, 1, InputClassification.PerInstanceData, 1),
				new InputElement("INSTANCE_TRANSFORM", 2, Format.R32G32B32A32_Float, InputElement.AppendAligned, 1, InputClassification.PerInstanceData, 1),
				new InputElement("INSTANCE_TRANSFORM", 3, Format.R32G32B32A32_Float, InputElement.AppendAligned, 1, InputClassification.PerInstanceData, 1),
				// Color of each instance
				new InputElement("COLOR", 0, Format.R32G32B32A32_Float, InputElement.AppendAligned, 1, InputClassification.PerInstanceData, 1),
			});
			// Create vertex and pixel shaders
			m_vertexShader = new VertexShader(m_device, vsByteCode);
			m_pixelShader = new PixelShader(m_device, psByteCode);

			// Dispose of unused data
			vsByteCode?.Dispose();
			psByteCode?.Dispose();

			// Create batches for primitives
			m_cubeBatch = new PrimitiveBatch(m_device, PrimitiveFactory.Cube());
			m_sphereBatch = new PrimitiveBatch(m_device, PrimitiveFactory.Sphere(0.5F));
			m_planeBatch = new PrimitiveBatch(m_device, PrimitiveFactory.Plane());
			m_customBatches = new Dictionary<Primitive, PrimitiveBatch>();

			// Setup matrix buffer
			m_matrixBuffer = new ConstantBuffer<MatrixBufferType>(m_device);
			m_matrices = new MatrixBufferType();

			// Setup light buffer
			m_lightBuffer = new ConstantBuffer<LightBufferType>(m_device, new LightBufferType() {
				AmbientColor = new Vector4(0.1F, 0.1F, 0.1F, 1.0F),
				DiffuseColor = new Vector4(1.0F, 1.0F, 1.0F, 1.0F),
				LightDirection = Vector3.Normalize(Vector3.Down + (Vector3.Left * 0.2F) + (Vector3.ForwardLH * 0.4F)),
			});
		}

		/// <summary>
		/// Renders everything that has been queued into the primitive renderer
		/// </summary>
		/// <param name="dxCamera">Camera object used for view and projection</param>
		/// <param name="world">Optional world transform for all objects</param>
		/// <param name="pixelshader">Custom pixel shader</param>
		public void Render(DXCamera dxCamera, Matrix? world = null, PixelShader pixelshader = null, bool clearBatches = true) {
			// Update matrix buffer with input camera matrices.
			m_matrices.WorldMatrix = Matrix.Transpose(world ?? Matrix.Identity);
			m_matrices.ViewMatrix = Matrix.Transpose(dxCamera.ViewMatrix);
			m_matrices.ProjectionMatrix = Matrix.Transpose(dxCamera.ProjectionMatrix);
			m_matrixBuffer.Write(m_matrices);

			// Set primitive topology and input layout
			m_context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
			m_context.InputAssembler.InputLayout = m_inputLayout;

			// Set vertex and pixel shaders
			m_context.VertexShader.Set(m_vertexShader);
			m_context.VertexShader.SetConstantBuffer(0, m_matrixBuffer);
			m_context.PixelShader.Set(pixelshader ?? m_pixelShader);
			m_context.PixelShader.SetConstantBuffer(0, m_lightBuffer);
			m_context.GeometryShader.Set(null);

			if (clearBatches) {
				// Render all batches and clear buffers
				m_cubeBatch.RenderAndClear();
				m_sphereBatch.RenderAndClear();
				m_planeBatch.RenderAndClear();

				foreach (var customBatch in m_customBatches) {
					customBatch.Value.RenderAndClear();
				}
			} else {
				// Render all batches and clear buffers
				m_cubeBatch.Render();
				m_sphereBatch.Render();
				m_planeBatch.Render();

				foreach (var customBatch in m_customBatches) {
					customBatch.Value.Render();
				}
			}
		}

		/// <summary>
		/// Clears all instances in the primitive renderer
		/// </summary>
		public void Clear() {
			m_cubeBatch.Clear();
			m_sphereBatch.Clear();
			m_planeBatch.Clear();
			foreach (var customBatch in m_customBatches) {
				customBatch.Value.Clear();
			}
		}

		/// <summary>
		/// Adds a cube to the render queue.
		/// </summary>
		/// <param name="transform">The cubes transform</param>
		/// <param name="color">The color of the cube</param>
		public void Cube(Matrix transform, Color color) {
			m_cubeBatch.AddInstance(ref transform, ref color);
		}

		/// <summary>
		/// Adds a cube to the render queue.
		/// </summary>
		/// <param name="transform">The cubes transform</param>
		/// <param name="color">The color of the cube</param>
		public void Cube(ref Matrix transform, ref Color color) {
			m_cubeBatch.AddInstance(ref transform, ref color);
		}


		/// <summary>
		/// Adds a sphere to the render queue.
		/// </summary>
		/// <param name="transform">The spheres transform</param>
		/// <param name="color">The color of the sphere</param>
		public void Sphere(Matrix transform, Color color) {
			m_sphereBatch.AddInstance(ref transform, ref color);
		}

		/// <summary>
		/// Adds a sphere to the render queue.
		/// </summary>
		/// <param name="transform">The spheres transform</param>
		/// <param name="color">The color of the sphere</param>
		public void Sphere(ref Matrix transform, ref Color color) {
			m_sphereBatch.AddInstance(ref transform, ref color);
		}

		/// <summary>
		/// Adds a plane to the render queue.
		/// </summary>
		/// <param name="transform">The planes transform</param>
		/// <param name="color">The color of the plane</param>
		public void Plane(Matrix transform, Color color) {
			m_planeBatch.AddInstance(ref transform, ref color);
		}

		/// <summary>
		/// Adds a plane to the render queue.
		/// </summary>
		/// <param name="transform">The planes transform</param>
		/// <param name="color">The color of the plane</param>
		public void Plane(ref Matrix transform, ref Color color) {
			m_planeBatch.AddInstance(ref transform, ref color);
		}

		public void Plane(Vector3 normal, Vector3 position, float scale, Color color, Matrix transform) {
			// Crossing vector
			var upVec = Vector3.Up;

			// If normal is exactly up or down, set crossing vector to direct left
			if (normal == Vector3.Up || normal == Vector3.Down) {
				upVec = Vector3.Left;
			} else {
				// If normal is facing mostly up, reverse up vector
				if (Vector3.Dot(normal, upVec) < 0.0F) {
					upVec = Vector3.Down;
				}
			}

			var left = Vector3.Cross(normal, upVec);
			var forward = Vector3.Cross(normal, left);
			var matrix = Matrix.Identity;

			// Set matrix rotation
			matrix.Up = normal;
			matrix.Left = -left;
			matrix.Forward = forward;

			// Normalize matrix
			matrix.Orthonormalize();

			Plane(Matrix.Scaling(scale) * matrix * Matrix.Translation(position) * transform, color);
		}


		/// <summary>
		/// Adds a custom primitive to the render queue
		/// </summary>
		/// <param name="primitive">The primitive to create</param>
		/// <param name="transform">The primitives transform</param>
		/// <param name="color">The color of the primitive</param>
		public void CustomPrimitive(Primitive primitive, Matrix transform, Color color) {
			if (!m_customBatches.ContainsKey(primitive)) {
				m_customBatches.Add(primitive, new PrimitiveBatch(m_device, primitive));
			}

			m_customBatches[primitive].AddInstance(ref transform, ref color);
		}


		protected override void OnDispose() {
			Utilities.Dispose(ref m_inputLayout);
			Utilities.Dispose(ref m_vertexShader);
			Utilities.Dispose(ref m_pixelShader);
			Utilities.Dispose(ref m_matrixBuffer);
			Utilities.Dispose(ref m_lightBuffer);
			Utilities.Dispose(ref m_cubeBatch);
			Utilities.Dispose(ref m_sphereBatch);
			Utilities.Dispose(ref m_planeBatch);

			foreach (var customBatch in m_customBatches) {
				customBatch.Value?.Dispose();
			}

			m_customBatches.Clear();
		}

		/// <summary>
		/// Quick and dirty shader source used for rendering.
		/// </summary>
		private const string SHADER_SOURCE = @"
		cbuffer MatrixBuffer {
			matrix worldMatrix;
			matrix viewMatrix;
			matrix projectionMatrix;
		};

		cbuffer LightBuffer {
			float4 ambientColor;
			float4 diffuseColor;
			float3 lightDirection;
			float padding;
		};

		struct VSInn {
			// Per vertex data
			float3 pos 			: POSITION0;
			float3 normal 		: NORMAL;
			float2 UV			: TEXCOORD;

			// Per instance data
			float4x4 inst_mat 	: INSTANCE_TRANSFORM;
			float4 color 		: COLOR;
		};

		struct PSInn {
			float4 pos 		: SV_POSITION;
			float4 color 	: COLOR;
			float3 normal 	: NORMAL;
		};

		PSInn VS(VSInn input) {
			PSInn output;

			// Multiply output position with per instance world matrix
			output.pos = mul(float4(input.pos, 1.0F), input.inst_mat);
			
			// Multiply with world, view and projection matrices
			output.pos = mul(output.pos, worldMatrix);
			output.pos = mul(output.pos, viewMatrix);
			output.pos = mul(output.pos, projectionMatrix);
			
			// Send color
			output.color = input.color;
			
			// Multiply normal with instance world matrix and normal world matrix
			output.normal = mul(input.normal, (float3x3)input.inst_mat);
			output.normal = mul(output.normal, (float3x3)worldMatrix);

			// Normalize normal.
			output.normal = normalize(output.normal);
		        
			return output;
		}

		float4 PS(PSInn input) : SV_TARGET {
		    float4 inputColor = input.color;
		    float4 color = ambientColor;
		    float3 lightDir = -lightDirection;
		    float lightIntensity = saturate(dot(input.normal, lightDir));

			if(lightIntensity > 0.0f) {
		        color += (diffuseColor * lightIntensity);
		    }

		    color = saturate(color * inputColor);
			color.a = input.color.a;
		    return color;
		}";
	}
}