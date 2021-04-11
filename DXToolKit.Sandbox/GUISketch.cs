using DXToolKit.Engine;
using SharpDX;
using SharpDX.Direct2D1;

namespace DXToolKit.Sandbox {
	public class TestElement : ActiveElement {
		public TestElement() {
			Draggable = true;
		}

		protected override void OnDrag() {
			X += Input.MouseMove.X;
			Y += Input.MouseMove.Y;
			base.OnDrag();
		}

		protected override void OnRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) {
			tools.Foreground.Rectangle();
			tools.InnerGlow();
			foreach (var child in ChildElements) {
				tools.OuterGlow(child.Bounds);
			}
		}
	}

	public class GUISketch : Sketch {
		private BasicTooltipElement m_tooltip;

		protected override void OnLoad() {
			EnableGUI();


			GUIElement.SetElementDefaults<Window>(new GUIDrawParameters {
				ShineOpacity = 0.0f,
				StyleInheritance = new StyleInheritance {
					ShineOpacity = true
				},
			});


			/*
			GUIElement.SetElementDefaults<Button>(new GUIDrawParameters() {

			});
			*/


			m_tooltip = GUI.Append(new BasicTooltipElement() {
				Text = "test"
			});


			var el = GUI.Append(new TestElement() {
				X = 100,
				Y = 100,
				Width = 200,
				Height = 200,
			});
			el.Append(new TestElement() {
				X = 50,
				Y = 50,
				Width = 50,
				Height = 50,
				ForegroundColor = GUIColor.Danger
			});


			var window = GUI.Append(new Window());


			var g = GUIGrid.Create(window.Body, grid => {
				grid.SetGridPadding(new GUIPadding(8));
				grid.SetElementPadding(new GUIPadding(4));
				grid.Column(new Button("test"), 4);
				grid.Column(new Button("test"), 4);
				grid.Column(new Button("test"), 4);
				grid.Column(new Button("test"), 4);
				grid.Column(new Button("test"), 4);
				grid.Column(new Button("test"), 4);
			});


			window.Body.Draggable = true;
			window.Body.Drag += () => {
				Debug.Log("Drag!");
				window.Body.Width += Input.MouseMove.X;
				window.Body.Height += Input.MouseMove.Y;
			};
			window.Body.BoundsChanged += () => {
				g.AllowParentResize = false;
				g.RunOrganize();
			};


			m_brush = new SolidColorBrush(m_renderTarget, Color.White);
		}

		private float m_30;
		private float m_60;
		private float m_120;

		private float m_30Offset;
		private float m_60Offset;
		private float m_120Offset;

		private SolidColorBrush m_brush;


		protected override void Update() {
			//m_tooltip.X = MousePosition.X;
			//m_tooltip.Y = MousePosition.Y;
		}

		protected override void Render() {
			m_renderTarget.BeginDraw();

			float xOffset = 0;
			float xMove = 100;

			m_30 += Time.DeltaTime;
			m_60 += Time.DeltaTime;
			m_120 += Time.DeltaTime;
			if (m_30 > 30 / 1000.0F) {
				m_30 = 0;
				m_30Offset += xMove * 30 / 1000.0F;
			}

			if (m_60 > 60 / 1000.0F) {
				m_60 = 0;
				m_60Offset += xMove * 60 / 1000.0F;
			}

			if (m_120 > 120 / 1000.0F) {
				m_120 = 0;
				m_120Offset += xMove * 120 / 1000.0F;
			}


			m_renderTarget.FillRectangle(new RectangleF(xOffset + m_30Offset, 0, 100, 100), m_brush);
			m_renderTarget.FillRectangle(new RectangleF(xOffset + m_60Offset, 0, 100, 100), m_brush);
			m_renderTarget.FillRectangle(new RectangleF(xOffset + m_120Offset, 0, 100, 100), m_brush);

			m_renderTarget.EndDraw();
		}

		protected override void OnUnload() { }
	}
}