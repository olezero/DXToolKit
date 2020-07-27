namespace DXToolKit.Sandbox {
	internal class Program {
		public static int Main(string[] args) {
			using (var sketch = new GizmoTestingSketch()) {
				return sketch.Run(args);
			}
		}
	}
}