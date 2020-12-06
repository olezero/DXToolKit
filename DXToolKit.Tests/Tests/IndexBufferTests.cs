using NUnit.Framework;
using SharpDX.Direct3D11;

namespace DXToolKit.Tests {
	[TestFixture]
	public class IndexBufferTests : FeatureBase {
		[Test]
		public void Basic() {
			var buffer = ToDispose(new IndexBuffer(m_device, 1));
			// Should contain 1 element
			Assert.AreEqual(1, buffer.ElementCount);
			// Should not be readable
			Assert.False(buffer.CanRead);
			// Should be writable
			Assert.True(buffer.CanWrite);
		}

		[Test]
		public void CreateWithInitialData() {
			var indices = new[] {1, 2, 3, 4, 5, 6};
			var buffer = ToDispose(new IndexBuffer(m_device, indices));
			// Should contain 1 element
			Assert.AreEqual(6, buffer.ElementCount);
			// Should not be readable
			Assert.False(buffer.CanRead);
			// Should not be writable
			Assert.False(buffer.CanWrite);
			// Read from GPU
			var result = ReadBuffer(buffer);
			// Should match up with input data
			Assert.AreEqual(indices, result);
		}

		[Test]
		public void CustomBuffer() {
			var indices = new[] {1, 2, 3, 4, 5, 6};
			var desc = new BufferDescription {
				Usage = ResourceUsage.Dynamic,
				BindFlags = BindFlags.IndexBuffer,
				OptionFlags = ResourceOptionFlags.None,
				CpuAccessFlags = CpuAccessFlags.Write,
				SizeInBytes = sizeof(int) * 6,
				StructureByteStride = sizeof(int)
			};
			var buffer = ToDispose(new IndexBuffer(m_device, 6, desc));
			// Should match input description
			Assert.AreEqual(desc, buffer.Description);
			// Should contain 1 element
			Assert.AreEqual(6, buffer.ElementCount);
			// Should not be readable
			Assert.False(buffer.CanRead);
			// Should be writable
			Assert.True(buffer.CanWrite);
			// Read from GPU
			var result = ReadBuffer(buffer);
			// Should be a empty array
			Assert.AreEqual(new[] {0, 0, 0, 0, 0, 0}, result);
			// Write updated array to index buffer
			buffer.WriteRange(indices);
			// Read back data from GPU
			result = ReadBuffer(buffer);
			// Should now match indices
			Assert.AreEqual(indices, result);
		}
	}
}