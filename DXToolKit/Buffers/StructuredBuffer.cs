using SharpDX;
using SharpDX.Direct3D11;

namespace DXToolKit {
	/// <summary>
	/// Structured buffer used by DirectCompute
	/// </summary>
	/// <typeparam name="T">Data type the buffer should hold</typeparam>
	public class StructuredBuffer<T> : ArrayBuffer<T> where T : struct {
		private UnorderedAccessView m_uav;

		/// <summary>
		/// Gets a reference to the UAV
		/// </summary>
		public UnorderedAccessView UAV => m_uav;

		/// <summary>
		/// Creates a new structured buffer, and initializes it with input data
		/// </summary>
		/// <param name="device">Graphics device to use when creating the buffer</param>
		/// <param name="data">Data to initialize the buffer with</param>
		public StructuredBuffer(GraphicsDevice device, T[] data) : base(device, data, new BufferDescription {
			Usage = ResourceUsage.Default,
			BindFlags = BindFlags.UnorderedAccess,
			OptionFlags = ResourceOptionFlags.BufferStructured,
			CpuAccessFlags = CpuAccessFlags.None,
			SizeInBytes = Utilities.SizeOf(data),
			StructureByteStride = Utilities.SizeOf<T>()
		}) {
			CreateUAV();
			OnBufferCreated += CreateUAV;
		}

		/// <summary>
		/// Creates a new structured buffer, and reserves memory equal to input elementCount times sizeOf(T)
		/// </summary>
		/// <param name="device">Graphics device to use when creating the buffer</param>
		/// <param name="elementCount">The number of elements that should be reserved for this stream</param>
		public StructuredBuffer(GraphicsDevice device, int elementCount) : base(device, elementCount, new BufferDescription {
			Usage = ResourceUsage.Default,
			BindFlags = BindFlags.UnorderedAccess,
			OptionFlags = ResourceOptionFlags.BufferStructured,
			CpuAccessFlags = CpuAccessFlags.None,
			SizeInBytes = Utilities.SizeOf<T>() * elementCount,
			StructureByteStride = Utilities.SizeOf<T>()
		}) {
			CreateUAV();
			OnBufferCreated += CreateUAV;
		}

		/// <summary>
		/// Creates a new structured buffer, and reserves memory equal to input elementCount times sizeOf(T)
		/// </summary>
		/// <param name="device">Graphics device to use when creating the buffer</param>
		/// <param name="elementCount">The number of elements that should be reserved for this stream</param>
		/// <param name="description">Custom description for the buffer</param>
		public StructuredBuffer(GraphicsDevice device, int elementCount, BufferDescription description) : base(device, elementCount, description) {
			CreateUAV();
			OnBufferCreated += CreateUAV;
		}

		private void CreateUAV() {
			m_uav?.Dispose();
			m_uav = new UnorderedAccessView(m_device, Buffer);
		}

		/// <summary>
		/// Releases all unmanaged resources held by the buffer
		/// </summary>
		protected override void OnDispose() {
			base.OnDispose();
			m_uav?.Dispose();
		}
	}
}