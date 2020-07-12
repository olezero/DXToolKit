using System;
using SharpDX.D3DCompiler;

namespace DXToolKit {
	/// <summary>
	/// Contains byte code and message for when a shader gets compiled.
	/// </summary>
	public class ShaderCompilationResult : IDisposable {
		/// <summary>
		/// Byte code of the shader
		/// </summary>
		public ShaderBytecode Bytecode;

		/// <summary>
		/// Compilation message
		/// </summary>
		public string Message;

		/// <summary>
		/// If the compilation succeeded
		/// </summary>
		public bool Success = false;

		/// <summary>
		/// Disposes of the result.
		/// </summary>
		public void Dispose() {
			Bytecode?.Dispose();
		}
	}
}