using SharpDX;

namespace DXToolKit {
	/// <summary>
	/// Holds a collection of different factory's used by DirectX
	/// </summary>
	public class FactoryCollection : System.IDisposable {
		private SharpDX.DXGI.Factory1 m_dxgiFactory;
		private SharpDX.Direct2D1.Factory m_d2dFactory;
		private SharpDX.DirectWrite.Factory m_dwFactory;

		/// <summary>
		/// Gets a reference to the DXGI Factory
		/// </summary>
		public SharpDX.DXGI.Factory1 DXGIFactory => m_dxgiFactory;

		/// <summary>
		/// Gets a reference to the Direct2D Factory
		/// </summary>
		public SharpDX.Direct2D1.Factory D2DFactory => m_d2dFactory;

		/// <summary>
		/// Gets a reference to the DirectWrite Factory.
		/// </summary>
		public SharpDX.DirectWrite.Factory DWFactory => m_dwFactory;

		/// <summary>
		/// Creates a new instance of the factory collection.
		/// </summary>
		/// <param name="dxgiFactory">DXGI Factory</param>
		/// <param name="d2dFactory">Direct2D Factory</param>
		/// <param name="dwFactory">DirectWrite Factory</param>
		public FactoryCollection(SharpDX.DXGI.Factory1 dxgiFactory, SharpDX.Direct2D1.Factory d2dFactory, SharpDX.DirectWrite.Factory dwFactory) {
			m_dxgiFactory = dxgiFactory;
			m_d2dFactory = d2dFactory;
			m_dwFactory = dwFactory;
		}

		/// <summary>
		/// Disposes of the FactoryCollection. Probably not a good idea to do this mid runtime, should only be done before closing the application.
		/// </summary>
		public void Dispose() {
			Utilities.Dispose(ref m_dxgiFactory);
			Utilities.Dispose(ref m_d2dFactory);
			Utilities.Dispose(ref m_dwFactory);
		}

		/// <summary>
		/// Gets the DXGI factory
		/// </summary>
		/// <param name="collection">The current factory collection</param>
		/// <returns>SharpDX.DXGI.Factory</returns>
		public static implicit operator SharpDX.DXGI.Factory(FactoryCollection collection) => collection.m_dxgiFactory;

		/// <summary>
		/// Gets the Direct2D Factory
		/// </summary>
		/// <param name="collection">The current factory collection</param>
		/// <returns>SharpDX.Direct2D.Factory</returns>
		public static implicit operator SharpDX.Direct2D1.Factory(FactoryCollection collection) => collection.m_d2dFactory;

		/// <summary>
		/// Gets the DirectWrite Factory
		/// </summary>
		/// <param name="collection">The current factory collection</param>
		/// <returns>SharpDX.DirectWrite.Factory</returns>
		public static implicit operator SharpDX.DirectWrite.Factory(FactoryCollection collection) => collection.m_dwFactory;
	}
}