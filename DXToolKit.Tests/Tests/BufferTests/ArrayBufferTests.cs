using System;
using NUnit.Framework;
using SharpDX.Direct3D11;

namespace DXToolKit.Tests {
	[TestFixture]
	public class ArrayBufferTests : FeatureBase {
		[Test]
		public void ReadWriteToStagingBuffer() {
			var desc = new BufferDescription {
				Usage = ResourceUsage.Staging,
				BindFlags = BindFlags.None,
				OptionFlags = ResourceOptionFlags.None,
				CpuAccessFlags = CpuAccessFlags.Write | CpuAccessFlags.Read,
				SizeInBytes = sizeof(float),
				StructureByteStride = 1,
			};

			// Create buffer
			var buffer = ToDispose(new ArrayBuffer<float>(m_device, 1, desc));
			// Should be writable
			Assert.True(buffer.CanWrite);
			Assert.True(buffer.CanRead);
			// Assert that the buffer contains a single element
			Assert.AreEqual(1, buffer.ElementCount);
			// Write a range of 2 to the buffer
			buffer.WriteRange(new float[] {1, 2});
			// Should now contain 2 elements
			Assert.AreEqual(2, buffer.ElementCount);
			// Description should be updated aswell
			Assert.AreEqual(sizeof(float) * 2, buffer.Description.SizeInBytes);
			// Read from buffer
			var result = buffer.ReadRange();
			// Should provide the correct float array
			Assert.AreEqual(new float[] {1, 2}, result);
		}

		[Test]
		public void ReadWriteExceptions() {
			var desc = new BufferDescription {
				Usage = ResourceUsage.Default,
				BindFlags = BindFlags.None,
				OptionFlags = ResourceOptionFlags.None,
				CpuAccessFlags = CpuAccessFlags.None,
				SizeInBytes = sizeof(float),
				StructureByteStride = 1,
			};

			var buffer = ToDispose(new ArrayBuffer<float>(m_device, 1, desc));

			Assert.AreEqual(desc, buffer.Description);
			Assert.AreEqual(1, buffer.ElementCount);
			Assert.False(buffer.CanWrite);
			Assert.False(buffer.CanRead);

			// Should not be writable
			Assert.Throws<Exception>(() => {
				buffer.WriteRange(new float[] {1, 2, 3, 4, 5, 6});
			});

			// Should not be readable
			Assert.Throws<Exception>(() => {
				buffer.ReadRange();
			});
		}
	}
}