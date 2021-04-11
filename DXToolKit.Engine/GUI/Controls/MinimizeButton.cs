using System;
using DXToolKit.GUI;

namespace DXToolKit.Engine {
	/// <summary>
	/// Minimize button
	/// </summary>
	public class MinimizeButton : IconButton {
		/// <inheritdoc />
		public MinimizeButton() : base(new MinimizeIcon()) { }

		/// <inheritdoc />
		public MinimizeButton(Action<GUIMouseEventArgs> onClick) : base(new MinimizeIcon(), onClick) { }
	}
}