using System;
using System.IO;
using SharpDX.D3DCompiler;

namespace DXToolKit {
	/// <summary>
	/// Include handler to handle local include files
	/// </summary>
	public class FXInclueHandler : Include {
		public IDisposable Shadow { get; set; }
		private string m_directory;

		public FXInclueHandler(string basefilename) {
			m_directory = basefilename.Remove(basefilename.LastIndexOf('\\'));
		}

		public Stream Open(IncludeType type, string fileName, Stream parentStream) {
			return File.OpenRead(m_directory + "\\" + fileName);
		}

		public void Close(Stream stream) {
			stream?.Dispose();
		}

		public void Dispose() {
			Shadow?.Dispose();
		}
	}
}