using DXToolKit.Engine;
using SharpDX;
using SharpDX.DirectWrite;

namespace DXToolKit.Engine {
	/// <summary>
	/// Simple label element.
	/// By default does not react to mouse input and cannot receive focus
	/// </summary>
	public class Label : GUIElement {
		/// <summary>
		/// Creates a new label
		/// </summary>
		/// <param name="text">The text of the label</param>
		public Label(string text) {
			Text = text;

			TextAlignment = TextAlignment.Leading;
			ParagraphAlignment = ParagraphAlignment.Center;
			TextOffset = new Vector2(2, 0);

			StyleInheritance.TextAlignment = true;
			StyleInheritance.ParagraphAlignment = true;
			StyleInheritance.TextOffset = true;

			Height = 12 * 2;
			Width = 12 * 8;
			CanReceiveFocus = false;
			CanReceiveKeyboardInput = false;
			CanReceiveMouseInput = false;
		}

		/// <inheritdoc />
		protected override void OnRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) {
			tools.Text();
		}
	}
}