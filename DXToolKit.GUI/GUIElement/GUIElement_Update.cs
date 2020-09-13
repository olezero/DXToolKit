using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX.DirectInput;

namespace DXToolKit.GUI {
	public partial class GUIElement {
		#region Update

		/// <summary>
		/// Pre updates to clear variables, move elements around, etc
		/// </summary>
		internal void PreFrame(GUISystem guiSystem) {
			if (m_enabled == false || IsDisposing || IsDisposed) return;

			// Reset all per frame variables
			MouseHovering = guiSystem.DragTarget == this;
			IsDragged = guiSystem.DragTarget == this;
			m_isFocused = guiSystem.FocusTarget == this;
			// m_isFocused = false;
			m_isMousePressed = false;
			m_containsMouse = false;

			if (m_isNewFocusTarget) {
				SetFocusTarget(this, guiSystem);
				m_isNewFocusTarget = false;
			}


			// Reshuffle children based on move actions added last frame
			/*
				if (m_moveActions.Count > 0) {
					foreach (var childElement in m_childElements) {
						childElement.ToggleRedraw();
					}
				}
			*/

			// If moving children, toggle a redraw to redraw all bitmaps on this element
			if (m_moveActions.Count > 0) {
				ToggleRedraw();
			}

			while (m_moveActions.Count > 0) {
				m_moveActions.Dequeue().Invoke();
			}

			// Get children to reset all variables
			for (var i = 0; i < m_childElements.Count; i++) {
				m_childElements[i].PreFrame(guiSystem);
			}

			// Run pre update after sorting etc is complete
			OnPreUpdate();
		}

		/// <summary>
		/// Runs virtual on update on all children
		/// </summary>
		internal void Update() {
			if (m_enabled == false || IsDisposing || IsDisposed) return;

			for (int i = m_childElements.Count - 1; i >= 0; i--) {
				m_childElements[i].Update();
			}

			OnUpdate();
		}

		/// <summary>
		/// Late update, run before rendering
		/// </summary>
		internal void LateUpdate() {
			// Get out of here if disposing
			if (m_enabled == false || IsDisposing || IsDisposed) return;

			if (m_hasBoundsChanged) {
				// Flip trigger
				m_hasBoundsChanged = false;
				// Call events
				OnBoundsChanged();
				// Toggle a redraw if bounds has changed
				ToggleRedraw();
			}

			if (m_hasLocationChanged) {
				// Flip trigger
				m_hasLocationChanged = false;
				// Call events
				OnLocationChanged();
			}

			if (m_hasSizeChanged) {
				// Flip trigger
				m_hasSizeChanged = false;
				// Call events
				OnResize();
				// Toggle a texture resize
				ToggleResize();
			}

			if (m_hasTextChanged) {
				// Flip trigger
				m_hasTextChanged = false;
				// Toggle a redraw
				ToggleRedraw();
				// Call event
				OnTextChanged(Text);
			}

			if (m_hasTextPropsChanged) {
				// Flip trigger
				m_hasTextPropsChanged = false;
				// Toggle redraw
				ToggleRedraw();
				// Fire events
				OnTextPropertiesChanged();
			}

			// Start with parent late update
			OnLateUpdate();

			// Then move to children
			for (int i = 0; i < m_childElements.Count; i++) {
				m_childElements[i].LateUpdate();
			}
		}

