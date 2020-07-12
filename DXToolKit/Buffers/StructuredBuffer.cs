using SharpDX;
using SharpDX.Direct3D11;

namespace DXToolKit {
	public class StructuredBuffer<T> : ArrayBuffer<T> where T : struct {
		private UnorderedAccessView m_unorderedAccessView;

		public UnorderedAccessView unorderedAccessView => m_unorderedAccessView;

		public StructuredBuffer(GraphicsDevice device, T[] data) : base(device, data, new BufferDescription() {
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

		public StructuredBuffer(GraphicsDevice device, int elementCount) : base(device, elementCount, new BufferDescription() {
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

		public StructuredBuffer(GraphicsDevice device, int elementCount, BufferDescription description) : base(device, elementCount, description) {
			CreateUAV();
			OnBufferCreated += CreateUAV;
		}


		private void CreateUAV() {
			m_unorderedAccessView?.Dispose();
			m_unorderedAccessView = new UnorderedAccessView(m_device, Buffer);
		}

		protected override void OnDispose() {
			base.OnDispose();
			m_unorderedAccessView?.Dispose();
		}
	}
}