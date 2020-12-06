using System;
using System.IO;
using System.Runtime.InteropServices;
using DXToolKit.Engine;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;

namespace DXToolKit.Sandbox {
	public class MatrixBuffer : DeviceComponent {
		[StructLayout(LayoutKind.Sequential)]
		private struct MatrixBufferType {
			public Matrix World;
			public Matrix View;
			public Matrix Proj;
		}

		private MatrixBufferType m_matrices;
		private ConstantBuffer<MatrixBufferType> m_buffer;

		public MatrixBuffer(GraphicsDevice device) : base(device) {
			m_buffer = new ConstantBuffer<MatrixBufferType>(m_device);
			m_matrices = new MatrixBufferType {
				World = Matrix.Identity,
				View = Matrix.Identity,
				Proj = Matrix.Identity,
			};
		}


		public void Set(Matrix world, Matrix view, Matrix proj, bool transpose = true) {
			if (transpose) {
				m_matrices.World = Matrix.Transpose(world);
				m_matrices.View = Matrix.Transpose(view);
				m_matrices.Proj = Matrix.Transpose(proj);
			} else {
				m_matrices.World = world;
				m_matrices.View = view;
				m_matrices.Proj = proj;
			}

			m_buffer.Write(m_matrices);
		}

		public void Set(ref Matrix world, ref Matrix view, ref Matrix proj, bool transpose = true) {
			if (transpose) {
				m_matrices.World = Matrix.Transpose(world);
				m_matrices.View = Matrix.Transpose(view);
				m_matrices.Proj = Matrix.Transpose(proj);
			} else {
				m_matrices.World = world;
				m_matrices.View = view;
				m_matrices.Proj = proj;
			}

			m_buffer.Write(m_matrices);
		}

		public void Apply(CommonShaderStage stage, int slot = 0) {
			stage.SetConstantBuffer(slot, m_buffer);
		}

		protected override void OnDispose() {
			Utilities.Dispose(ref m_buffer);
		}
	}

	public class Shader : DeviceComponent {
		private string m_shaderErrorMessage = null;
		private bool m_isValid = false;
		private FileSystemWatcher m_watcher;
		private VertexShader m_vertexShader;
		private PixelShader m_pixelShader;
		private GeometryShader m_geometryShader;
		private ShaderDescription m_description;
		private ShaderBytecode m_vsByteCode;
		private ShaderBytecode m_psByteCode;
		private ShaderBytecode m_gsByteCode;

		public ShaderBytecode VertexShaderBytecode => m_vsByteCode;
		public ShaderBytecode PixelShaderBytecode => m_psByteCode;
		public ShaderBytecode GeometryShaderBytecode => m_gsByteCode;

		public VertexShader VertexShader => m_vertexShader;

		public PixelShader PixelShader => m_pixelShader;

		public GeometryShader GeometryShader => m_geometryShader;

		public bool IsValid => m_isValid;

		public delegate void VSEventHandler(VertexShaderStage vertexShaderStage);

		public delegate void PSEventHandler(PixelShaderStage pixelShaderStage);

		public delegate void GSEventHandler(GeometryShaderStage geometryShaderStage);

		public delegate void CompileEventHandler(Shader sender, bool success, string errorMessage = null);

		public event VSEventHandler VertexShade;
		public event PSEventHandler PixelShade;
		public event GSEventHandler GeometryShade;
		public event Action CompileBegin;
		public event CompileEventHandler CompileEnd;

		protected virtual void OnVertexShade(VertexShaderStage vertexShaderStage) => VertexShade?.Invoke(vertexShaderStage);
		protected virtual void OnPixelShade(PixelShaderStage pixelShaderStage) => PixelShade?.Invoke(pixelShaderStage);
		protected virtual void OnGeometryShader(GeometryShaderStage geometryShaderStage) => GeometryShade?.Invoke(geometryShaderStage);
		protected virtual void OnCompileBegin() => CompileBegin?.Invoke();
		protected virtual void OnCompileEnd(Shader sender, bool success, string errorMessage = null) => CompileEnd?.Invoke(sender, success, errorMessage);

