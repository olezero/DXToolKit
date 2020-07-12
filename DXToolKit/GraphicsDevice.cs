using System;
using System.Windows.Forms;
using SharpDX.DXGI;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using DeviceContext = SharpDX.Direct3D11.DeviceContext;
using Device = SharpDX.Direct3D11.Device;
using FeatureLevel = SharpDX.Direct3D.FeatureLevel;

namespace DXToolKit {
	/// <summary>
	/// Base device used for all things DirectX
	/// </summary>
	public class GraphicsDevice : IDisposable {
		/// <summary>
		/// Event called directly after a resize request
		/// </summary>
		public event Action<ModeDescription, bool> EarlyResizeBegin;

		/// <summary>
		/// Event called before resizing the backbuffer. Useful for disposing of anything that references the backbuffer.
		/// </summary>
		public event Action OnResizeBegin;

		/// <summary>
		/// Event called after the backbuffer has been resized. Useful for recreating resources that references the backbuffer.
		/// </summary>
		public event Action OnResizeEnd;

		private FactoryCollection m_factory;
		private Device m_comDevice;
		private DeviceContext m_deviceContext;
		private SwapChain m_swapchain;
		private RenderTarget m_renderTarget;

		/// <summary>
		/// Gets a reference to the factory collection used by the graphics device.
		/// </summary>
		public FactoryCollection Factory => m_factory;

		/// <summary>
		/// Gets a reference to the com device used to create everything
		/// </summary>
		public Device ComDevice => m_comDevice;

		/// <summary>
		/// Gets a reference to the immediate context used by the device
		/// </summary>
		public DeviceContext Context => m_deviceContext;

		/// <summary>
		/// Gets a reference to the swapchain.
		/// </summary>
		public SwapChain Swapchain => m_swapchain;

		/// <summary>
		/// Gets a reference to the rendertarget used for direct2d rendering
		/// Be aware that after resizing backbuffer, the rendertarget will be recreated.
		/// </summary>
		public RenderTarget RenderTarget => m_renderTarget;

		/// <summary>
		/// Creates a new graphics device
		/// </summary>
		/// <param name="factory">Factory collection used for creation</param>
		/// <param name="window"> Window to tie the swapchain to</param>
		/// <param name="modeDescription">Mode description for the output width/height/format/refreshrate. NOTE: Device only supports Format.R8G8B8A8_UNorm</param>
		public GraphicsDevice(FactoryCollection factory, IWin32Window window, ModeDescription modeDescription) {
			if (window == null) throw new ArgumentNullException(nameof(window));
			m_factory = factory ?? throw new ArgumentNullException(nameof(factory));
			m_comDevice = new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport, FeatureLevel.Level_11_0);
			m_deviceContext = m_comDevice.ImmediateContext;
			m_swapchain = new SwapChain(m_factory, m_comDevice, new SwapChainDescription() {
				ModeDescription = modeDescription,
				Flags = SwapChainFlags.AllowModeSwitch,
				Usage = Usage.RenderTargetOutput,
				BufferCount = 1,
				IsWindowed = true,
				OutputHandle = window.Handle,
				SampleDescription = new SampleDescription(4, 0),
				SwapEffect = SwapEffect.Discard,
			});
			m_swapchain.ResizeTarget(ref modeDescription);
			CreateRenderTarget();
			OnResizeBegin += DisposeRenderTarget;
			OnResizeEnd += CreateRenderTarget;
		}

		/// <summary>
		/// Resizes the backbuffer to the new input mode description.
		/// Make sure any references to the old backbuffer is cleared before calling this method.
		/// </summary>
		/// <param name="modeDescription">The new mode description of the backbuffer</param>
		/// <param name="fullscreen">Use fullscreen.</param>
		public void Resize(ModeDescription modeDescription, bool fullscreen = false) {
			if (modeDescription.Format != Format.R8G8B8A8_UNorm) {
				throw new Exception("Graphics device only supports Format.R8G8B8A8_UNorm");
			}

			if (modeDescription.Width == 0 || modeDescription.Height == 0) return;

			// Run early resize begin to allow for system to update values
			EarlyResizeBegin?.Invoke(modeDescription, fullscreen);

			// Call event to allow for other resources to release their pointers to the back buffer.
			OnResizeBegin?.Invoke();

			// Resize buffers
			m_swapchain.ResizeBuffers(
				1,
				modeDescription.Width,
				modeDescription.Height,
				modeDescription.Format,
				SwapChainFlags.AllowModeSwitch
			);

			// Only change fullscreen mode if fullscreen has changed.
			if (m_swapchain.IsFullScreen != fullscreen) {
				if (fullscreen) {
					// Get containing output
					var output = m_swapchain.ContainingOutput;

					// If function success, set fullscreen state.
					if (output != null && output.IsDisposed == false) {
						m_swapchain.SetFullscreenState(true, output);
					}

					// Dispose of the output interface
					output?.Dispose();
				} else {
					// If setting to windowed, just pass null for the output.
					m_swapchain.SetFullscreenState(false, null);
				}
			}

			// Resize targets after fullscreen check, so when going from fullscreen to windowed the window is the correct size.
			m_swapchain.ResizeTarget(ref modeDescription);

			// Call event to allow for other resources to create their references to the new backbuffer.
			OnResizeEnd?.Invoke();
		}

		public void UnpackUnmanagedEffectFiles() {
			// Unpack DLL files
			UnmanagedDll.UnmanagedDLLManager.Unpack();
		}

		private void CreateRenderTarget() {
			// Get references to backbuffer as DXGI surface
			var backbuffer = m_swapchain.GetBackBuffer<Texture2D>(0);
			var surface = backbuffer.QueryInterface<Surface1>();
			// Create rendertarget using the backbuffer surface
			m_renderTarget = new RenderTarget(m_factory, surface, new RenderTargetProperties() {
				Type = RenderTargetType.Hardware,
				PixelFormat = new PixelFormat() {
					Format = Format.R8G8B8A8_UNorm,
					AlphaMode = AlphaMode.Premultiplied,
				},
			});
			// Dispose references to the backbuffer
			surface?.Dispose();
			backbuffer?.Dispose();
		}

		private void DisposeRenderTarget() {
			// Dispose of the rendertarget
			m_renderTarget?.Dispose();
		}

		public static implicit operator Device(GraphicsDevice device) => device.m_comDevice;
		public static implicit operator DeviceContext(GraphicsDevice device) => device.m_deviceContext;
		public static implicit operator SwapChain(GraphicsDevice device) => device.m_swapchain;
		public static implicit operator RenderTarget(GraphicsDevice device) => device.m_renderTarget;

		public void Dispose() {
			m_comDevice?.Dispose();
			m_deviceContext?.Dispose();
			m_swapchain?.Dispose();
			m_renderTarget?.Dispose();
		}
	}
}