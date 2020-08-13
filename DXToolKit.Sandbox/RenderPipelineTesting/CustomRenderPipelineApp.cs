using DXToolKit.Engine;
using SharpDX.DirectInput;

namespace DXToolKit.Sandbox {
	public class CustomRenderPipelineApp : DXApp {
		protected override void Initialize() {
			SceneManager.AddScene(new BaseScene(), "main", true);
			Time.ShowFPS = true;
		}

		protected override void Update() {
			if (Input.KeyDown(Key.Escape)) {
				Exit();
			}
		}

		protected override IRenderPipeline CreateRenderPipeline() {
			return new SketchPipeline(m_device);
			return base.CreateRenderPipeline();
			return new RenderPipeline(m_device);
		}
	}
}