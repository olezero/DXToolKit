using SharpDX.Direct3D11;
using SharpDX.DirectInput;

namespace DXToolKit.Engine {
	public abstract class Sketch : Scene {
		private SketchApp m_app;
		protected SketchPipeline Pipeline => (SketchPipeline) m_app.RenderPipeline;

		public void Run(string[] args) {
			m_app = new SketchApp(this);
			m_app.Run(args);
			m_app?.Dispose();
		}

		protected void ToggleWireframe() {
			Pipeline.FillMode = Pipeline.FillMode == FillMode.Solid ? FillMode.Wireframe : FillMode.Solid;
		}

		protected void ToggleBackCulling() {
			Pipeline.Cullmode = Pipeline.Cullmode == CullMode.None ? CullMode.Back : CullMode.None;
		}

		protected void ToggleDepth() {
			Pipeline.IsDepthEnabled = !Pipeline.IsDepthEnabled;
		}

		internal override void RunUpdate() {
			if (KeyDown(Key.F9)) {
				ToggleWireframe();
			}

			if (KeyDown(Key.F8)) {
				ToggleBackCulling();
			}

			base.RunUpdate();
		}
	}
}