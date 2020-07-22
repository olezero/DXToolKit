using System.Collections.Generic;
using DXToolKit.GUI;
using SharpDX.DirectInput;

namespace DXToolKit.Engine {
	/// <summary>
	/// Active elements causes redraws more often.
	/// Usually redrawn when mouse enters and leaves, also if a mouse button is pressed.
	/// </summary>
	public class ActiveElement : GUIElement {
		protected override void OnMouseEnter() {
			base.OnMouseEnter();
			ToggleRedraw();
		}

		protected override void OnMouseLeave() {
			base.OnMouseLeave();
			ToggleRedraw();
		}

		protected override void OnMouseDown(GUIMouseEventArgs args) {
			base.OnMouseDown(args);
			ToggleRedraw();
		}

		protected override void OnMouseUp(GUIMouseEventArgs args) {
			base.OnMouseUp(args);
			ToggleRedraw();
		}

		protected override void OnKeyDown(List<Key> keys) {
			base.OnKeyDown(keys);
			ToggleRedraw();
		}

		protected override void OnKeyUp(List<Key> keys) {
			base.OnKeyUp(keys);
			ToggleRedraw();
		}

		protected override void OnKeyPressed(List<Key> keys) {
			base.OnKeyPressed(keys);
			ToggleRedraw();
		}

		protected override void OnFocusGained() {
			base.OnFocusGained();
			ToggleRedraw();
		}

		protected override void OnFocusLost() {
			base.OnFocusLost();
			ToggleRedraw();
		}

		protected override void OnContainFocusGained() {
			base.OnContainFocusGained();
			ToggleRedraw();
		}

		protected override void OnContainFocusLost() {
			base.OnContainFocusLost();
			ToggleRedraw();
		}
	}
}