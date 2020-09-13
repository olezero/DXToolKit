using DXToolKit.Engine;
using SharpDX;
using SharpDX.DirectWrite;

namespace DXToolKit.Engine {
	/// <summary>
	/// Simple listbox option that only contains text
	/// </summary>
	public class ListboxOption : ActiveElement {
		/// <summary>
		/// Creates a list box option
		/// </summary>
		public ListboxOption() : this("") { }

		/// <summary>
		/// Creates a list box option
		/// </summary>
		/// <param name="text">Text of the option</param>
		public ListboxOption(string text) {
			Text = text;
			ParagraphAlignment = ParagraphAlignment.Center;
			Height = 20;
			TextOffset = new Vector2(2, 0);
		}

		/// <inheritdoc />
		protected override void OnRender(GUIDrawTools tools, ref GUIDrawParameters drawParameters) {
			tools.Text();
		}
	}
}