using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectInput;

namespace DXToolKit.GUI {
	/// <summary>
	/// Needed class to be able to handle input and render a GUI
	/// </summary>
	public sealed class GUISystem : DeviceComponent {
		/// <summary>
		/// Simple empty class that acts as a container for all other gui elements
		/// </summary>
		private class BaseElementType : GUIElement {
			public BaseElementType() : base(true) {
				// Should not be able to receive mouse input or keyboard input, probably
				CanReceiveMouseInput = false;
				CanReceiveKeyboardInput = false;
			}
		}

		/// <summary>
		/// Disposable list for cleanup when this system is disposed
		/// </summary>
		private List<IDisposable> m_disposables = new List<IDisposable>();

		/// <summary>
		/// The base element used to run update/render on
		/// </summary>
		private GUIElement m_baseElement;

		/// <summary>
		/// Controller for the hover target.
		/// Internal so GUIElements can set it
		/// </summary>
		internal GUIElement HoverTarget;

		/// <summary>
		/// Controller for the focus target
		/// Internal so GUIElements can set it
		/// </summary>
		internal GUIElement FocusTarget;

		/// <summary>
		/// Controller for the drag target
		/// Internal so GUIElements can set it
		/// </summary>
		internal GUIElement DragTarget;

		/// <summary>
		/// Gets the current element that is in focus
		/// </summary>
		public GUIElement FocusElement => FocusTarget;

		/// <summary>
		/// Gets the current element the mouse is over
		/// </summary>
		public GUIElement HoverElement => HoverTarget;

		/// <summary>
		/// Gets the current element that is being dragged
		/// </summary>
		public GUIElement DragElement => DragTarget;

		/// <summary>
		/// Gets the base element of the system
		/// </summary>
		public GUIElement BaseElement => m_baseElement;

		/// <summary>
		/// Gets the graphics device used by the GUISystem
		/// </summary>
		public GraphicsDevice GraphicsDevice => m_device;


		/// <summary>
		/// Creates a new GUI System to handle update/rendering of a GUI
		/// </summary>
		/// <param name="device">Device used to create resources like render textures and fonts</param>
		/// <param name="width">Width of the base element</param>
		/// <param name="height">Height of the base element</param>
		public GUISystem(GraphicsDevice device, int width, int height) : base(device) {
			// Create the base element with the correct size
			m_baseElement = new BaseElementType {
				// Set bounds to screen input size, usually screen size
				Bounds = new RectangleF(0, 0, width, height),
			};
			// Set base element
			GUIElement.BaseElement = m_baseElement;
		}

		/// <summary>
		/// Runs update on the GUI system
		/// </summary>
		/// <param name="deltaTime">The time since the last update</param>
		/// <param name="guiMouseArgs">Mouse event args to pass onto each element in the GUISystem</param>
		/// <param name="guiKeyboardArgs">Keyboard event args to pass onto each element in the GUISystem</param>
		/// <param name="didSystemCaptureMouseInput">Output value indicating if any element in the GUISystem handled mouse input</param>
		/// <param name="didSystemCaptureKeyboardInput">Output value indicating if any element in the GUISystem handled keyboard input</param>
		public void Update(float deltaTime, GUIMouseEventArgs guiMouseArgs, GUIKeyboardArgs guiKeyboardArgs, out bool didSystemCaptureMouseInput, out bool didSystemCaptureKeyboardInput) {
			// Default to false
			didSystemCaptureMouseInput = false;
			didSystemCaptureKeyboardInput = false;

			// Run pre frame update on base element to reset internal variables etc
			m_baseElement.PreFrame(this);

			// If there is a hover target, and left or right mouse is pressed, set focus
			if (HoverTarget != null && (guiMouseArgs.LeftMouseDown || guiMouseArgs.RightMouseDown)) {
				HoverTarget.Focus();
			}

			// If drag target is not null, just run handle mouse on that
			if (DragTarget != null) {
				// Run input handling on drag target
				DragTarget.HandleMouse(guiMouseArgs, this);
				// Set capture to true since we are dragging
				didSystemCaptureMouseInput = true;
			} else {
				// Else run normal input handling
				didSystemCaptureMouseInput = m_baseElement.HandleMouse(guiMouseArgs, this);
			}

			// Special rule if drag target was set this frame, we need to make sure system capture mouse is true, since dragging can start when m_baseElement.HandleMouse is called
			if (DragTarget != null) {
				didSystemCaptureMouseInput = true;
			}


			// Only run keyboard input on focus target
			if (FocusTarget != null && FocusTarget != m_baseElement) {
				didSystemCaptureKeyboardInput = FocusTarget.HandleKeyboard(guiKeyboardArgs, this);
			}

			// Run update on all elements
			m_baseElement.Update();

			// Handle tooltip stuff
			HandleTooltip(deltaTime, guiMouseArgs, guiKeyboardArgs);
		}


		/// <summary>
		/// Should be called just before rendering
		/// </summary>
		public void LateUpdate() {
			m_baseElement.LateUpdate();
		}

