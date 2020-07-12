using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace DXToolKit {
	public class TextureRenderer : DeviceComponent {
		private PixelShader m_pixelShader;
		private FullscreenQuad m_quad;
		private SamplerState m_samplerState;

		public TextureRenderer(GraphicsDevice device) : base(device) {
			m_pixelShader = new PixelShader(m_device, ShaderBytecode.Compile(SHADER_SOURCE, "PS", "ps_5_0"));
			m_quad = new FullscreenQuad(m_device);
			m_samplerState = new SamplerState(m_device, SamplerStateDescription.Default());
		}

		private void SetParameters() {
			m_context.InputAssembler.InputLayout = null;
			m_context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
			m_context.GeometryShader.Set(null);
		}

		/// <summary>
		/// Draws the texture to screen using a built inn pixel shader
		/// </summary>
		/// <param name="texture">The texture to draw</param>
		/// <param name="destination">Destination in screen coordinates</param>
		public void Draw(ShaderResourceView texture, RectangleF destination) {
			// Set default parameters
			SetParameters();

			// Set new viewport for the shader
			m_context.Rasterizer.SetViewport(new ViewportF(destination));

			// Set internal pixel shader
			m_context.PixelShader.Set(m_pixelShader);
			m_context.PixelShader.SetShaderResource(0, texture);
			m_context.PixelShader.SetSamplers(0, m_samplerState);

			// Render quad to screen
			m_quad.Render();
		}


		/// <summary>
		/// Draws a quad to the screen. Remember to use your own pixel shader to fill the quad
		/// </summary>
		/// <param name="destination"></param>
		public void Draw(RectangleF destination) {
			// Set default parameters
			SetParameters();
			// Set new viewport for the shader
			m_context.Rasterizer.SetViewport(new ViewportF(destination));
			// Render quad to screen
			m_quad.Render();
		}

		protected override void OnDispose() {
			Utilities.Dispose(ref m_pixelShader);
			Utilities.Dispose(ref m_quad);
		}

		private const string SHADER_SOURCE = @"
Texture2D g_texture;
SamplerState g_sampler;

float4 PS(float4 position : SV_Position, float2 uv : TEXCOORD) : SV_Target {
	return g_texture.Sample(g_sampler, uv);
};
";
	}
}