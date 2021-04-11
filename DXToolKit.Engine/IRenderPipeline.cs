using System;
using SharpDX;
using SharpDX.Direct3D11;

namespace DXToolKit.Engine {
	/// <summary>
	/// Interface for whats needed for a basic render pipeline
	/// </summary>
	public interface IRenderPipeline : IDisposable {
		/// <summary>
		/// Should clear the backbuffer by the input color
		/// </summary>
		/// <param name="clearColor">Color to clear the backbuffer with, can be NULL</param>
		void Begin(Color? clearColor = null);

		/// <summary>
		/// Present the backbuffer to the screen
		/// </summary>
		/// <param name="syncInterval">VSync</param>
		void Present(int syncInterval = 0);

		/// <summary>
		/// Set render targets back to defaults stored in the pipeline
		/// </summary>
		void SetDefaultRenderTargets();

		/// <summary>
		/// Return the render target view connected to the back buffer
		/// </summary>
		RenderTargetView GetRenderTargetView();

		/// <summary>
		/// Return the depth buffer view
		/// </summary>
		/// <returns></returns>
		DepthStencilView GetDepthStencilView();
	}
}