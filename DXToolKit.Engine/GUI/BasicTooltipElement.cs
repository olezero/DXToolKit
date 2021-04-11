using DXToolKit.GUI;
using SharpDX;
using SharpDX.DirectWrite;

// TODO: Under construction
#pragma warning disable

namespace DXToolKit.Engine {
	/// <summary>
	/// Basic tool tip
	/// </summary>
	public class BasicTooltipElement : GUIElement, ITooltipElement {
		private float m_fadeAmount;
		private bool m_runFade = true;

		/// <summary>
		/// Basic tool tip
		/// </summary>
		public BasicTooltipElement() {
			ParagraphAlignment = ParagraphAlignment.Center;
			TextOffset = new Vector2(4, 0);
		}

		public void OnOpen(string text, Vector2 mousePosition) {
			Text = text;
			Visible = true;
			var metrics = FontCalculator.GetMetrics(this);
			Width = metrics.Width + 10;
			Height = metrics.Height + 8;

			Right = mousePosition.X;
			CenterY = mousePosition.Y;
			if (X < 0) X = 0;
			if (Y < 0) Y = 0;
			if (Right > Parent.Right) Right = Parent.Right;
			if (Bottom > Parent.Bottom) Bottom = Parent.Bottom;
			Width = Mathf.Ceiling(Width);
			Height = Mathf.Ceiling(Height);

			Opacity = 0.0F;
			m_runFade = true;
			m_fadeAmount = 0;
			Animation.AddAnimation(0, 1, 250, (from, to, amount) => {
				Opacity = amount;
				return m_runFade;
			});
		}

		public void OnClose() {
			Visible = false;
			m_runFade = false;
		}

		protected override void OnRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) {
			tools.Background.TransparentRectangle();
			tools.Text();
		}
	}
}