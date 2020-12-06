using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using SharpDX;
using SharpDX.Direct3D11;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace DXToolKit.Tests {
	public abstract class FeatureBase {
		protected GraphicsDevice m_device => m_storedDevice;
		protected DeviceContext m_context => m_device.Context;
		protected FactoryCollection m_factory => m_device.Factory;
		private static GraphicsDevice m_storedDevice = null;

		public static void SetDevice(GraphicsDevice device) {
			m_storedDevice = device;
		}

		protected void Dump(object obj, Formatting formatting = Formatting.Indented) {
			if (obj is DeviceComponent) {
				Console.WriteLine(obj);
				return;
			}

			if (obj is CppObject) {
				Console.WriteLine(obj);
				return;
			}

			var str = JsonConvert.SerializeObject(obj, formatting);
			Console.WriteLine(str);
		}

		protected void dump(object obj, Formatting formatting = Formatting.Indented) {
			Dump(obj, formatting);
		}


		private List<IDisposable> m_disposables;

		protected T ToDispose<T>(T disposable) where T : IDisposable {
			if (m_disposables == null) {
				m_disposables = new List<IDisposable>();
			}

			m_disposables.Add(disposable);
			return disposable;
		}

		[TearDown]
		public void TearDown() {
			if (m_disposables != null) {
				foreach (var disposable in m_disposables) {
					disposable?.Dispose();
				}

				m_disposables?.Clear();
				m_disposables = null;
			}


			var objects = SharpDX.Diagnostics.ObjectTracker.FindActiveObjects();
			var report = SharpDX.Diagnostics.ObjectTracker.ReportActiveObjects();

			// Dispose of any objects that are still active
			if (objects.Count > 0) {
				Console.WriteLine();

				foreach (var obj in objects) {
					if (obj?.Object?.Target is IDisposable disposable) {
						disposable.Dispose();
					}
				}

				Console.WriteLine("ACTIVE COM OBJECTS:");
				Console.WriteLine(report);
				Console.WriteLine("END REPORT");
				Assert.Fail("Found undisposed com objects");
			}
		}


		public T ReadBufferSingle<T>(DXBuffer<T> buffer) where T : struct {
			var readBuffer = new Buffer(m_device, new BufferDescription {
				Usage = ResourceUsage.Staging,
				BindFlags = BindFlags.None,
				OptionFlags = ResourceOptionFlags.None,
				CpuAccessFlags = CpuAccessFlags.Read,
				SizeInBytes = buffer.Description.SizeInBytes,
				StructureByteStride = buffer.Description.StructureByteStride,
			});
			m_context.CopyResource(buffer, readBuffer);
			m_context.MapSubresource(readBuffer, MapMode.Read, MapFlags.None, out var stream);
			var result = stream.Read<T>();
			m_context.UnmapSubresource(readBuffer, 0);
			Utilities.Dispose(ref stream);
			Utilities.Dispose(ref readBuffer);
			return result;
		}


		public T[] ReadBuffer<T>(DXBuffer<T> buffer) where T : struct {
			var readBuffer = new Buffer(m_device, new BufferDescription {
				Usage = ResourceUsage.Staging,
				BindFlags = BindFlags.None,
				OptionFlags = ResourceOptionFlags.None,
				CpuAccessFlags = CpuAccessFlags.Read,
				SizeInBytes = buffer.Description.SizeInBytes,
				StructureByteStride = buffer.Description.StructureByteStride,
			});
			m_context.CopyResource(buffer, readBuffer);
			m_context.MapSubresource(readBuffer, MapMode.Read, MapFlags.None, out var stream);
			var result = stream.ReadRange<T>(buffer.Description.SizeInBytes / Utilities.SizeOf<T>());
			m_context.UnmapSubresource(readBuffer, 0);
			Utilities.Dispose(ref stream);
			Utilities.Dispose(ref readBuffer);
			return result;
		}
	}
}