		/// <summary>
		/// Renders the GUI
		/// </summary>
		/// <param name="renderTarget">The rendertarget to render the GUI to</param>
		public void Render(RenderTarget renderTarget) {
			renderTarget.Transform = Matrix3x2.Identity;
			renderTarget.BeginDraw();
			m_baseElement.Render(renderTarget, this);
			renderTarget.EndDraw();
		}

		/// <summary>
		/// Helper to handle disposing of resources in GUIElements
		/// </summary>
		/// <param name="disposable"></param>
		internal void ToDispose(IDisposable disposable) {
			m_disposables.Add(disposable);
		}

		/// <inheritdoc />
		protected override void OnDispose() {
			foreach (var disposable in m_disposables) {
				disposable?.Dispose();
			}

			// Remove references to base element
			if (GUIElement.BaseElement == m_baseElement) {
				GUIElement.BaseElement = null;
			}

			m_disposables?.Clear();
			Utilities.Dispose(ref m_tooltipElement);
			Utilities.Dispose(ref m_baseElement);
			HoverTarget = null;
			DragTarget = null;
			FocusTarget = null;

			// Some loose ends here to collect. All the elements are still in memory and should be cleaned up
			GC.Collect();
		}

		/// <summary>
		/// Resizes the base element to the input values
		/// </summary>
		public void ResizeBaseElement(int width, int height) {
			m_baseElement.Width = width;
			m_baseElement.Height = height;
		}


		private void HandleTooltip(float deltaTime, GUIMouseEventArgs mouseArgs, GUIKeyboardArgs keyboardArgs) {
			// If tooltip element is not set, get out of here
			if (m_tooltipElement == null) return;
			// Can be null if mouse is outside window bounds
			if (m_tooltipTarget == null) return;

			// If hover target is base element, ignore tooltip stuff
			if (HoverTarget == m_baseElement) {
				// Close if visible
				CloseToolTip();
				// Reset delay
				m_tooltipCurrentTime = 0.0F;
				// Get out of here
				return;
			}

			// If tooltip is visible and mouse has moved, close it
			if (m_isTooltipVisible && mouseArgs.MouseMove.Length() > 0.0F) {
				CloseToolTip();
				m_tooltipCurrentTime = 0.0F;
			}

			if (m_isTooltipVisible && keyboardArgs.KeysDown.Contains(Key.Escape)) {
				CloseToolTip();
				m_tooltipCurrentTime = 0.0F;
			}

			// Mouse is not hovering over base element, meaning we can do tooltip stuff
			if (m_tooltipTarget != HoverTarget) {
				// Close
				CloseToolTip();
				// Reset timer
				m_tooltipCurrentTime = deltaTime * 1000;
				// Set tool tip target
				m_tooltipTarget = HoverTarget;
				// Get out and wait for next frame
				return;
			}

			// Tooltip target and hover target should be the same
			m_tooltipCurrentTime += deltaTime * 1000;

			// If current timer is equal or greater then delay, open tooltip
			if (m_tooltipCurrentTime >= m_tooltipDelay) {
				// If hover target has any tooltip text
				if (m_tooltipTarget.Tooltip != null) {
					// Show tooltip with current text and mouse position
					ShowToolTip(m_tooltipTarget.Tooltip, mouseArgs.MousePosition);
				}
			}
		}


		private GUIElement m_tooltipElement;
		private float m_tooltipDelay = 1000.0F;
		private bool m_isTooltipVisible = false;
		private float m_tooltipCurrentTime;
		private GUIElement m_tooltipTarget;

		/// <summary>
		/// Sets a tool tip element to be used.
		/// This element has to inherit from GUIElement.
		/// GUISystem takes care of disposing of the element.
		/// </summary>
		public void SetTooltipElement(ITooltipElement element) {
			if (element is GUIElement guiElement) {
				if (m_tooltipElement != null) Utilities.Dispose(ref m_tooltipElement);
				m_tooltipElement = guiElement;
				m_tooltipElement.CanReceiveFocus = false;
				m_tooltipElement.CanReceiveMouseInput = false;
				m_tooltipElement.CanReceiveKeyboardInput = false;
			} else {
				throw new Exception("Input tool tip element must be a GUIElement");
			}
		}

		/// <summary>
		/// Sets the time delay in milliseconds for the tool tip to appear
		/// </summary>
		/// <param name="timeInMilliseconds">The time in milliseconds</param>
		public void SetTooltipPopupDelay(float timeInMilliseconds) {
			m_tooltipDelay = timeInMilliseconds;
		}

		private void ShowToolTip(string text, Vector2 mouseposition) {
			if (m_isTooltipVisible) return; // Makes sure this method only runs once
			if (DragTarget != null) return; // No tooltips while something is being dragged
			if (m_tooltipElement != null && m_baseElement != null) {
				m_baseElement.Append(m_tooltipElement);
				((ITooltipElement) m_tooltipElement)?.OnOpen(text, mouseposition);
				m_isTooltipVisible = true;
			}
		}

		private void CloseToolTip() {
			if (m_isTooltipVisible == false) return;
			if (m_tooltipElement != null && m_baseElement != null) {
				((ITooltipElement) m_tooltipElement)?.OnClose();
				m_isTooltipVisible = false;
			}
		}
	}
}