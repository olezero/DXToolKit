using System.Collections.Generic;
using SharpDX.DirectInput;
using DXToolKit.GUI;

namespace DXToolKit.Engine {
	/// <summary>
	/// Active elements causes redraws more often.
	/// Usually redrawn when mouse enters and leaves, also if a mouse button is pressed.
	/// </summary>
	public class ActiveElement : GUIElement {
		/// <inheritdoc />
		protected override void OnMouseEnter() {
			base.OnMouseEnter();
			ToggleRedraw();
		}

		/// <inheritdoc />
		protected override void OnMouseLeave() {
			base.OnMouseLeave();
			ToggleRedraw();
		}

		/// <inheritdoc />
		protected override void OnMouseDown(GUIMouseEventArgs args) {
			base.OnMouseDown(args);
			ToggleRedraw();
		}

		/// <inheritdoc />
		protected override void OnMouseUp(GUIMouseEventArgs args) {
			base.OnMouseUp(args);
			ToggleRedraw();
		}

		/// <inheritdoc />
		protected override void OnKeyDown(List<Key> keys) {
			base.OnKeyDown(keys);
			ToggleRedraw();
		}

		/// <inheritdoc />
		protected override void OnKeyUp(List<Key> keys) {
			base.OnKeyUp(keys);
			ToggleRedraw();
		}

		/// <inheritdoc />
		protected override void OnKeyPressed(List<Key> keys) {
			base.OnKeyPressed(keys);
			ToggleRedraw();
		}

		/// <inheritdoc />
		protected override void OnFocusGained() {
			base.OnFocusGained();
			ToggleRedraw();
		}

		/// <inheritdoc />
		protected override void OnFocusLost() {
			base.OnFocusLost();
			ToggleRedraw();
		}

		/// <inheritdoc />
		protected override void OnContainFocusGained() {
			base.OnContainFocusGained();
			ToggleRedraw();
		}

		/// <inheritdoc />
		protected override void OnContainFocusLost() {
			base.OnContainFocusLost();
			ToggleRedraw();
		}
	}
}