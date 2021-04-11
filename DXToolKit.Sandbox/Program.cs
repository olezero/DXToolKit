namespace DXToolKit.Sandbox {
	internal class Program {
		public static int Main(string[] args) {
			using (var sketch = new FrameRateTest()) {
				return sketch.Run(args);
			}
		}
	}
}