		public Shader(GraphicsDevice device, ShaderDescription description) : base(device) {
			m_description = description;
			Compile();
		}

		public void EnableWatcher() {
			Utilities.Dispose(ref m_watcher);
			m_watcher = LiveReload.CreateWatcher(m_description.file, file => {
				Compile();
			});
		}

		// Apply whatever shaders are loaded to the current context
		// Return a value indicating if the shader is valid
		public bool Apply(DeviceContext deviceContext = null) {
			if (!m_isValid) {
				// Log error messages if something is wrong
				if (m_description.DebugLog) {
					if (!string.IsNullOrEmpty(m_shaderErrorMessage)) {
						Debug.Log(m_shaderErrorMessage);
					} else {
						Debug.Log("Error in shader");
					}
				}

				return false;
			}

			var context = deviceContext ?? m_context;

			// Set parameters
			if (m_vertexShader != null) {
				context.VertexShader.Set(m_vertexShader);
				OnVertexShade(context.VertexShader);
			}

			if (m_pixelShader != null) {
				context.PixelShader.Set(m_pixelShader);
				OnPixelShade(context.PixelShader);
			}

			if (m_geometryShader != null) {
				context.GeometryShader.Set(m_geometryShader);
				OnGeometryShader(context.GeometryShader);
			}

			return true;
		}

		private void Compile() {
			OnCompileBegin();
			string errorMessage = "";
			m_isValid = false;
			VertexShader vs = null;
			PixelShader ps = null;
			GeometryShader gs = null;

			ShaderCompilationResult vsResult = null;
			ShaderCompilationResult psResult = null;
			ShaderCompilationResult gsResult = null;

			if (m_description.vsEntry != null) {
				vs = ShaderCompiler.TryCompile<VertexShader>(m_device, m_description.file, m_description.vsEntry, out vsResult);
			}

			if (m_description.psEntry != null) {
				ps = ShaderCompiler.TryCompile<PixelShader>(m_device, m_description.file, m_description.psEntry, out psResult);
			}

			if (m_description.gsEntry != null) {
				gs = ShaderCompiler.TryCompile<GeometryShader>(m_device, m_description.file, m_description.gsEntry, out gsResult);
			}

			if (vsResult != null) {
				if (vsResult.Success == false) {
					errorMessage += vsResult.Message + "\n";
				} else {
					Utilities.Dispose(ref m_vsByteCode);
					m_vsByteCode = vsResult.Bytecode;
				}
			}

			if (psResult != null) {
				if (psResult.Success == false) {
					errorMessage += psResult.Message + "\n";
				} else {
					Utilities.Dispose(ref m_psByteCode);
					m_psByteCode = psResult.Bytecode;
				}
			}

			if (gsResult != null) {
				if (gsResult.Success == false) {
					errorMessage += gsResult.Message + "\n";
				} else {
					Utilities.Dispose(ref m_gsByteCode);
					m_gsByteCode = gsResult.Bytecode;
				}
			}

			if (string.IsNullOrEmpty(errorMessage)) {
				Utilities.Dispose(ref m_vertexShader);
				Utilities.Dispose(ref m_pixelShader);
				Utilities.Dispose(ref m_geometryShader);

				// VS/PS/GS can be null, but thats fine, since we null check before actually rendering using a shader
				m_vertexShader = vs;
				m_pixelShader = ps;
				m_geometryShader = gs;
				m_isValid = true;
				OnCompileEnd(this, true);
			} else {
				m_isValid = false;
				m_shaderErrorMessage = errorMessage;
				OnCompileEnd(this, false, errorMessage);
			}
		}


		protected override void OnDispose() {
			Utilities.Dispose(ref m_watcher);
			Utilities.Dispose(ref m_pixelShader);
			Utilities.Dispose(ref m_vertexShader);
			Utilities.Dispose(ref m_geometryShader);
		}
	}
}