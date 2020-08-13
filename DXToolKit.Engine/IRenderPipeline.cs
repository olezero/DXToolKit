using System;
using SharpDX;
using SharpDX.Direct3D11;

namespace DXToolKit.Engine {
	public interface IRenderPipeline : IDisposable {
		void Begin(Color? clearColor = null);
		void Present(int syncInterval = 0);
		void SetDefaultRenderTargets();
		RenderTargetView GetRenderTargetView();
		DepthStencilView GetDepthStencilView();
	}
}