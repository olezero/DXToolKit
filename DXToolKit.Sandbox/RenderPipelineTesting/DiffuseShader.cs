using System;
using System.IO;
using System.Runtime.InteropServices;
using DXToolKit.Engine;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace DXToolKit.Sandbox {
	public class DiffuseShader : DeviceComponent {
		public bool IsValid { get; private set; }

		[StructLayout(LayoutKind.Sequential)]
		private struct MatrixBufferType {
			public Matrix World;
			public Matrix View;
			public Matrix Proj;
		}

		private InputLayout m_inputLayout;
		private VertexShader m_vertexShader;
		private PixelShader m_pixelShader;
		private FileSystemWatcher m_watcher;
		private string m_shaderErrorMessage;

		private ConstantBuffer<MatrixBufferType> m_matrixBuffer;
		private MatrixBufferType m_matrices;

		public DiffuseShader(GraphicsDevice device) : base(device) {
			m_matrixBuffer = new ConstantBuffer<MatrixBufferType>(m_device, m_matrices);
			m_matrices = new MatrixBufferType();

			m_watcher = LiveReload.CreateWatcher(@"C:\Programming\HLSLShaders\diffuse.fx", file => {
				IsValid = false;
				var vs = ShaderCompiler.TryCompile<VertexShader>(m_device, file, "VS", out var vsCompileResult);
				var ps = ShaderCompiler.TryCompile<PixelShader>(m_device, file, "PS", out var psCompileResult);

				if (vsCompileResult.Success && psCompileResult.Success) {
					InputLayout inputLayout = null;
					try {
						// Create new input layout based on shader
						inputLayout = new InputLayout(m_device, vsCompileResult.Bytecode, new[] {
							new InputElement("POSITION", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0),
							new InputElement("NORMAL", 0, Format.R32G32B32_Float, InputElement.AppendAligned, 0),
							new InputElement("TEXCOORD", 0, Format.R32G32_Float, InputElement.AppendAligned, 0),
						});
					}
					catch (Exception e) {
						Utilities.Dispose(ref vs);
						Utilities.Dispose(ref ps);
						Utilities.Dispose(ref inputLayout);
						m_shaderErrorMessage = "Error in inputlayout creation: " + e.Message;
						IsValid = false;
						return;
					}

					// Delete already stored data
					Utilities.Dispose(ref m_vertexShader);
					Utilities.Dispose(ref m_pixelShader);
					Utilities.Dispose(ref m_inputLayout);

					// Load shaders and set is valid to true
					m_vertexShader = vs;
					m_pixelShader = ps;
					m_inputLayout = inputLayout;

					// is valid
					IsValid = true;
				} else {
					// Else delete used data
					Utilities.Dispose(ref vs);
					Utilities.Dispose(ref ps);

					// Could print error messages
					m_shaderErrorMessage = $"VS: {vsCompileResult.Message}\nPS: {psCompileResult.Message}";

					// Is not valid
					IsValid = false;
				}
			}, true);
		}

		public void Render(Matrix world, Matrix view, Matrix projection, int indexCount) {
			if (!IsValid) {
				Debug.Log(m_shaderErrorMessage);
				return;
			}

			m_matrices.World = Matrix.Transpose(world);
			m_matrices.View = Matrix.Transpose(view);
			m_matrices.Proj = Matrix.Transpose(projection);
			m_matrixBuffer.Write(m_matrices);

			m_context.VertexShader.Set(m_vertexShader);
			m_context.VertexShader.SetConstantBuffer(0, m_matrixBuffer);
			m_context.PixelShader.Set(m_pixelShader);
			m_context.InputAssembler.InputLayout = m_inputLayout;
			m_context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
			m_context.DrawIndexed(indexCount, 0, 0);
		}

		protected override void OnDispose() {
			Utilities.Dispose(ref m_matrixBuffer);
			Utilities.Dispose(ref m_vertexShader);
			Utilities.Dispose(ref m_pixelShader);
			Utilities.Dispose(ref m_inputLayout);
			Utilities.Dispose(ref m_watcher);
		}
	}
}