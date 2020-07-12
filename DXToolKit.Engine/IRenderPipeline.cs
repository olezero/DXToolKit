using System;
using SharpDX;

namespace DXToolKit.Engine {
	public interface IRenderPipeline : IDisposable {
		void Begin(Color? clearColor = null);
		void Present(int syncInterval = 0);
		void SetDefaultRenderTargets();
	}
}