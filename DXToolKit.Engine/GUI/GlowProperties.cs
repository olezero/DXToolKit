namespace DXToolKit.Engine {
	/// <summary>
	/// Properties used when rendering glow effects
	/// </summary>
	public class GlowProperties {
		/// <summary>
		/// Gets or sets the size of the glow (in pixels from the full opacity to zero opacity)
		/// Disable if less then 0.01F
		/// </summary>
		public float Size;

		/// <summary>
		/// Gets or sets the opacity of the glow
		/// Disable if less then 0.01F
		/// </summary>
		public float Opacity;

		/// <summary>
		/// Gets or sets the color of the glow
		/// </summary>
		public GUIColor Color;

		/// <summary>
		/// Copy the glow parameters to a new instance
		/// </summary>
		internal GlowProperties Copy() {
			return new GlowProperties {
				Size = Size,
				Opacity = Opacity,
				Color = Color,
			};
		}

		/// <inheritdoc />
		public override string ToString() {
			return $"Size: {Size}, Opacity: {Opacity}, Color: {Color}";
		}
	}
}