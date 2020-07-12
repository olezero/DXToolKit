using System;
using SharpDX.Direct2D1;
using SharpDX.DXGI;
using DeviceContext = SharpDX.Direct3D11.DeviceContext;

namespace DXToolKit {
	/// <summary>
	/// Component used by the graphics device
	/// </summary>
	public abstract class DeviceComponent : IDisposable {
		/// <summary>
		/// Reference to the base device
		/// </summary>
		protected readonly GraphicsDevice m_device;

		/// <summary>
		/// Reference to the device immediate context
		/// </summary>
		protected readonly DeviceContext m_context;

		/// <summary>
		/// Reference to the swapchain
		/// </summary>
		protected readonly SwapChain m_swapchain;

		/// <summary>
		/// Reference to the rendertarget
		/// </summary>
		protected RenderTarget m_renderTarget => m_device;

		/// <summary>
		/// Sets all internal variables on the device component
		/// </summary>
		/// <param name="device">Graphics device used by the component</param>
		protected DeviceComponent(GraphicsDevice device) {
			m_device = device;
			m_context = device;
			m_swapchain = device;
		}

		/// <summary>
		/// Disposes of the device component.
		/// </summary>
		public void Dispose() {
			OnDispose();
		}

		/// <summary>
		/// Called when the component is disposed, use to release all unmanaged memory
		/// </summary>
		protected abstract void OnDispose();
	}
}