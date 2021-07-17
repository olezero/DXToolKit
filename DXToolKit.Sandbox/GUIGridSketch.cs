using DXToolKit.Engine;

namespace DXToolKit.Sandbox {
	public class CustomPanel : Panel {
		protected override void OnRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) {
			tools.Background.Rectangle();
			base.OnRender(tools, ref drawParameters);
		}
	}


	public class GUIGridSketch : Sketch {
		private GUIGrid panelGrid;

		protected override void OnLoad() {
			EnableGUI();


			var panel = GUI.Append(new CustomPanel() {
				X = 200,
				Y = 200,
				Height = 512,
				Width = 256,
			});

			panelGrid = GUIGrid.Create(panel, grid => {
				grid.AllowParentResize = true;
				grid.DynamicRowHeight = false;
				grid.AutoResizeElementHeigth = true;

				grid.SetGridPadding(new GUIPadding(3));
				grid.SetElementPadding(new GUIPadding(3));


				grid.Column(new Button("test 1") {Height = 20}, 12);
				grid.Column(new Button("test 2") {Height = 30}, 8);
				grid.Column(new Button("test 2") {Height = 50}, 4);
				grid.Column(new Button("test 3") {Height = 50}, 12);
			});

			panel.Append(new Button("Test") {
				X = panelGrid.X,
				Y = panelGrid.Y + panelGrid.Height
			});
		}

		protected override void Update() {
			Debug.Log(panelGrid.Height);
		}
	}
}