		/// <summary>
		/// Handles mouse input, returns a value indicating if the control has grabbed the mouse input
		/// </summary>
		internal bool HandleMouse(GUIMouseEventArgs args, GUISystem guiSystem) {
			// Get out of here if disposing
			if (IsDisposing || IsDisposed) return false;
			// Get out of here if element is not enabled
			if (m_enabled == false) return false;
			// Might not want to handle mouse events if the element is not visible
			if (m_visible == false) return false;


			bool isMouseHandled = false;
			//bool containsMouse = MouseScreenBounds.Contains(args.MousePosition);
			m_containsMouse = MouseScreenBounds.Contains(args.MousePosition);

			// Early handle of scrolling, since its kinda on top of everything else
			// Just dont run it if drag target is set
			// Reason for it being early is that usually renderoffset is set by mousewheel, and hover etc on child elements should reflect that in the same frame
			if (CanReceiveMouseInput && m_containsMouse && guiSystem.DragTarget == null) {
				if (m_mouseWheelNeedsFocus) {
					if (m_containsFocus) {
						if (Mathf.Abs(args.MouseWheelDelta) > 0) {
							OnMouseWheel(args.MouseWheelDelta);
						}
					}
				} else {
					if (Mathf.Abs(args.MouseWheelDelta) > 0) {
						OnMouseWheel(args.MouseWheelDelta);
					}
				}
			}

			// If drag target is currently set but left mouse button is not pressed, release the drag target
			if (guiSystem.DragTarget != null && args.LeftMousePressed == false) {
				guiSystem.DragTarget?.OnDragStop();
				guiSystem.DragTarget = null;
				// TODO - might have to run left mouse up here if contained
			}

			// If drag target is this element, end the left mouse is being pressed, continue calling drag
			if (guiSystem.DragTarget == this && args.LeftMousePressed) {
				OnDrag();
				// TODO - should probably run MousePressed events here
				// Reason is that if we start pressing a button and move the mouse away from the button it should still look "pressed" while the mouse is down
				// Click event should only be called if mouse is up while its actually hovering over the element
			}

			// Should only propagate mouse checking, if this control actually contains the mouse position
			if (m_containsMouse) {
				// Running in reverse because the last added child should be the first to receive update check
				for (int i = m_childElements.Count - 1; i >= 0; i--) {
					var childElement = m_childElements[i];
					// Get information if child has handled mouse input
					isMouseHandled = childElement.HandleMouse(args, guiSystem);
					// If child has handled mouse input, break loop since we dont need to check any more children
					if (isMouseHandled) {
						break;
					}
				}
			}

			// Somethings getting dragged, but its not this element, we should not handle any input
			if (guiSystem.DragTarget != null && guiSystem.DragTarget != this) {
				return false;
			}

			// If mouse is not yet handled, and current element contains mouse, set hover target
			if (!isMouseHandled && m_containsMouse) {
				// TODO - This will set hover target to utmost child first, before setting it to the correct one if "CanReceiveMouseInput" on the child is false
				// TODO - It works, but is that a good idea?
				// TODO - This causes underlying elements to call Mouse Enter and Mouse Leave each frame since hover target switches to child element, then switches back to parent element even if it should never switch to the child element
				// TODO - Special rule if m_parentElement is null?
				// Set hover target makes sure that element can receive input before calling events atleast
				// Top level element should get hover even if it has "CanReceiveMouseInput" as false


				// If element can receive focus or parent element is null, set hover target (This is to allow base element to take hover, which in turn will allow mouse to "stop" hovering over any elements at all)
				if (CanReceiveMouseInput || m_parentElement == null) {
					SetHoverTarget(this, guiSystem);
				}
			}

			// If mouse has not yet been handled by a child element, try to handle it here
			if (!isMouseHandled && CanReceiveMouseInput) {
				// If element contains mouse, set hover to true
				if (m_containsMouse) {
					// Set variable to indicate that mouse is hovering to true
					MouseHovering = true;
					// Call events
					OnMouseHover();
					// Handled to true
					isMouseHandled = true;
					// Do input checking, like clicking etc
					HandleMouseInput(args, guiSystem);
				}
			}


			// Return a status indicating if this element has handled mouse input (Usually true for the outer most child that contains the mouse position)
			return isMouseHandled;
		}


		private void HandleMouseInput(GUIMouseEventArgs args, GUISystem guiSystem) {
			if (args.LeftMouseDown && guiSystem.DragTarget != this && Draggable) {
				guiSystem.DragTarget = this;
				OnDragStart();
			}

			if (args.LeftMousePressed || args.RightMousePressed) {
				OnMousePressed(args);
				m_isMousePressed = true;
			}

			if (args.LeftMouseDown || args.RightMouseDown) {
				OnMouseDown(args);
			}

			if (args.LeftMouseUp || args.RightMouseUp) {
				OnMouseUp(args);
				OnClick(args);
			}

			if (m_isFocused) {
				if (args.LeftDoubleClick || args.RightDoubleClick) {
					OnDoubleClick(args);
				}
			}
		}


