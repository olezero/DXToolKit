using SharpDX;
using SharpDX.Direct3D11;

namespace DXToolKit.Engine {
	public class SketchPipeline : BasicPipeline {
		private RasterizerState m_rasterizer;
		private RasterizerStateDescription m_rasterizerDescription;

		public Color? ClearColor = null;

		public CullMode Cullmode {
			get => m_rasterizerDescription.CullMode;
			set {
				m_rasterizerDescription.CullMode = value;
				CreateRasterizer();
			}
		}

		public FillMode FillMode {
			get => m_rasterizerDescription.FillMode;
			set {
				m_rasterizerDescription.FillMode = value;
				CreateRasterizer();
			}
		}

		private DepthStencilState m_depthDisabled;
		private DepthStencilState m_depthEnabled;
		private bool m_isDepthEnabled = true;

		public bool IsDepthEnabled {
			get => m_isDepthEnabled;
			set {
				m_isDepthEnabled = value;
				m_context.OutputMerger.DepthStencilState = m_isDepthEnabled ? m_depthEnabled : m_depthDisabled;
			}
		}

		public SketchPipeline(GraphicsDevice device) : base(device) {
			var depthDesc = DepthStencilStateDescription.Default();
			m_depthEnabled = new DepthStencilState(m_device, depthDesc);
			depthDesc.IsDepthEnabled = false;

			m_depthDisabled = new DepthStencilState(m_device, depthDesc);
			m_rasterizerDescription = RasterizerStateDescription.Default();
			CreateRasterizer();
		}

		private void CreateRasterizer() {
			m_rasterizer?.Dispose();
			m_rasterizer = new RasterizerState(m_device, m_rasterizerDescription);
		}

		public override void Begin(Color? clearColor = null) {
			if (ClearColor != null) {
				clearColor = ClearColor;
			}

			base.Begin(clearColor);
			m_context.Rasterizer.State = m_rasterizer;
			m_context.OutputMerger.DepthStencilState = m_isDepthEnabled ? m_depthEnabled : m_depthDisabled;
		}

		protected override void OnDispose() {
			base.OnDispose();
			m_depthDisabled?.Dispose();
			m_depthEnabled?.Dispose();
			m_rasterizer?.Dispose();
		}
	}
}