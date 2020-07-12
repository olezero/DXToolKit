using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct2D1;

namespace DXToolKit.GUI {
	/// <summary>
	/// Needed class to be able to handle input and render a GUI
	/// </summary>
	public sealed class GUISystem : DeviceComponent {
		/// <summary>
		/// Simple empty class that acts as a container for all other gui elements
		/// </summary>
		private class BaseElementType : GUIElement {
			public BaseElementType() {
				// Should not be able to receive mouse input or keyboard input, probably
				CanReceiveMouseInput = false;
				CanReceiveKeyboardInput = false;
			}
		}

		private List<IDisposable> m_disposables = new List<IDisposable>();
		private GUIElement m_baseElement;

		internal GUIElement HoverTarget;
		internal GUIElement FocusTarget;
		internal GUIElement DragTarget;

		public GUIElement BaseElement => m_baseElement;
		public GraphicsDevice GraphicsDevice => m_device;


		public GUISystem(GraphicsDevice device, int width, int height) : base(device) {
			// Create the base element with the correct size
			m_baseElement = new BaseElementType {
				// Set bounds to screen input size, usually screen size
				Bounds = new RectangleF(0, 0, width, height),
			};
		}

		public void Update(GUIMouseEventArgs guiMouseArgs, GUIKeyboardArgs guiKeyboardArgs, out bool didSystemCaptureMouseInput, out bool didSystemCaptureKeyboardInput) {
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

		internal void ToDispose(IDisposable disposable) {
			m_disposables.Add(disposable);
		}

		protected override void OnDispose() {
			foreach (var disposable in m_disposables) {
				disposable?.Dispose();
			}

			m_disposables?.Clear();
			m_baseElement?.Dispose();
			HoverTarget = null;
			DragTarget = null;
			FocusTarget = null;

			// Some loose ends here to collect. All the elements are still in memory and should be cleaned up
			GC.Collect();
		}

		public void ResizeBaseElement(int width, int height) {
			m_baseElement.Width = width;
			m_baseElement.Height = height;
		}
	}
}