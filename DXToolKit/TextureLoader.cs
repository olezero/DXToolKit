using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Rectangle = System.Drawing.Rectangle;

namespace DXToolKit {
	public static class TextureLoader {
		public struct ImageData {
			public int Width;
			public int Height;
			public DataRectangle Data;
		}

		public static Texture2D FromFile(GraphicsDevice device, string filename, ResourceUsage usage = ResourceUsage.Default, BindFlags bindFlags = BindFlags.ShaderResource, CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None) {
			var data = LoadImage(filename);
			var texture = new Texture2D(device, new Texture2DDescription {
				Height = data.Height,
				Width = data.Width,
				Format = Format.R8G8B8A8_UNorm,
				Usage = ResourceUsage.Default,
				ArraySize = 1,
				BindFlags = BindFlags.ShaderResource,
				MipLevels = 1,
				OptionFlags = ResourceOptionFlags.None,
				SampleDescription = new SampleDescription(1, 0),
				CpuAccessFlags = CpuAccessFlags.None,
			}, data.Data);
			return texture;
		}


		public static Texture2D FromFile(GraphicsDevice device, string filename, Texture2DDescription description) {
			var data = LoadImage(filename);
			description.Width = data.Width;
			description.Height = data.Height;
			var texture = new Texture2D(device, description, data.Data);
			return texture;
		}

		public static ShaderResourceView ShaderResourceFromFile(GraphicsDevice device, string filename) {
			var tex = FromFile(device, filename);
			var shaderResource = new ShaderResourceView(device, tex);
			tex?.Dispose();
			return shaderResource;
		}

		public static ShaderResourceView SRVFromFile(GraphicsDevice device, string filename, bool generateMipmaps = true, ResourceUsage usage = ResourceUsage.Default, BindFlags bindFlags = BindFlags.ShaderResource, CpuAccessFlags cpuAccessFlags = CpuAccessFlags.None) {
			// Load image data
			var imageData = LoadImage(filename, out var width, out var height);

			// Create texture description
			var textureDescription = new Texture2DDescription {
				Width = width,
				Height = height,
				Format = Format.R8G8B8A8_UNorm,
				Usage = usage,
				ArraySize = 1,
				BindFlags = bindFlags,
				MipLevels = 1,
				OptionFlags = ResourceOptionFlags.None,
				SampleDescription = new SampleDescription(1, 0),
				CpuAccessFlags = cpuAccessFlags,
			};

			// If generating mip maps, prepare texture for automatic generation
			if (generateMipmaps) {
				textureDescription.MipLevels = 0;
				textureDescription.BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget;
				textureDescription.OptionFlags = ResourceOptionFlags.GenerateMipMaps;
			}

			// Create texture to store data
			var tex = new Texture2D(device, textureDescription);

			// Update the image width data
			device.Context.UpdateSubresource(imageData, tex, 0, width * 4);

			// Create SRV
			var srv = new ShaderResourceView(device, tex);

			if (generateMipmaps) {
				// Generate mip mips on the srv
				device.Context.GenerateMips(srv);

				// Setup new texture description for the final SRV
				textureDescription = new Texture2DDescription {
					Width = width,
					Height = height,
					Format = Format.R8G8B8A8_UNorm,
					Usage = usage,
					ArraySize = 1,
					BindFlags = bindFlags,
					MipLevels = tex.Description.MipLevels,
					OptionFlags = ResourceOptionFlags.None,
					SampleDescription = new SampleDescription(1, 0),
					CpuAccessFlags = cpuAccessFlags,
				};

				// Create new texture and copy mip mapped texture to it.
				var mipmapTex = new Texture2D(device, textureDescription);
				device.Context.CopyResource(tex, mipmapTex);

				// Create result mipmapSRV
				var mipmapSRV = new ShaderResourceView(device, mipmapTex);

				// Dispose of temp resources
				mipmapTex?.Dispose();
				srv?.Dispose();

				// Assign mip mapped SRV to the result SRV
				srv = mipmapSRV;
			}

			// Dispose of temp texture
			tex?.Dispose();

			// Return result
			return srv;
		}

		public static ImageData LoadImage(string filename) {
			var result = new ImageData();

			unsafe {
				if (Image.FromFile(filename) is Bitmap image) {
					// Load image from disk
					var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
					// Calculate total byte count in image
					var byteCount = bitmapData.Stride * bitmapData.Height;

					// Create two arrays to store input BGRA colors and converted RGBA colors
					var BGRAImageData = new byte[byteCount];
					var RGBAImageData = new byte[byteCount];

					// Copy bytes from loaded image to byte array
					Marshal.Copy(bitmapData.Scan0, BGRAImageData, 0, BGRAImageData.Length);

					for (int i = 0; i < BGRAImageData.Length; i += 4) {
						RGBAImageData[i] = BGRAImageData[i + 2];
						RGBAImageData[i + 1] = BGRAImageData[i + 1];
						RGBAImageData[i + 2] = BGRAImageData[i];
						RGBAImageData[i + 3] = BGRAImageData[i + 3];
					}

					// Get a pointer to the start of the array
					IntPtr intPtr;
					fixed (byte* ptr = RGBAImageData) {
						intPtr = (IntPtr) ptr;
					}

					// Null check
					if (intPtr == IntPtr.Zero) {
						throw new NullReferenceException();
					}

					// Assign values to result data
					result.Width = bitmapData.Width;
					result.Height = bitmapData.Height;
					result.Data = new DataRectangle(intPtr, bitmapData.Width * 4);

					// Dispose of any temporary data
					image?.Dispose();
					bitmapData = null;
				}
			}

			return result;
		}

		public static byte[] LoadImage(string filename, out int width, out int height) {
			width = -1;
			height = -1;

			if (Image.FromFile(filename) is Bitmap image) {
				// Load image from disk
				var bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

				// Calculate total byte count in image
				var byteCount = bitmapData.Stride * bitmapData.Height;

				// Create two arrays to store input BGRA colors and converted RGBA colors
				var BGRAImageData = new byte[byteCount];
				var RGBAImageData = new byte[byteCount];

				// Copy bytes from loaded image to byte array
				Marshal.Copy(bitmapData.Scan0, BGRAImageData, 0, BGRAImageData.Length);

				// Convert from BGRA to RGBA
				Parallel.For(0, BGRAImageData.Length / 4, index => {
					var i = index * 4;
					RGBAImageData[i + 0] = BGRAImageData[i + 2];
					RGBAImageData[i + 1] = BGRAImageData[i + 1];
					RGBAImageData[i + 2] = BGRAImageData[i + 0];
					RGBAImageData[i + 3] = BGRAImageData[i + 3];
				});

				// Set width and height
				width = bitmapData.Width;
				height = bitmapData.Height;

				// Dispose of image
				image?.Dispose();

				// return data
				return RGBAImageData;
			}

			return null;
		}
	}
}