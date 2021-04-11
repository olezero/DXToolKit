using System;
using System.Collections.Generic;
using SharpDX;

namespace DXToolKit.GUI {
	public partial class GUIElement {
		#region Hierarchy

		/// <summary>
		/// Appends a element to this element
		/// </summary>
		/// <param name="element">The element ot append</param>
		/// <typeparam name="T">Element type</typeparam>
		/// <returns>The appended element</returns>
		public T Append<T>(T element) where T : GUIElement {
			if (IsDisposing || element.IsDisposing) {
				throw new Exception("Trying to run append on a disposing element is not a good idea...");
			}

			// Store current parent for event invocation
			var previousParent = element.m_parentElement;

			// Remove from current parent
			element.m_parentElement?.Remove(element);

			// Add to child elements
			if (m_childElements.Contains(element) == false) {
				m_childElements.Add(element);

				if (element.m_tabbable && element.m_tabIndex == -1) {
					element.m_tabIndex = m_tabOffset++;
				}

				// Invoke events
				OnChildAppended(this, element);
			}

			// Set parent
			element.m_parentElement = this;

			// Invoke events on child, but only if parent actually changed
			if (element.m_parentElement != previousParent) {
				element.OnParentChanged(this, previousParent);

				// Invoke event on child. Run after element.m_parentElement = this; so Parent property is correctly set
				element.OnParentSet(this, element);
			}

			// Toggle redraw on element after its appended
			element.ToggleRedraw();

			return element;
		}

		/// <summary>
		/// Removes an element from this element
		/// </summary>
		/// <param name="element">The element to remove</param>
		/// <param name="dispose">If child should be disposed when removed</param>
		public void Remove(GUIElement element, bool dispose = false) {
			if (m_childElements.Contains(element)) {
				// Remove from child elements
				m_childElements.Remove(element);

				// Run events, should only run when the collection has actually changed
				OnChildRemoved(this, element);
				// Run events on child
				element.OnParentUnset(this, element);
			}

			// Set parent to null
			element.m_parentElement = null;

			// Dispose if needed
			if (dispose) {
				element.Dispose();
			}

			// Toggle a redraw after element is removed
			ToggleRedraw();
		}

		/// <summary>
		/// Removes all children from this element
		/// </summary>
		/// <param name="dispose">If child should be disposed when removed</param>
		public void RemoveAllChildren(bool dispose = true) {
			while (m_childElements.Count > 0) {
				Remove(m_childElements[0], dispose);
			}

			ToggleRedraw();
		}


		/// <summary>
		/// Moves this element to the front of parents child elements
		/// </summary>
		public void MoveToFront() {
			m_parentElement?.MoveMeToFront(this);
		}

		/// <summary>
		/// Moves this element to the back of parents child elements
		/// </summary>
		public void MoveToBack() {
			m_parentElement?.MoveMeToBack(this);
		}

		/// <summary>
		/// Enqueues a move to front operation.
		/// </summary>
		private void MoveMeToFront(GUIElement target) {
			// No need to move if child count is just 1, and check that the target actually is a child
			if (m_childElements.Count > 1 && m_childElements.Contains(target)) {
				// Check that input target is not already the last element
				if (m_childElements[m_childElements.Count - 1] == target) return;
				// This operation needs to be cached and run next frame, reason being that its usually called from within a update loop
				m_moveActions.Enqueue(() => {
					// Make a copy just to be safe
					var targetCopy = target;
					// Get current index
					var index = m_childElements.IndexOf(targetCopy);
					// Pop current index
					m_childElements.RemoveAt(index);
					// Add back to element gets in the last position
					m_childElements.Add(targetCopy);
				});
			}
		}

		/// <summary>
		/// Enqueues a move to back operation
		/// </summary>
		private void MoveMeToBack(GUIElement target) {
			// No need to move if child count is just 1, and check that the target actually is a child
			if (m_childElements.Count > 1 && m_childElements.Contains(target)) {
				// Check that input target is not already the first element
				if (m_childElements[0] == target) return;
				// This operation needs to be cached and run next frame, reason being that its usually called from within a update loop
				m_moveActions.Enqueue(() => {
					// Make a copy just to be safe
					var targetCopy = target;
					// Get current index
					var index = m_childElements.IndexOf(targetCopy);
					// Pop current index
					m_childElements.RemoveAt(index);
					// Insert at index 0
					m_childElements.Insert(0, targetCopy);
				});
			}
		}

		/// <summary>
		/// Gets every child recursively
		/// </summary>
		public GUIElement[] AllChildren() {
			var result = new List<GUIElement>();
			AllChildrenRecursive(ref result);
			return result.ToArray();
		}

		/// <summary>
		/// Recurse function to get all elements in a hierarchy
		/// </summary>
		/// <param name="elements"></param>
		private void AllChildrenRecursive(ref List<GUIElement> elements) {
			elements.Add(this);
			foreach (var childElement in m_childElements) {
				childElement.AllChildrenRecursive(ref elements);
			}
		}

		/// <summary>
		/// Converts a screen position to elements local position
		/// </summary>
		/// <param name="screenPosition"></param>
		/// <returns></returns>
		public Vector2 ScreenToLocal(Vector2 screenPosition) {
			return screenPosition - ScreenBounds.Location;
		}

		/// <summary>
		/// Converts a local element position to screen position
		/// </summary>
		/// <param name="localPosition"></param>
		/// <returns></returns>
		public Vector2 LocalToScreen(Vector2 localPosition) {
			return localPosition + ScreenBounds.Location;
		}

		#endregion
	}
}