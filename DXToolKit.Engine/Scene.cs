using System;
using SharpDX.Direct2D1;
using SharpDX.DXGI;
using DeviceContext = SharpDX.Direct3D11.DeviceContext;
using DXToolKit.GUI;
using SharpDX;
using SharpDX.DirectWrite;

// ReSharper disable VirtualMemberNeverOverridden.Global

namespace DXToolKit.Engine {
	public abstract class Scene : FunctionToolBox, IDisposable {
		protected GraphicsDevice m_device;
		protected DeviceContext m_context => m_device;
		protected RenderTarget m_renderTarget => m_device;
		protected SwapChain m_swapchain => m_device;
		protected DXToolKit.GUI.GUIElement GUI => m_isGuiEnabled ? m_guiSystem.BaseElement : throw new Exception("Trying to access gui before calling EnableGUI()");

		private GUIRenderTexture m_guiTexture;

		private GUISystem m_guiSystem;

		private GUIMouseEventArgs m_guiMouseEventArgs;
		private GUIKeyboardArgs m_guiKeyboardArgs;

		private bool m_guiCaptureMouse = false;
		private bool m_guiCaptureKeyboard = false;

		protected bool GUICaptureMouse => m_guiCaptureMouse;
		protected bool GUICaptureKeyboard => m_guiCaptureKeyboard;

		private bool m_isGuiEnabled = false;
		private bool m_isLoaded = false;

		private GUIColorPalette m_guiColorPalette;
		private GUIDrawTools m_guiDrawTools;

		internal virtual void RunLoad() {
			m_device = Graphics.Device;

			m_guiMouseEventArgs = new GUIMouseEventArgs();
			m_guiKeyboardArgs = new GUIKeyboardArgs();
			m_guiSystem = new GUISystem(m_device, EngineConfig.ScreenWidth, EngineConfig.ScreenHeight);
			m_guiTexture = new GUIRenderTexture(m_device, EngineConfig.ScreenWidth, EngineConfig.ScreenHeight);

			m_device.OnResizeEnd += ResizeGUI;

			OnLoad();

			// Run garbage collection after load. Reason is that OnLoad will take some time anyways, and we might aswell run a quick GC after loading is complete
			GC.Collect();

			m_isLoaded = true;
		}

		internal virtual void RunUnload() {
			OnUnload();

			// Unsubscribe from event, unload can run more then once, so make sure device is set
			if (m_device != null) {
				m_device.OnResizeEnd -= ResizeGUI;
			}

			m_device = null;
			m_guiSystem?.Dispose();
			m_guiSystem = null;
			m_guiMouseEventArgs = null;
			m_guiKeyboardArgs = null;
			m_guiTexture?.Dispose();
			m_guiTexture = null;
			m_guiDrawTools = null;
			Utilities.Dispose(ref m_guiColorPalette);
			m_isGuiEnabled = false;

			GC.Collect();

			m_isLoaded = false;
		}

		internal virtual void RunUpdate() {
			if (!m_isLoaded) return;

			if (m_isGuiEnabled) {
				// Gather input for GUI
				UpdateGUIEventArgs();
				// Run GUI update to check if mouse or keyboard was captured before running scene update
				m_guiSystem.Update(Time.DeltaTime, m_guiMouseEventArgs, m_guiKeyboardArgs, out m_guiCaptureMouse, out m_guiCaptureKeyboard);
				// If GUI unloaded scene, get out of here
				if (!m_isLoaded) return;
			}

			// Run scene update
			Update();

			// If update unloaded scene, get out of here
			if (!m_isLoaded) return;


			if (m_isGuiEnabled) {
				// Run gui late update
				m_guiSystem.LateUpdate();
			}

			/*
			if (m_guiSystem.FocusElement != null) {
				Debug.Log(m_guiSystem.FocusElement + " " + m_guiSystem.FocusElement.Text);
			}
			*/
		}

		public void RunFixedUpdate() {
			if (!m_isLoaded) return;
			FixedUpdate();
		}

