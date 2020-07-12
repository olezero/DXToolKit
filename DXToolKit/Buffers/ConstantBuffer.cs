using SharpDX;
using SharpDX.Direct3D11;

namespace DXToolKit {
	public class ConstantBuffer<T> : DXBuffer<T> where T : struct {
		/// <summary>
		/// Creates a new dynamic and writable constant buffer with the size of T
		/// </summary>
		/// <param name="device">Device used for creation</param>
		public ConstantBuffer(GraphicsDevice device) : base(device) {
			CreateBuffer(DefaultDescription());
		}

		/// <summary>
		/// Creates a new constant buffer with the input description
		/// </summary>
		/// <param name="device">Device used for creation</param>
		/// <param name="description">Description of the buffer</param>
		public ConstantBuffer(GraphicsDevice device, BufferDescription description) : base(device) {
			CreateBuffer(description);
		}

		/// <summary>
		/// Creates a new dynamic and writable constant buffer with the size of T
		/// </summary>
		/// <param name="device">Device used for creation</param>
		/// <param name="data">The data to set in the buffer</param>
		public ConstantBuffer(GraphicsDevice device, T data) : base(device) {
			CreateBuffer(data, DefaultDescription());
		}

		/// <summary>
		/// Creates a new dynamic and writable constant buffer with the size of T
		/// </summary>
		/// <param name="device">Device used for creation</param>
		/// <param name="data">The data to set in the buffer</param>
		/// <param name="description">Description of the buffer</param>
		public ConstantBuffer(GraphicsDevice device, T data, BufferDescription description) : base(device) {
			CreateBuffer(data, description);
		}

		/// <summary>
		/// Writes data to the buffer.
		/// </summary>
		/// <param name="data">The data to write</param>
		public void Write(T data) {
			OpenWrite().Write(data);
			CloseBuffer();
		}

		/// <summary>
		/// Default description for a constant buffer
		/// </summary>
		/// <returns></returns>
		private static BufferDescription DefaultDescription() =>
			new BufferDescription {
				Usage = ResourceUsage.Dynamic,
				BindFlags = BindFlags.ConstantBuffer,
				OptionFlags = ResourceOptionFlags.None,
				CpuAccessFlags = CpuAccessFlags.Write,
				SizeInBytes = Utilities.SizeOf<T>(),
				StructureByteStride = 0,
			};
	}
}