namespace DXToolKit.Engine {
	/// <summary>
	/// Provides a interface to allow object to be used by the GUIGrid
	/// </summary>
	public interface IGUIGriddable {
		/// <summary>
		/// Local X position of the object
		/// </summary>
		float X { get; set; }

		/// <summary>
		/// Local Y position of the object
		/// </summary>
		float Y { get; set; }

		/// <summary>
		/// Width of the object
		/// </summary>
		float Width { get; set; }

		/// <summary>
		/// Height of the object
		/// </summary>
		float Height { get; set; }
	}
}