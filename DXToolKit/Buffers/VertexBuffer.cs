using SharpDX;
using SharpDX.Direct3D11;

namespace DXToolKit {
	public class VertexBuffer<T> : ArrayBuffer<T> where T : struct {
		/// <summary>
		/// Vertex buffer binding used by the input assembler.
		/// </summary>
		private VertexBufferBinding m_binding;

		/// <summary>
		/// Gets a reference to the vertex buffer binding.
		/// </summary>
		public VertexBufferBinding VertexBufferBinding => m_binding;

		/// <summary>
		/// Creates a new vertex buffer that can only be accessed by the GPU.
		/// </summary>
		/// <param name="device">Base device used to create the buffer.</param>
		/// <param name="data">The data to set in the buffer.</param>
		public VertexBuffer(GraphicsDevice device, T[] data) : base(device, data, new BufferDescription() {
			Usage = ResourceUsage.Default,
			BindFlags = BindFlags.VertexBuffer,
			OptionFlags = ResourceOptionFlags.None,
			CpuAccessFlags = CpuAccessFlags.None,
			SizeInBytes = Utilities.SizeOf(data),
			StructureByteStride = Utilities.SizeOf<T>()
		}) {
			// Call set binding for the first time.
			SetBinding();
			// Create event binding to setup new vertex buffer binding after the buffer has been created.
			OnBufferCreated += SetBinding;
		}

		/// <summary>
		/// Creates a dynamic vertex buffer that can be written to by the CPU
		/// </summary>
		/// <param name="device">Base device used to create the buffer.</param>
		/// <param name="elementCount">Number of elements to allocate space for in the buffer</param>
		public VertexBuffer(GraphicsDevice device, int elementCount) : base(device, elementCount, new BufferDescription() {
			Usage = ResourceUsage.Dynamic,
			BindFlags = BindFlags.VertexBuffer,
			OptionFlags = ResourceOptionFlags.None,
			CpuAccessFlags = CpuAccessFlags.Write,
			SizeInBytes = Utilities.SizeOf<T>() * elementCount,
			StructureByteStride = Utilities.SizeOf<T>()
		}) {
			// Call set binding for the first time.
			SetBinding();
			// Create event binding to setup new vertex buffer binding after the buffer has been created.
			OnBufferCreated += SetBinding;
		}

		/// <summary>
		/// Creates a custom vertex buffer
		/// </summary>
		/// <param name="device">Base device used to create the buffer.</param>
		/// <param name="elementCount">Number of elements to allocate space for in the buffer</param>
		/// <param name="description">Description of the buffer</param>
		public VertexBuffer(GraphicsDevice device, int elementCount, BufferDescription description) : base(device, elementCount, description) {
			// Call set binding for the first time.
			SetBinding();
			// Create event binding to setup new vertex buffer binding after the buffer has been created.
			OnBufferCreated += SetBinding;
		}

		/// <summary>
		/// Implicit operator overload to vertex buffer binding.
		/// </summary>
		public static implicit operator VertexBufferBinding(VertexBuffer<T> vbuffer) => vbuffer.m_binding;

		private void SetBinding() {
			m_binding = new VertexBufferBinding(m_buffer, Utilities.SizeOf<T>(), 0);
		}
	}
}