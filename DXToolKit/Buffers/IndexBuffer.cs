using SharpDX;
using SharpDX.Direct3D11;

namespace DXToolKit {
	/// <summary>
	/// Index buffer used by DirectX. Based on ArrayBuffer of type Int
	/// </summary>
	public class IndexBuffer : ArrayBuffer<int> {
		/// <summary>
		/// Creates a new index buffer with input data.
		/// </summary>
		/// <param name="device">Device used for creation</param>
		/// <param name="data">Data to set in the buffer</param>
		public IndexBuffer(GraphicsDevice device, int[] data) : base(device, data, new BufferDescription() {
			Usage = ResourceUsage.Immutable,
			BindFlags = BindFlags.IndexBuffer,
			OptionFlags = ResourceOptionFlags.None,
			CpuAccessFlags = CpuAccessFlags.None,
			SizeInBytes = Utilities.SizeOf(data),
			StructureByteStride = Utilities.SizeOf<int>()
		}) { }

		/// <summary>
		/// Creates a dynamic index buffer that can be written to by the CPU
		/// </summary>
		/// <param name="device">Base device used to create the buffer.</param>
		/// <param name="elementCount">Number of elements to allocate space for in the buffer</param>
		public IndexBuffer(GraphicsDevice device, int elementCount) : base(device, elementCount, new BufferDescription() {
			Usage = ResourceUsage.Dynamic,
			BindFlags = BindFlags.IndexBuffer,
			OptionFlags = ResourceOptionFlags.None,
			CpuAccessFlags = CpuAccessFlags.Write,
			SizeInBytes = Utilities.SizeOf<int>() * elementCount,
			StructureByteStride = Utilities.SizeOf<int>(),
		}) { }

		/// <summary>
		/// Creates a custom index buffer
		/// </summary>
		/// <param name="device">Base device used to create the buffer.</param>
		/// <param name="elementCount">Number of elements to allocate space for in the buffer</param>
		/// <param name="description">Description of the buffer</param>
		public IndexBuffer(GraphicsDevice device, int elementCount, BufferDescription description) : base(device, elementCount, description) { }
	}
}