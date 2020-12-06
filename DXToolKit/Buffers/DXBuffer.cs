using System;
using SharpDX;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace DXToolKit {
	/// <summary>
	/// Base class for DirectX buffers
	/// </summary>
	/// <typeparam name="T">Sequential aligned structure to send to the GPU</typeparam>
	public abstract class DXBuffer<T> : DeviceComponent where T : struct {
		/// <summary>
		/// Buffer object used by DirectX
		/// </summary>
		protected Buffer m_buffer;

		/// <summary>
		/// Buffer description used by DirectX
		/// </summary>
		protected BufferDescription m_bufferDescription;

		/// <summary>
		/// Event fired when buffer is created
		/// </summary>
		protected event Action OnBufferCreated;

		/// <summary>
		/// Data stream for writing to the buffer, useful so it does not need to be recreated every time the buffer is updated
		/// </summary>
		private DataStream m_stream;

		/// <summary>
		/// Gets a reference to the underlying buffer.
		/// </summary>
		public Buffer Buffer => m_buffer;

		/// <summary>
		/// Gets the description of the buffer.
		/// </summary>
		public BufferDescription Description => m_bufferDescription;

		/// <summary>
		/// Gets a value indicating if the buffer can be read by the CPU
		/// </summary>
		public bool CanRead => m_bufferDescription.CpuAccessFlags.HasFlag(CpuAccessFlags.Read);

		/// <summary>
		/// Gets a value indicating if the buffer can be written to by the CPU
		/// </summary>
		public bool CanWrite => m_bufferDescription.CpuAccessFlags.HasFlag(CpuAccessFlags.Write);


		/// <summary>
		/// Creates an empty buffer object without description, use CreateBuffer(description) to initialize
		/// </summary>
		/// <param name="device">Device used for creation</param>
		protected DXBuffer(GraphicsDevice device) : base(device) { }

		/// <summary>
		/// Creates a new dx buffer with input description, does not initialize the underlying buffer object.
		/// Use CreateBuffer methods to set underlying buffer.
		/// </summary>
		/// <param name="device">Device used for creation</param>
		/// <param name="description">Description of the buffer</param>
		protected DXBuffer(GraphicsDevice device, BufferDescription description) : base(device) {
			m_bufferDescription = description;
		}

		/// <summary>
		/// Creates a new buffer with input data.
		/// </summary>
		/// <param name="device">Device used for creation</param>
		/// <param name="data">Data to set in the buffer</param>
		/// <param name="description">Description of the buffer</param>
		protected DXBuffer(GraphicsDevice device, T data, BufferDescription description) : base(device) {
			CreateBuffer(data, description);
		}

		/// <summary>
		/// Creates a new buffer with input data.
		/// </summary>
		/// <param name="device">Device used for creation</param>
		/// <param name="data">Data to set in the buffer</param>
		/// <param name="description">Description of the buffer</param>
		protected DXBuffer(GraphicsDevice device, T[] data, BufferDescription description) : base(device) {
			CreateBuffer(data, description);
		}

		/// <summary>
		/// Creates a new buffer based on input description
		/// If description is omitted the stored buffer description will be used
		/// </summary>
		/// <param name="description">BufferDescription to define the buffer</param>
		protected void CreateBuffer(BufferDescription? description = null) {
			// Set the description.
			SetDescription(description);
			// Dispose of old buffer
			m_buffer?.Dispose();
			// Create new buffer with new description
			m_buffer = new Buffer(m_device, m_bufferDescription);
			// Send a message that the buffer has been created.
			OnBufferCreated?.Invoke();
		}

		/// <summary>
		/// Creates a new buffer based on input description and initializes it with the input data struct
		/// If description is omitted the stored buffer description will be used
		/// </summary>
		/// <param name="data">The data to initialize the buffer with</param>
		/// <param name="description">BufferDescription to define the buffer</param>
		protected void CreateBuffer(T data, BufferDescription? description = null) {
			// Copy description if its not null
			SetDescription(description);
			// Make sure the size is correct based on input data.
			m_bufferDescription.SizeInBytes = Utilities.SizeOf<T>();
			m_bufferDescription.StructureByteStride = Utilities.SizeOf<T>();
			// Dispose of old buffer
			m_buffer?.Dispose();
			// Create new buffer with input data
			m_buffer = Buffer.Create(m_device, ref data, m_bufferDescription);
			// Send a message that the buffer has been created.
			OnBufferCreated?.Invoke();
		}

		/// <summary>
		/// Creates a new buffer based on input description and initializes it with the input data struct
		/// If description is omitted the stored buffer description will be used
		/// </summary>
		/// <param name="data">The data to initialize the buffer with</param>
		/// <param name="description">BufferDescription to define the buffer</param>
		protected void CreateBuffer(T[] data, BufferDescription? description = null) {
			// Copy description if its not null
			SetDescription(description);
			// Make sure the size is correct based on input data.
			m_bufferDescription.SizeInBytes = Utilities.SizeOf(data);
			m_bufferDescription.StructureByteStride = Utilities.SizeOf<T>();
			// Dispose of old buffer
			m_buffer?.Dispose();
			// Create new buffer with input data
			m_buffer = Buffer.Create(m_device, data, m_bufferDescription);
			// Send a message that the buffer has been created.
			OnBufferCreated?.Invoke();
		}

		/// <summary>
		/// Opens the buffer to allow for CPU access.
		/// </summary>
		/// <exception cref="Exception">Throws an exception if the buffer is not closed before trying to open.</exception>
		protected DataStream OpenBuffer(MapMode mode, MapFlags flags) {
			// Make sure the stream is disposed
			if (m_stream != null) throw new Exception("Stream was not null, call Closebuffer() before trying to open it again");
			// Map the buffer on the device context
			m_context.MapSubresource(m_buffer, mode, flags, out m_stream);
			// Return the stream
			return m_stream;
		}

		/// <summary>
		/// Opens the buffer for CPU write access
		/// </summary>
		protected DataStream OpenWrite(MapMode mode = MapMode.WriteDiscard, MapFlags flags = MapFlags.None) {
			// Open the buffer with input parameters
			return OpenBuffer(mode, flags);
		}

		/// <summary>
		/// Opens the buffer for CPU read access
		/// </summary>
		protected DataStream OpenRead(MapMode mode = MapMode.Read, MapFlags flags = MapFlags.None) {
			// Open the buffer with the input parameters
			return OpenBuffer(mode, flags);
		}

		/// <summary>
		/// Closes the buffer to allow the GPU to read it again.
		/// </summary>
		protected void CloseBuffer(int subresource = 0) {
			// Close the buffer and dispose of the stream
			m_context.UnmapSubresource(m_buffer, subresource);
			m_stream?.Dispose();
			m_stream = null;
		}

		/// <summary>
		/// Releases all unmanaged resources held by the buffer
		/// </summary>
		protected override void OnDispose() {
			Utilities.Dispose(ref m_buffer);
			Utilities.Dispose(ref m_stream);
		}

		private void SetDescription(BufferDescription? description) {
			if (description != null) {
				m_bufferDescription = (BufferDescription) description;
			}
		}

		/// <summary>
		/// Implicit operator overload to a normal buffer
		/// </summary>
		public static implicit operator Buffer(DXBuffer<T> buffer) => buffer.m_buffer;
	}
}