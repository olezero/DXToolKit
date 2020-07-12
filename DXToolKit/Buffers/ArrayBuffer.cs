using System;
using SharpDX.Direct3D11;

namespace DXToolKit {
	/// <summary>
	/// A buffer the contains an array of T (Vertex buffers, index buffers, etc)
	/// </summary>
	/// <typeparam name="T">A sequential aligned structure</typeparam>
	public class ArrayBuffer<T> : DXBuffer<T> where T : struct {
		private int m_elementCount;

		/// <summary>
		/// Gets the total amount of elements in the array buffer.
		/// </summary>
		public int ElementCount => m_elementCount;

		/// <summary>
		/// Creates a new array buffer based on input description
		/// </summary>
		/// <param name="device">Base device used to create the buffer</param>
		/// <param name="elementCount">Number of elements to allocate space for</param>
		/// <param name="description">Description of the buffer</param>
		public ArrayBuffer(GraphicsDevice device, int elementCount, BufferDescription description) : base(device) {
			m_elementCount = elementCount;
			CreateBuffer(description);
		}

		/// <summary>
		/// Creates a new array buffer that contains input data.
		/// </summary>
		/// <param name="device">Base device used to create the buffer</param>
		/// <param name="data">The data to initialize the buffer with</param>
		/// <param name="description">The description of the buffer</param>
		public ArrayBuffer(GraphicsDevice device, T[] data, BufferDescription description) : base(device) {
			m_elementCount = data.Length;
			CreateBuffer(data, description);
		}

		/// <summary>
		/// Writes data to the buffer. Existing data is overwritten.
		/// Allows different element count, but this will take some processor power since the buffer will have to be recreated.
		/// </summary>
		/// <param name="data">The data to write</param>
		/// <exception cref="Exception">Throws an exception if buffer cant be written to by the CPU</exception>
		public void WriteRange(T[] data) {
			if (!CanWrite) throw new Exception("Buffer cannot be written to by the CPU");

			// Different length of input data
			if (m_elementCount != data.Length) {
				// Update element count
				m_elementCount = data.Length;
				// Create new buffer and write the new data to it.
				CreateBuffer(data);
			} else {
				// Same size of data as buffer, write directly to the buffer.
				OpenWrite().WriteRange(data);
				// Close the buffer.
				CloseBuffer();
			}
		}

		/// <summary>
		/// Reads all the data from the buffer.
		/// </summary>
		/// <returns>Data in the buffer</returns>
		/// <exception cref="Exception">Throws an exception if buffer cant be read by the CPU</exception>
		public T[] ReadRange() {
			if (!CanRead) throw new Exception("Buffer cannot be read by the CPU");
			var stream = OpenRead();
			var result = stream.ReadRange<T>(m_elementCount);
			CloseBuffer();
			return result;
		}
	}
}