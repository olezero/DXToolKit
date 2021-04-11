using System;
using DXToolKit.GUI;

namespace DXToolKit.Engine {
	/// <summary>
	/// Graphic button that uses Cross Icon
	/// </summary>
	public class CloseButton : IconButton {
		/// <inheritdoc />
		public CloseButton() : base(new CrossIcon()) { }

		/// <inheritdoc />
		public CloseButton(Action<GUIMouseEventArgs> onClick) : base(new CrossIcon(), onClick) { }
	}
}