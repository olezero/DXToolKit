using System;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace DXToolKit {
	public static class TextureHelper {
		public static ShaderResourceView CreateSRV(GraphicsDevice device, ref Color[] data, int width, int height, BindFlags bindFlags = BindFlags.ShaderResource) {
			var tex = CreateTexture2D(device, ref data, width, height, bindFlags);
			var srv = new ShaderResourceView(device, tex);
			tex?.Dispose();
			return srv;
		}

		public static ShaderResourceView CreateSRV(GraphicsDevice device, ref Vector4[] data, int width, int height, BindFlags bindFlags = BindFlags.ShaderResource) {
			var tex = CreateTexture2D(device, ref data, width, height, bindFlags);
			var srv = new ShaderResourceView(device, tex);
			tex?.Dispose();
			return srv;
		}

		public static Texture2D CreateTexture2D(GraphicsDevice device, ref Color[] data, int width, int height, BindFlags bindFlags = BindFlags.ShaderResource) {
			return CreateTexture(device, ref data, new Texture2DDescription() {
				Usage = ResourceUsage.Default,
				Format = Format.R8G8B8A8_UNorm,
				Width = width,
				Height = height,
				ArraySize = 1,
				BindFlags = bindFlags,
				MipLevels = 1,
				OptionFlags = ResourceOptionFlags.None,
				SampleDescription = new SampleDescription(1, 0),
				CpuAccessFlags = CpuAccessFlags.None,
			});
		}

		public static Texture2D CreateTexture2D(GraphicsDevice device, ref Vector4[] data, int width, int height, BindFlags bindFlags = BindFlags.ShaderResource) {
			return CreateTexture(device, ref data, new Texture2DDescription() {
				Usage = ResourceUsage.Default,
				Format = Format.R32G32B32A32_Float,
				Width = width,
				Height = height,
				ArraySize = 1,
				BindFlags = bindFlags,
				MipLevels = 1,
				OptionFlags = ResourceOptionFlags.None,
				SampleDescription = new SampleDescription(1, 0),
				CpuAccessFlags = CpuAccessFlags.None,
			});
		}

		private static unsafe Texture2D CreateTexture(GraphicsDevice device, ref Color[] data, Texture2DDescription description) {
			fixed (Color* ptr = data) {
				var dataRect = new DataRectangle((IntPtr) ptr, description.Width * 4);
				var texture2D = new Texture2D(device, description, dataRect);
				return texture2D;
			}
		}

		private static unsafe Texture2D CreateTexture(GraphicsDevice device, ref Vector4[] data, Texture2DDescription description) {
			fixed (Vector4* ptr = data) {
				var dataRect = new DataRectangle((IntPtr) ptr, description.Width * 16);
				var texture2D = new Texture2D(device, description, dataRect);
				return texture2D;
			}
		}
	}
}