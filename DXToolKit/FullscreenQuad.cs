using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace DXToolKit {
	public class FullscreenQuad : DeviceComponent {
		private static RasterizerState m_rasterizer;
		private static DepthStencilState m_depthStencilState;

		private VertexShader m_vertexShader;

		public FullscreenQuad(GraphicsDevice device) : base(device) {
			var vsByteCode = ShaderBytecode.Compile(ShaderSource, "main", "vs_5_0");
			m_vertexShader = new VertexShader(m_device, vsByteCode);
			vsByteCode?.Dispose();


			if (m_rasterizer == null) {
				var desc = RasterizerStateDescription.Default();
				m_rasterizer = new RasterizerState(m_device, desc);
			}

			if (m_depthStencilState == null) {
				var desc = DepthStencilStateDescription.Default();
				desc.IsDepthEnabled = false;
				m_depthStencilState = new DepthStencilState(m_device, desc);
			}
		}

		public void Render() {
			var tempRasterizerState = m_context.Rasterizer.State;
			var tempDepthState = m_context.OutputMerger.DepthStencilState;

			m_context.OutputMerger.DepthStencilState = m_depthStencilState;
			m_context.Rasterizer.State = m_rasterizer;

			m_context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
			m_context.InputAssembler.InputLayout = null;
			m_context.VertexShader.Set(m_vertexShader);
			m_context.GeometryShader.Set(null);
			m_context.Draw(6, 0);

			m_context.Rasterizer.State = tempRasterizerState;
			m_context.OutputMerger.DepthStencilState = tempDepthState;
		}

		protected override void OnDispose() {
			m_vertexShader?.Dispose();
		}


		private const string ShaderSource = @"
struct Output
{
	float4 position_cs : SV_POSITION;
	float2 texcoord : TEXCOORD;
};

Output main(uint id: SV_VertexID)
{
	Output output;

	output.texcoord = float2((id << 1) & 2, id & 2);
	output.position_cs = float4(output.texcoord * float2(2, -2) + float2(-1, 1), 0, 1);

	return output;
}";
	}
}