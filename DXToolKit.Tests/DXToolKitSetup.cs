using System;
using NUnit.Framework;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Windows;

namespace DXToolKit.Tests {
	[SetUpFixture]
	public class DXToolKitSetup {
		protected FactoryCollection m_factoryCollection;
		protected GraphicsDevice m_device;

		[OneTimeSetUp]
		public void Setup() {
			var dxgiFactory = new Factory1();
			var d2dFactory = new SharpDX.Direct2D1.Factory1();
			var dwFactory = new SharpDX.DirectWrite.Factory1();
			var window = new RenderForm();
			m_factoryCollection = new FactoryCollection(dxgiFactory, d2dFactory, dwFactory);
			m_device = new GraphicsDevice(m_factoryCollection, window, new ModeDescription(1920, 1080, new Rational(60, 1), Format.R8G8B8A8_UNorm));
			FeatureBase.SetDevice(m_device);
			Configuration.EnableObjectTracking = true;
		}

		[OneTimeTearDown]
		public void TearDown() {
			Utilities.Dispose(ref m_factoryCollection);
			Utilities.Dispose(ref m_device);
		}
	}
}