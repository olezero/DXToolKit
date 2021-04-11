using System;
using NUnit.Framework;
using SharpDX;
using SharpDX.Direct3D11;

namespace DXToolKit.Tests {
	[TestFixture]
	public class ConstantBufferTests : FeatureBase {
		[Test]
		public void InvalidWhenNotMultipleOfSixteen() {
			Assert.Throws<Exception>(() => {
				ToDispose(new ConstantBuffer<float>(m_device));
			}, "Sizeof(T) must be a multiple of 16 bytes. Got 4 bytes");
		}


		[Test]
		public void Basic() {
			var cBuffer = ToDispose(new ConstantBuffer<Matrix>(m_device));
			Assert.NotNull(cBuffer);

			var desc = cBuffer.Description;
			Assert.AreEqual(64, desc.SizeInBytes);
			Assert.AreEqual(ResourceUsage.Dynamic, desc.Usage);
			Assert.AreEqual(BindFlags.ConstantBuffer, desc.BindFlags);
			Assert.AreEqual(CpuAccessFlags.Write, desc.CpuAccessFlags);
			Assert.AreEqual(ResourceOptionFlags.None, desc.OptionFlags);
			Assert.AreEqual(0, desc.StructureByteStride);
		}

		[Test]
		public void Writable() {
			var matrix = Matrix.Identity;
			// Initialize constant buffer with value
			var cBuffer = ToDispose(new ConstantBuffer<Matrix>(m_device, matrix));
			// Read from GPU
			var result = ReadBufferSingle(cBuffer);
			// Assert that input and output matrices are equal
			Assert.AreEqual(matrix, result);
			// Setup a random matrix
			matrix = Matrix.LookAtLH(new Vector3(100, 200, 300), new Vector3(500, 100, 10), Vector3.Left);
			// Assert that GPU matrix matches new matrix
			cBuffer.Write(matrix);
			result = ReadBufferSingle(cBuffer);
			Assert.AreEqual(matrix, result);
		}

		[Test]
		public void CustomDescription() {
			var matrix = Matrix.Identity;
			var desc = new BufferDescription {
				Usage = ResourceUsage.Dynamic,
				BindFlags = BindFlags.ConstantBuffer,
				OptionFlags = ResourceOptionFlags.None,
				CpuAccessFlags = CpuAccessFlags.Write,
				SizeInBytes = Utilities.SizeOf<Matrix>(),
				StructureByteStride = 0,
			};
			// Create constant buffer
			var cBuffer = ToDispose(new ConstantBuffer<Matrix>(m_device, desc));
			// Should be equal
			Assert.AreEqual(desc, cBuffer.Description);
			// Should be writable
			Assert.True(cBuffer.CanWrite);
			// Should not be readable
			Assert.False(cBuffer.CanRead);
			// Fetch matrix from buffer, should be empty
			Assert.AreEqual(new Matrix(), ReadBufferSingle(cBuffer));
			// Write updated matrix
			cBuffer.Write(matrix);
			// Fetch from GPU
			var result = ReadBufferSingle(cBuffer);
			// Should no longer be equal to empty matrix
			Assert.AreNotEqual(new Matrix(), result);
			// Should be equal to identity matrix
			Assert.AreEqual(matrix, result);
		}

		[Test]
		public void InitialDataWithDescription() {
			var matrix = Matrix.Identity;
			var desc = new BufferDescription {
				Usage = ResourceUsage.Dynamic,
				BindFlags = BindFlags.ConstantBuffer,
				OptionFlags = ResourceOptionFlags.None,
				CpuAccessFlags = CpuAccessFlags.Write,
				SizeInBytes = Utilities.SizeOf<Matrix>(),
				StructureByteStride = Utilities.SizeOf<Matrix>(),
			};
			// Create constant buffer
			var cBuffer = ToDispose(new ConstantBuffer<Matrix>(m_device, matrix, desc));
			// Should be equal
			Assert.AreEqual(desc, cBuffer.Description);
			// Should be writable
			Assert.True(cBuffer.CanWrite);
			// Should not be readable
			Assert.False(cBuffer.CanRead);
			// Fetch matrix from buffer, should be empty
			Assert.AreEqual(matrix, ReadBufferSingle(cBuffer));
			// Update matrix
			matrix = Matrix.LookAtLH(new Vector3(100, 200, 300), new Vector3(500, 100, 10), Vector3.Left);
			// Write updated matrix
			cBuffer.Write(matrix);
			// Fetch from GPU
			var result = ReadBufferSingle(cBuffer);
			// Should be equal to identity matrix
			Assert.AreEqual(matrix, result);
		}
	}
}