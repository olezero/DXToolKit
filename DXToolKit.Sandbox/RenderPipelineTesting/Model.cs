using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.DXGI;

namespace DXToolKit.Sandbox {
	public class Model : DeviceComponent {
		[StructLayout(LayoutKind.Sequential)]
		private struct VertexPositionNormalTexture {
			public Vector3 Position;
			public Vector3 Normal;
			public Vector2 UV;
		}

		public int TriangleCount => m_indexBuffer.ElementCount;

		private IndexBuffer m_indexBuffer;
		private VertexBuffer<VertexPositionNormalTexture> m_vertexBuffer;

		public Model(GraphicsDevice device, Primitive primitive) : this(device, primitive.Positions, primitive.Normals, primitive.UVs, primitive.Indices) { }
		public Model(GraphicsDevice device, IReadOnlyList<Vector3> positions, IReadOnlyList<Vector3> normals, IReadOnlyList<Vector2> uvs, int[] indices) : base(device) {
			var vertices = new VertexPositionNormalTexture[positions.Count];
			for (int i = 0; i < vertices.Length; i++) {
				vertices[i].Position = positions[i];
				vertices[i].Normal = normals[i];
				vertices[i].UV = uvs[i];
			}

			m_vertexBuffer = new VertexBuffer<VertexPositionNormalTexture>(m_device, vertices);
			m_indexBuffer = new IndexBuffer(m_device, indices);
		}

		public void Render() {
			m_context.InputAssembler.SetVertexBuffers(0, m_vertexBuffer);
			m_context.InputAssembler.SetIndexBuffer(m_indexBuffer, Format.R32_UInt, 0);
			m_context.DrawIndexed(m_indexBuffer.ElementCount, 0, 0);
		}

		protected override void OnDispose() {
			Utilities.Dispose(ref m_vertexBuffer);
			Utilities.Dispose(ref m_indexBuffer);
		}
	}
}