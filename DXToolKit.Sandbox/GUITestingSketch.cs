using DXToolKit.Engine;
using SharpDX.DirectWrite;

namespace DXToolKit.Sandbox {
	public class SomeGUIElement : ActiveElement {
		protected override void OnRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) {
			tools.Foreground.Rectangle();
			tools.BevelBorder();
			tools.InnerGlow(drawParameters.Bounds);
			tools.Text();
		}
	}

	public class GUITestingSketch : Sketch {
		protected override void OnLoad() {
			//var cmb = new Combobox();


			EnableGUI();


			GUI.Append(new Button());

			var someElement = GUI.Append(new SomeGUIElement() {
				X = 100,
				Y = 0,
				Width = 200,
				Height = 200,
				Tooltip = "Test 1234"
			});

			someElement.Append(new SomeGUIElement() {
				X = 20,
				Y = 20,
				Width = 100,
				Height = 100,
			});

			someElement.ForegroundColor = GUIColor.Info;
		}

		protected override void Update() { }
		protected override void Render() { }
		protected override void OnUnload() { }
	}
}