using System.Threading.Tasks;
using DXToolKit.Engine;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace DXToolKit.Sandbox {
	public class NoiseSketch : Sketch {
		private const int TEXTURE_SIZE = 256;

		private OpenSimplex2S m_simplex2S;
		private OpenSimplex2F m_simplex2F;
		private SimplexNoise m_simplex;
		private TextureRenderer m_textureRenderer;
		private SpriteBatch m_spriteBatch;
		private Texture2D m_texture;
		private ShaderResourceView m_srv;
		private Color[] m_colors;
		private Noise m_noise;

		private float m_xOffset = 0.0F;
		private float m_yOffset = 0.0F;
		private float m_zOffset = 0.0F;

		protected override void OnLoad() {
			m_simplex = new SimplexNoise(0);
			m_simplex2F = new OpenSimplex2F(0);
			m_simplex2S = new OpenSimplex2S(0);
			m_textureRenderer = new TextureRenderer(m_device);
			m_spriteBatch = new SpriteBatch(m_device);

			m_colors = new Color[TEXTURE_SIZE * TEXTURE_SIZE];


			m_texture = new Texture2D(m_device, new Texture2DDescription() {
				Format = Format.R8G8B8A8_UNorm,
				Height = TEXTURE_SIZE,
				Width = TEXTURE_SIZE,
				Usage = ResourceUsage.Dynamic,
				ArraySize = 1,
				BindFlags = BindFlags.ShaderResource,
				MipLevels = 1,
				OptionFlags = ResourceOptionFlags.None,
				SampleDescription = new SampleDescription(1, 0),
				CpuAccessFlags = CpuAccessFlags.Write
			});
			m_srv = new ShaderResourceView(m_device, m_texture);
			m_noise = new Noise(0);


			m_noise.SetSimplexGenerator(SimplexGenerator.Smooth);

			// GenerateSimplex();
			// m_texture = TextureHelper.CreateTexture2D(m_device, ref m_colors, TEXTURE_SIZE, TEXTURE_SIZE);
			// m_srv = new ShaderResourceView(m_device, m_texture);
		}


		protected override void Update() {
			GenerateNoise(m_xOffset, m_yOffset, m_zOffset);
			m_xOffset += Time.DeltaTime;
			m_yOffset += Time.DeltaTime;
			m_zOffset += Time.DeltaTime;

			m_context.MapSubresource(m_texture, 0, 0, MapMode.WriteDiscard, MapFlags.None, out DataStream stream);
			stream.WriteRange(m_colors);
			m_context.UnmapSubresource(m_texture, 0);
			stream.Dispose();
		}


		protected override void Render() {
			m_spriteBatch.Draw(m_srv, new RectangleF(256, 256, TEXTURE_SIZE, TEXTURE_SIZE));
			m_spriteBatch.Render();
		}


		protected override void OnUnload() {
			Utilities.Dispose(ref m_textureRenderer);
			Utilities.Dispose(ref m_spriteBatch);
			Utilities.Dispose(ref m_texture);
			Utilities.Dispose(ref m_srv);
		}

		private float minimumValue = 1000.0F;
		private float maximumValue = -1000.0F;


		private void GenerateNoise(float xOffset = 0.0F, float yOffset = 0.0f, float zOffset = 0.0F, float size = 5.0F) {
			Parallel.For(0, TEXTURE_SIZE, y => {
				for (int x = 0; x < TEXTURE_SIZE; x++) {
					var index = x + y * TEXTURE_SIZE;
					var xpos = (float) x / TEXTURE_SIZE * size + xOffset;
					var ypos = (float) y / TEXTURE_SIZE * size + yOffset;
					var noise = (float) m_noise.RidgedMultifractal(xpos, ypos, zOffset);
					minimumValue = Mathf.Min(noise, minimumValue);
					maximumValue = Mathf.Max(noise, maximumValue);
					noise = (noise + 1.0F) / 2.0F;
					m_colors[index].R = (byte) (noise * 255);
					m_colors[index].G = (byte) (noise * 255);
					m_colors[index].B = (byte) (noise * 255);
					m_colors[index].A = 255;
				}
			});

			Debug.Log($"Minimum {minimumValue}");
			Debug.Log($"Maximum {maximumValue}");
		}


		private void GenerateSimplex(float xOffset = 0.0F, float yOffset = 0.0f, float zOffset = 0.0F, float size = 5.0F) {
			Parallel.For(0, TEXTURE_SIZE, y => {
				for (int x = 0; x < TEXTURE_SIZE; x++) {
					var index = x + y * TEXTURE_SIZE;
					var xpos = (float) x / TEXTURE_SIZE * size + xOffset;
					var ypos = (float) y / TEXTURE_SIZE * size + yOffset;
					var noise = (float) m_simplex.Evaluate(xpos, ypos, zOffset);
					minimumValue = Mathf.Min(noise, minimumValue);
					maximumValue = Mathf.Max(noise, maximumValue);
					noise = (noise + 1.0F) / 2.0F;
					m_colors[index].R = (byte) (noise * 255);
					m_colors[index].G = (byte) (noise * 255);
					m_colors[index].B = (byte) (noise * 255);
					m_colors[index].A = 255;
				}
			});

			Debug.Log($"Minimum {minimumValue}");
			Debug.Log($"Maximum {maximumValue}");
		}

		private void GenerateSimplexS(float xOffset = 0.0F, float yOffset = 0.0f, float zOffset = 0.0F, float size = 5.0F) {
			Parallel.For(0, TEXTURE_SIZE, y => {
				for (int x = 0; x < TEXTURE_SIZE; x++) {
					var index = x + y * TEXTURE_SIZE;
					var xpos = (float) x / TEXTURE_SIZE * size + xOffset;
					var ypos = (float) y / TEXTURE_SIZE * size + yOffset;
					var noise = (float) m_simplex2S.Noise3_Classic(xpos, ypos, zOffset);
					minimumValue = Mathf.Min(noise, minimumValue);
					maximumValue = Mathf.Max(noise, maximumValue);
					noise = (noise + 1.0F) / 2.0F;
					m_colors[index].R = (byte) (noise * 255);
					m_colors[index].G = (byte) (noise * 255);
					m_colors[index].B = (byte) (noise * 255);
					m_colors[index].A = 255;
				}
			});

			Debug.Log($"Minimum {minimumValue}");
			Debug.Log($"Maximum {maximumValue}");
		}

		private void GenerateSimplexF(float xOffset = 0.0F, float yOffset = 0.0f, float zOffset = 0.0F, float size = 5.0F) {
			Parallel.For(0, TEXTURE_SIZE, y => {
				for (int x = 0; x < TEXTURE_SIZE; x++) {
					var index = x + y * TEXTURE_SIZE;
					var xpos = (float) x / TEXTURE_SIZE * size + xOffset;
					var ypos = (float) y / TEXTURE_SIZE * size + yOffset;
					var noise = (float) m_simplex2F.Noise3_Classic(xpos, ypos, zOffset);
					minimumValue = Mathf.Min(noise, minimumValue);
					maximumValue = Mathf.Max(noise, maximumValue);
					noise = (noise + 1.0F) / 2.0F;
					m_colors[index].R = (byte) (noise * 255);
					m_colors[index].G = (byte) (noise * 255);
					m_colors[index].B = (byte) (noise * 255);
					m_colors[index].A = 255;
				}
			});

			Debug.Log($"Minimum {minimumValue}");
			Debug.Log($"Maximum {maximumValue}");
		}
	}
}