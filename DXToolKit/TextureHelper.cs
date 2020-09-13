using System;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace DXToolKit {
	public static class TextureHelper {
		public static ShaderResourceView CreateSRV(GraphicsDevice device, ref Color[] data, int width, int height, BindFlags bindFlags = BindFlags.ShaderResource) {
			var tex = CreateTexture2D(device, ref data, width, height, bindFlags);
			var srv = new ShaderResourceView(device, tex);
			Utilities.Dispose(ref tex);
			return srv;
		}

		public static ShaderResourceView CreateSRV(GraphicsDevice device, ref Vector4[] data, int width, int height, BindFlags bindFlags = BindFlags.ShaderResource) {
			var tex = CreateTexture2D(device, ref data, width, height, bindFlags);
			var srv = new ShaderResourceView(device, tex);
			tex?.Dispose();
			return srv;
		}

		public static Texture2D CreateTexture2D(GraphicsDevice device, ref Color[] data, int width, int height, BindFlags bindFlags = BindFlags.ShaderResource) {
			return CreateTexture(device, ref data, new Texture2DDescription {
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
			return CreateTexture(device, ref data, new Texture2DDescription {
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

		public static ShaderResourceView CreateSRVArray(GraphicsDevice device, ref Color[][] data, int width, int height, int depth) {
			var texture = CreateTexture(device, ref data, new Texture2DDescription {
				Format = Format.R8G8B8A8_UNorm,
				Height = height,
				Width = width,
				ArraySize = depth,
				Usage = ResourceUsage.Default,
				BindFlags = BindFlags.ShaderResource,
				MipLevels = 1,
				OptionFlags = ResourceOptionFlags.None,
				SampleDescription = new SampleDescription(1, 0),
				CpuAccessFlags = CpuAccessFlags.None
			});
			var srv = new ShaderResourceView(device, texture);
			Utilities.Dispose(ref texture);
			return srv;
		}

		private static unsafe Texture2D CreateTexture(GraphicsDevice device, ref Color[][] data, Texture2DDescription description) {
			var dataRects = new DataRectangle[data.Length];
			for (int i = 0; i < dataRects.Length; i++) {
				fixed (Color* ptr = data[i]) {
					dataRects[i] = new DataRectangle((IntPtr) ptr, description.Width * 4);
				}
			}

			return new Texture2D(device, description, dataRects);
		}


		/// <summary>
		/// Creates a texture2DArray based on input shader resource view array
		/// </summary>
		/// <param name="device"></param>
		/// <param name="srvs"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <returns></returns>
		public static ShaderResourceView CreateSRVArray(GraphicsDevice device, ref ShaderResourceView[] srvs, int width, int height) {
			var texArray = new Texture2D(device, new Texture2DDescription {
				Format = srvs[0].Description.Format,
				Height = height,
				Width = width,
				ArraySize = srvs.Length,
				Usage = ResourceUsage.Default,
				BindFlags = BindFlags.ShaderResource,
				MipLevels = 1,
				OptionFlags = ResourceOptionFlags.None,
				SampleDescription = new SampleDescription(1, 0),
				CpuAccessFlags = CpuAccessFlags.None
			});

			for (int i = 0; i < srvs.Length; i++) {
				device.Context.CopySubresourceRegion(srvs[i].Resource, 0, null, texArray, i);
			}

			var srv = new ShaderResourceView(device, texArray);
			Utilities.Dispose(ref texArray);
			return srv;
		}
	}
}