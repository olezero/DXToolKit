using System;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace DXToolKit {
	public static class ShaderCompiler {
		/// <summary>
		/// Enum for readability in switch
		/// </summary>
		private enum ShaderType {
			VertexShader,
			PixelShader,
			GeometryShader,
			ComputeShader,
			DomainShader,
			HullShader,
		}

		/// <summary>
		/// Compiles a shader from a file, returning th byte code
		/// </summary>
		/// <param name="filename">The target fx/hlsl file</param>
		/// <param name="entry">The entry point of the shader</param>
		/// <param name="profile">The profile to use when compiling</param>
		/// <param name="flags">Shader flags used</param>
		/// <returns>Shader byte code for the shader</returns>
		public static ShaderBytecode FromFile(string filename, string entry, string profile, ShaderFlags flags = ShaderFlags.OptimizationLevel1) {
			// Create include
			using (var include = new FXInclueHandler(filename)) {
				// Compile and return the shader
				return ShaderBytecode.CompileFromFile(filename, entry, profile, flags, EffectFlags.None, null, include);
			}
		}


		/// <summary>
		/// Compiles shader byte code with the fx_5_0 profile
		/// </summary>
		/// <param name="filename">Target FX file</param>
		/// <param name="shaderFlags">Shader flags</param>
		/// <param name="effectFlags">Effect flags</param>
		/// <param name="defines">Shader defines</param>
		/// <returns>Byte code of compiled FX file</returns>
		public static ShaderBytecode FromFile(string filename, ShaderFlags shaderFlags = ShaderFlags.OptimizationLevel1, EffectFlags effectFlags = EffectFlags.None, ShaderMacro[] defines = null) {
			// Create include
			using (var include = new FXInclueHandler(filename)) {
				// Compile and return the shader
				return ShaderBytecode.CompileFromFile(filename, "fx_5_0", shaderFlags, EffectFlags.None, null, include);
			}
		}

		/// <summary>
		/// Tries to compile a shader
		/// </summary>
		/// <param name="device">Device used to create the shader</param>
		/// <param name="filename">Target file to compile</param>
		/// <param name="entrypoint">Entry point of the shader</param>
		/// <param name="result">Compilation result</param>
		/// <param name="flags">Shader flags to use</param>
		/// <typeparam name="T">The type of shader (VertexShader, PixelShader, GeometryShader, ComputeShader, DomainShader or HullShader)</typeparam>
		/// <returns>The compiled shader on success, or null on exception</returns>
		public static T TryCompile<T>(GraphicsDevice device, string filename, string entrypoint, out ShaderCompilationResult result, ShaderFlags flags = ShaderFlags.OptimizationLevel1) where T : DeviceChild {
			// Get the profile by the type
			var profile = GetProfileByType<T>(out var type);

			// Create empty compile result
			CompilationResult compileResult = null;

			// Create include handler
			var includeHandler = new FXInclueHandler(filename);

			// Create result shader compilation result
			result = new ShaderCompilationResult {
				Bytecode = null,
				Message = null,
				Success = false,
			};

			try {
				// Try to compile, this is where exceptions are thrown.
				compileResult = ShaderBytecode.CompileFromFile(filename, entrypoint, profile, flags, EffectFlags.None, null, includeHandler);

				// Set result variables as if everything succeeded
				result.Bytecode = compileResult;
				result.Message = compileResult.Message;

				// Trim result message if its not null
				if (result.Message != null) {
					result.Message = result.Message.Trim();
				}

				result.Success = true;
			}
			catch (Exception e) {
				// If compilation raised a exception, its probably a syntax error
				// Dispose of every used variable
				compileResult?.Dispose();
				includeHandler?.Dispose();

				// Set success to false
				result.Success = false;

				// Get exception message, which will be the compilation error message
				result.Message = e.Message;

				// Trim result message if its not null
				if (result.Message != null) {
					result.Message = result.Message.Trim();
				}

				// Dispose and set byte code to null
				result.Bytecode?.Dispose();
				result.Bytecode = null;

				// Return null.
				return null;
			}

			// If we get here, compilation succeeded

			// Create shader based on type of T
			var shader = CreateShaderByType<T>(device, type, compileResult.Bytecode);
			// Dispose of every used variable
			includeHandler?.Dispose();
			compileResult?.Dispose();
			// Return the shader
			return shader;
		}

		/// <summary>
		/// Tries to compile a shader
		/// </summary>
		/// <param name="device">Device used to create the shader</param>
		/// <param name="filename">Target shader file</param>
		/// <param name="entrypoint">Entry point of the shader</param>
		/// <param name="flags">Shader flags</param>
		/// <typeparam name="T">The type of shader (VertexShader, PixelShader, GeometryShader, ComputeShader, DomainShader or HullShader)</typeparam>
		/// <returns>Compiled shader on success, null if there was an error</returns>
		public static T TryCompile<T>(GraphicsDevice device, string filename, string entrypoint, ShaderFlags flags = ShaderFlags.OptimizationLevel1) where T : DeviceChild {
			var ret = TryCompile<T>(device, filename, entrypoint, out var result, flags);
			result?.Dispose();
			return ret;
		}

		/// <summary>
		/// Tries to compile a shader
		/// </summary>
		/// <param name="device">Device used to create the shader</param>
		/// <param name="shader">Reference to a shader. If compilation succeeds the old shader will be disposed and variable will be reassigned to the newly compiled shader. If it fails, the shader will remain untouched.</param>
		/// <param name="filename">Target shader file</param>
		/// <param name="entrypoint">Entry point of the shader</param>
		/// <param name="flags">Shader flags</param>
		/// <typeparam name="T">The type of shader (VertexShader, PixelShader, GeometryShader, ComputeShader, DomainShader or HullShader)</typeparam>
		public static void CompileFromFile<T>(GraphicsDevice device, ref T shader, string filename, string entrypoint, ShaderFlags flags = ShaderFlags.OptimizationLevel1) where T : DeviceChild {
			var result = TryCompile<T>(device, filename, entrypoint, flags);
			if (result != null) {
				// Dispose of old shader
				shader?.Dispose();
				// Reassign shader
				shader = result;
			}
		}

		/// <summary>
		/// Tries to compile a shader
		/// </summary>
		/// <param name="device">Device used to create the shader</param>
		/// <param name="shader">Reference to a shader. If compilation succeeds the old shader will be disposed and variable will be reassigned to the newly compiled shader. If it fails, the shader will remain untouched.</param>
		/// <param name="filename">Target shader file</param>
		/// <param name="entrypoint">Entry point of the shader</param>
		/// <param name="compilationResult">Compilation result with message, success and byte code</param>
		/// <param name="flags">Shader flags</param>
		/// <typeparam name="T">The type of shader (VertexShader, PixelShader, GeometryShader, ComputeShader, DomainShader or HullShader)</typeparam>
		public static void CompileFromFile<T>(GraphicsDevice device, ref T shader, string filename, string entrypoint, out ShaderCompilationResult compilationResult, ShaderFlags flags = ShaderFlags.OptimizationLevel1) where T : DeviceChild {
			var result = TryCompile<T>(device, filename, entrypoint, out compilationResult, flags);
			if (result != null) {
				// Dispose of old shader
				shader?.Dispose();
				// Reassign shader
				shader = result;
			}
		}

		/// <summary>
		/// Checks the input type and makes it more readable later
		/// </summary>
		private static string GetProfileByType<T>(out ShaderType shaderType) {
			var profile = "";
			var type = typeof(T);

			switch (type.Name) {
				case "VertexShader":
					profile = "vs_5_0";
					shaderType = ShaderType.VertexShader;
					break;
				case "PixelShader":
					profile = "ps_5_0";
					shaderType = ShaderType.PixelShader;
					break;
				case "GeometryShader":
					profile = "gs_5_0";
					shaderType = ShaderType.GeometryShader;
					break;
				case "ComputeShader":
					profile = "cs_5_0";
					shaderType = ShaderType.ComputeShader;
					break;
				case "DomainShader":
					profile = "ds_5_0";
					shaderType = ShaderType.DomainShader;
					break;
				case "HullShader":
					profile = "hs_5_0";
					shaderType = ShaderType.HullShader;
					break;
				default:
					throw new NotImplementedException("Shader type not implemented");
			}

			return profile;
		}

		/// <summary>
		/// Creates a shader based on type
		/// </summary>
		private static T CreateShaderByType<T>(GraphicsDevice device, ShaderType shaderType, byte[] bytecode) where T : DeviceChild {
			DeviceChild shader = null;
			switch (shaderType) {
				case ShaderType.VertexShader:
					shader = new VertexShader(device, bytecode);
					break;
				case ShaderType.PixelShader:
					shader = new PixelShader(device, bytecode);
					break;
				case ShaderType.GeometryShader:
					shader = new GeometryShader(device, bytecode);
					break;
				case ShaderType.ComputeShader:
					shader = new ComputeShader(device, bytecode);
					break;
				case ShaderType.DomainShader:
					shader = new DomainShader(device, bytecode);
					break;
				case ShaderType.HullShader:
					shader = new HullShader(device, bytecode);
					break;
				default:
					throw new NotImplementedException("Shader type not implemented");
			}

			return shader as T;
		}
	}
}