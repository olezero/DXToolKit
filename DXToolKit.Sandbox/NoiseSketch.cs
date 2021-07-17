using System;
using System.Threading.Tasks;
using DXToolKit.Engine;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace DXToolKit.Sandbox {
	public class GraphDrawer : DeviceComponent {
		private SolidColorBrush m_lineBrush;
		private SolidColorBrush m_graphBrush;

		public GraphDrawer(GraphicsDevice device) : base(device) {
			m_lineBrush = new SolidColorBrush(m_device, Color.White);
			m_graphBrush = new SolidColorBrush(m_device, Color.White);
		}

		public void DrawGraph(RectangleF destination, float[] values) {
			var delta = destination.Width / (values.Length - 1);
			var mid = destination.Y + destination.Height / 2.0F;
			var heightScale = destination.Height / 2.0F * 0.5F;

			var minVal = float.MaxValue;
			var maxVal = float.MinValue;

			foreach (var value in values) {
				minVal = Mathf.Min(value, minVal);
				maxVal = Mathf.Max(value, maxVal);
			}

			m_renderTarget.BeginDraw();
			for (var i = 0; i < values.Length - 1; i++) {
				var x1Pos = delta * i;
				var x2Pos = delta * i + delta;
				var y1Pos = Mathf.Map(values[i + 0], minVal, maxVal, -heightScale, heightScale);
				var y2Pos = Mathf.Map(values[i + 1], minVal, maxVal, -heightScale, heightScale);

				m_renderTarget.DrawLine(
					new Vector2(x1Pos + destination.X, y1Pos + mid),
					new Vector2(x2Pos + destination.X, y2Pos + mid),
					m_lineBrush,
					2.0F
				);
			}
			m_renderTarget.DrawRectangle(destination, m_graphBrush, 2.0F);
			m_renderTarget.EndDraw();
		}

		protected override void OnDispose() {
			Utilities.Dispose(ref m_lineBrush);
			Utilities.Dispose(ref m_graphBrush);
		}
	}

	public class NoiseSketch : Sketch {
		private const int TEXTURE_SIZE = 128;

		private OpenSimplex2S m_simplex2S;
		private OpenSimplex2F m_simplex2F;
		private SimplexNoise m_simplex;
		private TextureRenderer m_textureRenderer;
		private SpriteBatch m_spriteBatch;
		private Texture2D m_texture;
		private ShaderResourceView m_srv;
		private Color[] m_colors;
		private Noise m_noise;
		private GraphDrawer m_graphDrawer;

		private float m_xOffset = 5.0F;
		private float m_yOffset = 5.0F;
		private float m_zOffset = 0.0F;
		private float m_timer = 0.0F;

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

			m_graphDrawer = new GraphDrawer(m_device);

			// GenerateSimplex();
			// m_texture = TextureHelper.CreateTexture2D(m_device, ref m_colors, TEXTURE_SIZE, TEXTURE_SIZE);
			// m_srv = new ShaderResourceView(m_device, m_texture);
		}


		protected override void Update() {
			return;

			m_timer += Time.DeltaTime;

			GenerateNoise(m_xOffset, m_yOffset, m_zOffset);
			// m_xOffset += Time.DeltaTime;
			// m_yOffset += Time.DeltaTime;
			m_zOffset += Time.DeltaTime;

			m_context.MapSubresource(m_texture, 0, 0, MapMode.WriteDiscard, MapFlags.None, out DataStream stream);
			stream.WriteRange(m_colors);
			m_context.UnmapSubresource(m_texture, 0);
			stream.Dispose();
		}


		protected override void Render() {
			var values = new float[128];
			for (var i = 0; i < values.Length; i++) {
				values[i] = (float) m_simplex2S.Noise2(i * 0.1, 0.2);
			}

			m_graphDrawer.DrawGraph(new RectangleF(128, 128, 512, 512), values);


			m_spriteBatch.Draw(m_srv, new RectangleF(256, 256, 512, 512));
			m_spriteBatch.Render();
		}


		protected override void OnUnload() {
			Utilities.Dispose(ref m_textureRenderer);
			Utilities.Dispose(ref m_spriteBatch);
			Utilities.Dispose(ref m_texture);
			Utilities.Dispose(ref m_srv);
			Utilities.Dispose(ref m_graphDrawer);
		}

		private float minimumValue = 1000.0F;
		private float maximumValue = -1000.0F;

		private float signalMod(float signal) {
			return 0.0F;
		}


		private float NoiseGen(float x, float y, float z) {
			const float freq = 1.2F;
			const float per = 0.5F; // How much to add on each octave
			const float laq = 2.0F; // How much to scale up each octave
			const int oct = 12;
			const float div = 1.0F - per;


			float value = 0.0F;
			float cp = 1.0F;

			x *= freq;
			y *= freq;
			z *= freq;

			for (int i = 0; i < oct; i++) {
				var signal = (float) m_simplex2S.Noise3_Classic(x, y, z);
				signal *= signal;
				signal = Mathf.Abs(signal);
				signal *= -1;

				value += signal * cp;

				x *= laq;
				y *= laq;
				z *= laq;
				cp *= per;
			}


			return value;
		}


		private void GenerateNoise(float xOffset = 0.0F, float yOffset = 0.0f, float zOffset = 0.0F, float size = 5.0F) {
			Parallel.For(0, TEXTURE_SIZE, y => {
				for (int x = 0; x < TEXTURE_SIZE; x++) {
					var index = x + y * TEXTURE_SIZE;
					var xpos = (float) x / TEXTURE_SIZE * size + xOffset;
					var ypos = (float) y / TEXTURE_SIZE * size + yOffset;
					// var noise = (float) m_noise.Perlin(xpos, ypos, zOffset);

					var noise = NoiseGen(xpos, ypos, zOffset);
					minimumValue = Mathf.Min(noise, minimumValue);
					maximumValue = Mathf.Max(noise, maximumValue);
					// noise = (noise + 1.0F) / 2.0F;
					noise = Mathf.Map(noise, minimumValue, maximumValue, 0, 1);

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