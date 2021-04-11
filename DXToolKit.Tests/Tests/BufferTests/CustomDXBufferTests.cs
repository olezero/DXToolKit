using System;
using NUnit.Framework;
using SharpDX.Direct3D11;

namespace DXToolKit.Tests {
	[TestFixture]
	public class CustomDXBufferTests : FeatureBase {
		class MyBuffer : DXBuffer<int> {
			public MyBuffer(GraphicsDevice device) : base(device) { }
			public MyBuffer(GraphicsDevice device, BufferDescription description) : base(device, description) { }
			public MyBuffer(GraphicsDevice device, int data, BufferDescription description) : base(device, data, description) { }
			public MyBuffer(GraphicsDevice device, int[] data, BufferDescription description) : base(device, data, description) { }

			public int TestRead() {
				var stream = OpenRead();
				var result = stream.Read<int>();
				CloseBuffer();
				return result;
			}

			public void TestWrite(int data) {
				var stream = OpenWrite();
				stream.Write(data);
				CloseBuffer();
			}

			public void TestOpenTwiceShouldThrowError() {
				OpenWrite();
				OpenWrite();
			}
		}


		[Test]
		public void Constructor() {
			var desc = new BufferDescription();
			var buffer = ToDispose(new MyBuffer(m_device));
			Assert.IsNull(buffer.Buffer);
			Assert.AreEqual(desc, buffer.Description);
			Assert.False(buffer.CanWrite);
			Assert.False(buffer.CanRead);
		}

		[Test]
		public void ConstructorWithEmptyDescription() {
			var desc = new BufferDescription();
			var buffer = ToDispose(new MyBuffer(m_device, desc));
			Assert.IsNull(buffer.Buffer);
			Assert.AreEqual(desc, buffer.Description);
			Assert.False(buffer.CanWrite);
			Assert.False(buffer.CanRead);
		}

		[Test]
		public void ConstructorWithArrayAndDescription() {
			var desc = new BufferDescription {
				Usage = ResourceUsage.Staging,
				BindFlags = BindFlags.None,
				OptionFlags = ResourceOptionFlags.None,
				CpuAccessFlags = CpuAccessFlags.Read,
				SizeInBytes = sizeof(int) * 3,
				StructureByteStride = sizeof(int),
			};

			var buffer = ToDispose(new MyBuffer(m_device, new[] {10, 20, 30}, desc));

			Assert.IsNotNull(buffer.Buffer);
			Assert.AreEqual(desc, buffer.Description);
			Assert.False(buffer.CanWrite);
			Assert.True(buffer.CanRead);
			var data = buffer.TestRead();
			Assert.AreEqual(data, 10);
		}

		[Test]
		public void ReadableBuffer() {
			var desc = new BufferDescription {
				Usage = ResourceUsage.Staging,
				BindFlags = BindFlags.None,
				OptionFlags = ResourceOptionFlags.None,
				CpuAccessFlags = CpuAccessFlags.Read,
				SizeInBytes = sizeof(int),
				StructureByteStride = sizeof(int),
			};

			var buffer = ToDispose(new MyBuffer(m_device, 10, desc));
			Assert.IsNotNull(buffer.Buffer);
			Assert.AreEqual(desc, buffer.Description);
			Assert.False(buffer.CanWrite);
			Assert.True(buffer.CanRead);
			Assert.AreEqual(10, buffer.TestRead());
		}

		[Test]
		public void WritableBuffer() {
			var desc = new BufferDescription {
				Usage = ResourceUsage.Dynamic,
				BindFlags = BindFlags.VertexBuffer,
				OptionFlags = ResourceOptionFlags.None,
				CpuAccessFlags = CpuAccessFlags.Write,
				SizeInBytes = sizeof(int),
				StructureByteStride = sizeof(int),
			};

			var buffer = ToDispose(new MyBuffer(m_device, 10, desc));
			Assert.IsNotNull(buffer.Buffer);
			Assert.AreEqual(desc, buffer.Description);
			Assert.True(buffer.CanWrite);
			Assert.False(buffer.CanRead);
			buffer.TestWrite(5);
			var result = ReadBufferSingle(buffer);
			Assert.AreEqual(5, result);
		}

		[Test]
		public void CannoOpenBufferTwiceWithoutClosing() {
			var desc = new BufferDescription {
				Usage = ResourceUsage.Dynamic,
				BindFlags = BindFlags.VertexBuffer,
				OptionFlags = ResourceOptionFlags.None,
				CpuAccessFlags = CpuAccessFlags.Write,
				SizeInBytes = sizeof(int),
				StructureByteStride = sizeof(int),
			};

			var buffer = ToDispose(new MyBuffer(m_device, 10, desc));
			Assert.Throws<Exception>(() => {
				buffer.TestOpenTwiceShouldThrowError();
			});
		}
	}
}