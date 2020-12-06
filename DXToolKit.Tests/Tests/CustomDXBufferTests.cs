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
		}

		[Test]
		public void sandbox() {
			var desc = new BufferDescription {
				Usage = ResourceUsage.Staging,
				BindFlags = BindFlags.None,
				OptionFlags = ResourceOptionFlags.None,
				CpuAccessFlags = CpuAccessFlags.Read | CpuAccessFlags.Write,
				SizeInBytes = sizeof(int),
				StructureByteStride = 0,
			};
			var buffer = ToDispose(new MyBuffer(m_device, 10, desc));
			Assert.IsNotNull(buffer.Buffer);
			Assert.AreEqual(desc, buffer.Description);
			Assert.True(buffer.CanWrite);
			Assert.True(buffer.CanRead);
			
			
			//Assert.AreEqual(10, buffer.TestRead());

			buffer.TestWrite(20);
			
			//buffer.TestWrite(20);
			//Assert.AreEqual(20, buffer.TestRead());
		}


		[Test]
		public void SimpleConstructor() {
			var desc = new BufferDescription();
			var buffer = ToDispose(new MyBuffer(m_device));
			Assert.IsNull(buffer.Buffer);
			Assert.AreEqual(desc, buffer.Description);
			Assert.False(buffer.CanWrite);
			Assert.False(buffer.CanRead);
		}

		[Test]
		public void SimpleConstructorWithEmptyDescription() {
			var desc = new BufferDescription();
			var buffer = ToDispose(new MyBuffer(m_device, desc));
			Assert.IsNull(buffer.Buffer);
			Assert.AreEqual(desc, buffer.Description);
			Assert.False(buffer.CanWrite);
			Assert.False(buffer.CanRead);
		}
	}
}