		internal bool HandleKeyboard(GUIKeyboardArgs args, GUISystem guiSystem) {
			// Get out of here if we cant receive any keyboard input
			if (CanReceiveKeyboardInput == false) {
				return false;
			}

			// Temp var to check if any keys were pressed when checking for input
			var result = false;

			// Make sure this is the focus target
			if (guiSystem.FocusTarget == this) {
				// Handle keyboard input based on args
				if (args.KeysUp.Count > 0) {
					OnKeyUp(args.KeysUp);
					result = true;
				}

				if (args.KeysDown.Count > 0) {
					OnKeyDown(args.KeysDown);
					result = true;
				}

				if (args.KeysPressed.Count > 0) {
					OnKeyPressed(args.KeysPressed);
					result = true;
				}

				if (string.IsNullOrEmpty(args.TextInput) == false) {
					OnTextInput(args.TextInput);
					result = true;
				}

				if (args.RepeatKey != null) {
					OnRepeatKey((Key) args.RepeatKey);
					result = true;
				}

				// Handle tabbing here
				if (args.RepeatKey == Key.Tab) {
					if (args.KeysPressed.Contains(Key.LeftShift) || args.KeysPressed.Contains(Key.LeftShift)) {
						// Prev
						TabPrevious();
					} else {
						// Next
						TabNext();
					}
				}
			}

			// Return true if this control can capture input and result is true
			return CanCaptureKeyboardInput && result;
		}


		private void SetHoverTarget(GUIElement target, GUISystem guiSystem) {
			// If new target is different, run events
			if (guiSystem.HoverTarget != target) {
				// Check if current target is set, and that it can receive mouse input
				if (guiSystem.HoverTarget != null && guiSystem.HoverTarget.CanReceiveMouseInput) {
					// Call on leave
					guiSystem.HoverTarget.OnMouseLeave();
				}

				// Check if new target can receive input
				if (target.CanReceiveMouseInput) {
					// Call on enter
					target.OnMouseEnter();
				}

				// Apply new hover target. This should not be affected by CanReceiveMouseInput, since we want to set hover target to a base element if mouse is not over anything
				guiSystem.HoverTarget = target;
			}
		}

		/// <summary>
		/// Sets focus on this element the next PreUpdate
		/// </summary>
		public void Focus() {
			// Changed to this since focus changing needs gui system
			m_isNewFocusTarget = true;
		}

		private static void SetFocusTarget(GUIElement target, GUISystem guiSystem) {
			// Get out if focus target has not changed
			if (target == guiSystem.FocusTarget) {
				return;
			}

			// If input target is null, all focus should be lost
			if (target == null) {
				if (guiSystem.FocusTarget != null) {
					// Need to reset all of focus target's parents that contain focus
					var parent = guiSystem.FocusTarget.Parent;
					// Recurse up through the tree to find all parents of the current focus target
					while (parent != null) {
						// If parent knows it contains focus, run disable contains focus and run events
						if (parent.m_containsFocus) {
							parent.m_containsFocus = false;
							parent.OnContainFocusLost();
						}

						parent = parent.Parent;
					}

					guiSystem.FocusTarget.m_isFocused = false;
					guiSystem.FocusTarget.OnFocusLost();
					guiSystem.FocusTarget = null;
				}
			} else {
				// Only switch if new target can receive focus
				if (target.CanReceiveFocus) {
					// If target exists, lose focus
					guiSystem.FocusTarget?.OnFocusLost();
					var currentFocusTarget = guiSystem.FocusTarget;
					guiSystem.FocusTarget = target;
					guiSystem.FocusTarget.OnFocusGained();
					CalculateContainsFocus(target, currentFocusTarget);
				}
			}
		}

		private static void CalculateContainsFocus(GUIElement newTarget, GUIElement oldTarget) {
			foreach (var el in newTarget.Lineage()) {
				if (el.m_containsFocus == false) {
					el.OnContainFocusGained();
				}

				el.m_containsFocus = true;
			}

			if (oldTarget != null) {
				foreach (var el in oldTarget.Lineage()) {
					var stillContains = false;
					foreach (var ch in el.AllChildren()) {
						if (ch == newTarget) {
							stillContains = true;
						}
					}

					if (el == newTarget) {
						stillContains = true;
					}

					if (!stillContains) {
						el.m_containsFocus = false;
						el.OnContainFocusLost();
					} else {
						// Can break if we found a element in the lineage that still should contain focus
						break;
					}
				}
			}
		}

		/// <summary>
		/// Gets the lineage going from this element all the way to the root element
		/// </summary>
		/// <param name="includeSelf">If result should include this element</param>
		/// <returns>Enumerable of GUIElements</returns>
		public IEnumerable<GUIElement> Lineage(bool includeSelf = true) {
			var target = this;
			if (includeSelf == false) {
				target = Parent;
			}

			while (target != null) {
				yield return target;
				target = target.Parent;
			}
		}

		#endregion
	}
}