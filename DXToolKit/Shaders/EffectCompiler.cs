using System;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace DXToolKit {
	public static class EffectCompiler {
		public static Effect FromFile(GraphicsDevice device, string filename, ShaderFlags flags = ShaderFlags.OptimizationLevel1, EffectFlags effectFlags = EffectFlags.None, ShaderMacro[] defines = null) {
			var byteCode = ShaderCompiler.FromFile(filename, flags, effectFlags, defines);
			var effect = new Effect(device, byteCode, effectFlags, filename);
			return effect;
		}

		public static void TryCompile(GraphicsDevice device, ref Effect effect, string filename, out ShaderCompilationResult result, ShaderFlags flags = ShaderFlags.OptimizationLevel1, EffectFlags effectFlags = EffectFlags.None, ShaderMacro[] defines = null) {
			using (var fxInclude = new FXInclueHandler(filename)) {
				result = new ShaderCompilationResult {
					Bytecode = null,
					Message = null,
					Success = false,
				};

				try {
					var compile = ShaderBytecode.CompileFromFile(filename, "fx_5_0", flags, effectFlags, defines, fxInclude);
					result.Bytecode = compile.Bytecode;
					result.Message = compile.Message;
					if (result.Message != null && result.Message.Contains("warning X4717: Effects deprecated for D3DCompiler_47\n")) {
						result.Message = result.Message.Replace("warning X4717: Effects deprecated for D3DCompiler_47\n", "");
					}


					if (result.Message != null) {
						result.Message = result.Message.Trim();
					}

					// Set success to true at the end.
					result.Success = true;
				}
				catch (Exception e) {
					result.Message = e.Message;
					if (result.Message != null && result.Message.Contains("warning X4717: Effects deprecated for D3DCompiler_47\n")) {
						result.Message = result.Message.Replace("warning X4717: Effects deprecated for D3DCompiler_47\n", "");
					}

					if (result.Message != null) {
						result.Message = result.Message.Trim();
					}

					result.Bytecode = null;
					result.Success = false;
				}
			}

			if (result.Success) {
				effect?.Dispose();
				effect = new Effect(device, result.Bytecode, effectFlags, filename);
			}
		}
	}
}