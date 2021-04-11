using NUnit.Framework;
using SharpDX;
using SharpDX.Direct3D11;

namespace DXToolKit.Tests {
	[TestFixture]
	public class VertexBufferTests : FeatureBase {
		[Test]
		public void ImplicitVertexBufferBinding() {
			var buffer = ToDispose(new VertexBuffer<Vector3>(m_device, 12));
			Assert.DoesNotThrow(() => {
				var binding = (VertexBufferBinding) buffer;
				Assert.AreEqual(binding, buffer.VertexBufferBinding);
			});
		}

		[Test]
		public void ConstructorElementCount() {
			var buffer = ToDispose(new VertexBuffer<Vector3>(m_device, 12));

			Assert.NotNull(buffer.Buffer);

			Assert.AreEqual(Utilities.SizeOf<Vector3>() * 12, buffer.Description.SizeInBytes);
			Assert.AreEqual(ResourceUsage.Dynamic, buffer.Description.Usage);
			Assert.AreEqual(BindFlags.VertexBuffer, buffer.Description.BindFlags);
			Assert.AreEqual(CpuAccessFlags.Write, buffer.Description.CpuAccessFlags);
			Assert.AreEqual(ResourceOptionFlags.None, buffer.Description.OptionFlags);
			Assert.AreEqual(Utilities.SizeOf<Vector3>(), buffer.Description.StructureByteStride);

			Assert.AreEqual(0, buffer.VertexBufferBinding.Offset);
			Assert.AreEqual(Utilities.SizeOf<Vector3>(), buffer.VertexBufferBinding.Stride);
		}

		[Test]
		public void ConstructorArray() {
			var buffer = ToDispose(new VertexBuffer<float>(m_device, new[] {1.0F, 2.0F, 3.0F}));

			Assert.NotNull(buffer.Buffer);

			Assert.AreEqual(Utilities.SizeOf<float>() * 3, buffer.Description.SizeInBytes);
			Assert.AreEqual(ResourceUsage.Immutable, buffer.Description.Usage);
			Assert.AreEqual(BindFlags.VertexBuffer, buffer.Description.BindFlags);
			Assert.AreEqual(CpuAccessFlags.None, buffer.Description.CpuAccessFlags);
			Assert.AreEqual(ResourceOptionFlags.None, buffer.Description.OptionFlags);
			Assert.AreEqual(Utilities.SizeOf<float>(), buffer.Description.StructureByteStride);

			Assert.AreEqual(0, buffer.VertexBufferBinding.Offset);
			Assert.AreEqual(Utilities.SizeOf<float>(), buffer.VertexBufferBinding.Stride);
		}

		[Test]
		public void ConstructorElementCountWithDescription() {
			var desc = new BufferDescription {
				Usage = ResourceUsage.Dynamic,
				BindFlags = BindFlags.VertexBuffer,
				OptionFlags = ResourceOptionFlags.None,
				CpuAccessFlags = CpuAccessFlags.Write,
				SizeInBytes = Utilities.SizeOf<float>() * 8,
				StructureByteStride = Utilities.SizeOf<float>()
			};
			var buffer = ToDispose(new VertexBuffer<float>(m_device, 8, desc));
			Assert.AreEqual(desc, buffer.Description);
			Assert.NotNull(buffer.Buffer);
			Assert.AreEqual(0, buffer.VertexBufferBinding.Offset);
			Assert.AreEqual(Utilities.SizeOf<float>(), buffer.VertexBufferBinding.Stride);

			var fArr = new float[] {
				1, 2, 3, 4, 5, 6, 7, 8
			};
			buffer.WriteRange(fArr);
			var result = ReadBuffer(buffer);
			Assert.AreEqual(fArr, result);
		}

		[Test]
		public void AllowWritingDifferentArrayLengthsToBuffer() {
			var buffer = ToDispose(new VertexBuffer<float>(m_device, 4));
			// Should be able to write different length arrays to dynamic vertex buffer
			Assert.DoesNotThrow(() => {
				buffer.WriteRange(new float[] {1, 2, 3, 4});
				buffer.WriteRange(new float[] {1, 2, 3, 4, 5, 6});
			});
		}
	}
}