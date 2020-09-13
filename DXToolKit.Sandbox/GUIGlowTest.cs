using DXToolKit.Engine;

namespace DXToolKit.Sandbox {
	public class MyGUIElement : GUIElement {
		private Button m_button;

		public MyGUIElement() {
			Append(new Slider(GUIDirection.Vertical, f => {
				InnerGlow.Size = f;
			}) {
				X = 0,
				Y = 0,
				MinValue = 0.0F,
				MaxValue = 50.0F,
				Value = InnerGlow.Size,
			});

			Append(new Slider(GUIDirection.Vertical, f => {
				InnerGlow.Opacity = f;
			}) {
				X = 0,
				Y = 25,
				MinValue = 0.0F,
				MaxValue = 1.0F,
				Value = InnerGlow.Opacity,
			});


			m_button = Append(new Button("BTN") {
				X = 200,
				Y = 200,
				Width = 120,
				Height = 24,
			});
		}

		protected override void OnRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) {
			tools.Foreground.Rectangle();
			tools.OuterGlow(m_button.Bounds);
		}

		protected override void PostRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) {
			tools.InnerGlow(m_button.Bounds);
		}
	}

	public class GUIGlowSketch : Sketch {
		protected override void OnLoad() {
			EnableGUI();
			GUI.Append(new MyGUIElement {
				Width = 512,
				Height = 512,
				X = 100,
				Y = 100,
			});
		}

		protected override void Update() { }
		protected override void Render() { }
		protected override void OnUnload() { }
	}
}