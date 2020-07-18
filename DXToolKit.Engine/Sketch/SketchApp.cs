using System.Drawing;
using SharpDX.DirectInput;

namespace DXToolKit.Engine {
	internal class SketchApp : DXApp {
		private Sketch m_sketchScene;

		public SketchApp(Sketch sketchScene) {
			m_sketchScene = sketchScene;
		}


		protected override void Initialize() {
			// Setup base 
			Time.ShowFPS = true;
			m_renderform.Location = new Point(100, 100);

			// Load sketch scene
			SceneManager.AddScene(m_sketchScene, m_sketchScene, true);
		}


		protected override void Update() {
			if (Input.KeyDown(Key.Escape)) {
				Exit();
			}
		}

		protected override IRenderPipeline CreateRenderPipeline() {
			// Setup a basic render pipeline
			return new SketchPipeline(m_device);
		}
	}
}