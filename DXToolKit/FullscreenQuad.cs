using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace DXToolKit {
	/// <summary>
	/// Represents a renderable Polygon that stretches over the whole screen
	/// </summary>
	public class FullscreenQuad : DeviceComponent {
		private RasterizerState m_rasterizer;
		private DepthStencilState m_depthStencilState;

		private VertexShader m_vertexShader;

		/// <summary>
		/// Creates a new instance of the FullscreenQuad
		/// </summary>
		/// <param name="device">Graphics device to use when creating the quad</param>
		public FullscreenQuad(GraphicsDevice device) : base(device) {
			var vsByteCode = ShaderBytecode.Compile(ShaderSource, "main", "vs_5_0");
			m_vertexShader = new VertexShader(m_device, vsByteCode);
			Utilities.Dispose(ref vsByteCode);

			m_rasterizer = new RasterizerState(m_device, RasterizerStateDescription.Default());
			var desc = DepthStencilStateDescription.Default();
			desc.IsDepthEnabled = false;
			m_depthStencilState = new DepthStencilState(m_device, desc);
		}

		/// <summary>
		/// Renders the quad to the screen
		/// </summary>
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

		/// <summary>
		/// Disposes of all unmanaged memory
		/// </summary>
		protected override void OnDispose() {
			Utilities.Dispose(ref m_vertexShader);
			Utilities.Dispose(ref m_rasterizer);
			Utilities.Dispose(ref m_depthStencilState);
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