using SharpDX;
using SharpDX.Direct3D11;

namespace DXToolKit.Sandbox {
	internal class Program {
		public static int Main(string[] args) {
			using (var sketch = new QuadtreeTesting()) {
				return sketch.Run(args);
			}
		}
	}
}