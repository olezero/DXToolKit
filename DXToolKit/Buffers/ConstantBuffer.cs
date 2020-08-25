using System;
using SharpDX;
using SharpDX.Direct3D11;

namespace DXToolKit {
	/// <summary>
	/// Constant buffer used by the GPU
	/// </summary>
	/// <typeparam name="T">Struct type the constant buffer should use</typeparam>
	public class ConstantBuffer<T> : DXBuffer<T> where T : struct {
		/// <summary>
		/// Creates a new dynamic and writable constant buffer with the size of T
		/// </summary>
		/// <param name="device">Device used for creation</param>
		public ConstantBuffer(GraphicsDevice device) : base(device) {
			CheckSizeRequirements();
			CreateBuffer(DefaultDescription());
		}

		/// <summary>
		/// Creates a new constant buffer with the input description
		/// </summary>
		/// <param name="device">Device used for creation</param>
		/// <param name="description">Description of the buffer</param>
		public ConstantBuffer(GraphicsDevice device, BufferDescription description) : base(device) {
			CheckSizeRequirements();
			CreateBuffer(description);
		}

		/// <summary>
		/// Creates a new dynamic and writable constant buffer with the size of T
		/// </summary>
		/// <param name="device">Device used for creation</param>
		/// <param name="data">The data to set in the buffer</param>
		public ConstantBuffer(GraphicsDevice device, T data) : base(device) {
			CheckSizeRequirements();
			CreateBuffer(data, DefaultDescription());
		}

		/// <summary>
		/// Creates a new dynamic and writable constant buffer with the size of T
		/// </summary>
		/// <param name="device">Device used for creation</param>
		/// <param name="data">The data to set in the buffer</param>
		/// <param name="description">Description of the buffer</param>
		public ConstantBuffer(GraphicsDevice device, T data, BufferDescription description) : base(device) {
			CheckSizeRequirements();
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
		/// Checks the size of T to make sure its a multiple of 16
		/// </summary>
		/// <exception cref="Exception">Throws exception if invalid size of T</exception>
		private static void CheckSizeRequirements() {
			if (Utilities.SizeOf<T>() % 16 != 0) {
				throw new Exception($"Sizeof(T) must be a multiple of 16 bytes. Got {Utilities.SizeOf<T>()} bytes");
			}
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