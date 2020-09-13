using System;
using DXToolKit.Engine;
using SharpDX;
using SharpDX.Direct2D1;

namespace DXToolKit.Sandbox {
	
	
	public class ShinyElement : GUIElement {
		public ShinyElement() {
			Draggable = true;
		}

		protected override void OnUpdate() {
			ToggleRedraw();
			base.OnUpdate();
		}


		protected override void OnDrag() {
			Width += Input.MouseMove.X;
			Height += Input.MouseMove.Y;
			base.OnDrag();
		}

		protected override void OnRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) {
			tools.Foreground.Rectangle();
			tools.Shine(drawParameters.Bounds);
		}
	}

	public class ShinyButton : Button {
		protected override void OnRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) {
			base.OnRender(tools, ref drawParameters);
		}
	}

	public class GUIShineTest : Sketch {
		protected override void OnLoad() {
			EnableGUI();
			GUI.Append(new ShinyElement {
				X = 100,
				Y = 100,
				Width = 512,
				Height = 128,
			});
		}

		protected override void Update() {
			if (Input.MousePressed(MouseButton.Left)) {
				c.X += MouseMove.X;
				c.Y += MouseMove.Y;
			}

			if (Input.MousePressed(MouseButton.Right)) {
				b.X += MouseMove.X;
				b.Y += MouseMove.Y;
			}
		}


		private Vector2 a = new Vector2(200, 500);
		private Vector2 b = new Vector2(1000, 500);
		private Vector2 c = new Vector2(1000, 250);

		protected override void Render() {
			/*
			var brush = GUIColorPalette.Current[GUIColor.Light, GUIBrightness.Brightest];

			m_renderTarget.BeginDraw();


			m_renderTarget.DrawLine(a, c, brush);

			m_renderTarget.FillEllipse(new Ellipse(a, 2, 2), brush);
			m_renderTarget.FillEllipse(new Ellipse(b, 2, 2), brush);
			m_renderTarget.FillEllipse(new Ellipse(c, 2, 2), brush);


			var d = new Vector2(0, 0);
			d = GetClosestPointOnLineSegment(a, c, b);
			*/

			/*
			if (Math.Abs(a.X - c.X) < 0.001F) {
				// AC is vertical
				d = new Vector2(a.X, b.Y);
				Debug.Log("AC vertical");
			} else if (Math.Abs(a.Y - c.Y) < 0.001F) {
				// AC is horizontal
				d = new Vector2(b.X, a.Y);
				Debug.Log("AC horizontal");
			} else {
				// Slope of AC
				var m = (c.Y - a.Y) / (c.X - a.X);

				// 0 == horizontal, inf == vertical

				Debug.Log(m);
			}
			*/

			/*
			brush = GUIColorPalette.Current[GUIColor.Primary, GUIBrightness.Brightest];
			m_renderTarget.FillEllipse(new Ellipse(d, 5, 5), brush);

			m_renderTarget.EndDraw();
			*/
		}


		public Vector2 GetClosestPointOnLineSegment(Vector2 A, Vector2 B, Vector2 P, bool constrain = false) {
			var AP = P - A; // Vector from A to P   
			var AB = B - A; // Vector from A to B  

			var magnitudeAB = AB.LengthSquared(); // Magnitude of AB vector (it's length squared)     
			var ABAPproduct = Vector2.Dot(AP, AB); // The DOT product of a_to_p and a_to_b     
			var distance = ABAPproduct / magnitudeAB; // The normalized "distance" from a to your closest point  

			if (constrain) {
				if (distance < 0) return A;
				if (distance > 1) return B;
				return A + AB * distance;
			}

			return A + AB * distance;
		}

		protected override void OnUnload() { }
	}
}