		internal virtual void RunRender() {
			// If not loaded, get out of here
			if (!m_isLoaded) return;

			// Run basic render calls
			Render();

			// Run post render calls
			PostRender();

			// Check that gui is enabled before trying to render
			if (m_isGuiEnabled) {
				// Set global draw tools and color palette before rendering GUI
				GUIDrawTools.Current = m_guiDrawTools;
				GUIColorPalette.Current = m_guiColorPalette;

				// Clear base texture
				m_guiTexture.Clear(Color.Transparent);

				// Get gui system to render the whole gui to the texture
				m_guiSystem.Render(m_guiTexture.RenderTarget);

				// Render the gui texture to the screen
				m_renderTarget.BeginDraw();
				m_renderTarget.DrawBitmap(m_guiTexture.Bitmap, 1.0F, BitmapInterpolationMode.Linear);
				m_renderTarget.EndDraw();
			}
		}

		protected virtual void OnLoad() { }
		protected virtual void OnUnload() { }
		protected virtual void FixedUpdate() { }
		protected virtual void Update() { }
		protected virtual void Render() { }
		protected virtual void PostRender() { }

		public void Dispose() {
			RunUnload();
		}

		private void UpdateGUIEventArgs() {
			m_guiMouseEventArgs.MousePosition = Input.MousePosition;
			m_guiMouseEventArgs.MouseMove = Input.MouseMove;

			m_guiMouseEventArgs.LeftMouseDown = Input.MouseDown(MouseButton.Left);
			m_guiMouseEventArgs.RightMouseDown = Input.MouseDown(MouseButton.Right);

			m_guiMouseEventArgs.LeftMousePressed = Input.MousePressed(MouseButton.Left);
			m_guiMouseEventArgs.RightMousePressed = Input.MousePressed(MouseButton.Right);

			m_guiMouseEventArgs.LeftMouseUp = Input.MouseUp(MouseButton.Left);
			m_guiMouseEventArgs.RightMouseUp = Input.MouseUp(MouseButton.Right);

			m_guiMouseEventArgs.LeftDoubleClick = Input.MouseDoubleClick(MouseButton.Left);
			m_guiMouseEventArgs.RightDoubleClick = Input.MouseDoubleClick(MouseButton.Right);

			m_guiMouseEventArgs.MouseWheelDelta = Input.MouseWheelDelta;

			m_guiKeyboardArgs.KeysDown = Input.KeysDown;
			m_guiKeyboardArgs.KeysUp = Input.KeysUp;
			m_guiKeyboardArgs.KeysPressed = Input.KeysPressed;
			m_guiKeyboardArgs.RepeatKey = Input.RepeatingKey;
			m_guiKeyboardArgs.TextInput = Input.TextInput;
		}

		private void ResizeGUI() {
			// Make sure gui system and gui texture exists
			if (m_guiSystem != null && m_guiTexture != null) {
				m_guiSystem.ResizeBaseElement(EngineConfig.ScreenWidth, EngineConfig.ScreenHeight);
				m_guiTexture.Resize(EngineConfig.ScreenWidth, EngineConfig.ScreenHeight);
			}
		}


		protected void EnableGUI(GUIColorPalette palette = null, GUIDrawTools drawTools = null) {
			if (m_isGuiEnabled) {
				return;
			}

			m_isGuiEnabled = true;

			// Dispose of current palette if already set
			Utilities.Dispose(ref m_guiColorPalette);

			m_guiColorPalette = palette ?? new GUIColorPalette(m_device, GUIColorPaletteDescription.Cyborg);
			m_guiDrawTools = drawTools ?? new BasicGUIDrawTools();
			SetTooltipElement(new BasicTooltipElement());
		}


		/// <summary>
		/// Sets the element to be used as a tool tip
		/// </summary>
		public void SetTooltipElement(ITooltipElement guiElement) {
			if (m_isGuiEnabled) {
				m_guiSystem.SetTooltipElement(guiElement);
			}
		}

		/// <summary>
		/// Sets the time in milliseconds the mouse has to hover over a element before a tooltip is displayed
		/// Default: 1000ms
		/// </summary>
		public void SetTooltipPopupDelay(float delay) {
			if (m_isGuiEnabled) {
				m_guiSystem.SetTooltipPopupDelay(delay);
			}
		}
	}
}